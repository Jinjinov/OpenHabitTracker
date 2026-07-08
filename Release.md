## Description is updated via repo files

- Flathub - `net.openhabittracker.OpenHabitTracker.metainfo.xml` and `net.openhabittracker.OpenHabitTracker.yaml`
- Snap Store - `snapcraft.yaml`
- PWA - `OpenHabitTracker.Web\index.html` for `https://openhabittracker.net/`
- GitHub Container Registry - `README.md`

## Description must be updated manually via web UI

- Microsoft Store - Partner Center (listing upload: use `msstore-listings.ps1`; publish click manual)
- Google Play - Play Console (listing upload: use `fastlane supply`; rollout click manual)
- App Store / Mac App Store - App Store Connect (listing upload: use `fastlane deliver`; submit-for-review manual)
- Docker Hub - Docker Hub web UI (use `docker-release.ps1`)

- APKPure - auto-synced from Google Play

---

- update "© 2026" if necessary

- increase version number (use `bump-version.ps1`)
- increase ApplicationVersion (use `bump-version.ps1`)

- update VersionHistory.md
- update net.openhabittracker.OpenHabitTracker.metainfo.xml (use `bump-version.ps1`)

- create tag on git
- update commit hash in net.openhabittracker.OpenHabitTracker.yaml (use `flathub-update.sh`)

OpenHabitTracker.Web
- update index.html (use `bump-version.ps1`) -> FTP upload to server (use `deploy.ps1 web`)

OpenHabitTracker.Blazor.Maui
- publish and upload to Windows Store
    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-windows10.0.19041.0 -p:SelfContained=true -p:PublishAppxPackage=true
- publish and upload to Google Play Store + apk FTP upload to server (apk FTP upload: use `deploy.ps1 apk`)
    dotnet publish -c Release -f:net9.0-android /p:AndroidKeyStore=True /p:AndroidSigningKeyStore=IdiditGoogleStore.keystore /p:AndroidSigningStorePass=******** /p:AndroidSigningKeyAlias=IdiditGooglePlay /p:AndroidSigningKeyPass==********
    - create GitHub release with attached apk: (use `github-release.ps1`)
        - Web UI
            - "Draft a new release"
            - "Choose a tag"
            - Title: "Version 1.9.9" Notes: paste the matching entry from VersionHistory.md
            - Drag and drop the APK into the assets area
            - "Publish release"
        - CLI:
            gh release create 1.9.9 --title "Version 1.9.9" --notes "<paste from VersionHistory.md>"
            gh release upload 1.9.9 <path-to-apk>
- publish and upload to Apple App Store
    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-ios -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.ios"
- publish and upload to Apple Mac App Store + pkg FTP upload to server (pkg FTP upload: use `deploy-pkg.sh`)
    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-maccatalyst -p:MtouchLink=SdkOnly -p:CreatePackage=true -p:EnableCodeSigning=true -p:EnablePackageSigning=true -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.macos" -p:CodesignEntitlements="Platforms\MacCatalyst\Entitlements.plist" -p:PackageSigningKey="3rd Party Mac Developer Installer: Urban Dzindzinovic (53V66WG4KU)"

OpenHabitTracker.Blazor.Photino
- Publish to Folder -> zip FTP upload to server (use `deploy.ps1 photino`)
- Flatpak (use `flathub-update.sh`)
    - flatpak-builder --download-only
    - python3 flatpak-dotnet-generator.py `nuget-sources.json`
    - flatpak-builder
    - update https://github.com/Jinjinov/net.openhabittracker.OpenHabitTracker : net.openhabittracker.OpenHabitTracker.yaml
    - update https://github.com/Jinjinov/net.openhabittracker.OpenHabitTracker : `nuget-sources.json`
    - pull request: https://github.com/flathub/net.openhabittracker.OpenHabitTracker
    - trigger bot build
- Snapcraft (use `snap-release.sh`)
    - build
    - upload

OpenHabitTracker.Blazor.Wasm
- Publish to Folder -> FTP upload to server (use `deploy.ps1 wasm`)

OpenHabitTracker.Blazor.Web
- set username, password, JWT in `appsettings.json`
- Publish to Folder -> FTP upload to server (use `deploy.ps1 server`)
- Docker (use `docker-release.ps1`)
    - docker compose build
    - push to Docker Hub
    - push to GitHub Container Registry

OpenHabitTracker.Blazor.Wpf
- Publich ClickOnce -> zip FTP upload to server (use `deploy.ps1 wpf`)
