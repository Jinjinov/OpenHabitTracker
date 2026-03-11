# Notes:

nuget.config

<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <add key="PhotinoPackages" value="./OpenHabitTracker.Blazor.Photino/Packages" />
    </packageSources>
</configuration>

---------------------------------------------------------------------------------------------------

Host and deploy ASP.NET Core Blazor WebAssembly on IIS:

https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly?view=aspnetcore-9.0#install-the-url-rewrite-module

Install the URL Rewrite Module:

https://www.iis.net/downloads/microsoft/url-rewrite

https://www.iis.net/downloads/microsoft/url-rewrite#additionalDownloads

---------------------------------------------------------------------------------------------------

text: ¡!
background shape: Circle
font: Miltonian
Regular 400 Normal
font size: 110
font color: #EEEEEE
background color: #333333

---------------------------------------------------------------------------------------------------

RegEx to find all trailing whitespace: (?<=\S)[ ]{2,}$

---------------------------------------------------------------------------------------------------

Screenshots dimensions for macOS should be: 1280x800 1440x900 2560x1600 2880x1800
https://stackoverflow.com/questions/67972372/why-are-window-height-and-window-width-not-exact-c-wpf
a difference of 7px in Height and 14px in Width, header 31px

You must upload a screenshot for 12.9-inch iPad Pro (2nd generation) displays.
                          2048 x 2732 or 2732 x 2048
You must upload a screenshot for 13-inch iPad displays.
2064 x 2752, 2752 x 2064, 2048 x 2732 or 2732 x 2048

You must upload a screenshot for 6.7-inch iPhone displays.
1290 x 2796 or 2796 x 1290
You must upload a screenshot for 6.5-inch iPhone displays.
1242 x 2688, 2688 x 1242, 1284 x 2778 or 2778 x 1284
You must upload a screenshot for 5.5-inch iPhone displays.
1242 x 2208 or 2208 x 1242

---------------------------------------------------------------------------------------------------

https://github.com/dotnet/maui/wiki/Release-Versions

`dotnet --info`

`dotnet workload search`

`dotnet workload list`

`dotnet workload update`

`dotnet nuget locals all --list`

`dotnet nuget locals all --clear`

---------------------------------------------------------------------------------------------------

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

Linux:

Photino.Native.so
sudo apt-get install libwebkit2gtk-4.1

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

Default project: OpenHabitTracker.EntityFrameworkCore
Add-Migration Initial -StartupProject OpenHabitTracker.Blazor.WinForms

dotnet tool update --global dotnet-ef
dotnet ef migrations add Initial --project OpenHabitTracker.EntityFrameworkCore --startup-project OpenHabitTracker.Blazor.WinForms

---------------------------------------------------------------------------------------------------

- Transient services are created each time they are requested

- Scoped services are created once per scope (usually per HTTP request in web applications)

    Scoped dependencies in server-side Blazor will live for the duration of the user's session.

    Instances of Scoped dependencies will be shared across pages and components for a single user, 
    but not between different users and not across different tabs in the same browser.

    Scoped services in client-side Blazor are instantiated when the application starts and are available throughout its lifetime.

    !!! Scoped services in Blazor WASM have the same instance before and after `app.Run();`

    !!! Scoped services in Blazor WebView DO NOT have the same instance before and after `app.Run();`

    !!! Scoped services in Blazor Server ARE NOT available before `app.Run();`

- Singleton services are created once per application lifetime

---------------------------------------------------------------------------------------------------

!!! Blazor lifecycle methods swallow exceptions !!! -> remove `protected override async Task OnInitializedAsync()`
!!! Blazor @inject swallows constructor exceptions !!! -> add `@using Microsoft.Extensions.Logging` @inject ILogger Logger

Component instantiation: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#component-instantiation
When Blazor creates instances of your components, it invokes their constructors, as well as constructors for any DI services being supplied to them via @inject or the [Inject] attribute. 
If any of these constructors throws an exception, or if the setters for [Inject] properties throw exceptions, this is fatal to the circuit because it's impossible for the framework to carry out the intentions of the developer.

Lifecycle methods: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#lifecycle-methods
During the lifetime of components, Blazor invokes lifecycle methods on components such as OnInitialized, OnParametersSet, ShouldRender, OnAfterRender, and the ...Async versions of these. 
If any of these lifecycle methods throws an exception, synchronously or asynchronously, this is fatal to the circuit because the framework no longer knows whether or how to render that component.

---------------------------------------------------------------------------------------------------

https://github.com/dotnet/sdk/issues/13395 - EmbeddedResource with two dots in Filename not working

https://github.com/dotnet/roslyn/issues/43820 - Embedded Resources with multiple dots in name does not get embedded

Visual Studio seems to recognise files named in the format <name>.<locale>.<ext> as being specific to a locale, resulting in the creation of a satellite assembly
OpenHabitTracker.dll
de\OpenHabitTracker.resources.dll
en\OpenHabitTracker.resources.dll
es\OpenHabitTracker.resources.dll
sl\OpenHabitTracker.resources.dll

in the `es\OpenHabitTracker.resources.dll` the file `MyEmbeddedResource.es-ES.json` becomes `MyEmbeddedResource.json`

var satelliteAssembly = resourceSource.Assembly.GetSatelliteAssembly(CultureInfo.CreateSpecificCulture("es"));
var names = satelliteAssembly.GetManifestResourceNames();

solution: WithCulture="false"

<ItemGroup>
    <EmbeddedResource Include="test.en.json" WithCulture="false" />
    <EmbeddedResource Include="Resources\**" WithCulture="false" />
<ItemGroup>

---------------------------------------------------------------------------------------------------

EventCallback<T> Error cannot convert from 'method group' to 'EventCallback'

workaround: TValue="int" / TValue="long" for <InputNumber> <InputSelect> <InputDate> <InputRadio> but not <InputCheckbox> <InputText> <InputTextArea>

---------------------------------------------------------------------------------------------------

Blazor's enhanced navigation and form handling avoid the need for a full-page reload and preserves more of the page state
    https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-8.0#enhanced-navigation-and-form-handling

Blazor NavigationManager.NavigateTo always scrolls page to the top
    https://github.com/dotnet/aspnetcore/issues/40190#issuecomment-1324689082

---------------------------------------------------------------------------------------------------

Blazor magic strings: blazor-error-ui , --blazor-load-percentage , --blazor-load-percentage-text , blazor-error-boundary , validation-errors , validation-message

https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/BootErrors.ts
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts#L230
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Web/ErrorBoundary.cs#L48
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationSummary.cs#L76
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationMessage.cs#L74

---------------------------------------------------------------------------------------------------

The following built-in Razor components are provided by the Blazor framework:

https://learn.microsoft.com/en-us/aspnet/core/blazor/components/built-in-components?view=aspnetcore-8.0

App
AntiforgeryToken			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.antiforgerytoken?view=aspnetcore-8.0
Authentication				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.webassembly.authentication.remoteauthenticatorviewcore-1?view=aspnetcore-8.0
AuthorizeView				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.authorization.authorizeview?view=aspnetcore-8.0
CascadingValue				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.cascadingvalue-1?view=aspnetcore-8.0
DataAnnotationsValidator	https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.dataannotationsvalidator?view=aspnetcore-8.0
DynamicComponent			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.dynamiccomponent?view=aspnetcore-8.0
Editor<T>					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.editor-1?view=aspnetcore-8.0
EditForm					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.editform?view=aspnetcore-8.0
<form>
ErrorBoundary				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.errorboundary?view=aspnetcore-8.0
FocusOnNavigate				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.focusonnavigate?view=aspnetcore-8.0
HeadContent					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.headcontent?view=aspnetcore-8.0
<head>
HeadOutlet					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.headoutlet?view=aspnetcore-8.0
InputCheckbox				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputcheckbox?view=aspnetcore-8.0
<input type="checkbox">
InputDate					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputdate-1?view=aspnetcore-8.0
<input type="date">
InputFile					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputfile?view=aspnetcore-8.0
<input type="file">
InputNumber					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputnumber-1?view=aspnetcore-8.0
<input type="number">
InputRadio					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputradio-1?view=aspnetcore-8.0
<input type="radio">
InputRadioGroup				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputradiogroup-1?view=aspnetcore-8.0
<fieldset>
InputSelect					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputselect-1?view=aspnetcore-8.0
<select>
InputText					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputtext?view=aspnetcore-8.0
<input type="text">
InputTextArea				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputtextarea?view=aspnetcore-8.0
<textarea>
LayoutView					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.layoutview?view=aspnetcore-8.0
MainLayout
NavLink						https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.navlink?view=aspnetcore-8.0
<a>
NavMenu
<nav>
PageTitle					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.pagetitle?view=aspnetcore-8.0
<title>
QuickGrid					https://learn.microsoft.com/en-us/aspnet/core/blazor/components/quickgrid?view=aspnetcore-8.0
Router						https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.router?view=aspnetcore-8.0
RouteView					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routeview?view=aspnetcore-8.0
SectionContent				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.sections.sectioncontent?view=aspnetcore-8.0
SectionOutlet				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.sections.sectionoutlet?view=aspnetcore-8.0
ValidationSummary			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.validationsummary?view=aspnetcore-8.0
Virtualize					https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.virtualization.virtualize-1?view=aspnetcore-8.0

---------------------------------------------------------------------------------------------------

Snap: Preinstalled on Ubuntu and derivatives, available for other distros but not preinstalled.
    https://snapcraft.io/docs/dotnet-apps
    https://snapcraft.io/docs/dotnet-plugin

    https://snapcraft.io/docs/gnome-extension
    extensions: [gnome]

    https://snapcraft.io/docs/snapcraft-yaml-schema

    sudo snap install snapcraft --classic

    By default, Snapcraft relies on a build provider to create an isolated build environment inside which applications can be built and packaged as snaps without changing the host system.

    sudo snap install lxd

    sudo lxd init --auto

    sudo usermod -aG lxd $USER

    newgrp lxd

    To build snapcraft.yaml run:
        snapcraft pack --debug

        sudo snap install openhabittracker_1.1.5_amd64.snap --dangerous --devmode

        snap list

        snap run openhabittracker

        snapcraft login

        snapcraft upload --release=stable openhabittracker_1.1.5_amd64.snap

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

git rev-parse 1.1.5

git ls-remote https://github.com/Jinjinov/OpenHabitTracker.git refs/tags/1.1.5

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

    docker tag openhabittracker jinjinov/openhabittracker:1.1.6
    docker push jinjinov/openhabittracker:1.1.6

    docker tag openhabittracker jinjinov/openhabittracker:latest
    docker push jinjinov/openhabittracker:latest

    https://hub.docker.com/repository/docker/jinjinov/openhabittracker

    https://hub.docker.com/r/jinjinov/openhabittracker

GitHub Container Registry:

    GitHub -> Settings -> Developer settings -> Personal access tokens -> Docker Hub/GHCR Access Token

    echo <GitHubToken> | docker login ghcr.io -u Jinjinov --password-stdin

    docker tag openhabittracker ghcr.io/jinjinov/openhabittracker:1.1.6
    docker push ghcr.io/jinjinov/openhabittracker:1.1.6

    docker tag openhabittracker ghcr.io/jinjinov/openhabittracker:latest
    docker push ghcr.io/jinjinov/openhabittracker:latest

    https://github.com/users/Jinjinov/packages/container/package/openhabittracker

    https://github.com/Jinjinov/OpenHabitTracker/pkgs/container/openhabittracker

---------------------------------------------------------------------------------------------------

Disable connection pooling:
Add Pooling=False to your connection string to ensure connections are fully closed and disposed between operations.
Disabling pooling (i.e. setting Pooling=False) forces EF Core to create a new physical SQLite connection every time a DbContext is instantiated rather than reusing one from the pool.
This means that when a new connection is opened, it starts a new transaction and takes a fresh snapshot of the database—so it will immediately see all changes committed by other contexts.
It can also help release locks faster since the connection is truly closed when the context is disposed.

"Data Source=mydb.db;Cache=Shared"
"Cache=Shared" only affects how SQLite caches pages in memory—it allows multiple connections to share a common page cache—
but it does not change the fact that in WAL mode each connection’s transaction sees a snapshot of the database from when its transaction began.

SaveChanges(); // force write from .db-wal to .db with:
context.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");

Disable WAL mode: Switch to the default DELETE journal mode (e.g. by adding ?journal_mode=delete to your connection string or executing PRAGMA journal_mode=DELETE once).
options.UseSqlite("Data Source=mydb.db;Mode=ReadWriteCreate;Journal Mode=Delete");
// disable wal:
context.Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");

---------------------------------------------------------------------------------------------------

CRF (Constant Rate Factor) controls quality vs file size.

mp4 - H.264 CRF scale is 0–51:

CRF	    Quality	                        File size
0	    Lossless	                    Very large
15–18	Very high (visually lossless)	Large
23	    Good (default)	                Medium
28–33	Acceptable	                    Small
51	    Worst	                        Very small

mkv - VP9 CRF scale is 0–63:

CRF	    Quality	        File size
0	    Lossless	    Very large
15–20	Very high	    Large
33	    Good (default)	Medium
40–50	Acceptable	    Small
63	    Worst	        Very small

---------------------------------------------------------------------------------------------------
