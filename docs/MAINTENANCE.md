# Maintenance

## Version and Package Metadata

Update metadata in:

- `src/AF.Umbraco.Azure.Blob.Media.Storage/AF.Umbraco.Azure.Blob.Media.Storage.csproj`

Relevant fields:

- `Version`
- `PackageVersion`
- `PackageReleaseNotes`
- description/title/tags if behavior changes

Keep `umbraco-marketplace.json` aligned with actual runtime behavior.

## Dependency Management

Core package dependencies:

- `Umbraco.Cms.Web.Common`
- `Umbraco.StorageProviders.AzureBlob`
- `Umbraco.StorageProviders.AzureBlob.ImageSharp`

When upgrading, verify compatibility across all supported Umbraco majors (15/16/17).

## Operational Regression Checklist

1. Startup checks required config for both `Media` and `ImageSharp`.
2. Startup connectivity check passes/fails correctly.
3. Container ensure logic creates missing containers during startup checks.
4. Cache-retention cleanup respects `ImageSharp:CacheRetention` settings.
5. ImageSharp cache writes under expected container/path layout.
6. Smoke endpoints are available only when `AF_SMOKE_TESTS=1`.

## Documentation Alignment Checklist

When behavior changes, update in same change set:

- `README.md`
- `docs/API_REFERENCE.md`
- `docs/ARCHITECTURE.md`
- `docs/CONFIGURATION.md`
- `docs/DEVELOPMENT.md`
- `docs/PROJECT_STRUCTURE.md`
- `docs/CHANGELOG.md`
- `umbraco-marketplace.json` (if relevant)

## Logging Conventions

Use prefix:

- `[AFUABMS]`

Maintain consistent severity:

- `LogInformation`: successful checks
- `LogCritical`: startup-blocking configuration/connectivity failures
- `LogError`: runtime failures or container ensure failures before rethrow
- `LogWarning`: non-blocking package warnings

## Known Build Caveat

Building multiple host samples in parallel can temporarily lock shared schema files.

If this occurs, build hosts sequentially.
