# API Reference

## Public Composition Entry Point

### `AF.Umbraco.Azure.Blob.Media.Storage.Composers.AzureBlobComposer`

Registers all runtime behaviors of the package.

Main registrations:

- `AddAzureBlobMediaFileSystem()`
- `AddAzureBlobFileSystem("ImageSharp")`
- `AddAzureBlobImageSharpCache("ImageSharp", "cache")`
- `AzureBlobStartupConnectivityHostedService`
- `AzureBlobCacheRetentionCleanupHostedService`
- optional `AzureBlobSmokeTestsMiddleware` when `AF_SMOKE_TESTS=1`

Pipeline filters:

- `AzureBlobSmokeTests` (only when `AF_SMOKE_TESTS=1`)

## Hosted Service

### `AF.Umbraco.Azure.Blob.Media.Storage.Bootstrap.AzureBlobStartupConnectivityHostedService`

Fail-fast startup checker.

Responsibilities:

- requires sections:
  - `Umbraco:Storage:AzureBlob:Media`
  - `Umbraco:Storage:AzureBlob:ImageSharp`
- requires keys per section:
  - `ConnectionString`
  - `ContainerName`
- checks connectivity with `BlobServiceClient.GetPropertiesAsync`
- ensures containers exist with `BlobContainerClient.CreateIfNotExistsAsync` when auto-create is enabled
- fails startup when requirements are not met

Container auto-create setting resolution:

1. `Umbraco:Storage:AzureBlob:{Section}:CreateContainerIfNotExists`
2. `Umbraco:Storage:AzureBlob:CreateContainerIfNotExists`
3. default `true`

Log prefix: `[AFUABMS]`

### `AF.Umbraco.Azure.Blob.Media.Storage.Bootstrap.AzureBlobCacheRetentionCleanupHostedService`

Background cleanup service for ImageSharp cache retention.

Responsibilities:

- reads `Umbraco:Storage:AzureBlob:ImageSharp:CacheRetention`
- applies normal mode retention (`Enabled`, `NumberOfDays`)
- applies test mode retention (`TestModeEnable`, `TestModeSweepSeconds`, `TestModeMaxAgeMinutes`)
- deletes expired cache blobs under cache prefixes in the configured ImageSharp container

Log prefix: `[AFUABMS]`

### `AF.Umbraco.Azure.Blob.Media.Storage.Middlewares.AzureBlobSmokeTestsMiddleware`

Registered only when `AF_SMOKE_TESTS=1`.

Endpoints:

- `GET /smoke/health`
  - returns `{ "status": "ok" }`
- `GET /smoke/debug-test`
  - verifies filesystem reachability (`FileExists` on generated path)
- `POST /smoke/media-upload`
  - writes, checks, and reads back a text file in media storage

## Resources

Localization resources used by package messages:

- `Resources/AzureBlobFileSystem.resx`
- `Resources/AzureBlobFileSystem.en-US.resx`
- `Resources/AzureBlobFileSystem.it-IT.resx`
- `Resources/AzureBlobFileSystem.de-DE.resx`
- `Resources/AzureBlobFileSystem.fr-FR.resx`
- `Resources/AzureBlobFileSystem.es-ES.resx`
- `Resources/AzureBlobFileSystem.da-DK.resx`
