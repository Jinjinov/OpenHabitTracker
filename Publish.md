Publish Windows:

dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-windows10.0.19041.0 -p:SelfContained=true -p:GenerateAppxPackageOnBuild=true

dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-windows10.0.19041.0 -p:SelfContained=true -p:PublishAppxPackage=true

set msix version in Package.appxmanifest

---------------------------------------------------------------------------------------------------

Publish iOS:

run on iOS simulator:
    dotnet build OpenHabitTracker.Blazor.Maui.csproj -t:Run -c:Release -f:net9.0-ios
    dotnet build OpenHabitTracker.Blazor.Maui.csproj -t:Run -c:Release -f:net9.0-ios -p:_DeviceName=:v2:udid=YOUR_UDID
    https://learn.microsoft.com/en-us/dotnet/maui/ios/cli?view=net-maui-8.0#launch-the-app-on-a-specific-simulator

dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-ios -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.ios"

OpenHabitTracker/OpenHabitTracker.Blazor.Maui/bin/Release/net9.0-ios/ios-arm64/publish/OpenHT.ipa

Publish macOS:

run on macOS:
    dotnet build OpenHabitTracker.Blazor.Maui.csproj -t:Run -c:Release -f:net9.0-maccatalyst

dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c:Release -f:net9.0-maccatalyst -p:MtouchLink=SdkOnly -p:CreatePackage=true -p:EnableCodeSigning=true -p:EnablePackageSigning=true -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.macos" -p:CodesignEntitlements="Platforms\MacCatalyst\Entitlements.plist" -p:PackageSigningKey="3rd Party Mac Developer Installer: Urban Dzindzinovic (53V66WG4KU)"

OpenHabitTracker/OpenHabitTracker.Blazor.Maui/bin/Release/net9.0-maccatalyst/publish/OpenHT-1.1.5.pkg

---------------------------------------------------------------------------------------------------

Android:

run on Android emulator:
    dotnet build -t:Run -f:net9.0-android
    https://dev.to/csharpfritz/i-built-an-android-app-on-my-linux-machine-using-net-7-and-maui-41if

F-Droid
    not possible: https://forum.f-droid.org/t/why-isnt-c-net-maui-supported/24842

APKPure
    https://apkpure.com/submit-apk
    https://developer.apkpure.com/
    https://iphone.apkpure.com/ipa-install-online

---------------------------------------------------------------------------------------------------

Snap: Preinstalled on Ubuntu and derivatives, available for other distros but not preinstalled.
    https://snapcraft.io/docs/dotnet-apps
    https://snapcraft.io/docs/dotnet-plugin

    https://snapcraft.io/docs/gnome-extension
    extensions: [ gnome, dotnet9 ]

    https://documentation.ubuntu.com/snapcraft/latest/reference/extensions/dotnet-extensions/

    https://snapcraft.io/docs/snapcraft-yaml-schema

    sudo snap install snapcraft --classic

    By default, Snapcraft relies on a build provider to create an isolated build environment inside which applications can be built and packaged as snaps without changing the host system.

    sudo snap install lxd

    sudo lxd init --auto

    sudo usermod -aG lxd $USER

    newgrp lxd

    dotnet9 extension is experimental, SNAPCRAFT_ENABLE_EXPERIMENTAL_EXTENSIONS enables it

    To build snapcraft.yaml run:
        SNAPCRAFT_ENABLE_EXPERIMENTAL_EXTENSIONS=1 snapcraft pack --debug

            on pack error:
            
            snapcraft clean openhabittracker

        sudo snap install openhabittracker_1.1.7_amd64.snap --dangerous --devmode

        snap list

        snap run openhabittracker

        snapcraft login

        snapcraft upload --release=stable openhabittracker_1.1.7_amd64.snap

        snapcraft status openhabittracker

    https://snapcraft.io/docs/registering-your-app-name
    https://snapcraft.io/account
    https://snapcraft.io/docs/pre-built-apps

    https://snapcraft.io/docs/gpu-support

    https://snapcraft.io/snaps
    https://dashboard.snapcraft.io/register-snap/
    https://dashboard.snapcraft.io/register-snap-feedback/openhabittracker/

    https://github.com/AvaloniaUI/Avalonia/discussions/15245
    https://forum.snapcraft.io/t/launching-net-7-binaries-seems-is-broken/37880
    https://forum.snapcraft.io/t/problem-to-build-snap-for-net6-0/31251

---------------------------------------------------------------------------------------------------

Flatpak: Preinstalled on Fedora, available for other distros but not preinstalled.

    https://github.com/flathub/org.freedesktop.Sdk.Extension.dotnet9

    https://docs.flatpak.org/en/latest/dotnet.html
    https://docs.flatpak.org/en/latest/available-runtimes.html

    https://flatpak.org/setup/Ubuntu
    https://github.com/flatpak/flatpak-builder-tools
    https://github.com/flatpak/flatpak-builder-tools/tree/master/dotnet

    https://github.com/NickvisionApps/FlatpakGenerator

    sudo apt install flatpak

    sudo apt install gnome-software-plugin-flatpak

    flatpak remote-add --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo
    flatpak remote-add --user --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo

    sudo apt install flatpak-builder

        flatpak-builder --version

        sudo add-apt-repository ppa:flatpak/development
        sudo apt update
        sudo apt install flatpak-builder

git rev-parse 1.2.1

git ls-remote https://github.com/Jinjinov/OpenHabitTracker.git refs/tags/1.2.1

    flatpak-builder build-dir --user --install-deps-from=flathub --download-only net.openhabittracker.OpenHabitTracker.yaml --force-clean

from parent of OpenHabitTracker:

    python3 flatpak-dotnet-generator.py --dotnet 9 --freedesktop 25.08 nuget-sources.json OpenHabitTracker/OpenHabitTracker.Blazor.Photino/OpenHabitTracker.Blazor.Photino.csproj

    desktop-file-validate net.openhabittracker.OpenHabitTracker.desktop

    sudo apt install appstream-util
    appstream-util validate-relax net.openhabittracker.OpenHabitTracker.metainfo.xml
    appstream-util validate net.openhabittracker.OpenHabitTracker.metainfo.xml

    flatpak install -y flathub org.flatpak.Builder

    flatpak run --command=flatpak-builder-lint org.flatpak.Builder appstream net.openhabittracker.OpenHabitTracker.metainfo.xml

from parent of OpenHabitTracker:

    flatpak-builder build-dir --user --force-clean --install --repo=repo net.openhabittracker.OpenHabitTracker.yaml

    Error: Failure spawning rofiles-fuse, exit_status: 1024
    flatpak-builder build-dir --user --force-clean --install --repo=repo net.openhabittracker.OpenHabitTracker.yaml --disable-rofiles-fuse

    flatpak run net.openhabittracker.OpenHabitTracker
        
        ----------------------------------------------------------------------------------------------------------------
        https://docs.flathub.org/docs/for-app-authors/linter/

        https://github.com/flathub-infra/flatpak-builder-lint
        https://github.com/flathub-infra/flatpak-builder-lint#flatpak

        flatpak install flathub -y org.flatpak.Builder
        flatpak run --command=flatpak-builder-lint org.flatpak.Builder --help

        flatpak run --command=flatpak-builder-lint org.flatpak.Builder manifest net.openhabittracker.OpenHabitTracker.yaml
        flatpak run --command=flatpak-builder-lint org.flatpak.Builder repo repo
                                                                        ^	^	the second "repo" is the folder named "repo" created by flatpak-builder in the same folder as the manifest yaml file
        ----------------------------------------------------------------------------------------------------------------

        https://docs.flathub.org/docs/for-app-authors/submission/#submission-pr

        1.
        update: https://github.com/Jinjinov/net.openhabittracker.OpenHabitTracker
            net.openhabittracker.OpenHabitTracker.yaml
            nuget-sources.json

        2.
        pull request: https://github.com/flathub/net.openhabittracker.OpenHabitTracker

        🚧 Test build enqueued.
        🚧 Started test build.
        ✅ Test build succeeded.

        if the test fails:
            - push a fix
            - update tag and commit in yaml
            - comment in the GitHub pull request:
                bot, build net.openhabittracker.OpenHabitTracker

        3.
        Merge pull request

        4.
        update https://github.com/Jinjinov/net.openhabittracker.OpenHabitTracker from https://github.com/flathub/net.openhabittracker.OpenHabitTracker

        5.
        pull https://github.com/Jinjinov/net.openhabittracker.OpenHabitTracker

    https://flathub.org/
    https://github.com/flathub/flathub

    https://github.com/flathub/org.nickvision.money/blob/master/org.nickvision.money.json
    https://github.com/flathub/org.nickvision.tubeconverter/blob/master/org.nickvision.tubeconverter.json

    https://github.com/flathub/net.jenyay.Outwiker/blob/master/net.jenyay.Outwiker.yml
    https://github.com/flathub/in.cinny.Cinny/blob/master/in.cinny.Cinny.yml

---------------------------------------------------------------------------------------------------

https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port

docker compose up: Starts services.
docker compose down: Stops and removes services.
docker compose build: Builds images.
docker compose ps: Lists containers.
docker compose logs: Shows logs.
docker compose stop: Stops containers.
docker compose start: Starts stopped containers.
docker compose restart: Restarts containers.
docker compose exec: Runs a command inside a container.
docker compose run: Runs a one-off command in a container.

old Python tool:
    docker-compose build
    docker-compose up -d

new Go tool:
    docker compose build
    docker compose up -d

DO NOT RUN DOCKER IMAGE FROM THE UI, BECAUSE docker-compose.yml IS NOT USED!!!

Docker Hub:

    docker login

    docker tag openhabittracker jinjinov/openhabittracker:1.2.1
    docker push jinjinov/openhabittracker:1.2.1

    docker tag openhabittracker jinjinov/openhabittracker:latest
    docker push jinjinov/openhabittracker:latest

    https://hub.docker.com/repository/docker/jinjinov/openhabittracker

    https://hub.docker.com/r/jinjinov/openhabittracker

GitHub Container Registry:

    GitHub -> Settings -> Developer settings -> Personal access tokens -> Docker Hub/GHCR Access Token

    echo <GitHubToken> | docker login ghcr.io -u Jinjinov --password-stdin

    docker tag openhabittracker ghcr.io/jinjinov/openhabittracker:1.2.1
    docker push ghcr.io/jinjinov/openhabittracker:1.2.1

    docker tag openhabittracker ghcr.io/jinjinov/openhabittracker:latest
    docker push ghcr.io/jinjinov/openhabittracker:latest

    https://github.com/users/Jinjinov/packages/container/package/openhabittracker - gets redirected to:

    https://github.com/Jinjinov/OpenHabitTracker/pkgs/container/openhabittracker

---------------------------------------------------------------------------------------------------
