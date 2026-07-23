# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A minimal .NET 9 isolated-worker Azure Functions app (Azure Functions v4). Single HTTP-triggered
function (`HttpTriggerFunction.cs`) that echoes back a `name` parameter from the query string or
POST body.

## Commands

Build:
```
dotnet build
```

Run locally (requires Azure Functions Core Tools `func` on PATH):
```
func start
```

Test the running function:
```
curl "http://localhost:7071/api/HttpTriggerFunction?name=World"
```

There are no test projects in this repo currently.

## Commit conventions

Use [Conventional Commits](https://www.conventionalcommits.org/) for every commit message:

```
feat: agregar autenticación JWT
fix: corregir validación del token
docs: actualizar README
refactor: simplificar servicio
chore: actualizar dependencias
```

Common types: `feat`, `fix`, `docs`, `refactor`, `chore`, `test`, `ci`. This is required for
[Release Please](https://github.com/googleapis/release-please) (`release-please.yml`,
`.release-please-config.json`) to compute the next SemVer version and generate `CHANGELOG.md`
correctly — e.g. `fix:` drives a patch bump, `feat:` a minor bump, `feat!:`/`BREAKING CHANGE:` a
major bump. PR titles are enforced against this convention by the `validate-pr-title` job in
`ci.yml`.

## Architecture

- `Program.cs` — isolated-worker host entry point. Builds a `HostBuilder`, wires up
  `ConfigureFunctionsWorkerDefaults()` and Application Insights telemetry, then runs the host.
- `HttpTriggerFunction.cs` — the function class. Each Azure Function is a method decorated with
  `[Function("Name")]` inside a plain class; the class is instantiated via DI (constructor
  injection, e.g. `ILogger<T>`), not manually wired anywhere. New functions are added the same way:
  a new class/method with a trigger attribute (`HttpTrigger`, `TimerTrigger`, etc.) — no central
  registration file to update.
- `host.json` — worker-wide host configuration (currently just Application Insights sampling).
- `local.settings.json` — local-only app settings/connection strings for `func start`. Gitignored;
  never commit real secrets here.
- This is the isolated worker model (`Microsoft.Azure.Functions.Worker.*` packages, `OutputType>Exe`),
  not the older in-process model — APIs like `HttpRequestData`/`HttpResponseData` and
  `HostBuilder`/`ConfigureFunctionsWorkerDefaults` are specific to this model.
