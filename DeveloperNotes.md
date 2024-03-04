# Notes:

text: ¡!
background shape: Circle
font: Miltonian
Regular 400 Normal
font size: 110
font color: #EEEEEE
background color: #333333

---------------------------------------------------------------------------------------------------

Screenshots dimensions should be: 1280x800 1440x900 2560x1600 2880x1800
https://stackoverflow.com/questions/67972372/why-are-window-height-and-window-width-not-exact-c-wpf
a difference of 7px in Height and 14px in Width, header 31px

---------------------------------------------------------------------------------------------------

Component instantiation
When Blazor creates instances of your components, it invokes their constructors, as well as constructors for any DI services being supplied to them via @inject or the [Inject] attribute. 
If any of these constructors throws an exception, or if the setters for [Inject] properties throw exceptions, this is fatal to the circuit because it's impossible for the framework to carry out the intentions of the developer.

Lifecycle methods
During the lifetime of components, Blazor invokes lifecycle methods on components such as OnInitialized, OnParametersSet, ShouldRender, OnAfterRender, and the ...Async versions of these. 
If any of these lifecycle methods throws an exception, synchronously or asynchronously, this is fatal to the circuit because the framework no longer knows whether or how to render that component.

---------------------------------------------------------------------------------------------------

https://github.com/dotnet/sdk/issues/13395 - EmbeddedResource with two dots in Filename not working

https://github.com/dotnet/roslyn/issues/43820 - Embedded Resources with multiple dots in name does not get embedded

Visual Studio seems to recognise files named in the format <name>.<locale>.<ext> as being specific to a locale, resulting in the creation of a satellite assembly
Ididit.dll
de\Ididit.resources.dll
en\Ididit.resources.dll
es\Ididit.resources.dll
sl\Ididit.resources.dll

in the `es\Ididit.resources.dll` the file `MyEmbeddedResource.es-ES.json` becomes `MyEmbeddedResource.json`

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

https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-8.0#enhanced-navigation-and-form-handling
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

---

System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

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
            await UpdateScreenWidth();
        }
    }

    async Task UpdateScreenWidth()
    {
        screenWidth = await JsRuntime.InvokeAsync<int>("getScreenWidth");
        StateHasChanged(); // Ensure UI updates after screen width change
    }

    [JSInvokable]
    public async Task UpdateScreenWidth(int screenWidth)
    {
        this.screenWidth = screenWidth;
        StateHasChanged(); // Ensure UI updates after screen width change
    }
}

---------------------------------------------------------------------------------------------------

The path that works is within the App Sandbox:
	~/Library/Containers/app-bundle-id/Data/
	/Users/ddarby/Library/Containers/com.cerescape.Accountable/Data/Documents/

System.Environment.SpecialFolder.ApplicationData
System.Environment.SpecialFolder.LocalApplicationData

Microsoft.Maui.Storage.FileSystem.Current.CacheDirectory
Microsoft.Maui.Storage.FileSystem.Current.AppDataDirectory

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
