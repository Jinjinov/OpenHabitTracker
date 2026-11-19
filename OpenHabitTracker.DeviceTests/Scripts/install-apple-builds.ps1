# Builds the MAUI app for the Apple device tests: the iOS simulator in Release as net.openhabittracker.uitest
# and in Debug as net.openhabittracker.uitest.debug, and macCatalyst in Debug as net.openhabittracker.uitest.debug.
# iOS Release, because trimming and AOT apply only there; its page is reachable only through the accessibility tree.
# iOS Debug, for the tests that need the page's DOM: BlazorWebView makes the WKWebView inspectable only when developer tools are registered, in Debug.
# macCatalyst Debug, because the Release bundle is signed for the App Store and cannot launch on this Mac.
# The separate bundle ids keep the builds installed side by side, and apart from an everyday install of net.openhabittracker.
# The bundles land in ios/release, ios/debug and maccatalyst/debug, which the fixtures install from.

param(
    # Another checkout can be passed to make an older or deliberately broken build the build under test.
    [string]$SourceRoot = (Join-Path $PSScriptRoot '../..'),
    [string]$LogFolder = (Join-Path ([IO.Path]::GetTempPath()) 'oht-apple-build')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/appium-env.ps1"
. "$PSScriptRoot/work-folder.ps1"

$SourceRoot = (Resolve-Path $SourceRoot).Path
New-Item -ItemType Directory -Force $LogFolder | Out-Null

$WorkFolder = Sync-WorkFolder $SourceRoot

# The csproj's Release RuntimeIdentifier for iOS is the device's; the simulator's replaces it.
# macCatalyst would otherwise default to x64 and run under Rosetta.
$Builds = @(
    @{ Name = 'ios/release'; Platform = 'iOS'; Framework = 'net10.0-ios'; RuntimeIdentifier = 'iossimulator-arm64'; Configuration = 'Release'; BundleId = 'net.openhabittracker.uitest'; InfoPlist = 'Info.plist' }
    @{ Name = 'ios/debug'; Platform = 'iOS'; Framework = 'net10.0-ios'; RuntimeIdentifier = 'iossimulator-arm64'; Configuration = 'Debug'; BundleId = 'net.openhabittracker.uitest.debug'; InfoPlist = 'Info.plist' }
    @{ Name = 'maccatalyst/debug'; Platform = 'MacCatalyst'; Framework = 'net10.0-maccatalyst'; RuntimeIdentifier = 'maccatalyst-arm64'; Configuration = 'Debug'; BundleId = 'net.openhabittracker.uitest.debug'; InfoPlist = 'Contents/Info.plist' }
)

foreach ($build in $Builds) {
    $logFile = Join-Path $LogFolder "$($build.Name.Replace('/', '-')).log"

    # The platform's Info.plist names the bundle id itself, which wins over ApplicationId, so the copy's Info.plist is rewritten.
    plutil -replace CFBundleIdentifier -string $build.BundleId (Join-Path $WorkFolder "OpenHabitTracker.Blazor.Maui/Platforms/$($build.Platform)/Info.plist")
    if ($LASTEXITCODE -ne 0) { throw "plutil could not set the bundle id $($build.BundleId)" }

    # The SDK recompiles the manifest only when its inputs file changes, which names the Info.plist but not its content,
    # so an edited Info.plist would ship the bundle id of the last compile.
    Remove-Item -Force -ErrorAction SilentlyContinue (Join-Path $WorkFolder "OpenHabitTracker.Blazor.Maui/obj/$($build.Configuration)/$($build.Framework)/$($build.RuntimeIdentifier)/AppManifest.plist")

    Push-Location (Join-Path $WorkFolder 'OpenHabitTracker.Blazor.Maui')
    try {
        dotnet build -f $build.Framework -c $build.Configuration "-p:RuntimeIdentifier=$($build.RuntimeIdentifier)" "-p:ApplicationId=$($build.BundleId)" *> $logFile
        if ($LASTEXITCODE -ne 0) { throw "The $($build.Name) build failed, see $logFile" }
    }
    finally {
        Pop-Location
    }

    $app = Join-Path $WorkFolder "OpenHabitTracker.Blazor.Maui/bin/$($build.Configuration)/$($build.Framework)/$($build.RuntimeIdentifier)/OpenHT.app"

    $bundleId = (plutil -extract CFBundleIdentifier raw (Join-Path $app $build.InfoPlist)).Trim()
    if ($bundleId -ne $build.BundleId) { throw "$app carries the bundle id $bundleId, not $($build.BundleId)" }

    $target = Join-Path $DeviceTestsFolder $build.Name
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    New-Item -ItemType Directory -Force $target | Out-Null

    # ditto keeps the bundle's symlinks and signature intact.
    ditto $app (Join-Path $target 'OpenHT.app')
    if ($LASTEXITCODE -ne 0) { throw "ditto could not copy $app" }

    Write-Host "Built $($build.BundleId) from $SourceRoot into $target"
}
