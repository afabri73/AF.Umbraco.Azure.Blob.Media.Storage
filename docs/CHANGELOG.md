# Changelog

## 1.0.0

- First public release.

### Documentation

- Full documentation alignment completed across `README.md` and `docs/*`.
- Updated architecture, API surface, configuration, development, maintenance, and project-structure docs to match the current compiled package behavior.
- Removed references to legacy alternate-provider logic from technical documentation.

### Runtime Behavior

- Composer-centered registration based on official Umbraco Azure Blob providers.
- Startup fail-fast checks implemented via hosted service:
  - required section/key checks
  - storage account connectivity check
  - container ensure/existence check with configurable auto-create
- Added configurable ImageSharp cache-retention cleanup hosted service.
  - configuration path: `Umbraco:Storage:AzureBlob:ImageSharp:CacheRetention`
  - supports normal mode (`Enabled`, `NumberOfDays`) and test mode (`TestModeEnable`, `TestModeSweepSeconds`, `TestModeMaxAgeMinutes`)
- Optional smoke endpoints available only with `AF_SMOKE_TESTS=1`.

### Configuration Clarity

- Clarified required keys (`ConnectionString`, `ContainerName`) for both `Media` and `ImageSharp`.
- Clarified optional keys (`ContainerRootPath`, `VirtualPath`, `CreateContainerIfNotExists`).
- Added explicit guidance for:
  - separate-container topology (recommended)
  - shared-container topology with explicit root isolation
  - per-host container naming convention for shared storage accounts
