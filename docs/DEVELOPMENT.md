# Development

## Prerequisites

- .NET SDK supporting `net9.0` and `net10.0`
- Azurite or Azure Storage account
- SQL Server (for local Umbraco host samples)

## Build Package

```bash
dotnet build src/AF.Umbraco.Azure.Blob.Media.Storage/AF.Umbraco.Azure.Blob.Media.Storage.csproj
```

## Build Host Samples

```bash
dotnet build src/Umbraco.Cms.15.x/Umbraco.Cms.15.x.csproj
dotnet build src/Umbraco.Cms.16.x/Umbraco.Cms.16.x.csproj
dotnet build src/Umbraco.Cms.17.x/Umbraco.Cms.17.x.csproj
```

## Run Host Sample

```bash
dotnet run --project src/Umbraco.Cms.17.x/Umbraco.Cms.17.x.csproj
```

Configure `appsettings.Local.json` for selected host before run.

## Recommended Local Test Matrix

1. Separate containers with minimal settings.
2. Shared container with explicit `ContainerRootPath` isolation.
3. Auto-create disabled with pre-existing containers.
4. Auto-create disabled with missing container (startup should fail).
5. Media upload in backoffice (standard Umbraco behavior).

## Smoke Endpoints

Enable smoke middleware:

```bash
AF_SMOKE_TESTS=1
```

Routes:

- `GET /smoke/health`
- `GET /smoke/debug-test`
- `POST /smoke/media-upload`

## Smoke Script

```bash
scripts/run-host-smoke.sh <project_path> <tfm> <port> <container_base>
```

Example:

```bash
scripts/run-host-smoke.sh src/Umbraco.Cms.17.x/Umbraco.Cms.17.x.csproj net10.0 5057 umbraco17
```

Environment overrides:

- `AZURE_BLOB_CONNECTION_STRING`
- `UMBRACO_DB_DSN`
- `UMBRACO_DB_PROVIDER`

## Debugging Startup Failures

Check logs for `[AFUABMS]` entries.

Typical fail-fast causes:

- missing `Media` or `ImageSharp` section
- missing `ConnectionString` or `ContainerName`
- invalid storage credentials/endpoint
- missing container when `CreateContainerIfNotExists=false`

## Coding Notes

- keep package logic based on official Umbraco Azure Blob providers
- avoid introducing alternate storage backends in this package
- keep docs aligned with real compiled surface on every change
