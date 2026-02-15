# Configuration

## Configuration Root

All settings are under:

- `Umbraco:Storage:AzureBlob`

## Required Sections

- `Umbraco:Storage:AzureBlob:Media`
- `Umbraco:Storage:AzureBlob:ImageSharp`

## Required Keys Per Section

- `ConnectionString`
- `ContainerName`

If one section or one required key is missing, startup fails.

## Optional Keys Per Section

- `ContainerRootPath`
- `VirtualPath`
- `CreateContainerIfNotExists`
- `CacheRetention` (ImageSharp section only)

### Notes

- `ContainerRootPath` isolates blob prefixes inside a container.
- `VirtualPath` controls URL path mapping used by the filesystem.
- `CreateContainerIfNotExists` can be set per section or globally.

## `CreateContainerIfNotExists` Resolution

1. `Umbraco:Storage:AzureBlob:Media:CreateContainerIfNotExists` or `...:ImageSharp:...`
2. `Umbraco:Storage:AzureBlob:CreateContainerIfNotExists`
3. default `true`

When final value is `false`, missing containers block startup.

## ImageSharp Cache Retention

Configuration path:

- `Umbraco:Storage:AzureBlob:ImageSharp:CacheRetention`

Available keys:

- `Enabled` (bool, default `false`)
- `NumberOfDays` (int, default `90`)
- `TestModeEnable` (bool, default `false`)
- `TestModeSweepSeconds` (int, default `30`)
- `TestModeMaxAgeMinutes` (int, default `10`)

Behavior:

- normal mode:
  - cleanup active only when `Enabled=true`
  - max age set by `NumberOfDays`
  - sweep interval fixed at 12 hours
- test mode (`TestModeEnable=true`):
  - cleanup forced active
  - max age set by `TestModeMaxAgeMinutes`
  - sweep interval set by `TestModeSweepSeconds`

## Recommended Topologies

### 1. Separate Containers (recommended)

- `Media.ContainerName = umbraco17-media`
- `ImageSharp.ContainerName = umbraco17-cache`

Expected behavior:

- media blobs in `umbraco17-media`
- cache blobs in `umbraco17-cache` under `cache/...`

### 2. Shared Container (supported)

- `Media.ContainerName = umbraco17`
- `ImageSharp.ContainerName = umbraco17`

Use explicit isolation:

- `Media.ContainerRootPath = media`
- `ImageSharp.ContainerRootPath = cache`

Without explicit isolation, media/cache overlap may occur.

## Full Examples

### Example A: Minimal required keys (separate containers)

```json
{
  "Umbraco": {
    "Storage": {
      "AzureBlob": {
        "Media": {
          "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=azurite-storage;AccountKey=...;BlobEndpoint=http://127.0.0.1:10000/azurite-storage;",
          "ContainerName": "umbraco17-media"
        },
        "ImageSharp": {
          "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=azurite-storage;AccountKey=...;BlobEndpoint=http://127.0.0.1:10000/azurite-storage;",
          "ContainerName": "umbraco17-cache"
        }
      }
    }
  }
}
```

### Example B: Shared container with explicit roots

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
          "VirtualPath": "~/media"
        }
      }
    }
  }
}
```

### Example C: Separate containers with explicit roots

```json
{
  "Umbraco": {
    "Storage": {
      "AzureBlob": {
        "Media": {
          "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=azurite-storage;AccountKey=...;BlobEndpoint=http://127.0.0.1:10000/azurite-storage;",
          "ContainerName": "umbraco17-media",
          "ContainerRootPath": "media",
          "VirtualPath": "~/media"
        },
        "ImageSharp": {
          "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=azurite-storage;AccountKey=...;BlobEndpoint=http://127.0.0.1:10000/azurite-storage;",
          "ContainerName": "umbraco17-cache",
          "ContainerRootPath": "cache",
          "VirtualPath": "~/media"
        }
      }
    }
  }
}
```

## Shared Account Naming Convention

For multiple Umbraco hosts sharing one storage account:

- Umbraco 15: `umbraco15-media`, `umbraco15-cache`
- Umbraco 16: `umbraco16-media`, `umbraco16-cache`
- Umbraco 17: `umbraco17-media`, `umbraco17-cache`

This prevents cross-host blob mixing.
