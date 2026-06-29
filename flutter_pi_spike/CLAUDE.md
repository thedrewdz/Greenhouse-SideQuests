# CLAUDE

Claude / Claude Code entry point for the **flutter-pi spike**.

## What This Is

A self-contained throwaway spike with one job: **prove we can create and deploy a `flutter-pi` application on a Raspberry Pi 4.** It renders a full-screen Flutter app and responds to touch. That's the whole goal.

It exists to de-risk the Greenhouse Main Unit UI direction (Flutter via flutter-pi) before committing to it. You do **not** need any other repository to work on this spike.

## Deliberately Self-Contained

- **Claude-only.** There is no `AGENTS.md` and no Codex/Copilot bridge here. This `CLAUDE.md` is the whole harness.
- **No external dependencies on the Greenhouse Documentation repository.** Everything needed is in this directory. Do not go read other repos to work the spike.
- It lives in the `side-quests` monorepo as an independent top-level project, like the others.

## Scope

**In scope:** build the app, deploy it to the Pi 4 via flutter-pi, confirm it renders full-screen and accepts touch input.

**Out of scope - do not add:** REST/services, MQTT, networking, CLEAN architecture, bloc/cubit, state-management packages, OpenAPI clients, or any production conventions. This is a spike. Keep it minimal; resist the urge to "do it properly." If the spike tempts you toward architecture, stop - that belongs in the real `/ui` repository, not here.

## How To Build and Deploy

The procedure, prerequisites, and pass/fail criteria are in `README.md`, with the detailed agent guidance and gotchas in `docs/skills/flutter-pi-build-and-deploy.md`. Read the skill pack before build/deploy work.

## Tooling: Dart MCP Server

The Dart MCP server is configured in `.mcp.json` (`dart mcp-server`; requires Dart SDK >= 3.9.0-dev; experimental). Launch Claude Code from this directory so it is discovered, and approve it via `/mcp`.

- Use `analyze_files`, `dart_format`, `dart_fix`, and `run_tests` to verify the app on your dev machine.
- For interactive bring-up, run on a **standard Flutter target** (desktop/emulator) where `hot_reload`/`widget_inspector` apply.
- **flutter-pi is not a standard Flutter device:** the MCP runtime/device tools do not drive the Pi. On-device runs are launched via flutter-pi directly and verified by eye (see README).

## Definition of Done

The spike passes when, on the Raspberry Pi 4:

1. The app builds with `flutterpi_tool` in release mode.
2. It launches full-screen on the touchscreen and the text/icon render correctly.
3. Tapping the screen increments the tap counter, flips the background, and shows plausible touch coordinates.

Record the result (pass/fail, board, Flutter/flutter-pi versions, any friction) so it can inform ADR 0002 in the documentation repo. Then this spike's purpose is served.
