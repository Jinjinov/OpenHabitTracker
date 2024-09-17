# Notes:

text: ¡!
background shape: Circle
font: Miltonian
Regular 400 Normal
font size: 110
font color: #EEEEEE
background color: #333333

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

.NET runtime version: 8.0.8
.NET SDK version: 8.0.400
dotnet workload version: 8.0.82 for windows, 34.0.138 for Android, 17.5.8030 for iOS, macOS
NuGet package version: 8.0.8

Windows version: 10.0.19041.0
Windows supported version: 10.0.17763.0
Windows SDK version: 1.5.240607001
Android SDK version: 34 - one version of Android Studion - for multiple Android SDK versions just select and download them
iOS version: 17.5 - each version of Xcode comes with its own SDK version - for multiple SDK versions install multiple versions of Xcode (rename the .app before installing new version)
Xcode version: 15.4
macOS version: 14.4

https://github.com/dotnet/maui/wiki/Release-Versions

`dotnet --info`

`dotnet workload search`

`dotnet workload list`

Installed Workload Id      Manifest Version       Installation Source
---------------------------------------------------------------------------------
android                    34.0.138/8.0.100       SDK 8.0.400, VS 17.11.35303.130
aspire                     8.2.0/8.0.100          SDK 8.0.400, VS 17.11.35303.130
ios                        17.5.8030/8.0.100      SDK 8.0.400, VS 17.11.35303.130
maccatalyst                17.5.8030/8.0.100      SDK 8.0.400, VS 17.11.35303.130
maui-windows               8.0.82/8.0.100         SDK 8.0.400, VS 17.11.35303.130
wasm-tools                 8.0.8/8.0.100          SDK 8.0.400, VS 17.11.35303.130
wasm-tools-net6            8.0.8/8.0.100          SDK 8.0.400, VS 17.11.35303.130

`dotnet workload update`

NuGet package versions: MauiVersion == Manifest Version
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="$(MauiVersion)" />

Publish Windows:

dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c Release -f net8.0-windows10.0.19041.0 /p:SelfContained=true /p:GenerateAppxPackageOnBuild=true

dotnet publish OpenHabitTracker.Blazor.Maui.csproj -c Release -f net8.0-windows10.0.19041.0 /p:SelfContained=true /p:PublishAppxPackage=true

set msix version in Package.appxmanifest

---------------------------------------------------------------------------------------------------

Publish iOS:

run on iOS simulator:
    dotnet build -t:Run -f net8.0-ios
    dotnet build -t:Run -f net8.0-ios -p:_DeviceName=:v2:udid=YOUR_UDID
    https://learn.microsoft.com/en-us/dotnet/maui/ios/cli?view=net-maui-8.0#launch-the-app-on-a-specific-simulator

dotnet publish OpenHabitTracker.Blazor.Maui.csproj -f net8.0-ios -c Release -p:ArchiveOnBuild=true -p:RuntimeIdentifier=ios-arm64  -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.ios"

Publish macOS:

run on macOS:
    dotnet build -t:Run -f net8.0-maccatalyst

dotnet publish -f net8.0-maccatalyst -c Release -p:MtouchLink=SdkOnly -p:CreatePackage=true -p:EnableCodeSigning=true -p:EnablePackageSigning=true -p:CodesignKey="Apple Distribution: Urban Dzindzinovic (53V66WG4KU)" -p:CodesignProvision="openhabittracker.macos" -p:CodesignEntitlements="Platforms\MacCatalyst\Entitlements.plist" -p:PackageSigningKey="3rd Party Mac Developer Installer: Urban Dzindzinovic (53V66WG4KU)"

---------------------------------------------------------------------------------------------------

Linux:

Photino.Native.so
sudo apt-get install libwebkit2gtk-4.1

---------------------------------------------------------------------------------------------------

Android:

run on Android emulator:
    dotnet build -t:Run -f net8.0-android
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

dotnet ef migrations add Initial --startup-project OpenHabitTracker.Blazor.WinForms

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

using Microsoft.AspNetCore.Components.Forms; InputFile / download
using Microsoft.Maui.Storage; IFilePicker / namespace CommunityToolkit.Maui.Storage; interface IFileSaver
using Microsoft.Win32; OpenFileDialog / SaveFileDialog
using System.Windows.Forms; OpenFileDialog / SaveFileDialog

---------------------------------------------------------------------------------------------------

Blazor's enhanced navigation and form handling avoid the need for a full-page reload and preserves more of the page state
    https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-8.0#enhanced-navigation-and-form-handling

Blazor NavigationManager.NavigateTo always scrolls page to the top
    https://github.com/dotnet/aspnetcore/issues/40190#issuecomment-1324689082

---------------------------------------------------------------------------------------------------

Localization:

https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-8.0
https://github.com/xaviersolau/BlazorJsonLocalization

---------------------------------------------------------------------------------------------------

Blazor magic strings: blazor-error-ui , --blazor-load-percentage , --blazor-load-percentage-text , blazor-error-boundary , validation-errors , validation-message

https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/BootErrors.ts
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts#L230
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Web/ErrorBoundary.cs#L48
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationSummary.cs#L76
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationMessage.cs#L74

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

- Calendar
	- 7 row, one for each day of the week
	- 6 columns = one month - 4 full weeks = 28 - another 0/1/2/3 days can take max 2 weeks more
	- find the last monday of the previous month
		DateTime currentDate = DateTime.Now;
		DateTime lastDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
		DateTime lastMonday = lastDayOfMonth.AddDays((int)DayOfWeek.Monday - (int)lastDayOfMonth.DayOfWeek);
	- previous month DaysInMonth
	- this month DaysInMonth
	- next month until sunday - until max 14.

---------------------------------------------------------------------------------------------------

public class ApplicationDbContext : DbContext
{
    public DbSet<ListItem> Items { get; set; }
    private const string DatabaseName = "myItems.db";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        String databasePath;
        switch (Device.RuntimePlatform)
        {
            case Device.iOS:
                databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "..", "Library", DatabaseName);
                break;
            case Device.Android:
                databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), DatabaseName);
                break;
            default:
                throw new NotImplementedException("Platform not supported");
        }
        optionsBuilder.UseSqlite($"Filename={databasePath}"); // “Filename” and “DataSource” are aliases for “Data Source”
    }
}

SQLitePCL.Batteries_V2.Init();

[assembly: Preserve(typeof(System.Linq.Queryable), AllMembers = true)]
[assembly: Preserve(typeof(System.DateTime), AllMembers = true)]
[assembly: Preserve(typeof(System.Linq.Enumerable), AllMembers = true)]
[assembly: Preserve(typeof(System.Linq.IQueryable), AllMembers = true)]

Microsoft.Data.Sqlite.SqliteException: 'SQLite Error 14: 'unable to open database file'.'

<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />

var sqlitePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"OlsonSoftware\FinanceManager");
Directory.CreateDirectory(sqlitePath);
optionsBuilder.UseSqlite($"Data Source={sqlitePath}\fmd.db");

---------------------------------------------------------------------------------------------------

https://github.com/EdCharbeneau/BlazorSize/wiki

// wwwroot/js/interop.js
window.getScreenWidth = function () {
    return window.innerWidth;
};

window.addEventListener("resize", function() {
    var screenWidth = window.innerWidth;
    DotNet.invokeMethodAsync('YourAssemblyName', 'UpdateScreenWidth', screenWidth);
});

@code {
    int screenWidth;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            screenWidth = await JsRuntime.InvokeAsync<int>("getScreenWidth");
        }
    }

    [JSInvokable]
    public async Task UpdateScreenWidth(int screenWidth)
    {
        this.screenWidth = screenWidth;
        StateHasChanged();
    }
}

---------------------------------------------------------------------------------------------------

The path that works is within the App Sandbox:
	~/Library/Containers/app-bundle-id/Data/
	/Users/ddarby/Library/Containers/com.cerescape.Accountable/Data/Documents/

System.Environment.SpecialFolder.ApplicationData
System.Environment.SpecialFolder.LocalApplicationData

Microsoft.Maui.Storage.FileSystem.Current.CacheDirectory
"C:\\Users\\Urban\\AppData\\Local\\Packages\\ididit.blazor.maui_9zz4h110yvjzm\\LocalCache"

Microsoft.Maui.Storage.FileSystem.Current.AppDataDirectory
"C:\\Users\\Urban\\AppData\\Local\\Packages\\ididit.blazor.maui_9zz4h110yvjzm\\LocalState"

public string CacheDirectory
	=> PlatformCacheDirectory;

public string AppDataDirectory
	=> PlatformAppDataDirectory;

static string CleanPath(string path) =>
	string.Join("_", path.Split(Path.GetInvalidFileNameChars()));

static string AppSpecificPath =>
	Path.Combine(CleanPath(AppInfoImplementation.PublisherName), CleanPath(AppInfo.PackageName));

string PlatformCacheDirectory
	=> AppInfoUtils.IsPackagedApp
		? ApplicationData.Current.LocalCacheFolder.Path
		: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppSpecificPath, "Cache");

string PlatformAppDataDirectory
	=> AppInfoUtils.IsPackagedApp
		? ApplicationData.Current.LocalFolder.Path
		: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSpecificPath, "Data");

---------------------------------------------------------------------------------------------------

Bug: Release version on Android shows loading screen and immediately closes - Debug version works ok

How to debug: open Logcat in Android studio, set filter to "level:ERROR", start Android emulator, start the Release version

2024-08-29 22:24:47.732 29433-29433 penhabittracker         
net.openhabittracker                 
E  * Assertion at /__w/1/s/src/mono/mono/mini/aot-runtime.c:3810, 
condition `is_ok (error)' not met, 
function:decode_patch, 
module 'OpenHT.dll.so' is unusable (GUID of dependent assembly Microsoft.AspNetCore.Components.WebView.Maui doesn't match (expected '25DD9A5A-6B30-4279-9CB3-056987FB48E7', got '7D3793C6-311B-4DDD-9CF3-6EC16FF9BC9D'))

Delete bin and obj folders from your project.
Clear the local NuGet cache on your machine.

https://devblogs.microsoft.com/visualstudio/introducing-visual-studio-rollback/

/p:RunAOTCompilation=False /p:PublishTrimmed=False

AndroidLinkMode=None is the same as setting PublishTrimmed=false

<PropertyGroup>
    <EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
    <RunAOTCompilation>False</RunAOTCompilation>
    <PublishTrimmed>False</PublishTrimmed>
</PropertyGroup>

<PropertyGroup Condition="$(TargetFramework.Contains('-android')) and $(Configuration)=='Release'">
	<AndroidLinkResources>true</AndroidLinkResources>
	<AndroidLinkMode>None</AndroidLinkMode>
	<RunAOTCompilation>false</RunAOTCompilation>
	<AndroidEnableProfiledAot>false</AndroidEnableProfiledAot>
</PropertyGroup>

<PropertyGroup Condition="$(TargetFramework.Contains('-ios')) and $(Configuration)=='Release'">
	<MtouchLink>None</MtouchLink>
</PropertyGroup>

<PropertyGroup Condition="$(TargetFramework.Contains('-android')) and '$(Configuration)' == 'Release'">
	<AndroidLinkMode>None</AndroidLinkMode>
	<RunAOTCompilation>false</RunAOTCompilation>
	<AndroidEnableProfiledAot>false</AndroidEnableProfiledAot>
</PropertyGroup>

---------------------------------------------------------------------------------------------------