# Temp-Test

ESP32 firmware for reading the attached temperature/humidity sensor and posting readings to the Azure Function endpoint.

## Local Secrets

Runtime credentials are kept out of Git in `include/secrets.h`.

To recreate the local secrets file:

1. Copy `include/secrets.example.h` to `include/secrets.h`.
2. Replace the placeholder values for `WIFI_SSID`, `WIFI_PASS`, and `AZURE_FN_KEY`.

`include/secrets.h` is ignored by Git, while `include/secrets.example.h` is tracked as the safe template.
