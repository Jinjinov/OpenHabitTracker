# Builds the MAUI app for the iOS simulator twice, Release as net.openhabittracker.uitest and Debug as net.openhabittracker.uitest.debug.
# Release, because trimming and AOT apply only there; its page is reachable only through the accessibility tree.
# Debug, for the tests that need the page's DOM: BlazorWebView makes the WKWebView inspectable only when developer tools are registered, in Debug.
# The separate bundle ids keep both builds installed side by side, and apart from an everyday install of net.openhabittracker.
# The bundles land in ios/release and ios/debug, which every iOS fixture installs unless the simulator already has exactly that bundle.

param(
    # Another checkout can be passed to make an older or deliberately broken build the build under test.
    [string]$SourceRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$LogFolder = (Join-Path ([IO.Path]::GetTempPath()) 'oht-ios-build')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/appium-env.ps1"
. "$PSScriptRoot/work-folder.ps1"

$SourceRoot = (Resolve-Path $SourceRoot).Path
New-Item -ItemType Directory -Force $LogFolder | Out-Null

$WorkFolder = Sync-WorkFolder $SourceRoot

$Builds = [ordered]@{
    'release' = @{ Configuration = 'Release'; BundleId = 'net.openhabittracker.uitest' }
    'debug' = @{ Configuration = 'Debug'; BundleId = 'net.openhabittracker.uitest.debug' }
}

foreach ($name in $Builds.Keys) {
    $build = $Builds[$name]
    $logFile = Join-Path $LogFolder "$name.log"

    # The iOS Info.plist names the bundle id itself, which wins over ApplicationId, so the copy's Info.plist is rewritten.
    plutil -replace CFBundleIdentifier -string $build.BundleId (Join-Path $WorkFolder 'OpenHabitTracker.Blazor.Maui/Platforms/iOS/Info.plist')
    if ($LASTEXITCODE -ne 0) { throw "plutil could not set the bundle id $($build.BundleId)" }

    # The SDK recompiles the manifest only when its inputs file changes, which names the Info.plist but not its content,
    # so an edited Info.plist would ship the bundle id of the last compile.
    Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $WorkFolder "OpenHabitTracker.Blazor.Maui/obj/$($build.Configuration)/net10.0-ios/iossimulator-arm64/AppManifest.plist")

    # The csproj's Release RuntimeIdentifier is the device's; the simulator's replaces it.
    Push-Location (Join-Path $WorkFolder 'OpenHabitTracker.Blazor.Maui')
    try {
        dotnet build -f net10.0-ios -c $build.Configuration -p:RuntimeIdentifier=iossimulator-arm64 "-p:ApplicationId=$($build.BundleId)" *> $logFile
        if ($LASTEXITCODE -ne 0) { throw "The $name build failed, see $logFile" }
    }
    finally {
        Pop-Location
    }

    $app = Join-Path $WorkFolder "OpenHabitTracker.Blazor.Maui/bin/$($build.Configuration)/net10.0-ios/iossimulator-arm64/OpenHT.app"

    $bundleId = (plutil -extract CFBundleIdentifier raw (Join-Path $app 'Info.plist')).Trim()
    if ($bundleId -ne $build.BundleId) { throw "$app carries the bundle id $bundleId, not $($build.BundleId)" }

    $target = Join-Path $DeviceTestsFolder "ios/$name"
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    New-Item -ItemType Directory -Force $target | Out-Null

    # ditto keeps the bundle's symlinks and signature intact.
    ditto $app (Join-Path $target 'OpenHT.app')
    if ($LASTEXITCODE -ne 0) { throw "ditto could not copy $app" }

    Write-Host "Built $($build.BundleId) from $SourceRoot into $target"
}
