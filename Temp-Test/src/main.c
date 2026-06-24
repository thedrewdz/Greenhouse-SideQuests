#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <time.h>

#include "driver/i2c_master.h"
#include "esp_err.h"
#include "esp_event.h"
#include "esp_http_client.h"
#include "esp_log.h"
#include "esp_netif.h"
#include "esp_sntp.h"
#include "esp_wifi.h"
#include "freertos/FreeRTOS.h"
#include "freertos/event_groups.h"
#include "freertos/task.h"
#include "nvs_flash.h"

#include "esp_crt_bundle.h"
#include "secrets.h"

#define I2C_PORT    I2C_NUM_0
#define I2C_SDA_PIN 21
#define I2C_SCL_PIN 22
#define I2C_FREQ_HZ 100000

#define WIFI_CONNECTED_BIT BIT0
#define WIFI_FAIL_BIT BIT1
#define WIFI_MAX_RETRY 10
#define READ_INTERVAL_MS 300000

#define AZURE_FN_URL "https://fn-greenhouse-djcuazgkefd8b3c8.centralus-01.azurewebsites.net/api/fnpost"

// SHTC3 commands (MSB first)
#define CMD_WAKEUP  0x3517
#define CMD_SLEEP   0xB098
#define CMD_MEASURE 0x7CA2  // T-first, clock stretch

static const char *TAG = "sensor_app";
static EventGroupHandle_t g_wifiEvents;
static int g_wifiRetry;
static i2c_master_bus_handle_t g_i2cBus;

static esp_err_t addDevice(uint8_t addr, i2c_master_dev_handle_t *outDev)
{
	i2c_device_config_t devCfg = {
		.dev_addr_length = I2C_ADDR_BIT_LEN_7,
		.device_address = addr,
		.scl_speed_hz = I2C_FREQ_HZ,
	};

	return i2c_master_bus_add_device(g_i2cBus, &devCfg, outDev);
}

static void syncTime(void)
{
	time_t now = time(NULL);
	if (now > 1700000000) {
		return;
	}

	esp_sntp_setoperatingmode(SNTP_OPMODE_POLL);
	esp_sntp_setservername(0, "pool.ntp.org");
	esp_sntp_init();

	for (int i = 0; i < 15; i++) {
		vTaskDelay(pdMS_TO_TICKS(1000));
		now = time(NULL);
		if (now > 1700000000) {
			ESP_LOGI(TAG, "Time synchronized via SNTP");
			return;
		}
	}

	ESP_LOGW(TAG, "SNTP sync timeout; timestamp may be default");
}

static void makeTimestamp(char *out, size_t outLen)
{
	time_t now = time(NULL);
	struct tm tmUtc = {0};
	gmtime_r(&now, &tmUtc);
	strftime(out, outLen, "%Y-%m-%dT%H:%M:%SZ", &tmUtc);
}

static esp_err_t postReading(float temp, float rh, const char *timestamp)
{
	char payload[160] = {0};
	int payloadLen = snprintf(
		payload,
		sizeof(payload),
		"{\"temperature\":%.1f,\"humidity\":%.1f,\"timestamp\":\"%s\"}",
		temp,
		rh,
		timestamp);

	if (payloadLen <= 0 || payloadLen >= (int)sizeof(payload)) {
		return ESP_ERR_INVALID_SIZE;
	}

	esp_http_client_config_t cfg = {
		.url = AZURE_FN_URL,
		.method = HTTP_METHOD_POST,
		.timeout_ms = 10000,
		.crt_bundle_attach = esp_crt_bundle_attach,
	};

	esp_http_client_handle_t client = esp_http_client_init(&cfg);
	if (!client) {
		return ESP_ERR_NO_MEM;
	}

	esp_http_client_set_header(client, "Content-Type", "application/json");
	esp_http_client_set_header(client, "x-functions-key", AZURE_FN_KEY);
	esp_http_client_set_post_field(client, payload, payloadLen);

	esp_err_t err = esp_http_client_perform(client);
	if (err != ESP_OK) {
		esp_http_client_cleanup(client);
		return err;
	}

	int statusCode = esp_http_client_get_status_code(client);
	esp_http_client_cleanup(client);

	if (statusCode < 200 || statusCode >= 300) {
		ESP_LOGW(TAG, "Azure Function returned HTTP %d", statusCode);
		return ESP_FAIL;
	}

	return ESP_OK;
}

static void onWifiEvent(void *arg, esp_event_base_t eventBase, int32_t eventId, void *eventData)
{
	if (eventBase == WIFI_EVENT && eventId == WIFI_EVENT_STA_START) {
		esp_wifi_connect();
		return;
	}

	if (eventBase == WIFI_EVENT && eventId == WIFI_EVENT_STA_DISCONNECTED) {
		if (g_wifiRetry < WIFI_MAX_RETRY) {
			esp_wifi_connect();
			g_wifiRetry++;
			ESP_LOGW(TAG, "Wi-Fi reconnect attempt %d/%d", g_wifiRetry, WIFI_MAX_RETRY);
		} else {
			xEventGroupSetBits(g_wifiEvents, WIFI_FAIL_BIT);
		}
		return;
	}

	if (eventBase == IP_EVENT && eventId == IP_EVENT_STA_GOT_IP) {
		ip_event_got_ip_t *event = (ip_event_got_ip_t *)eventData;
		ESP_LOGI(TAG, "Wi-Fi connected, IP: " IPSTR, IP2STR(&event->ip_info.ip));
		g_wifiRetry = 0;
		xEventGroupSetBits(g_wifiEvents, WIFI_CONNECTED_BIT);
	}
}

static esp_err_t initNvs(void)
{
	esp_err_t err = nvs_flash_init();
	if (err == ESP_ERR_NVS_NO_FREE_PAGES || err == ESP_ERR_NVS_NEW_VERSION_FOUND) {
		ESP_ERROR_CHECK(nvs_flash_erase());
		err = nvs_flash_init();
	}
	return err;
}

static esp_err_t connectWifi(void)
{
	esp_err_t err = initNvs();
	if (err != ESP_OK) {
		return err;
	}

	err = esp_netif_init();
	if (err != ESP_OK) {
		return err;
	}

	err = esp_event_loop_create_default();
	if (err != ESP_OK && err != ESP_ERR_INVALID_STATE) {
		return err;
	}

	esp_netif_create_default_wifi_sta();

	wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
	err = esp_wifi_init(&cfg);
	if (err != ESP_OK) {
		return err;
	}

	g_wifiEvents = xEventGroupCreate();
	if (!g_wifiEvents) {
		return ESP_ERR_NO_MEM;
	}

	esp_event_handler_instance_t wifiAnyId;
	esp_event_handler_instance_t gotIp;
	err = esp_event_handler_instance_register(WIFI_EVENT, ESP_EVENT_ANY_ID, &onWifiEvent, NULL, &wifiAnyId);
	if (err != ESP_OK) {
		return err;
	}
	err = esp_event_handler_instance_register(IP_EVENT, IP_EVENT_STA_GOT_IP, &onWifiEvent, NULL, &gotIp);
	if (err != ESP_OK) {
		return err;
	}

	wifi_config_t wifiCfg = {0};
	strncpy((char *)wifiCfg.sta.ssid, WIFI_SSID, sizeof(wifiCfg.sta.ssid) - 1);
	strncpy((char *)wifiCfg.sta.password, WIFI_PASS, sizeof(wifiCfg.sta.password) - 1);
	wifiCfg.sta.threshold.authmode = WIFI_AUTH_WPA2_PSK;
	wifiCfg.sta.pmf_cfg.capable = true;
	wifiCfg.sta.pmf_cfg.required = false;

	err = esp_wifi_set_mode(WIFI_MODE_STA);
	if (err != ESP_OK) {
		return err;
	}
	err = esp_wifi_set_config(WIFI_IF_STA, &wifiCfg);
	if (err != ESP_OK) {
		return err;
	}
	err = esp_wifi_start();
	if (err != ESP_OK) {
		return err;
	}

	ESP_LOGI(TAG, "Connecting to Wi-Fi SSID: %s", WIFI_SSID);
	EventBits_t bits = xEventGroupWaitBits(
		g_wifiEvents,
		WIFI_CONNECTED_BIT | WIFI_FAIL_BIT,
		pdFALSE,
		pdFALSE,
		pdMS_TO_TICKS(20000));

	if (bits & WIFI_CONNECTED_BIT) {
		syncTime();
		return ESP_OK;
	}

	if (bits & WIFI_FAIL_BIT) {
		return ESP_FAIL;
	}

	return ESP_ERR_TIMEOUT;
}

static uint8_t calcCrc(const uint8_t *data, size_t len)
{
	uint8_t crc = 0xFF;

	for (size_t i = 0; i < len; i++) {
		crc ^= data[i];
		for (int bit = 0; bit < 8; bit++) {
			crc = (crc & 0x80) ? (uint8_t)((crc << 1) ^ 0x31) : (uint8_t)(crc << 1);
		}
	}

	return crc;
}

static esp_err_t writeCmd(uint8_t addr, uint16_t cmd)
{
	uint8_t buf[2] = {(uint8_t)(cmd >> 8), (uint8_t)(cmd & 0xFF)};
	i2c_master_dev_handle_t dev;
	esp_err_t err = addDevice(addr, &dev);
	if (err != ESP_OK) {
		return err;
	}

	err = i2c_master_transmit(dev, buf, sizeof(buf), 100);
	i2c_master_bus_rm_device(dev);
	return err;
}

// Scans the bus and returns the first responding address, or 0 if none found.
static uint8_t scanBus(void)
{
	ESP_LOGI(TAG, "Scanning I2C bus (SDA=%d, SCL=%d)...", I2C_SDA_PIN, I2C_SCL_PIN);
	uint8_t found = 0;

	for (uint8_t addr = 0x03; addr <= 0x77; addr++) {
		esp_err_t err = i2c_master_probe(g_i2cBus, addr, 50);
		if (err == ESP_OK) {
			ESP_LOGI(TAG, "  Device found at 0x%02X", addr);
			if (!found) {
				found = addr;
			}
		}
	}

	if (!found) {
		ESP_LOGW(TAG, "No I2C devices found. Check wiring and pullups.");
	}

	return found;
}

static esp_err_t readSensor(uint8_t addr, float *temp, float *rh)
{
	uint8_t raw[6] = {0};
	esp_err_t err;

	err = writeCmd(addr, CMD_WAKEUP);
	if (err != ESP_OK) {
		return err;
	}

	vTaskDelay(pdMS_TO_TICKS(10));
	err = writeCmd(addr, CMD_MEASURE);
	if (err != ESP_OK) {
		ESP_LOGE(TAG, "Measure cmd failed: %s", esp_err_to_name(err));
		writeCmd(addr, CMD_SLEEP);
		return err;
	}

	vTaskDelay(pdMS_TO_TICKS(20));

	i2c_master_dev_handle_t dev;
	err = addDevice(addr, &dev);
	if (err != ESP_OK) {
		writeCmd(addr, CMD_SLEEP);
		return err;
	}

	err = i2c_master_receive(dev, raw, sizeof(raw), 100);
	i2c_master_bus_rm_device(dev);
	writeCmd(addr, CMD_SLEEP);
	if (err != ESP_OK) {
		return err;
	}

	if (calcCrc(&raw[0], 2) != raw[2] || calcCrc(&raw[3], 2) != raw[5]) {
		return ESP_ERR_INVALID_CRC;
	}

	uint16_t raw_t  = ((uint16_t)raw[0] << 8) | raw[1];
	uint16_t raw_rh = ((uint16_t)raw[3] << 8) | raw[4];

	*temp = -45.0f + 175.0f * ((float)raw_t  / 65535.0f);
	*rh   = 100.0f *           ((float)raw_rh / 65535.0f);

	if (*rh < 0.0f)   *rh = 0.0f;
	if (*rh > 100.0f) *rh = 100.0f;

	return ESP_OK;
}

static esp_err_t initI2C(void)
{
	i2c_master_bus_config_t cfg = {
		.i2c_port = I2C_PORT,
		.sda_io_num = I2C_SDA_PIN,
		.scl_io_num = I2C_SCL_PIN,
		.clk_source = I2C_CLK_SRC_DEFAULT,
		.glitch_ignore_cnt = 7,
		.flags.enable_internal_pullup = true,
	};

	return i2c_new_master_bus(&cfg, &g_i2cBus);
}

void app_main(void)
{
	esp_err_t err = initI2C();
	if (err != ESP_OK) {
		ESP_LOGE(TAG, "I2C init failed: %s", esp_err_to_name(err));
		return;
	}

	err = connectWifi();
	if (err != ESP_OK) {
		ESP_LOGE(TAG, "Wi-Fi connect failed: %s", esp_err_to_name(err));
		return;
	}

	uint8_t addr = scanBus();
	if (!addr) {
		ESP_LOGE(TAG, "No sensor found. Halting.");
		return;
	}

	ESP_LOGI(TAG, "Reading sensor at 0x%02X every 5 seconds...", addr);

	while (true) {
		float temp = 0.0f;
		float rh   = 0.0f;
		char timestamp[32] = {0};

		err = readSensor(addr, &temp, &rh);
		if (err == ESP_OK) {
			makeTimestamp(timestamp, sizeof(timestamp));
			printf("{\"temperature\": %.1f, \"humidity\": %.1f, \"timestamp\": \"%s\"}\n", temp, rh, timestamp);

			err = postReading(temp, rh, timestamp);
			if (err != ESP_OK) {
				ESP_LOGW(TAG, "Post failed: %s", esp_err_to_name(err));
			}
		} else {
			ESP_LOGW(TAG, "Read failed: %s", esp_err_to_name(err));
		}

		vTaskDelay(pdMS_TO_TICKS(READ_INTERVAL_MS));
	}
}
