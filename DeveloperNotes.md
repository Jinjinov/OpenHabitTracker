# Notes:

desktop: https://youtu.be/qsC7lX3yZ-A
sidebar: https://youtu.be/dq1OmpjBBNk
mobile:  https://youtu.be/zYAg-PXe7FI https://youtube.com/shorts/zYAg-PXe7FI

---------------------------------------------------------------------------------------------------

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

Linux:

Photino.Native.so
sudo apt-get install libwebkit2gtk-4.1

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

Never use Playwright's built-in video recording - it records VP8 and the quality is not acceptable.

---------------------------------------------------------------------------------------------------

Architecture: Identity Map + Repository (what the ideal design should be)
    Goal: single-user, all-in-memory store for a Blazor app where all data fits in memory.
    The right pattern is Identity Map + Repository:

    One class (AppStore / Repository) that:
    - holds all dictionaries — the identity map: one canonical live object per entity, keyed by id
    - is the ONLY class that holds a reference to IDataAccess — private, never exposed
    - every method that touches data does two things atomically: call DataAccess AND update the dict
    - nested collections (habit.TimesDone, task.Items, category.Notes/Tasks/Habits) are wired
      at load time by filtering the already-loaded flat dicts

    Lazy loading applies at the collection level AND per-instance for expensive collections:
    - collection level: `if (Times is null) load all times from DB`
    - per-instance: load habit.TimesDone only when a habit is selected (see lazy loading note below)
    - once loaded, objects are the canonical instances stored in the dict

    NOTE on lazy loading — kept intentionally for Times:
    In the predecessor app (Ididit), all TimesDone were loaded upfront. After 5 years of daily use
    with ~50 habits done 1-2× per day, the Times table grew to ~180,000 records and the app became
    slow. OpenHabitTracker introduced per-habit lazy loading of TimesDone specifically to solve this.
    Items are small (handful of checklist entries per habit/task) and not a concern.
    The app runs on multiple backends: IndexedDB (Blazor WASM) is slower than SQLite (MAUI, Desktop,
    Server) — lazy loading Times is most important on WASM but the code path is shared across all.

    Services:
    - take only the store as dependency
    - contain only business logic (selection state, UI-triggered operations)
    - never call IDataAccess directly

    Modern analogy: client-side state store (Redux/Zustand/MobX)
    - ClientState/ClientData = the store (single source of truth)
    - services = action handlers
    - components = readers
    - the Redux rule applies: only the store mutates state — services reaching for DataAccess
      directly is the same anti-pattern as mutating Redux state outside a reducer

    Current implementation vs ideal:
    - identity map dicts                          CORRECT  (ClientData)
    - per-DataLocation isolation                  CORRECT  (_clientDataByLocation)
    - bulk lazy load with null guards             CORRECT  (if (X is null) pattern)
    - wire sub-collections from flat dicts        CORRECT  (ClientData.GetHabits(), ClientState.LoadNotes/LoadTasks/LoadHabits)
    - CRUD Add operations update dicts            CORRECT
    - per-instance loads register into dicts      CORRECT  (LoadTimesDone, Initialize, Start, AddTimeDone, AddItem, RemoveTimeDone, DeleteItem)
    - CategoryModel sub-lists wired at runtime    CORRECT  (LoadNotes/LoadTasks/LoadHabits + Add mutations + ChangeCategory)
    - DataAccess private to store                 MISSING  (exposed as public property, services use it directly)

public DataAccess:

    DataAccess is public on ClientState
        Services call _clientState.DataAccess directly for mutations (Add, Update, Remove)
        This is an enforcement problem — the invariant cannot be violated if DataAccess is private.

    consider making DataAccess private
        DataAccess is currently public on ClientState so services can reach it directly
        long-term: make it private, add explicit ClientState methods for every operation
        services call ClientState methods → ClientState calls DataAccess + updates dict
        this enforces the invariant at compile time, not by convention

---------------------------------------------------------------------------------------------------

Bug: NoteComponent Close button requires two clicks after editing content with a line added/removed, then scrolling down to the Close button.

Conditions for the bug to trigger:
- Second edit of the same note (first edit always works)
- A line is added or removed in the textarea (row count changes)
- The user scrolls down inside the `.child-column` scroll container to reach the Close button
- First click does nothing, second click closes

Root cause:

The `calculateAutoHeight` JS function (called via `setCalculateAutoHeight` in `OnAfterRenderAsync`) sets `e.target.style.height = 'auto'` before setting the final height. This temporary collapse causes the browser to reflow the layout and scroll the `.child-column` container upward to keep the focused textarea in view. This scroll happens silently within a single frame — no visible flicker — but it occurs between `pointerdown` and `click`. By the time `click` fires, the Close button has scrolled out from under the cursor and the click lands on the note list behind it.

`window.scrollY` is always 0 because the scroll container is the inner `.child-column` div, not the window.

The intermediate `height = 'auto'` collapse is the standard technique for auto-shrinking textareas (without it, height only grows). The bug appears only on the second edit because the first edit also fires `setCalculateAutoHeight`, but the user hasn't scrolled yet so the scroll reset is a no-op.

Fix (workaround): in `calculateAutoHeight`, save the `.child-column` ancestor's `scrollTop` before setting `height = 'auto'` and restore it immediately after setting the final height. The scroll container stays exactly where the user left it, so `click` fires on the button.

Fix (proper): replace `rows="@(Note.Content.Count(c => c == '\n') + 1)"` and `SetCalculateAutoHeight` entirely with a single CSS property on the textarea: `field-sizing:content`. The browser handles auto-height natively — no JS, no reflow, no scroll side effects. Supported since Chrome 123, Firefox 128, Safari 18 (all mid-2024).

---------------------------------------------------------------------------------------------------

VS 2022 error: "The project doesn't know how to run the profile with name 'Windows Machine' and command 'MsixPackage'."

Longstanding, unresolved, years-old VS bug — reported repeatedly against dotnet/maui since 2022
(issues #2778, #3725, #4797, #14862, #22167). Every time, the MAUI team closes it saying it's a
Visual Studio bug, not theirs, and redirects to the VS Feedback tool. No official root cause has
ever been published. If it happens after a VS update with zero project changes, this is likely it.

Things that do NOT fix it (all tried, all ruled out on this machine):
- `dotnet workload update` / `dotnet workload repair`
- confirming Windows 10 SDK (10.0.19041.0) is installed
- deleting bin/obj and rebuilding
- adding/changing `<WindowsPackageType>MSIX</WindowsPackageType>` in the csproj
- Configuration Manager Deploy checkbox / "Create a Windows MSIX package" project property
- restarting the machine alone
- Windows Store Apps troubleshooter
- Controlled Folder Access (check: `Get-MpPreference | Select EnableControlledFolderAccess`)
- Windows Defender real-time protection (`Set-MpPreference -DisableRealtimeMonitoring $true`)
- stopping AppXSvc / ClipSVC services

What actually fixed it here: running `chkdsk C: /f` (offline, requires restart). A prior
`chkdsk C: /scan` (online, no restart needed) had reported "Windows has found problems that must
be fixed offline" — real NTFS corruption on the drive. After `chkdsk /f` ran at boot and repaired
it, VS's packaged Windows launch started working again with no other change. NOTE: this was never
100% proven as the exact mechanism (see the WindowsApps\Deleted tangent below, which was real but
turned out NOT to be the actual fix — VS worked fine even with an orphaned package folder still
stuck in that path). If this recurs, try `chkdsk C: /scan` first; if it reports offline problems,
run `chkdsk C: /f` and restart before trying anything else below.

Diagnostic technique for this class of bug (VS shows only a generic dialog, Build Output is
empty because the failure happens in VS's launch-profile resolution, before/without MSBuild):
reproduce the same deployment step directly via CLI to get the real underlying error:

    powershell.exe -NoProfile -Command "Import-Module Appx -UseWindowsPowerShell; Add-AppxPackage -Register '<path>\bin\Debug\net9.0-windows10.0.19041.0\win10-x64\AppxManifest.xml' -ForceApplicationShutdown"

(`Add-AppxPackage` is a Windows PowerShell-only cmdlet — in PowerShell 7/pwsh it needs
`Import-Module Appx -UseWindowsPowerShell` first, or just use `powershell.exe` directly.)

If that fails, get the detailed AppX deployment event log for the failure's ActivityId (printed
in the error output):

    Get-AppPackageLog -ActivityID <guid-from-error-output> | Format-List *

Tangent (real bug, found via the above, but NOT the actual fix for the VS error above — kept
here because it's a genuine, reproducible issue and the diagnostic technique is reusable):
`Add-AppxPackage -Register` can fail with `0x80070003` because the AppX Deployment Service
opportunistically tries to garbage-collect ALL orphaned entries under
`C:\Program Files\WindowsApps\Deleted\` as part of ANY package registration — completely
unrelated packages included (e.g. an old Windows Terminal auto-update leftover blocked
registering this app). Log shows: "The last successful state reached was Indexed. Failure
occurred before reaching the next state Resolved. hr: 0x80070003" plus
"error 0x12C: Deleting file ... failed" (0x12C / 300 decimal = ERROR_OPLOCK_NOT_GRANTED,
"the oplock request is denied") for files inside that stuck folder.
If this happens, check for orphaned folders: `Test-Path "C:\Program Files\WindowsApps\Deleted\<PackageFullName>"`.
Nothing normal clears a stuck one — tried and failed, in order: take ownership (`takeown /f ... /r /d y`),
grant Administrators Full Control (`icacls ... /grant Administrators:F /t`), clear read-only/system/hidden
attributes (`attrib -r -s -h ... /s /d`), delete as SYSTEM via PsExec
(`psexec -s -i cmd.exe` then `rmdir /s /q`), Controlled Folder Access off, Defender real-time off.
`handle64.exe` (Sysinternals) found no process holding the file open even when run elevated —
`net helpmsg 300` = "The oplock request is denied" proves it IS genuinely locked by something,
just not by anything visible as a normal process handle (kernel filter driver / AppXSvc's own
internal state, not caught by the AntiVirusProduct WMI check either). The one thing that DID
clear it: Sysinternals `movefile.exe`, which schedules a delete via
`PendingFileRenameOperations` to run at the next boot, before any services start:

    Invoke-WebRequest -Uri "https://live.sysinternals.com/movefile.exe" -OutFile "$env:TEMP\movefile.exe" -UseBasicParsing
    & "$env:TEMP\movefile.exe" "<file1>" ""
    & "$env:TEMP\movefile.exe" "<file2>" ""    # repeat per file, files BEFORE the folder
    & "$env:TEMP\movefile.exe" "<the folder itself>" ""
    # then restart

Even after that specific folder was gone, `Add-AppxPackage -Register` still failed identically —
just pointing at a DIFFERENT orphaned Windows Terminal version folder next. There can be more than
one. This whole area may just be permanently messy on a machine that's had years of Windows
Terminal auto-updates, and none of it may matter for the actual VS bug (see chkdsk note above).

Useful diagnostic tools, none require installation, all official Microsoft/Sysinternals,
downloaded on demand from `https://live.sysinternals.com/<toolname>.exe`:
- `handle64.exe -accepteula -nobanner <filename>` — find which process has a file open
- `PsExec64.exe -accepteula -s -i cmd.exe` — open a shell running as NT AUTHORITY\SYSTEM
- `movefile.exe <path> ""` — schedule a file/folder for deletion on next boot (empty second arg = delete, not move)

---------------------------------------------------------------------------------------------------

Running / debugging the MAUI Windows app from the CLI, without Visual Studio:

Packaged (MSIX) apps CANNOT be run directly from CLI in one command — this is an official,
permanent Microsoft limitation, not a bug: "The published app doesn't work if you try to run it
directly with the executable file out of the publish folder. The way to run the app is to first
install it through the packaged MSIX file."
(https://learn.microsoft.com/en-us/dotnet/maui/windows/deployment/publish-cli)
`dotnet run` / `dotnet build -t:Run` on a packaged project launches the raw loose .exe with no
package identity, which throws immediately:
`System.Runtime.InteropServices.COMException (0x80040154): Class not registered (REGDB_E_CLASSNOTREG)`
at `Microsoft.Windows.ApplicationModel.WindowsAppRuntime.DeploymentManagerCS.AutoInitialize` —
this is expected/by design for packaged apps run unpackaged, not a sign anything is broken.

Two real CLI options:

1. Packaged, two steps, no csproj changes needed:

    dotnet publish OpenHabitTracker.Blazor.Maui.csproj -f net9.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64

   then install the resulting .msix from `bin\Release\...\AppPackages\...\` (double-click it, or
   `Add-AppxPackage -Path <file>.msix`, which requires the package to be signed — the temp/debug
   signing VS does automatically isn't replicated by plain `dotnet publish` from the CLI).

2. Unpackaged, ONE command, works immediately, no install step, no csproj changes (property
   override only — does not persist, does not touch the csproj file):

    dotnet run --project OpenHabitTracker.Blazor.Maui.csproj -f net9.0-windows10.0.19041.0 -p:WindowsPackageType=None --no-launch-profile

   Both flags are required:
   - `-p:WindowsPackageType=None` — overrides packaged mode for just this invocation.
   - `--no-launch-profile` — skips `Properties/launchSettings.json` entirely, because its
     `"Windows Machine"` profile has `"commandName": "MsixPackage"`, which is Visual-Studio-only
     and `dotnet run` doesn't understand it: "The launch profile "(Default)" could not be applied.
     A usable launch profile could not be located." Without this flag, option 2 fails even though
     the actual unpackaged run would otherwise work fine.

   To make this permanent (not just a one-off CLI override), see the "Convert a packaged .NET MAUI
   Windows app to unpackaged" steps in https://learn.microsoft.com/en-us/dotnet/maui/windows/setup
   — sets `<WindowsPackageType>None</WindowsPackageType>` in the csproj and changes
   `launchSettings.json` commandName from `MsixPackage` to `Project`. Tradeoff: loses
   Store-submission packaging and any package-identity-gated WinRT APIs.

For actual breakpoint debugging (not just running) without full Visual Studio: VS Code + the
official ".NET MAUI" extension (installs C# Dev Kit) supports F5 debugging on Windows/Android/iOS/
macOS — it automates the same install-then-attach dance VS does internally, so it needs the same
packaged-app install step under the hood; there's no way around that requirement for a packaged
app, only automating it away. For unpackaged mode, any debugger can just attach to the process
started by `dotnet run` directly (e.g. VS Code Run → Attach to Process).

Since MAUI 9, brand NEW projects (scaffolded by the template) default to unpackaged
(`WindowsPackageType` unset resolves differently for new-project templates than for existing/
upgraded ones — an EXISTING project that was already packaged before upgrading to net9.0-windows
stays packaged; nothing flips silently on its own just because the TargetFramework changed).
Confirmed via a dotnet/maui maintainer (mattleibow, in a related GitHub issue): "I don't think the
internal defaults have changed in the windows app sdk, so you have to set it. We just switched the
default in maui templates." — i.e. this is a template-generation-time default, not a build-time
default, so it never silently affects this project.

---------------------------------------------------------------------------------------------------

Line endings - the repo is LF everywhere:

The git blobs were always LF; the CRLF working trees on Windows came from Git for Windows'
system-level core.autocrlf=true, an installer default nobody chose.
The fix has three parts:
- .gitattributes with `* text=auto eol=lf` forces LF checkout on all machines and overrides
  autocrlf (binaries are auto-detected and untouched).
- .editorconfig already had end_of_line = lf, so editors write LF for new lines -
  before the fix, git and the editors fought each other (that is where mixed-ending files came from).
- `git config --global core.autocrlf false` stops the conversion in other repos on the machine
  (global config beats system config, no admin needed).

Why LF matters: bash scripts genuinely fail with CRLF -
the shebang becomes `#!/bin/bash<CR>` ("bad interpreter") and every line's trailing CR
produces "command not found" errors.
Windows tooling has handled LF for years: VS 2022, VS Code, even Notepad since 2018.
PowerShell .ps1 files are fine with LF
(only Authenticode-signed scripts care about exact bytes; this repo does not sign).
Visual Studio opens an LF .sln without complaint.

Exception to know about: .bat and .cmd files DO want CRLF -
cmd.exe can skip goto labels and misparse multi-line commands in LF-only batch files.
This repo has none; if one is ever added, give it a `*.bat text eol=crlf` line in .gitattributes.

Recipe to refresh a working tree after changing .gitattributes (clean tree required):
`git rm --cached -r . && git reset --hard` -
only line endings change when the blobs are already LF.

---------------------------------------------------------------------------------------------------

Guided tour (GTour) crash in Blazor Server / Docker only - diagnosed July 8, 2026, NOT yet fixed:

Clicking a Help button (any of the 13 `StartTour` methods: Main.razor and 12 pages/components,
each `await GTourService.StartTour(tourId)`) throws in the Docker (Blazor Server) build:
`InvalidOperationException: The current thread is not associated with the Dispatcher.`
at `ComponentBase.StateHasChanged` -> `GTourComponent.set_IsActive` -> `CancelTour` -> ... ->
`Main.StartTour` <- `CallStateHasChangedOnAsyncCompletion`.

Root cause: the GTour NuGet library calls `StateHasChanged()` directly (no `InvokeAsync`).
Blazor Server asserts `StateHasChanged` runs on the render Dispatcher and throws off it.
WASM has no dispatcher enforcement, so the PWA works. It is a timing race: the tour start
arrives as an async continuation, and whether it resumes on the Dispatcher depends on whether
the awaited work completed synchronously - local VS Debug (fast, in-process) stays on the
Dispatcher, Docker (Release, real SignalR/JS-interop latency) resumes on a thread-pool thread.
Same binary, different scheduling - "works in VS, fails in the container".

Fix (not applied): wrap the call in all 13 `StartTour` methods -
`await InvokeAsync(() => GTourService.StartTour(tourId));` - which marshals GTour's StateHasChanged
onto the Dispatcher. No-op cost on WASM/MAUI. Fixing only Main.razor moves the crash to the next
page's Help button, so all 13 need it. Deferred to its own session.

---------------------------------------------------------------------------------------------------

Release-note and store-listing text conventions (settled July 8, 2026):

Every release-note / listing bullet is sentence case (capitalize the first word) - the English
store convention (Microsoft, Google and AP style guides all capitalize list items, even fragments).
Mid-sentence proper nouns keep their case ("macOS, iOS"). The bullet CHARACTER differs by medium
but the case does not:

- VersionHistory.md: `- ` (standard Markdown), capitalized. This is the single source.
- OpenHabitTracker.Web/index.html: `<li>` (renders the browser's disc `*`), capitalized.
- fastlane store descriptions + the Play changelog (changelogs/*.txt): literal `*` bullet, capitalized.
- metainfo.xml: AppStream markup, `<p>` paragraphs (no bullet character), capitalized.

bump-version.ps1 copies the VersionHistory.md items VERBATIM into index.html, metainfo.xml and the
Play changelog (no case transform), so keep VersionHistory.md capitalized and the rest follows.

AppStream metainfo.xml rules (freedesktop spec + Flathub quality guidelines): `<description>`
(app and per-`<release>`) allows only `<p>`, `<ul>`, `<ol>`, `<li>`, and inline `<em>`/`<code>` -
never a literal bullet character; lists are `<ul><li>`, the software center draws the bullet.
Flathub prefers a few `<p>` paragraphs or a short list for release notes over long bullet lists,
~2-3 sentences. All 12 release entries use `<p>` (1.1.0 was the lone `<ul><li>` outlier, converted).

---------------------------------------------------------------------------------------------------

Web asset hygiene - the shared wwwroot ships files no runtime loads:

The OpenHabitTracker.Blazor library wwwroot is copied into every host under
`_content/OpenHabitTracker.Blazor/`, so trimming it shrinks every platform at once:
the Android APK, the iOS/macOS packages, the Windows MSIX, the Photino/WPF/WinForms bundles,
and the Wasm/PWA download.

Categories that ship but are never loaded at runtime in a packaged or WebView host:
- Source maps (`.map`): fetched only when browser devtools are open, never in a packaged app.
- Precompressed copies (`.br`/`.gz`): only an HTTP server doing content-encoding negotiation
  serves these; a WebView reads files off disk and never selects them.
- Non-minified `bootstrap.css` per theme: the theme switcher only ever loads `.min.css`
  (`jsInterop.js` SetTheme sets the `#theme-link` href to
  `_content/OpenHabitTracker.Blazor/bootstrap/${theme}/bootstrap.min.css`).
- The `bootstrap/js/` folder: no host loads it locally; the Web site index.html uses the jsDelivr CDN.
- Legacy font formats (`.eot`, `.ttf`, `.woff`, and SVG fonts): every target engine reads `.woff2`
  (iOS 15+, Android 7+ WebView, WebView2, WebKit2GTK).

The `.br`/`.gz` copies are the one case that needs care.
The browser and server hosts (Wasm/PWA, and Blazor Server in Docker) serve them over HTTP,
so they must keep compression.
The WebView hosts (MAUI, Photino, WPF, WinForms) read assets from disk and never serve them,
so the copies are dead weight there.
They originate in the shared library's own build (`CompressionEnabled` defaults to true for net6+),
so a consumer copies them in even with its own compression disabled -
setting `CompressionEnabled=false` only in a host project does nothing on its own.
Set `<CompressionEnabled>false</CompressionEnabled>` in the shared library and in each WebView host
(MAUI, Photino, WPF, WinForms): the library then ships no compressed copies, and the WebView hosts
do not re-create them.
The Wasm/PWA and Blazor Server hosts keep compression because each re-compresses the resolved
static assets at its own publish.
This is verified: with the shared library's compression off, the Wasm publish still regenerates
the `.br`/`.gz` (292 files), so the PWA and the web server are unaffected.

Gotcha: `CompressionEnabled=false` is honored, but an incremental build does not delete a compressed
copy that already exists in a project's `bin`/`obj`, and the static web asset manifest keeps pointing
at it - so the packaged output looks unchanged (a byte-identical APK, which is how this was first missed).
Delete `bin`/`obj` and rebuild to flush stale compressed copies before measuring.

The file deletions (maps, non-min css, unused js, legacy fonts) are safe on every host,
because a deleted map or non-min css was dead on the web server too.

When removing the legacy font files, the `@font-face` blocks in `app.css` must list only `woff2`.
Otherwise a Chromium WebView selects the first supported format in the list
(`woff` before `woff2`) and then 404s on the deleted file.

---------------------------------------------------------------------------------------------------

Android APK size - composition and the arm64-only direct APK:

The default .NET Android Release build produces a universal APK with two ABIs,
`arm64-v8a` and `x86_64` (the 32-bit `armeabi-v7a` and `x86` are already excluded).
Native code for both ABIs dominates the APK, and profiled AOT (`libaot-*.so`, on by default
for Release) sits on top of the IL assembly blob.
`x86_64` is emulator-only weight - every real Android device is `arm64-v8a`.

An arm64-only APK is roughly half the size (about 52 MB to about 26 MB as of 1.2.3).
The two outputs come from two publishes distinguished by `AndroidPackageFormat`:
the default (`aab`) keeps both ABIs, and `-p:AndroidPackageFormat=apk` restricts to `android-arm64`
via a csproj condition (commands in Release.md and Automation/Release.md).
The Google Play AAB keeps both ABIs (Play splits per device, so arm64 users still download an
arm64-sized artifact and x86_64 / ChromeOS coverage is preserved), and the website keeps the
universal APK. Only the GitHub release APK is arm64-only.

---------------------------------------------------------------------------------------------------

iOS simulator forensics (MAUI app, bundle id net.openhabittracker, app name OpenHT):

- Data container: `xcrun simctl get_app_container <udid> net.openhabittracker data` -
  SQLite DB at `Library/OpenHT.db`, Preferences at `Library/Preferences/net.openhabittracker.plist`.
  The container path changes on every reinstall (each `dotnet build -t:Run` redeploy),
  not on relaunch - re-resolve it before reading.
- `xcrun simctl spawn <udid> defaults read net.openhabittracker` says "domain does not exist"
  even when the plist exists, because it cannot see the app sandbox.
  Read the plist file directly with `plutil -p`.
- Editing the plist from outside: terminate the app and kill the simulator runtime's cfprefsd first,
  or its in-memory cache clobbers the edit on the next app write.
  The sim's cfprefsd binary path contains "simruntime", not the device UDID - match accordingly.
  Cleanest counter reset: `xcrun simctl uninstall`.
- Proving whether code executed:
  `xcrun simctl spawn <udid> log stream --level debug --predicate 'processImagePath CONTAINS "OpenHT"'`
  logs every NSUserDefaults read as "found no value for key X" -
  the absence of an expected key read proves a code path never ran.
- Verifying which code is deployed: the main assembly in the bundle is `OpenHT.dll`
  (csproj AssemblyName), not OpenHabitTracker.Blazor.Maui.dll.
  `strings OpenHT.dll | grep <MethodName>` shows type and member names (UTF-8 heap)
  but not C# string literals (UTF-16, invisible to plain `strings`).
- macCatalyst: the container at `~/Library/Containers/net.openhabittracker/` is TCC-protected
  from direct reads, but `defaults read net.openhabittracker` on the host Mac works -
  it goes through cfprefsd, which handles the sandbox redirect.

---------------------------------------------------------------------------------------------------
