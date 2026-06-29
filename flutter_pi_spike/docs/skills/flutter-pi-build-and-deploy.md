# Skill: flutter-pi Build and Deploy (Spike)

## Purpose

Guide an agent to build this minimal Flutter app and deploy it to a Raspberry Pi 4 via the `flutter-pi` embedder, which renders directly to KMS/DRM + GLES with no X11/Wayland and no browser.

This is spike guidance. The only goal is to prove build + deploy + render + touch on the Pi 4.

## Scope Discipline

- Do **not** add architecture, state management, networking, or production conventions. Keep the app minimal (see `CLAUDE.md` scope). If a change is not needed to prove build/deploy/render/touch, do not make it.

## Prerequisites

- Dev machine: Flutter SDK >= 3.10.5; `flutterpi_tool` activated (`flutter pub global activate flutterpi_tool`).
- Raspberry Pi 4: `flutter-pi` installed; 3D acceleration with KMS/DRM enabled; a connected touchscreen; booted to console (no desktop environment required).

## Verify First (dev machine)

Before deploying, confirm the project is healthy - directly or via the Dart MCP server:

- `dart format .` (MCP: `dart_format`)
- `dart analyze` (MCP: `analyze_files`) - must be clean
- `flutter test` (MCP: `run_tests`)
- Optional interactive check: `flutter run -d linux` (or another desktop target) - this is where MCP `hot_reload`/`widget_inspector` work. flutter-pi itself is not a standard Flutter device.

## Build

- Build a release bundle targeting the Pi 4 with `flutterpi_tool` (release/AOT is the goal; debug bundles are for bring-up only). Exact flags vary by `flutterpi_tool` version - typically an arch (`arm64`) and a CPU/target (`pi4`) plus `--release`.
- Pin the Flutter SDK, `flutterpi_tool`, and the flutter-pi engine together: a release app needs an engine built for the matching runtime mode.
- If the build complains about missing platform scaffolding on your toolchain version, run `flutter create .` once in the project directory to backfill it (harmless; `lib/main.dart` and `pubspec.yaml` remain authoritative), then rebuild.

## Deploy and Run (Pi 4)

- Copy the build output to the Pi.
- Run it with `flutter-pi` (release mode to match the bundle), pointing at the bundle directory.
- The app should take over the framebuffer full-screen.

## Common Gotchas

- **KMS/DRM not enabled** -> blank screen or a GL/EGL error. Ensure the GL driver / KMS is enabled (e.g. via `raspi-config`).
- **Permissions** -> the user running flutter-pi typically needs access to `/dev/dri/*` and the input devices (often the `video`/`render`/`input` groups).
- **Release/debug engine mismatch** -> a release bundle needs the release engine; mismatches fail at launch.
- **No touch** -> flutter-pi reads input via libinput; confirm the touchscreen is detected at the OS level (`libinput list-devices`).

## Pass Criteria

On the Pi 4: builds in release mode, launches full-screen, renders the title/icon, and tapping increments the counter / flips the background / shows plausible coordinates. Record the result (board, versions, friction) for ADR 0002.
