#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <time.h>

#include "driver/gpio.h"
#include "driver/i2c_master.h"
#include "esp_crt_bundle.h"
#include "esp_err.h"
#include "esp_event.h"
#include "esp_http_client.h"
#include "esp_log.h"
#include "esp_netif.h"
#include "esp_sntp.h"
#include "esp_timer.h"
#include "esp_wifi.h"
#include "freertos/FreeRTOS.h"
#include "freertos/event_groups.h"
#include "freertos/task.h"
#include "nvs_flash.h"

#include "secrets.h"

#define I2C_PORT    I2C_NUM_0
#define I2C_SDA_PIN 21
#define I2C_SCL_PIN 22
#define I2C_FREQ_HZ 100000

// Relays actuate the fan/valve. LEDs mirror relay state for local status.
// GPIO 35 cannot be used here: on ESP32 it is input-only (no output driver).
// GPIO 13/14 were also ruled out: they're the JTAG pins (MTCK/MTMS) and sit
// weakly pulled/floating before app_main configures them, which showed up as
// a faint glow through the LEDs' 220ohm resistors while old firmware (or the
// bootloader, pre-app_main) held the pins. Moved to 32/33 instead.
#define PIN_LED_FAN   GPIO_NUM_32
#define PIN_LED_VALVE GPIO_NUM_33
#define PIN_RELAY_FAN   GPIO_NUM_16
#define PIN_RELAY_VALVE GPIO_NUM_17

#define STARTUP_BLINK_MS  1000
#define READ_INTERVAL_MS  15000
#define UPLOAD_INTERVAL_MS 60000

// Drives the cooldown-blink LED update. Half of this is the on/off duration,
// so the LED completes one full on-off cycle every LED_BLINK_PERIOD_MS.
#define LED_BLINK_PERIOD_MS 1000
#define TICK_MS (LED_BLINK_PERIOD_MS / 2)

// All temperatures are Celsius. Humidity is relative humidity (RH), as a percentage.
#define TEMP_VALVE_ON_C   25.0f
#define TEMP_VALVE_OFF_C  23.0f
#define RH_FAN_ON_PCT     70.0f
#define RH_FAN_OFF_PCT    60.0f

#define ACTUATOR_MAX_ON_MS  (5 * 60 * 1000)
#define ACTUATOR_COOLDOWN_MS (10 * 60 * 1000)

#define WIFI_CONNECTED_BIT BIT0
#define WIFI_FAIL_BIT BIT1
#define WIFI_MAX_RETRY 10

#define AZURE_FN_URL "https://fn-greenhouse-djcuazgkefd8b3c8.centralus-01.azurewebsites.net/api/fnpost"

// SHTC3 commands (MSB first)
#define CMD_WAKEUP  0x3517
#define CMD_SLEEP   0xB098
#define CMD_MEASURE 0x7CA2  // T-first, clock stretch

static const char *TAG = "spray_test";
static i2c_master_bus_handle_t g_i2cBus;
static EventGroupHandle_t g_wifiEvents;
static int g_wifiRetry;

// 0 means "not in cooldown". Compared against esp_timer_get_time()-derived
// millisecond timestamps, which start at 0 on boot, so 0 is never a valid deadline.
static bool g_valveOn;
static int64_t g_valveOnSinceMs;
static int64_t g_valveCooldownUntilMs;

static bool g_fanOn;
static int64_t g_fanOnSinceMs;
static int64_t g_fanCooldownUntilMs;

static int64_t nowMs(void)
{
	return esp_timer_get_time() / 1000;
}

static void setFanRelay(bool on)
{
	gpio_set_level(PIN_RELAY_FAN, on);
}

static void setValveRelay(bool on)
{
	gpio_set_level(PIN_RELAY_VALVE, on);
}

// Called every TICK_MS. Solid on/off follows the relay outside of cooldown;
// during cooldown the relay is forced off and the LED blinks at 1Hz instead,
// so a locked-out actuator is visually obvious without a serial monitor.
static void updateActuatorLeds(int64_t now)
{
	bool blinkPhase = ((now / TICK_MS) % 2) == 0;

	if (g_valveCooldownUntilMs != 0) {
		gpio_set_level(PIN_LED_VALVE, blinkPhase);
	} else {
		gpio_set_level(PIN_LED_VALVE, g_valveOn);
	}

	if (g_fanCooldownUntilMs != 0) {
		gpio_set_level(PIN_LED_FAN, blinkPhase);
	} else {
		gpio_set_level(PIN_LED_FAN, g_fanOn);
	}
}

static esp_err_t initOutputs(void)
{
	gpio_config_t cfg = {
		.pin_bit_mask = (1ULL << PIN_LED_FAN) | (1ULL << PIN_LED_VALVE) |
			(1ULL << PIN_RELAY_FAN) | (1ULL << PIN_RELAY_VALVE),
		.mode = GPIO_MODE_OUTPUT,
		.pull_up_en = GPIO_PULLUP_DISABLE,
		.pull_down_en = GPIO_PULLDOWN_DISABLE,
		.intr_type = GPIO_INTR_DISABLE,
	};

	esp_err_t err = gpio_config(&cfg);
	if (err != ESP_OK) {
		return err;
	}

	setFanRelay(false);
	setValveRelay(false);
	gpio_set_level(PIN_LED_FAN, 0);
	gpio_set_level(PIN_LED_VALVE, 0);
	return ESP_OK;
}

// Flashes both status LEDs briefly so a restart is visible without a monitor attached.
// Relays are intentionally left untouched here; this is a visual-only check.
static void startupBlink(void)
{
	gpio_set_level(PIN_LED_FAN, 1);
	gpio_set_level(PIN_LED_VALVE, 1);
	vTaskDelay(pdMS_TO_TICKS(STARTUP_BLINK_MS));
	gpio_set_level(PIN_LED_FAN, 0);
	gpio_set_level(PIN_LED_VALVE, 0);
}

static esp_err_t addDevice(uint8_t addr, i2c_master_dev_handle_t *outDev)
{
	i2c_device_config_t devCfg = {
		.dev_addr_length = I2C_ADDR_BIT_LEN_7,
		.device_address = addr,
		.scl_speed_hz = I2C_FREQ_HZ,
	};

	return i2c_master_bus_add_device(g_i2cBus, &devCfg, outDev);
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

// Opens above 25C, closes at/below 23C. A run capped at 5 minutes that ends
// because the target was reached forces a 10 minute cooldown before the valve
// may open again, guarding against a relay stuck cycling near the threshold.
static void updateValve(float temp)
{
	int64_t now = nowMs();

	if (g_valveCooldownUntilMs != 0) {
		if (now < g_valveCooldownUntilMs) {
			return;
		}
		g_valveCooldownUntilMs = 0;
		ESP_LOGI(TAG, "Valve cooldown expired; resuming normal operation");
	}

	if (g_valveOn) {
		if (temp <= TEMP_VALVE_OFF_C) {
			bool ranFullDuration = (now - g_valveOnSinceMs) >= ACTUATOR_MAX_ON_MS;
			setValveRelay(false);
			g_valveOn = false;
			if (ranFullDuration) {
				g_valveCooldownUntilMs = now + ACTUATOR_COOLDOWN_MS;
				ESP_LOGI(TAG, "Valve reached %.1fC after max on-time; closing and starting 10 min cooldown", temp);
			} else {
				ESP_LOGI(TAG, "Temperature %.1fC <= %.1fC; closing valve", temp, TEMP_VALVE_OFF_C);
			}
		}
	} else if (temp > TEMP_VALVE_ON_C) {
		setValveRelay(true);
		g_valveOn = true;
		g_valveOnSinceMs = now;
		ESP_LOGI(TAG, "Temperature %.1fC > %.1fC; opening valve", temp, TEMP_VALVE_ON_C);
	}
}

// Runs above 70%RH, stops at/below 60%RH. If 5 minutes pass without reaching
// the target, the fan is force-stopped and locked off for a 10 minute
// cooldown, guarding against a fan running indefinitely to no effect.
static void updateFan(float rh)
{
	int64_t now = nowMs();

	if (g_fanCooldownUntilMs != 0) {
		if (now < g_fanCooldownUntilMs) {
			return;
		}
		g_fanCooldownUntilMs = 0;
		ESP_LOGI(TAG, "Fan cooldown expired; resuming normal operation");
	}

	if (g_fanOn) {
		if (rh <= RH_FAN_OFF_PCT) {
			setFanRelay(false);
			g_fanOn = false;
			ESP_LOGI(TAG, "Humidity %.1f%% <= %.1f%%; stopping fan", rh, RH_FAN_OFF_PCT);
		} else if ((now - g_fanOnSinceMs) >= ACTUATOR_MAX_ON_MS) {
			setFanRelay(false);
			g_fanOn = false;
			g_fanCooldownUntilMs = now + ACTUATOR_COOLDOWN_MS;
			ESP_LOGI(TAG, "Fan exceeded max on-time without reaching %.1f%%RH; stopping and starting 10 min cooldown", RH_FAN_OFF_PCT);
		}
	} else if (rh > RH_FAN_ON_PCT) {
		setFanRelay(true);
		g_fanOn = true;
		g_fanOnSinceMs = now;
		ESP_LOGI(TAG, "Humidity %.1f%% > %.1f%%; starting fan", rh, RH_FAN_ON_PCT);
	}
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

// Failure here is non-fatal: local sensing and relay control must keep
// working without a network, per the platform's local-first requirement.
// Only cloud upload is skipped while Wi-Fi is unavailable.
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

static bool wifiConnected(void)
{
	return g_wifiEvents != NULL && (xEventGroupGetBits(g_wifiEvents) & WIFI_CONNECTED_BIT) != 0;
}

static void makeTimestamp(char *out, size_t outLen)
{
	time_t now = time(NULL);
	struct tm tmUtc = {0};
	gmtime_r(&now, &tmUtc);
	strftime(out, outLen, "%Y-%m-%dT%H:%M:%SZ", &tmUtc);
}

static esp_err_t postReading(float temp, float rh, bool valveOn, bool fanOn)
{
	char timestamp[32] = {0};
	makeTimestamp(timestamp, sizeof(timestamp));

	char payload[192] = {0};
	int payloadLen = snprintf(
		payload,
		sizeof(payload),
		"{\"temperature\":%.1f,\"humidity\":%.1f,\"valveOn\":%s,\"fanOn\":%s,\"timestamp\":\"%s\"}",
		temp,
		rh,
		valveOn ? "true" : "false",
		fanOn ? "true" : "false",
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

void app_main(void)
{
	esp_err_t err = initOutputs();
	if (err != ESP_OK) {
		ESP_LOGE(TAG, "GPIO init failed: %s", esp_err_to_name(err));
		return;
	}

	startupBlink();

	err = initI2C();
	if (err != ESP_OK) {
		ESP_LOGE(TAG, "I2C init failed: %s", esp_err_to_name(err));
		return;
	}

	err = connectWifi();
	if (err != ESP_OK) {
		ESP_LOGW(TAG, "Wi-Fi connect failed: %s; continuing with local control only", esp_err_to_name(err));
	}

	uint8_t addr = scanBus();
	if (!addr) {
		ESP_LOGE(TAG, "No sensor found. Halting.");
		return;
	}

	ESP_LOGI(TAG, "Reading sensor at 0x%02X every %d ms...", addr, READ_INTERVAL_MS);

	float lastTemp = 0.0f;
	float lastRh = 0.0f;
	bool haveReading = false;
	int64_t lastReadMs = 0;
	int64_t lastUploadMs = 0;

	while (true) {
		int64_t now = nowMs();

		// Runs every tick so cooldown LEDs keep blinking between sensor reads.
		updateActuatorLeds(now);

		if ((now - lastReadMs) >= READ_INTERVAL_MS) {
			lastReadMs = now;
			float temp = 0.0f;
			float rh   = 0.0f;

			err = readSensor(addr, &temp, &rh);
			if (err == ESP_OK) {
				printf("{\"temperature\": %.1f, \"humidity\": %.1f}\n", temp, rh);
				updateValve(temp);
				updateFan(rh);
				lastTemp = temp;
				lastRh = rh;
				haveReading = true;
			} else {
				ESP_LOGW(TAG, "Read failed: %s", esp_err_to_name(err));
			}

			if (haveReading && (now - lastUploadMs) >= UPLOAD_INTERVAL_MS) {
				if (wifiConnected()) {
					err = postReading(lastTemp, lastRh, g_valveOn, g_fanOn);
					if (err != ESP_OK) {
						ESP_LOGW(TAG, "Upload failed: %s", esp_err_to_name(err));
					}
				} else {
					ESP_LOGW(TAG, "Wi-Fi not connected; skipping upload");
				}
				lastUploadMs = now;
			}
		}

		vTaskDelay(pdMS_TO_TICKS(TICK_MS));
	}
}
