param(
  [string]$EnvFile = (Join-Path $PSScriptRoot "..\.env.production")
)

$ErrorActionPreference = "Stop"
$compose = Join-Path $PSScriptRoot "..\docker\docker-compose.prod.yml"

if (-not (Test-Path $EnvFile)) {
  Write-Error "Missing env file: $EnvFile. Copy deploy/.env.production.example first."
}

Get-Content $EnvFile | ForEach-Object {
  if ($_ -match '^\s*#' -or $_ -match '^\s*$') { return }
  $parts = $_.Split('=', 2)
  if ($parts.Length -eq 2) {
    [System.Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
  }
}

foreach ($key in @("POSTGRES_PASSWORD", "JWT_KEY", "PUBLIC_ORIGIN", "PUBLIC_HOST")) {
  if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) {
    Write-Error "Missing required variable: $key"
  }
}

docker compose --env-file $EnvFile -f $compose build
docker compose --env-file $EnvFile -f $compose up -d
Write-Host "Deployed. Check https://$($env:PUBLIC_HOST)/health"
