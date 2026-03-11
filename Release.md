- increase version number
- increase ApplicationVersion

- create tag on git
- update commit hash in net.openhabittracker.OpenHabitTracker.yaml

- update VersionHistory.md

OpenHabitTracker.Web
- update index.html -> FTP upload to server

OpenHabitTracker.Blazor.Maui
- publish and upload to Windows Store
    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-windows10.0.19041.0 -p:SelfContained=true -p:PublishAppxPackage=true
- publish and upload to Google Play Store + apk FTP upload to server
    dotnet publish -c Release -f:net9.0-android ...
- publish and upload to Apple App Store
    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-ios -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.ios"
- publish and upload to Apple Mac App Store + pkg FTP upload to server
    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-maccatalyst -p:MtouchLink=SdkOnly -p:CreatePackage=true -p:EnableCodeSigning=true -p:EnablePackageSigning=true -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.macos" -p:CodesignEntitlements="Platforms\MacCatalyst\Entitlements.plist" -p:PackageSigningKey="3rd Party Mac Developer Installer: Urban Dzindzinovic (53V66WG4KU)"

OpenHabitTracker.Blazor.Photino
- Publish to Folder -> zip FTP upload to server
- Flatpak
    - flatpak-builder --download-only
    - python3 flatpak-dotnet-generator.py `nuget-sources.json`
    - flatpak-builder
    - update https://github.com/Jinjinov/net.openhabittracker.OpenHabitTracker : net.openhabittracker.OpenHabitTracker.yaml
    - update https://github.com/Jinjinov/net.openhabittracker.OpenHabitTracker : `nuget-sources.json`
    - pull request: https://github.com/flathub/net.openhabittracker.OpenHabitTracker
    - trigger bot build
- Snapcraft
    - build
    - upload

OpenHabitTracker.Blazor.Wasm
- Publish to Folder -> FTP upload to server

OpenHabitTracker.Blazor.Web
- set username, password, JWT in `appsettings.json`
- Publish to Folder -> FTP upload to server
- Docker
    - docker compose build
    - push to Docker Hub
    - push to GitHub Container Registry

OpenHabitTracker.Blazor.Wpf
- Publich ClickOnce -> zip FTP upload to server
