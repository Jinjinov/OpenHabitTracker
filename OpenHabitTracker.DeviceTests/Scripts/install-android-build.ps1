# Builds the MAUI app in Release as net.openhabittracker.uitest and installs it on the emulator,
# and builds the latest release tag the same way for the upgrade test.
# Release, because trimming and AOT apply only there. Debug-signed, because no keystore properties are passed,
# which is also what lets the release tag's build update to the build under test.
# The separate package id matters: a differently signed build installed over net.openhabittracker uninstalls it first,
# which silently deletes its data.
# The APKs land in apks: under-test.apk, which every Android fixture installs unless the device already has exactly that APK,
# and previous.apk with previous.tag naming the tag it was built from.

param(
    # Another checkout can be passed to make an older or deliberately broken build the build under test.
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$Device = 'emulator-5554',
    [string]$LogFolder = (Join-Path $env:TEMP 'oht-android-build')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\appium-env.ps1"
. "$PSScriptRoot\work-folder.ps1"

$PackageId = 'net.openhabittracker.uitest'

$SourceRoot = (Resolve-Path $SourceRoot).Path
New-Item -ItemType Directory -Force $LogFolder | Out-Null

$apkFolder = Join-Path $DeviceTestsFolder 'apks'
New-Item -ItemType Directory -Force $apkFolder | Out-Null

# Builds the app in the given copy of the source and returns the APK, checked to carry the test package id.
# WebViewDevTools lets Appium into the page on a phone, which, unlike the emulator, keeps a Release build's WebView closed.
function Build-Apk([string]$Root, [string]$LogFile) {
    Push-Location (Join-Path $Root 'OpenHabitTracker.Blazor.Maui')
    try {
        dotnet build -f net10.0-android -c Release "-p:ApplicationId=$PackageId" "-p:WebViewDevTools=true" *> $LogFile
        if ($LASTEXITCODE -ne 0) { throw "The build failed, see $LogFile" }
    }
    finally {
        Pop-Location
    }

    $apk = Join-Path $Root "OpenHabitTracker.Blazor.Maui\bin\Release\net10.0-android\$PackageId-Signed.apk"

    $aapt2 = Get-ChildItem (Join-Path $env:ANDROID_HOME 'build-tools\*\aapt2.exe') | Sort-Object { [version]$_.Directory.Name } | Select-Object -Last 1
    $apkPackage = (& $aapt2.FullName dump packagename $apk).Trim()
    if ($apkPackage -ne $PackageId) { throw "$apk carries the package $apkPackage, not $PackageId" }

    return $apk
}

# The build under test.
$WorkFolder = Sync-WorkFolder $SourceRoot
$apk = Build-Apk $WorkFolder (Join-Path $LogFolder 'under-test.log')
Copy-Item $apk (Join-Path $apkFolder 'under-test.apk') -Force

# The latest release, built from its tag only when the tag changed. The tag's files come from git archive, so the checkout is not touched.
$latestTag = git -C $SourceRoot tag --list --sort=-v:refname | Where-Object { $_ -match '^\d+(\.\d+){2,3}$' } | Select-Object -First 1
if (-not $latestTag) { throw "$SourceRoot has no release tag" }

$previousTagFile = Join-Path $apkFolder 'previous.tag'
$previousApk = Join-Path $apkFolder 'previous.apk'
$builtTag = if (Test-Path $previousTagFile) { (Get-Content $previousTagFile -Raw).Trim() } else { $null }

if ($builtTag -ne $latestTag -or -not (Test-Path $previousApk)) {
    $releaseFolder = Join-Path $DeviceTestsFolder 'release'
    if (Test-Path $releaseFolder) { Remove-Item -Recurse -Force $releaseFolder }
    New-Item -ItemType Directory -Force $releaseFolder | Out-Null

    $archive = Join-Path $DeviceTestsFolder "release-$latestTag.zip"
    git -C $SourceRoot archive --format=zip -o $archive $latestTag
    if ($LASTEXITCODE -ne 0) { throw "git could not archive the tag $latestTag" }
    Expand-Archive $archive $releaseFolder
    Remove-Item $archive

    $releaseApk = Build-Apk $releaseFolder (Join-Path $LogFolder "release-$latestTag.log")
    Copy-Item $releaseApk $previousApk -Force
    Set-Content $previousTagFile $latestTag

    Write-Host "Built previous.apk from the tag $latestTag"
}

# The build's own Install target has succeeded without installing anything, logging only a failed uninstall as a warning,
# so the APK is installed here, where a failure is an error. The fixtures install it themselves when the device has anything else.
$adb = Join-Path $env:ANDROID_HOME 'platform-tools\adb.exe'

$output = & $adb -s $Device install -r $apk 2>&1 | Out-String

# An older checkout carries a lower version code, which Android refuses as an update.
# The test package holds only test data, so it is reinstalled from scratch instead.
if ($output -match 'INSTALL_FAILED_VERSION_DOWNGRADE') {
    & $adb -s $Device uninstall $PackageId | Out-Null
    $output = & $adb -s $Device install $apk 2>&1 | Out-String
}

if ($LASTEXITCODE -ne 0) { throw "adb could not install $apk`n$output" }

Write-Host "Installed $PackageId from $SourceRoot on $Device"
