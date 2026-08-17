<#
.SYNOPSIS
    Runs an EF Core command against the configured database.

.DESCRIPTION
    Reads ConnectionStrings:Production out of the API's user-secrets and passes it to the
    design-time factory through UPCSG_CONNECTION, for this process only.

    The point is that the connection string is never typed, pasted or echoed. Pasting it
    into a shell is how it gets mangled: an unquoted value ends at the first ';' because
    PowerShell treats that as a statement separator, and a '#' in a password starts a
    comment that swallows the rest of the line. Both produce a truncated string and a
    "Format of the initialization string does not conform to specification" error that
    points at a perfectly valid connection string.

    It also means the secret never lands in your shell history.

.EXAMPLE
    ./scripts/ef.ps1 database update
    ./scripts/ef.ps1 migrations add AddSomething
    ./scripts/ef.ps1 dbcontext info
#>

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$EfArgs
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$api = Join-Path $root "UpcsgWeb.Api"
$infra = Join-Path $root "UpcsgWeb.Infrastructure"

if (-not $EfArgs) {
    Write-Host "Usage: ./scripts/ef.ps1 <ef command>   e.g. database update" -ForegroundColor Yellow
    exit 1
}

# Same file the API reads, so there is one place to look when something is wrong.
$localSettings = Join-Path $api "appsettings.Development.local.json"
$conn = $null

if (Test-Path $localSettings) {
    # Strip // comments: the .NET config provider tolerates them, ConvertFrom-Json does not.
    $text = (Get-Content $localSettings -Raw) -replace '(?m)^\s*//.*$', ''
    $conn = ($text | ConvertFrom-Json).ConnectionStrings.Production
}

# Falls back to user-secrets so an existing setup keeps working.
if ([string]::IsNullOrWhiteSpace($conn)) {
    $secretsId = [regex]::Match(
        (Get-Content (Join-Path $api "UpcsgWeb.Api.csproj") -Raw),
        '<UserSecretsId>([^<]+)</UserSecretsId>').Groups[1].Value

    $secretsFile = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secretsId\secrets.json"

    if (Test-Path $secretsFile) {
        $conn = (Get-Content $secretsFile -Raw | ConvertFrom-Json).'ConnectionStrings:Production'
    }
}

if ([string]::IsNullOrWhiteSpace($conn)) {
    Write-Error @"
No connection string found.

Put it in UpcsgWeb.Api/appsettings.Development.local.json (git-ignored):

  {
    "ConnectionStrings": { "Production": "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require" }
  }
"@
}

# Confirm the target without revealing the credentials. Dropping or migrating the wrong
# database is the mistake this one line prevents.
$hostName = [regex]::Match($conn, '(?i)(?:Host|Server)=([^;]+)').Groups[1].Value
$dbName = [regex]::Match($conn, '(?i)Database=([^;]+)').Groups[1].Value
Write-Host "Target: $dbName on $hostName" -ForegroundColor Cyan

$env:UPCSG_CONNECTION = $conn

try {
    Push-Location $infra
    & dotnet ef @EfArgs --project . --startup-project .
    exit $LASTEXITCODE
}
finally {
    Pop-Location

    # Cleared so it cannot leak into anything else run from this session.
    Remove-Item Env:\UPCSG_CONNECTION -ErrorAction SilentlyContinue
}
