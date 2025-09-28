OpenHabitTracker.Blazor.Maui
- Windows Store
- Google Play Store + apk download
- Apple App Store
- Apple Mac App Store + pkg download

OpenHabitTracker.Blazor.Photino
- Publish to Folder -> zip download
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
- Publish to Folder -> FTP copy to server

OpenHabitTracker.Blazor.Web
- Publish to Folder -> FTP copy to server
- Docker
    - docker compose build
    - push to Docker Hub
    - push to GitHub Container Registry

OpenHabitTracker.Blazor.Wpf
- Publich ClickOnce -> zip download
