# AF.Umbraco.Azure.Blob.Media.Storage

Azure Blob media storage provider for Umbraco 15/16/17 on .NET 9/10.

This package configures Umbraco to use Azure Blob for:

- Media filesystem
- ImageSharp cache
- Startup fail-fast checks (configuration, connectivity, containers)
- Configurable ImageSharp cache-retention cleanup
- Optional smoke endpoints (`AF_SMOKE_TESTS=1`)

**No `Program.cs` changes are required.**

## Compatibility

- Umbraco CMS: `15.x`, `16.x`, `17.x`
- .NET: `9.0`, `10.0`

## Dependencies

- `Umbraco.StorageProviders.AzureBlob`
- `Umbraco.StorageProviders.AzureBlob.ImageSharp`

## Test hosts and smoke CI
- Local compatibility hosts are included under src/Umbraco.Cms.15.x, src/Umbraco.Cms.16.x, and src/Umbraco.Cms.17.x.
- Each host supports local overrides through appsettings.Local.json.

## Installation

```bash
dotnet add package AF.Umbraco.Azure.Blob.Media.Storage
```

## Required Configuration

Required sections:

- `Umbraco:Storage:AzureBlob:Media`
- `Umbraco:Storage:AzureBlob:ImageSharp`

Required keys in both sections:

- `ConnectionString`
- `ContainerName`

If a required section/key is missing, startup is blocked.

## Optional Configuration

Optional keys:

- `ContainerRootPath`
- `VirtualPath`
- `CreateContainerIfNotExists`

## Cache Retention Cleanup

Configuration path:

- `Umbraco:Storage:AzureBlob:ImageSharp:CacheRetention`

Supported keys:

- `Enabled` (default `false`)
- `NumberOfDays` (default `90`)
- `TestModeEnable` (default `false`)
- `TestModeSweepSeconds` (default `30`)
- `TestModeMaxAgeMinutes` (default `10`)

Behavior:

- normal mode: deletes cache blobs older than `NumberOfDays` every 12 hours
- test mode: sweep/max-age controlled by `TestModeSweepSeconds` / `TestModeMaxAgeMinutes`

## Storage Layout

Recommended setup: separate containers.

- `Media.ContainerName = umbraco17-media`
- `ImageSharp.ContainerName = umbraco17-cache`

Shared-container setup is supported, but use explicit root isolation:

- `Media.ContainerRootPath = media`
- `ImageSharp.ContainerRootPath = cache`

Without explicit isolation, media and cache paths can overlap.

## Configuration Example

```json
{
  "Umbraco": {
    "Storage": {
      "AzureBlob": {
        "CreateContainerIfNotExists": true,
        "Media": {
          "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=azurite-storage;AccountKey=...;BlobEndpoint=http://127.0.0.1:10000/azurite-storage;",
          "ContainerName": "umbraco17",
          "ContainerRootPath": "media",
          "VirtualPath": "~/media"
        },
        "ImageSharp": {
          "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=azurite-storage;AccountKey=...;BlobEndpoint=http://127.0.0.1:10000/azurite-storage;",
          "ContainerName": "umbraco17",
          "ContainerRootPath": "cache",
          "VirtualPath": "~/media",
          "CacheRetention": {
            "Enabled": false,
            "NumberOfDays": 90,
            "TestModeEnable": false,
            "TestModeSweepSeconds": 30,
            "TestModeMaxAgeMinutes": 10
          }
        }
      }
    }
  }
}
```

## Smoke Endpoints (Opt-In)

Enable:

```bash
AF_SMOKE_TESTS=1
```

Endpoints:

- `GET /smoke/health`
- `GET /smoke/debug-test`
- `POST /smoke/media-upload`

## Documentation

- `docs/API_REFERENCE.md`
- `docs/ARCHITECTURE.md`
- `docs/CHANGELOG.md`
- `docs/CONFIGURATION.md`
- `docs/DEVELOPMENT.md`
- `docs/MAINTENANCE.md`
- `docs/PROJECT_STRUCTURE.md`
