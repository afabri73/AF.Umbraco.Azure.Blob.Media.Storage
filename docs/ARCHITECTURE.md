# Architecture

## Overview

This package integrates Azure Blob storage into Umbraco media and ImageSharp cache pipelines using official Umbraco Azure Blob providers.

Design goals:

- zero host `Program.cs` changes
- deterministic startup checks (fail-fast)
- explicit and predictable media/cache storage behavior

## High-Level Components

- `AzureBlobComposer`
  - central composition entry point
- `AzureBlobStartupConnectivityHostedService`
  - startup config/connectivity/container checks
- `AzureBlobCacheRetentionCleanupHostedService`
  - scheduled retention cleanup for ImageSharp cache blobs
- `AzureBlobSmokeTestsMiddleware` (opt-in)
  - diagnostics endpoints for local and CI

## Registration Model

Package registration is composer-driven (`IComposer`) and runs after ImageSharp composition.

At compose time:

1. media filesystem is mapped to Azure Blob (`Media` section)
2. named Azure Blob filesystem `ImageSharp` is registered
3. ImageSharp cache is bound to named filesystem `ImageSharp` with root `cache`
4. smoke middleware is added only if `AF_SMOKE_TESTS=1`
5. startup hosted service is registered for fail-fast checks

## Startup Flow

1. Read `Media` and `ImageSharp` Azure Blob sections.
2. Check required keys (`ConnectionString`, `ContainerName`).
3. Verify account connectivity (`GetPropertiesAsync`).
4. Ensure media/cache containers exist.
   - create when auto-create enabled
   - fail when disabled and missing
5. Continue boot only on success.

## Storage Layout Behavior

Recommended topology:

- separate containers (`<host>-media`, `<host>-cache`)

Supported topology:

- shared container with explicit prefixes:
  - media: `ContainerRootPath = media`
  - cache: `ContainerRootPath = cache`

ImageSharp cache registration in code always uses logical cache root `cache`.

## Error Handling Model

### Startup

- invalid configuration or connectivity error blocks startup
- logs are emitted with prefix `[AFUABMS]`

## Diagnostics Model

When `AF_SMOKE_TESTS=1`, middleware exposes:

- liveness route (`/smoke/health`)
- storage reachability route (`/smoke/debug-test`)
- storage write/read route (`/smoke/media-upload`)

These routes are disabled by default.
