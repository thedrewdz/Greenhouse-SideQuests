# flutter-pi spike

A minimal Flutter app whose only purpose is to **prove we can build and deploy a `flutter-pi` application on a Raspberry Pi 4** - it renders full-screen and responds to touch. **Validated PASS on 2026-06-29** (Pi 4, 8 GB, Debian Bookworm).

Self-contained experiment in the `side-quests` monorepo. Claude-only harness (`CLAUDE.md`); no dependency on other repositories.

## What it does

Full-screen screen with a title, icon, the detected screen size, and a tap counter. Tapping (or mouse-clicking) anywhere increments the counter, shows the pointer coordinates, and flips the background colour - unmistakable proof that rendering and pointer/touch input both work on the device.

## Prerequisites (the parts that actually bit us)

- **Flutter SDK 3.41.x.** flutterpi_tool 0.11.0 compiles only against Flutter 3.41.x; newer Flutter (3.44+) breaks it. Pin the SDK, e.g. `git -C ~/flutter checkout 3.41.9` then `flutter --version`.
- **flutterpi_tool is a project dev-dependency** (already in `pubspec.yaml`), run via `dart run`. Do **not** `flutter pub global activate` it - Dart pub forbids globally activating SDK-dependent tools.
- **Raspberry Pi 4:** the `flutter-pi` runtime installed, KMS/DRM 3D acceleration enabled (default on Bookworm), booted to **console** (no desktop compositor, so flutter-pi can take the display), user in the `render`/`video`/`input` groups.
- **Touch is a separate USB connection** - HDMI carries video only. No USB touch lead = no touch events (mouse clicks still work as pointer input).

See `docs/skills/flutter-pi-build-and-deploy.md` for full detail and gotchas.

## Verify on the dev machine

```bash
flutter pub get
dart format .
dart analyze
flutter test
```

## Build and run on the Pi 4

```bash
# Build a release bundle for the Pi 4:
dart run flutterpi_tool build --arch=arm64 --cpu=pi4 --release

# Launch full-screen. The --release flag is REQUIRED (without it flutter-pi
# defaults to debug and looks for kernel_blob.bin). Bundle dir: build/flutter-pi/pi4-64
flutter-pi --release build/flutter-pi/pi4-64
```

Ctrl+C to quit. Launching over SSH works on a console-booted Pi (no compositor holds the display).

## Pass / fail

Passes when, on the Pi 4: it builds in release mode, launches full-screen, renders correctly, and tapping (or mouse-clicking) flips the background and increments the counter. **Result: PASS (2026-06-29)** - recorded in ADR 0002 in the documentation repo.

## Known issue (out of scope, deferred)

The test panel's EDID misreports its modes (no native 1024x600), so output is scaled and blurry. Not a flutter-pi problem. Candidate fix: a custom EDID via `drm.edid_firmware`. Likely revisited against the production display.
