# flutter-pi spike

A minimal Flutter app whose only purpose is to **prove we can build and deploy a `flutter-pi` application on a Raspberry Pi 4** - it renders full-screen and responds to touch.

Self-contained experiment in the `side-quests` monorepo. Claude-only harness (`CLAUDE.md`); no dependency on other repositories.

## What it does

Shows a full-screen screen with a title, an icon, the detected screen size, and a tap counter. Tapping anywhere increments the counter, prints the touch coordinates, and flips the background colour - unmistakable proof that rendering and touch input both work on the device.

## Prerequisites

- **Dev machine:** Flutter SDK >= 3.10.5, and `flutterpi_tool` (`flutter pub global activate flutterpi_tool`).
- **Raspberry Pi 4:** `flutter-pi` installed, 3D acceleration with KMS/DRM enabled, a connected touchscreen, booted to the console (no desktop/X needed).

See `docs/skills/flutter-pi-build-and-deploy.md` for detail and gotchas.

## Verify on the dev machine

```bash
flutter pub get
dart format .
dart analyze
flutter test
# optional sanity check on a desktop target:
# flutter run -d linux   (or windows/macos)
```

## Build and deploy to the Pi 4

```bash
# Build a release bundle for the Pi 4 (exact flags can vary by flutterpi_tool version):
flutterpi_tool build --arch=arm64 --cpu=pi4 --release

# Copy the build output to the Pi (adjust host/path), then on the Pi run:
flutter-pi --release /path/to/bundle
```

If your `flutterpi_tool` version needs platform scaffolding that a minimal project lacks, run `flutter create .` in this directory once (harmless - your `lib/main.dart` and `pubspec.yaml` stay authoritative), then rebuild.

## Pass / fail

The spike **passes** when, on the Pi 4:

1. It builds with `flutterpi_tool` in release mode.
2. It launches full-screen and renders the title and icon correctly.
3. Tapping increments the counter, flips the background, and shows plausible coordinates.

Record the outcome (pass/fail, board, Flutter + flutter-pi versions, friction) to feed back into ADR 0002 in the documentation repo.
