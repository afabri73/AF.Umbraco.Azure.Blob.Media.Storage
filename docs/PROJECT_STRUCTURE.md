# Project Structure

## Root

- `README.md`
  Main package overview and quick-start details.
- `docs/`
  Technical documentation set.
- `scripts/`
  Utility scripts (including host smoke runner).
- `src/`
  Package and host sample projects.
- `umbraco-marketplace.json`
  Marketplace metadata.

## Main Package

Path:

- `src/AF.Umbraco.Azure.Blob.Media.Storage/`

Primary runtime folders:

- `Composers/`
  - package auto-composition entry point
- `Bootstrap/`
  - startup hosted service for fail-fast checks
- `Middlewares/`
  - optional smoke test middleware
- `Resources/`
  - localization resources for package messages

Additional notes:

- `bin/` and `obj/` are build artifacts.
- some empty folders may remain from previous refactors; they are not part of active runtime behavior.

## Host Sample Projects

- `src/Umbraco.Cms.15.x/`
- `src/Umbraco.Cms.16.x/`
- `src/Umbraco.Cms.17.x/`

These projects are used for local testing and smoke checks across Umbraco majors.

## Scripts

- `scripts/run-host-smoke.sh`

Automates host startup and smoke endpoint checks.
