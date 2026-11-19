# Installs the Appium server and the drivers the device tests run against on this machine, each at a pinned version.
# Needs Node.js first: winget install --id OpenJS.NodeJS.LTS --exact --version 26.13.1 on Windows, brew install node on macOS.
# On macOS PowerShell itself comes from dotnet tool install --global PowerShell, not from Homebrew, whose package brings its own dotnet.
# Every driver release requires one Appium server line, so the versions below change together.

$ErrorActionPreference = 'Stop'

$AppiumVersion = '4.0.0'

# Android and Windows are tested on the Windows PC, iOS and macCatalyst on the Mac.
$Drivers = if ($IsWindows) {
    [ordered]@{
        'uiautomator2' = @{ Package = 'appium-uiautomator2-driver'; Version = '9.1.0' }
        # Installed from npm by package name, which works whether or not Appium registers the wincore short name.
        'wincore' = @{ Package = 'appium-wincore-driver'; Version = '4.1.2' }
    }
}
else {
    [ordered]@{
        'xcuitest' = @{ Package = 'appium-xcuitest-driver'; Version = '13.1.3' }
        'mac2' = @{ Package = 'appium-mac2-driver'; Version = '5.0.5' }
    }
}

. "$PSScriptRoot/appium-env.ps1"

npm install --global "appium@$AppiumVersion"
if ($LASTEXITCODE -ne 0) { throw "npm could not install appium@$AppiumVersion" }

$installed = appium driver list --installed --json | ConvertFrom-Json

foreach ($name in $Drivers.Keys) {
    $driver = $Drivers[$name]

    if ($installed.PSObject.Properties.Name -contains $name) {
        if ($installed.$name.version -eq $driver.Version) {
            Write-Host "$name $($driver.Version) is already installed"
            continue
        }

        appium driver uninstall $name
        if ($LASTEXITCODE -ne 0) { throw "appium could not uninstall $name" }
    }

    appium driver install --source=npm "$($driver.Package)@$($driver.Version)"
    if ($LASTEXITCODE -ne 0) { throw "appium could not install $($driver.Package)@$($driver.Version)" }
}

foreach ($name in $Drivers.Keys) {
    appium driver doctor $name
    if ($LASTEXITCODE -ne 0) { throw "appium driver doctor $name found a problem" }
}
