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

# Read the secret from user-secrets rather than the environment or a file.
$secretsId = [regex]::Match(
    (Get-Content (Join-Path $api "UpcsgWeb.Api.csproj") -Raw),
    '<UserSecretsId>([^<]+)</UserSecretsId>').Groups[1].Value

$secretsFile = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secretsId\secrets.json"

if (-not (Test-Path $secretsFile)) {
    Write-Error "No user-secrets found. Run: dotnet user-secrets set `"ConnectionStrings:Production`" `"<connection string>`" --project UpcsgWeb.Api"
}

$conn = (Get-Content $secretsFile -Raw | ConvertFrom-Json).'ConnectionStrings:Production'

if ([string]::IsNullOrWhiteSpace($conn)) {
    Write-Error "ConnectionStrings:Production is not set in user-secrets."
}

# Confirm the target without revealing the credentials — dropping or migrating the wrong
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
