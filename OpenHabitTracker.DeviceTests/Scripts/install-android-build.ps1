# Builds the MAUI app in Release and installs it on the emulator as net.openhabittracker.uitest.
# Release, because trimming and AOT apply only there. Debug-signed, because no keystore properties are passed.
# The separate package id matters: a differently signed build installed over net.openhabittracker uninstalls it first,
# which silently deletes its data.

param(
    # Another checkout can be passed to install an older or deliberately broken build under the same id.
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$Device = 'emulator-5554',
    [string]$LogFile = (Join-Path $env:TEMP 'oht-uitest-build.log')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\appium-env.ps1"

$PackageId = 'net.openhabittracker.uitest'

# The build runs on a copy of the working tree, never in the checkout itself.
# In a shared obj folder an incremental build kept the manifest of an earlier store build, so the APK carried
# net.openhabittracker whatever ApplicationId said, and a later store build could pick up this build's id the same way.
# The copy keeps its own bin and obj between runs, so repeated builds stay incremental.
# Each checkout gets its own copy: robocopy keeps the files' timestamps, so one checkout's older files mirrored over another's
# look older than the build output and the incremental build skips them.
$SourceRoot = (Resolve-Path $SourceRoot).Path
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($SourceRoot.ToLowerInvariant()))).Substring(0, 12)
$WorkFolder = Join-Path $env:LOCALAPPDATA "OpenHabitTracker.DeviceTests\$sourceHash"

robocopy $SourceRoot $WorkFolder /MIR /XD bin obj .git .vs .idea AppPackages publish /XF *.keystore *.pfx *.user /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy could not mirror $SourceRoot to $WorkFolder" }

Push-Location (Join-Path $WorkFolder 'OpenHabitTracker.Blazor.Maui')
try {
    dotnet build -f net10.0-android -c Release "-p:ApplicationId=$PackageId" *> $LogFile
    if ($LASTEXITCODE -ne 0) { throw "The build failed, see $LogFile" }
}
finally {
    Pop-Location
}

$apk = Join-Path $WorkFolder "OpenHabitTracker.Blazor.Maui\bin\Release\net10.0-android\$PackageId-Signed.apk"

$aapt2 = Get-ChildItem (Join-Path $env:ANDROID_HOME 'build-tools\*\aapt2.exe') | Sort-Object { [version]$_.Directory.Name } | Select-Object -Last 1
$apkPackage = (& $aapt2.FullName dump packagename $apk).Trim()
if ($apkPackage -ne $PackageId) { throw "$apk carries the package $apkPackage, not $PackageId, so it is not installed" }

# The build's own Install target has succeeded without installing anything, logging only a failed uninstall as a warning,
# so the APK is installed here, where a failure is an error.
$adb = Join-Path $env:ANDROID_HOME 'platform-tools\adb.exe'

& $adb -s $Device install -r $apk
if ($LASTEXITCODE -ne 0) { throw "adb could not install $apk" }

Write-Host "Installed $PackageId from $SourceRoot on $Device"
