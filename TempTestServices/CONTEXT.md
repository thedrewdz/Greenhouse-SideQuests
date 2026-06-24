# Repository Context

TempTestServices is a small .NET test workspace under the Greenhouse umbrella.
Its purpose is to experiment with service patterns while keeping the shape close
to production Greenhouse services.

## Current State

- Runtime: .NET 10.
- Host: Azure Functions isolated worker.
- Domain layer: `TempTest.Domain`.
- Application layer: `TempTest.Application`.
- Infrastructure layer: `TempTest.Infrastructure`.
- Test project: `TempTest.Application.Tests`.
- Function adapter: `TempTest.Functions/TempTest.Functions`.
- Solution file: `TempTest.Functions/TempTest.Functions.slnx`.

## External Context

The canonical Greenhouse platform context lives in:

<https://github.com/thedrewdz/Greenhouse-Documentation>

Use that repository for architecture, agent policy, platform vocabulary, device
model, MQTT contracts, ADRs, journey docs, and skill-specific implementation
guidance.

## Design Intent

This repository should become a clean, testable Functions-based service
workspace:

- Triggers stay thin.
- Application use cases own behavior.
- Infrastructure details stay replaceable.
- Contracts are explicit.
- Tests describe behavior rather than implementation trivia.

## Open Decisions

- Whether to move or add a root-level solution file.
- Whether to introduce test projects immediately or when the first real behavior
  lands.
- Which Greenhouse domain slice this test service will model first.
