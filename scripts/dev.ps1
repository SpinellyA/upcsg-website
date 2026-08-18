<#
.SYNOPSIS
    Starts the API and the Blazor dev server as detached background processes.

.DESCRIPTION
    Two things make "just run it in a terminal" awkward here, and this script exists to
    handle both.

    First, the two projects share UpcsgWeb.Shared. Launching them at the same time makes
    both MSBuild runs try to write UpcsgWeb.Shared.dll at once, and the loser dies with
    "CS2012: Cannot open ... for writing". So this builds the solution once, up front,
    then launches both with --no-build.

    Second, a server started from an editor or an agent session belongs to that session
    and dies with it, which is why the site is reachable one minute and refusing
    connections the next. Start-Process detaches them, so they keep running until you
    stop them or reboot.

    Output goes to .dev-logs, which is gitignored.

.PARAMETER Stop
    Stops whatever this script started, using the pid file in .dev-logs.

.PARAMETER Status
    Reports whether each server is listening and answering.

.PARAMETER SkipBuild
    Launches without rebuilding. Only safe when nothing has changed since the last run.

.EXAMPLE
    ./scripts/dev.ps1
    ./scripts/dev.ps1 -Status
    ./scripts/dev.ps1 -Stop

.NOTES
    Reaching the site from a phone on the same wifi needs more than this: both servers
    have to bind 0.0.0.0 instead of localhost, the API's Cors:AllowedOrigins needs the
    phone-facing origin, wwwroot/appsettings.Development.json has to point Api:BaseUrl at
    the machine's LAN address instead of localhost, and Windows Firewall has to allow
    inbound TCP on 5005 and 5027. That last one is a security setting, so it is left to
    you rather than done here.
#>

[CmdletBinding()]
param(
    [switch] $Stop,
    [switch] $Status,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$logs = Join-Path $repo '.dev-logs'
$pidFile = Join-Path $logs 'pids.json'

$servers = @(
    [pscustomobject]@{ Name = 'api';      Project = 'UpcsgWeb.Api/UpcsgWeb.Api.csproj';           Port = 5027; Probe = 'http://localhost:5027/api/members' }
    [pscustomobject]@{ Name = 'frontend'; Project = 'UpcsgWeb.FrontEnd/UpcsgWeb.FrontEnd.csproj'; Port = 5005; Probe = 'http://localhost:5005/' }
)

function Test-Server($server) {
    try {
        $response = Invoke-WebRequest -Uri $server.Probe -UseBasicParsing -TimeoutSec 5
        return [int] $response.StatusCode
    } catch {
        return 0
    }
}

if ($Stop) {
    if (-not (Test-Path $pidFile)) {
        Write-Host 'Nothing recorded in .dev-logs/pids.json. Nothing to stop.'
        return
    }
    # Assign before enumerating. Windows PowerShell's ConvertFrom-Json hands a JSON array
    # down the pipeline as a single Object[] item, so iterating the pipeline directly
    # yields one entry that is the whole array. Assignment unrolls it; the cast then
    # covers the other direction, where a lone entry arrives as a bare object.
    $entries = [array] (Get-Content $pidFile -Raw | ConvertFrom-Json)
    foreach ($entry in $entries) {
        try {
            Stop-Process -Id $entry.Pid -Force -ErrorAction Stop
            Write-Host ("stopped {0} (pid {1})" -f $entry.Name, $entry.Pid)
        } catch {
            Write-Host ("{0} (pid {1}) was already gone" -f $entry.Name, $entry.Pid)
        }
    }
    Remove-Item $pidFile -Force
    return
}

if ($Status) {
    foreach ($server in $servers) {
        $code = Test-Server $server
        if ($code -eq 200) {
            Write-Host ("{0,-9} up    {1}" -f $server.Name, $server.Probe)
        } else {
            Write-Host ("{0,-9} DOWN  {1}" -f $server.Name, $server.Probe)
        }
    }
    return
}

if (-not (Test-Path $logs)) { New-Item -ItemType Directory -Path $logs | Out-Null }

if (-not $SkipBuild) {
    Write-Host 'Building once so the two launches do not race for UpcsgWeb.Shared.dll...'
    & dotnet build (Join-Path $repo 'UpcsgWeb.FrontEnd.slnx') -v q --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Build failed. Not launching anything.'
        exit 1
    }
}

$started = @()
foreach ($server in $servers) {
    if ((Test-Server $server) -eq 200) {
        # Record the pid of a server this run did not start, otherwise -Stop later has
        # no idea it exists and silently leaves it running.
        $owner = Get-NetTCPConnection -LocalPort $server.Port -State Listen -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty OwningProcess
        if ($owner) { $started += [pscustomobject]@{ Name = $server.Name; Pid = [int] $owner } }
        Write-Host ("{0} is already answering on {1}, leaving it alone" -f $server.Name, $server.Port)
        continue
    }

    $arguments = @('run', '--project', $server.Project, '--launch-profile', 'http')
    if (-not $SkipBuild) { $arguments += '--no-build' }

    $process = Start-Process -FilePath 'dotnet' -ArgumentList $arguments `
        -WorkingDirectory $repo -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $logs "$($server.Name).log") `
        -RedirectStandardError (Join-Path $logs "$($server.Name).err.log")

    $started += [pscustomobject]@{ Name = $server.Name; Pid = $process.Id }
    Write-Host ("starting {0} (pid {1})" -f $server.Name, $process.Id)
}

if ($started.Count -gt 0) {
    $started | ConvertTo-Json | Set-Content $pidFile -Encoding utf8
}

Write-Host 'Waiting for both to answer...'
foreach ($server in $servers) {
    $code = 0
    foreach ($attempt in 1..30) {
        $code = Test-Server $server
        if ($code -eq 200) { break }
        Start-Sleep -Seconds 2
    }
    if ($code -eq 200) {
        Write-Host ("{0,-9} ready {1}" -f $server.Name, $server.Probe)
    } else {
        Write-Host ("{0,-9} did not come up. See .dev-logs/{0}.log" -f $server.Name)
    }
}

Write-Host ''
Write-Host 'Open http://localhost:5005 in your browser.'
Write-Host 'Stop them later with ./scripts/dev.ps1 -Stop'
