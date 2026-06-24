# TempTestServices

TempTestServices is a test-oriented .NET workspace for Greenhouse service experiments.
The current solution contains an Azure Functions host and an application-layer class
library. Treat this repository as production-shaped even when the behavior is
temporary: keep architecture, naming, dependency boundaries, tests, and
observability consistent with the shared Greenhouse guidance.

## Canonical Documentation

The shared documentation repository is the source of truth for platform context,
agent instructions, architecture, contracts, and .NET engineering style:

- [Greenhouse Documentation](https://github.com/thedrewdz/Greenhouse-Documentation)
- [Greenhouse Documentation AGENTS.md](https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/AGENTS.md)
- [Greenhouse Documentation CONTEXT.md](https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/CONTEXT.md)
- [SOLID skill](https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/.agents/skills/solid/SKILL.md)
- [Implementation skill](https://github.com/thedrewdz/Greenhouse-Documentation/blob/main/.agents/skills/implementation/SKILL.md)

Before making architectural or implementation changes, agents should read this
repo's `AGENTS.md`, then follow the read order in the shared documentation repo.

## Project Layout

```text
TempTestServices/
  AGENTS.md
  CONTEXT.md
  README.md
  TempTest.Application/
    TempTest.Application.csproj
  TempTest.Application.Tests/
    TempTest.Application.Tests.csproj
  TempTest.Domain/
    TempTest.Domain.csproj
  TempTest.Functions/
    TempTest.Functions.slnx
    TempTest.Functions/
      TempTest.Functions.csproj
  TempTest.Infrastructure/
    TempTest.Infrastructure.csproj
```

## Local Development

Prerequisites:

- .NET SDK 10.x
- Azure Functions Core Tools v4, when running the Functions host locally

Useful commands:

```powershell
dotnet restore .\TempTest.Functions\TempTest.Functions.slnx
dotnet build .\TempTest.Functions\TempTest.Functions.slnx
```

Run the Functions app from the project directory:

```powershell
cd .\TempTest.Functions\TempTest.Functions
func start
```

Configure Azure SQL before running the `fnPost` function. For local development,
set `SqlConnectionString` in `local.settings.json`. In deployed environments,
prefer `ConnectionStrings:SensorData` or `SqlConnectionString` through app
configuration.

Local Entra authentication can use:

```text
Server=tcp:svr-greenhouse.database.windows.net,1433;Initial Catalog=db-greenhouse;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;
```

For the deployed Azure Function App, use its system-assigned managed identity and
configure:

```text
SqlConnectionString=Server=tcp:svr-greenhouse.database.windows.net,1433;Initial Catalog=db-greenhouse;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;
```

Apply EF Core migrations from the repository root:

```powershell
dotnet ef database update --project .\TempTest.Infrastructure\TempTest.Infrastructure.csproj --startup-project .\TempTest.Infrastructure\TempTest.Infrastructure.csproj --context TempTestDbContext
```

## Architecture Expectations

- Keep Azure Functions as an adapter layer. Function classes should translate
  triggers into application requests and delegate business behavior.
- Put orchestration, use cases, and domain-facing contracts in application-layer
  projects.
- Keep domain entities and invariants in `TempTest.Domain`.
- Keep Entity Framework Core and Azure SQL persistence in `TempTest.Infrastructure`.
- Keep infrastructure details behind interfaces and inject dependencies through
  constructor injection.
- Add focused tests as behavior is introduced.
- Do not commit local secrets. `local.settings.json` is for local development
  only.
