OpenHabitTracker.Blazor.Maui
- publish and upload to Windows Store
- publish and upload to Google Play Store + apk FTP upload to server
- publish and upload to Apple App Store
- publish and upload to Apple Mac App Store + pkg FTP upload to server

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
    - set username, password, JWT in `.env`
    - docker compose build
    - push to Docker Hub
    - push to GitHub Container Registry

OpenHabitTracker.Blazor.Wpf
- Publich ClickOnce -> zip FTP upload to server
