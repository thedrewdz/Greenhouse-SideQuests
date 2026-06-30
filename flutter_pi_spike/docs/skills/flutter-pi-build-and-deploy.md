# Skill: flutter-pi Build and Deploy (Spike)

## Purpose

Guide an agent to build this minimal Flutter app and deploy it to a Raspberry Pi 4 via the `flutter-pi` embedder (renders directly to KMS/DRM + GLES, no X11/Wayland, no browser).

The only goal is to prove build + deploy + render + touch on the Pi 4. The steps below are the procedure that actually worked (validated 2026-06-29).

## Scope Discipline

- Do **not** add architecture, state management, networking, or production conventions. If a change isn't needed to prove build/deploy/render/touch, don't make it.

## Prerequisites

- **Flutter SDK 3.41.x.** flutterpi_tool 0.11.0 compiles only against Flutter 3.41.x. Flutter 3.44+ breaks it on changed internal `flutter_tools` APIs (`HostPlatform.linux_riscv64`, ArtifactUpdater progress-context methods). Pin it: `git -C ~/flutter checkout 3.41.9` then `flutter --version` to re-bootstrap.
- **flutterpi_tool as a dev-dependency, run via `dart run`.** Do NOT `flutter pub global activate flutterpi_tool` - Dart pub forbids globally activating packages that depend on the Flutter SDK (dart-lang/pub#4001), and `flutter pub global run` fails the same way.
- **Raspberry Pi 4:** `flutter-pi` runtime built/installed; KMS/DRM 3D acceleration enabled (default on Bookworm via `vc4-kms-v3d`); booted to console; user in `render`/`video`/`input` groups.

## Verify First (dev machine)

Directly or via the Dart MCP server:

- `dart format .` (MCP: `dart_format`)
- `dart analyze` (MCP: `analyze_files`) - must be clean
- `flutter test` (MCP: `run_tests`)
- Optional: `flutter run -d linux` on a desktop target for hot reload / inspector. flutter-pi is **not** a standard Flutter device, so the MCP runtime tools don't drive it.

## Build

```
dart run flutterpi_tool build --arch=arm64 --cpu=pi4 --release
```

- Release/AOT is the goal. The first build downloads the flutter-pi engine artifacts (slow).
- Output bundle: `build/flutter-pi/<target>/` (e.g. `build/flutter-pi/pi4-64/`). Assets are **flat** in that dir (no `flutter_assets/` subdir in this layout), alongside `app.so`, `libflutter_engine.so`, and a version-matched `flutter-pi` binary.

## Run (Pi 4)

```
flutter-pi --release build/flutter-pi/pi4-64
```

- The **`--release` flag is required** - without it flutter-pi defaults to debug mode and looks for `kernel_blob.bin`, which a release bundle doesn't have.
- Runs full-screen on the framebuffer; Ctrl+C to quit. Launching over SSH works on a console-booted Pi (no compositor owns the display).

## Common Gotchas

- **"unsupported for global executables"** -> use the dev-dependency + `dart run` (not global activate).
- **Compile errors inside flutterpi_tool against flutter_tools** -> Flutter SDK is too new; pin to 3.41.x.
- **`kernel_blob.bin does not exist`** -> you forgot the `--release` flag.
- **Taps do nothing / no touch** -> touch is a **separate USB connection** (HDMI is video only). Confirm a touch device exists: `grep -iE "Name=|Handlers=" /proc/bus/input/devices` and `lsusb`. A **mouse click** proves the input pipeline independently of the touch hardware.
- **Blank screen / wrong resolution / blur** -> the panel's EDID. KMS must be enabled; a lying EDID with no native mode causes scaling/blur (the open issue on the test panel).
- **SSH PATH** -> `~/.bashrc` PATH additions are skipped for non-interactive SSH; export `PATH` inline when running `ssh host '...'` commands.

## Pass Criteria

Builds in release mode, launches full-screen, renders, and tapping/clicking flips the background + increments the counter. Validated PASS 2026-06-29 (Pi 4, Flutter 3.41.9). Recorded in ADR 0002.
