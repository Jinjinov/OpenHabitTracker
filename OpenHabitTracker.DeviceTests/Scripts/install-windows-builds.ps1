# Builds the two Windows hosts that ship in Release, from a copy of the working tree, and installs them the way they ship:
# the MAUI app as a package under its Store identity, and the WinForms app through Velopack.
# They replace any install of the app and keep its data folders, because this PC keeps no personal install.
# Also downloads the latest release's installer from the website, which the Velopack update test installs first.

param(
    # Another checkout can be passed to install an older or deliberately broken build.
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\..'),
    [string]$LogFolder = (Join-Path $env:TEMP 'oht-windows-build')
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\work-folder.ps1"

$SourceRoot = (Resolve-Path $SourceRoot).Path
$WorkFolder = Sync-WorkFolder $SourceRoot
New-Item -ItemType Directory -Force $LogFolder | Out-Null

# MSIX, published with the Store's command and installed as a signed package.
# A Release build's own layout, or the package's contents registered in place, shows Edge's "can't reach https://0.0.0.1/" page instead of the app,
# and Windows 10 installs no unsigned package, so it is signed with a test certificate under the manifest's publisher name.
$maui = Join-Path $WorkFolder 'OpenHabitTracker.Blazor.Maui'
$appPackages = Join-Path $maui 'AppPackages'
if (Test-Path $appPackages) { Remove-Item -Recurse -Force $appPackages }

Push-Location $maui
try {
    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net10.0-windows10.0.19041.0 -p:SelfContained=true -p:PublishAppxPackage=true *> "$LogFolder\msix.log"
    if ($LASTEXITCODE -ne 0) { throw "The MSIX publish failed, see $LogFolder\msix.log" }
}
finally {
    Pop-Location
}

$msix = Get-ChildItem $appPackages -Recurse -Filter '*_x64.msix' | Select-Object -First 1
$identity = ([xml](Get-Content (Join-Path $maui 'Platforms\Windows\Package.appxmanifest'))).Package.Identity
$publisher = $identity.Publisher
$packageName = $identity.Name

$certificateName = 'OpenHabitTracker device tests'
$certificate = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $publisher -and $_.FriendlyName -eq $certificateName } | Select-Object -First 1
if (-not $certificate) {
    $certificate = New-SelfSignedCertificate -Type Custom -Subject $publisher -KeyUsage DigitalSignature -FriendlyName $certificateName -CertStoreLocation Cert:\CurrentUser\My `
        -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}') -NotAfter (Get-Date).AddYears(5)
}

# Windows trusts a package signer only from the machine's Trusted People store, which needs administrator rights once, behind a UAC prompt.
if (-not (Get-ChildItem Cert:\LocalMachine\TrustedPeople | Where-Object Thumbprint -eq $certificate.Thumbprint)) {
    $certificateFile = Join-Path $LogFolder 'device-tests.cer'
    Export-Certificate -Cert $certificate -FilePath $certificateFile | Out-Null
    $trust = Start-Process powershell.exe -Verb RunAs -ArgumentList '-NoProfile', '-Command', "Import-Certificate -FilePath '$certificateFile' -CertStoreLocation Cert:\LocalMachine\TrustedPeople" -PassThru -Wait
    if ($trust.ExitCode -ne 0) { throw "The test certificate could not be trusted (exit $($trust.ExitCode))" }
}

$signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe' | Sort-Object { [version]$_.Directory.Parent.Name } | Select-Object -Last 1
& $signtool.FullName sign /fd SHA256 /sha1 $certificate.Thumbprint $msix.FullName *> "$LogFolder\sign.log"
if ($LASTEXITCODE -ne 0) { throw "signtool could not sign $($msix.FullName), see $LogFolder\sign.log" }

# The Appx module loads only in Windows PowerShell.
# Installing over a package of the same version changes nothing, so the installed one is removed first, its data with it.
powershell.exe -NoProfile -Command "Get-AppxPackage -Name '$packageName' | Remove-AppxPackage -ErrorAction Stop; Add-AppxPackage -Path '$($msix.FullName)' -ForceApplicationShutdown -ErrorAction Stop"
if ($LASTEXITCODE -ne 0) { throw "Windows could not install $($msix.FullName)" }

Write-Host "Installed $packageName from $SourceRoot"

# WinForms, published and packed the way deploy.ps1 does it.
# The version is one patch above the latest release on the website's feed: the installed app checks that feed on every start,
# and a build under test below it would be replaced by the release.
$feed = 'https://openhabittracker.net/download/win/'
$latest = (Invoke-RestMethod "$($feed)releases.win.json").Assets | ForEach-Object { [semver]$_.Version } | Sort-Object | Select-Object -Last 1
$version = "$($latest.Major).$($latest.Minor).$($latest.Patch + 1)-uitest"

$VelopackFolder = Join-Path $DeviceTestsFolder 'velopack'
$publish = Join-Path $VelopackFolder 'publish'
$current = Join-Path $VelopackFolder 'current'
$previous = Join-Path $VelopackFolder 'previous'

foreach ($folder in $publish, $current) {
    if (Test-Path $folder) { Remove-Item -Recurse -Force $folder }
}

$winForms = Join-Path $WorkFolder 'OpenHabitTracker.Blazor.WinForms'

dotnet publish (Join-Path $winForms 'OpenHabitTracker.Blazor.WinForms.csproj') -c Release -r win-x64 --self-contained -o $publish *> "$LogFolder\winforms.log"
if ($LASTEXITCODE -ne 0) { throw "The WinForms publish failed, see $LogFolder\winforms.log" }

vpk pack -u OpenHT -v $version -p $publish -e OpenHT.exe --packTitle OpenHabitTracker --packAuthors Jinjinov -i (Join-Path $winForms 'favicon.ico') --framework webview2 -o $current *> "$LogFolder\vpk.log"
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed, see $LogFolder\vpk.log" }

# The update test installs the latest release from its own installer, kept until a newer release replaces it.
$previousSetup = Join-Path $previous "OpenHabitTracker.Windows.$latest.exe"
if (-not (Test-Path $previousSetup)) {
    if (Test-Path $previous) { Remove-Item -Recurse -Force $previous }
    New-Item -ItemType Directory -Force $previous | Out-Null
    Invoke-WebRequest "$($feed)OpenHabitTracker.Windows.$latest.exe" -OutFile $previousSetup
}

# Velopack's Setup refuses an app that is already installed, so the installed one is removed first; its data folder is not touched.
$update = Join-Path $env:LOCALAPPDATA 'OpenHT\Update.exe'
if (Test-Path $update) {
    Get-Process OpenHT -ErrorAction SilentlyContinue | Stop-Process -Force
    $uninstall = Start-Process $update -ArgumentList 'uninstall', '--silent' -PassThru -Wait
    if ($uninstall.ExitCode -ne 0) { throw "Velopack could not uninstall the installed app (exit $($uninstall.ExitCode))" }
}

$setup = Get-ChildItem $current -Filter '*Setup.exe' | Select-Object -First 1
$install = Start-Process $setup.FullName -ArgumentList '--silent', '--log', "`"$LogFolder\setup.log`"" -PassThru -Wait
if ($install.ExitCode -ne 0) { throw "Velopack Setup failed (exit $($install.ExitCode)), see $LogFolder\setup.log" }

Write-Host "Installed OpenHT $version from $SourceRoot"
