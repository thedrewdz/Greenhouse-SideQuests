# Agent Instructions

This repository is prepared for Codex and other coding agents. It is a test
project, but changes should still follow the architecture and engineering style
used by the broader Greenhouse platform.

## First Reads

Read these files before modifying code:

1. This file.
2. `CONTEXT.md`.
3. `README.md`.
4. The shared Greenhouse documentation entry point:
   <https://github.com/thedrewdz/Greenhouse-Documentation>

In the shared documentation repository, use this read order unless the task gives
more specific direction:

1. `AGENTS.md`
2. `CONTEXT.md`
3. `architecture.md`
4. `device-model.md`
5. `mqtt-topics.md`
6. `vision.md`

## Required Skill Guidance

For .NET work, consult the shared skill files before making design choices:

- <https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/.agents/skills/solid/SKILL.md>
- <https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/.agents/skills/implementation/SKILL.md>

If the change touches MQTT contracts, platform integration, or main control unit
behavior, also consult:

- <https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/skills/mqtt-contract-integration-dotnet.md>
- <https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/skills/grill-with-docs-main-control-unit.md>

## Local Architecture Rules

- Treat `TempTest.Functions` as the delivery adapter for Azure Functions.
- Treat `TempTest.Application` as the application layer for use cases,
  interfaces, commands, queries, and DTOs.
- Treat `TempTest.Domain` as the domain layer for entities and invariants.
- Treat `TempTest.Infrastructure` as the infrastructure layer for EF Core,
  persistence, and other external details.
- Keep business logic out of trigger methods.
- Prefer explicit contracts and constructor injection.
- Do not use a service locator pattern.
- Keep external systems, storage, time, and environment access behind interfaces.
- Avoid adding broad abstractions until there is behavior that needs them.

## Coding Standards

- Target .NET 10.
- Keep nullable reference types enabled.
- Use file-scoped namespaces for new C# files.
- Prefer `async` APIs for I/O and external calls.
- Keep public types intentional and named around domain behavior.
- Keep functions small enough to review comfortably.
- Add tests with meaningful behavior names when adding logic.

## Verification

Before handing work back, run the most relevant available checks:

```powershell
dotnet restore .\TempTest.Functions\TempTest.Functions.slnx
dotnet build .\TempTest.Functions\TempTest.Functions.slnx
```

If tests are added later, run the relevant test project as well.

## Secrets And Local Files

- Do not commit credentials, connection strings, API keys, or developer-specific
  settings.
- Treat `TempTest.Functions/TempTest.Functions/local.settings.json` as local-only.
- Prefer documented configuration keys and environment variables over embedded
  values.
