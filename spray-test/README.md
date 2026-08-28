# spray-test

ESP32 firmware that reads temperature (Celsius) and relative humidity (RH, %) from the attached SHTC3 sensor, drives a valve and fan relay from that data, and posts each reading to the Azure Function endpoint.

## Behavior

- Sensor is read every 15 s.
- Valve opens above 25C, closes at/below 23C. A run that hits the 5-minute cap and ends because the target was reached triggers a 10-minute cooldown, during which the valve is locked off.
- Fan starts above 70% RH, stops at/below 60% RH. A run that hits the 5-minute cap *without* reaching the target is force-stopped and triggers the same 10-minute cooldown.
- Current temperature, humidity, valve state, and fan state are POSTed every 60 s. Wi-Fi loss only disables uploads — local sensing and relay control keep running.

## Local Secrets

Runtime credentials are kept out of Git in `include/secrets.h`.

To recreate the local secrets file:

1. Copy `include/secrets.example.h` to `include/secrets.h`.
2. Replace the placeholder values for `WIFI_SSID`, `WIFI_PASS`, and `AZURE_FN_KEY`.

`include/secrets.h` is ignored by Git, while `include/secrets.example.h` is tracked as the safe template.
