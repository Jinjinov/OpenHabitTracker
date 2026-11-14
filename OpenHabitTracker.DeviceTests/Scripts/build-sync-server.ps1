# Publishes the self-hosted server (OpenHabitTracker.Blazor.Web) to the folder the sync tests start it from.
# The tests start and stop it themselves, with an empty database each run.

param(
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$LogFile = (Join-Path $env:TEMP 'oht-sync-server-build.log')
)

$ErrorActionPreference = 'Stop'

$output = Join-Path $env:LOCALAPPDATA 'OpenHabitTracker.DeviceTests\server'

dotnet publish (Join-Path $SourceRoot 'OpenHabitTracker.Blazor.Web') -c Release -o $output *> $LogFile
if ($LASTEXITCODE -ne 0) { throw "The publish failed, see $LogFile" }

Write-Host "Published the server to $output"
