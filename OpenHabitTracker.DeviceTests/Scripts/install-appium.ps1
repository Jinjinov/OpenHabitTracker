# Installs the Appium server and the drivers the device tests run against, each at a pinned version.
# Needs Node.js first: winget install --id OpenJS.NodeJS.LTS --exact --version 26.13.1
# Every driver release requires one Appium server line, so the versions below change together.

$ErrorActionPreference = 'Stop'

$AppiumVersion = '4.0.0-beta.8'

$Drivers = [ordered]@{
    'uiautomator2' = @{ Package = 'appium-uiautomator2-driver'; Version = '9.1.0' }
}

. "$PSScriptRoot\appium-env.ps1"

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
