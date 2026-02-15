#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 4 ]]; then
  echo "Usage: $0 <project_path> <tfm> <port> <container_base>" >&2
  exit 1
fi

project_path="$1"
tfm="$2"
port="$3"
container_base="$4"
project_dir="$(dirname "$project_path")"
local_settings="$project_dir/appsettings.Local.json"
log_file="$project_dir/smoke-${tfm}.log"
pid_file="$project_dir/smoke-${tfm}.pid"
default_azurite_cs="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=YOUR-AZURITE-ACCOUNT-KEY;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
connection_string="${AZURE_BLOB_CONNECTION_STRING:-$default_azurite_cs}"
default_db_dsn='Server=localhost;Database=UMBRACO_17_FOR_PACKAGES;User ID=umbraco_packages_user;Password=YOUR-SQL-PASSWORD;Trust Server Certificate=True;Multiple Active Result Sets=True;Encrypt=True'
db_dsn="${UMBRACO_DB_DSN:-$default_db_dsn}"
db_provider="${UMBRACO_DB_PROVIDER:-Microsoft.Data.SqlClient}"
default_unattended_password="YOUR-UNATTENDED-PASSWORD"
unattended_password="${UMBRACO_UNATTENDED_PASSWORD:-$default_unattended_password}"

if [[ "$connection_string" == *"YOUR-AZURITE-ACCOUNT-KEY"* ]]; then
  echo "Set AZURE_BLOB_CONNECTION_STRING with a valid Azure Blob or Azurite key before running smoke tests." >&2
  exit 1
fi

if [[ "$db_dsn" == *"YOUR-SQL-PASSWORD"* ]]; then
  echo "Set UMBRACO_DB_DSN with a valid SQL connection string before running smoke tests." >&2
  exit 1
fi

if [[ "$unattended_password" == "YOUR-UNATTENDED-PASSWORD" ]]; then
  echo "Set UMBRACO_UNATTENDED_PASSWORD before running smoke tests." >&2
  exit 1
fi

cleanup() {
  if [[ -f "$pid_file" ]]; then
    pid="$(cat "$pid_file")"
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid" || true
      wait "$pid" 2>/dev/null || true
    fi
    rm -f "$pid_file"
  fi
}

trap cleanup EXIT

cat > "$local_settings" <<JSON
{
  "ConnectionStrings": {
    "umbracoDbDSN": "${db_dsn}",
    "ProviderName": "${db_provider}"
  },
  "Umbraco": {
    "CMS": {
      "Unattended": {
        "InstallUnattended": true,
        "UpgradeUnattended": true,
        "UnattendedUserName": "smoke-admin",
        "UnattendedUserEmail": "smoke@example.local",
        "UnattendedUserPassword": "${unattended_password}"
      }
    },
    "Storage": {
      "AzureBlob": {
        "Media": {
          "ConnectionString": "${connection_string}",
          "ContainerName": "${container_base}-media"
        },
        "ImageSharp": {
          "ConnectionString": "${connection_string}",
          "ContainerName": "${container_base}-cache"
        }
      }
    }
  }
}
JSON

AF_SMOKE_TESTS=1 \
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project "$project_path" -c Release -f "$tfm" --no-build --urls "http://127.0.0.1:${port}" > "$log_file" 2>&1 &

echo $! > "$pid_file"

echo "Waiting for host on port ${port}..."
for _ in $(seq 1 180); do
  if curl -fsS "http://127.0.0.1:${port}/smoke/health" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

if ! curl -fsS "http://127.0.0.1:${port}/smoke/health" >/dev/null 2>&1; then
  echo "Host failed to boot for ${project_path} (${tfm})." >&2
  cat "$log_file" >&2 || true
  exit 1
fi

debug_response="$(curl -fsS "http://127.0.0.1:${port}/smoke/debug-test")"
echo "$debug_response"

if [[ "$debug_response" != *'"status":"ok"'* ]]; then
  echo "Debug smoke test failed: unexpected response." >&2
  cat "$log_file" >&2 || true
  exit 1
fi

response="$(curl -fsS -X POST "http://127.0.0.1:${port}/smoke/media-upload")"
echo "$response"

if [[ "$response" != *'"exists":true'* ]]; then
  echo "Media upload smoke test failed: unexpected response." >&2
  cat "$log_file" >&2 || true
  exit 1
fi

echo "Smoke test passed for ${project_path} (${tfm})."
