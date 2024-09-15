# TODO:

write unit tests: https://bunit.dev/ https://github.com/bUnit-dev/bUnit

https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/uitest-appium-nunit/
https://github.com/dotnet/maui-samples/tree/main/8.0/UITesting/BasicAppiumNunitSample

https://devblogs.microsoft.com/dotnet/dotnet-maui-ui-testing-appium/
https://github.com/jfversluis/Template.Maui.UITesting

---------------------------------------------------------------------------------------------------

https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#global-exception-handling
https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#error-boundaries
https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#alternative-global-exception-handling

https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#places-where-errors-may-occur
https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#component-instantiation
https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-8.0#lifecycle-methods

add `@using Microsoft.Extensions.Logging` @inject ILogger Logger

https://stackoverflow.com/questions/57539330/is-there-a-way-to-globally-catch-all-unhandled-errors-in-a-blazor-single-page-ap
https://stackoverflow.com/questions/66695516/tracking-down-unhandled-exceptions-in-blazor-webassembly
https://stackoverflow.com/questions/75534867/how-to-globally-handle-catch-exceptions-in-blazor-server-c-sharp

OpenHabitTracker.Blazor.Server:
	- @page "/Error"
	- app.UseExceptionHandler("/Error");

<ErrorBoundary>
    <ChildContent>
        <EmbeddedCounter />
    </ChildContent>
    <ErrorContent>
        <p class="errorUI">A rotten gremlin got us. Sorry!</p>
    </ErrorContent>
</ErrorBoundary>

https://stackoverflow.com/questions/70198098/catch-and-display-on-the-page-any-error-in-a-net-maui-blazor-project
https://github.com/bUnit-dev/bUnit/issues/410
https://github.com/bUnit-dev/bUnit/pull/418

To use <ErrorBoundary> the DI needs IErrorBoundaryLogger

You should check if each platform already registers IErrorBoundaryLogger on start to DI with AddSingleton<IErrorBoundaryLogger, ErrorBoundaryLogger>();

If it does not register it, write your own and register it

https://github.com/dotnet/maui/issues/4502
https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebAssembly/WebAssembly/src/Services/WebAssemblyErrorBoundaryLogger.cs

public sealed class ErrorBoundaryLogger : IErrorBoundaryLogger
{
    private readonly ILogger<ErrorBoundary> _errorBoundaryLogger;

    public ErrorBoundaryLogger(ILogger<ErrorBoundary> errorBoundaryLogger)
    {
        _errorBoundaryLogger = errorBoundaryLogger ?? throw new ArgumentNullException(nameof(errorBoundaryLogger));
    }

    public ValueTask LogErrorAsync(Exception exception)
    {
        // For client-side code, all internal state is visible to the end user.
        // We can just log directly to the console.
        _errorBoundaryLogger.LogError(exception, "ErrorBoundary");

        return ValueTask.CompletedTask;
    }
}

---------------------------------------------------------------------------------------------------

find out why `padding-left: 12px !important;` is needed on iOS - try: `padding-left: env(safe-area-inset-left) !important;`

---------------------------------------------------------------------------------------------------

fix AppData GetUserData() which calls InitializeContent()
search for `// TODO:: remove temp fix`
InitializeItems and InitializeTimes have null checks and do not update data when called in GetUserData()
	both load data directly from DB with _dataAccess.GetTimes() and _dataAccess.GetItems()
	but LoadTimesDone also loads data with _dataAccess.GetTimes() - these are not the same objects as in InitializeTimes
	and ItemService.Initialize also loads data with _dataAccess.GetItems() - these are not the same objects as in InitializeItems
	user can add or remove Items and Times list but the code does not update Items and Times in the AppData
	so without temp fix, GetUserData() would return Items and Times that were loaded with Initialize()
remove these from class AppData:
	public Dictionary<long, TimeModel>? Times { get; set; }
	public Dictionary<long, ItemModel>? Items { get; set; }
or make sure that other services update them
this is a big problem - services use _dataAccess on their own, but AppData is supposed to represent the current state - as the only source of truth
Ididit did not have this problem, `Repository` was the only class with `IDatabaseAccess` and represented the current state

---------------------------------------------------------------------------------------------------

https://v2.tauri.app/distribute/flatpak/

runtime: org.gnome.Platform
runtime-version: '46'
sdk: org.gnome.Sdk

https://v2.tauri.app/distribute/snapcraft/

layout:
  /usr/lib/$SNAPCRAFT_ARCH_TRIPLET/webkit2gtk-4.1:
    bind: $SNAP/usr/lib/$SNAPCRAFT_ARCH_TRIPLET/webkit2gtk-4.1

build-packages:
      libwebkit2gtk-4.1-dev

stage-packages:
      libwebkit2gtk-4.1-0

---------------------------------------------------------------------------------------------------

I've solved my issue:

1) I've changed my .yaml to

name: snaptest
base: core18
version: '0.1'
summary: Single-line elevator pitch for your amazing snap # 79 char long summary
description: | This is my-snap's description. You have a paragraph or two to tell the most important story about your snap. Keep it under 100 words though, we live in tweetspace and your description wants to look good in the snap store.

grade: devel # must be 'stable' to release into candidate/stable channels
confinement: devmode # use 'strict' once you have the right plugs and slots

apps:
snaptest:
command: ./ProjectA

parts:
mysnap:
plugin: dotnet
dotnet-version: 6.0
dotnet-runtime-version: '6.0.0'
source: .
source-type: local
override-build: |
dotnet build -c Release
dotnet publish -r linux-x64 -c Release -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true -o $SNAPCRAFT_PART_INSTALL
chmod 0755 $SNAPCRAFT_PART_INSTALL/ProjectA

2) I've used 'snapcraft --use-lxd' instead of 'snapcraft snap'

---------------------------------------------------------------------------------------------------

https://docs.tryphotino.io/Photino-Comparison-with-Electron
https://github.com/tryphotino/photino.Documentation/issues/18
https://github.com/tryphotino/photino.Samples/tree/debug/Photino.PublishPhotino

Snap: Preinstalled on Ubuntu and derivatives, available for other distros but not preinstalled.
	https://snapcraft.io/docs/dotnet-apps
	https://snapcraft.io/docs/dotnet-plugin

	sudo snap install snapcraft --classic

	By default, Snapcraft relies on a build provider to create an isolated build environment inside which applications can be built and packaged as snaps without changing the host system.
	sudo snap install lxd

	snapcraft --debug

	sudo snap install openhabittracker_1.0.0_amd64.snap --devmode --dangerous

	snap list

	snap run openhabittracker

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

Flatpak: Preinstalled on Fedora, available for other distros but not preinstalled.
	error: 'net.openhabittracker' is not a valid application name: Names must contain at least 2 periods

	https://github.com/flathub/org.freedesktop.Sdk.Extension.dotnet8

	https://docs.flatpak.org/en/latest/dotnet.html
	https://docs.flatpak.org/en/latest/available-runtimes.html

	https://flatpak.org/setup/Ubuntu
	https://github.com/flatpak/flatpak-builder-tools
	https://github.com/flatpak/flatpak-builder-tools/tree/master/dotnet

	https://github.com/NickvisionApps/FlatpakGenerator

	sudo apt install flatpak

	sudo apt install gnome-software-plugin-flatpak

	flatpak remote-add --if-not-exists flathub https://dl.flathub.org/repo/flathub.flatpakrepo

	sudo apt install flatpak-builder

	runtime: org.freedesktop.Platform
	runtime-version: '23.08'
	sdk: org.freedesktop.Sdk

	flatpak-builder build-dir --user --install-deps-from=flathub --download-only net.openhabittracker.app.yaml --force-clean

    <Configuration>Release</Configuration>
    <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>

	python3 flatpak-dotnet-generator.py --dotnet 8 --freedesktop 23.08 nuget-sources.json OpenHabitTracker/OpenHabitTracker.Blazor.Photino/OpenHabitTracker.Blazor.Photino.csproj

	flatpak-builder build-dir --user --force-clean --install --repo=repo net.openhabittracker.app.yaml

	flatpak run net.openhabittracker.app

	https://flathub.org/
	https://github.com/flathub/flathub

	https://github.com/flathub/org.nickvision.money/blob/master/org.nickvision.money.json
	https://github.com/flathub/org.nickvision.tubeconverter/blob/master/org.nickvision.tubeconverter.json

	https://github.com/flathub/net.jenyay.Outwiker/blob/master/net.jenyay.Outwiker.yml
	https://github.com/flathub/in.cinny.Cinny/blob/master/in.cinny.Cinny.yml

---------------------------------------------------------------------------------------------------

Auto sync to external folder (that can be part of Google Drive, OneDrive, iCloud, Dropbox)

save and load without file dialog
sync: - save last loaded filename to DB, load if there is a new file / more recent

AndroidManifest.xml

MANAGE_EXTERNAL_STORAGE

<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

using System;
using System.IO;
using System.Runtime.InteropServices;
using Android.Content.PM;
using Android.OS;
using Xamarin.Essentials;
using Android;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V4.Content;

if (ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
{
    ActivityCompat.RequestPermissions(MainActivity.Instance, new string[] { Manifest.Permission.WriteExternalStorage }, 1);
}

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
}
if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
}
if (RuntimeInformation.IsOSPlatform(OSPlatform.Android))
{
    path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "MyAppFolder");
    return Path.Combine(Android.OS.Environment.ExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath, "MyAppFolder");
}
if (RuntimeInformation.IsOSPlatform(OSPlatform.iOS))
{
    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
}

---------------------------------------------------------------------------------------------------

setup Authentication
	<!--<script src="_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js"></script>-->
	@* <CascadingAuthenticationState> *@
	@* </CascadingAuthenticationState> *@
	move LoginDisplay / @NavBarFragment.GetNavBarFragment() to Backup
	appsettings.json
	appsettings.Development.json

Backup
	Google Drive
	OneDrive
	iCloud
	Dropbox
		WASM authorisation - REST
		desktop authorisation - OpenHabitTracker.Google.Apis - using Google.Apis.Auth.OAuth2;
		mobile authorisation - `ASP.NET Core`

Blazor Server / Web
	`ASP.NET Core`
	SQL Server
	version history: https://learn.microsoft.com/en-us/ef/core/providers/sql-server/temporal-tables
	table Users
	column UserId in every other table
	EF Core: use `DbContextFactory`

Host 24/7 on Raspberry Pi

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

when all habit items are done, habit is done
when all task items are done, task is done

content background:
	list all possible colors
	whole <div>, not just Title

repeat:
	add `StartAt` / `PlannedAt` to Habit ? some starting point for repeat interval
	weekly: which day in week
	monthly: which day (or week/day - second monday) in month
	yearly: which day (date) in year

textarea Tabs
	make markdown Tabs look the same as in textarea
	insert Tabs in multiple rows

Show only habits with ratio `over` / `under`

horizontal calendar with vertical weeks

---------------------------------------------------------------------------------------------------

replace all `@inject AppData AppData` with appropriate services

call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
	save TotalTimeSpent
	save AverageInterval
	on Habit Initialize - load only last week (last X days, displayed in small calendar)
	call LoadTimesDone for large calendar

benchmark method time & render time

read Settings from DB before Run() - !!! Transient / Scoped / Singleton !!!

??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ???

common `Router`
	OpenHabitTracker.Blazor - Routes.razor
	OpenHabitTracker.Blazor.Wasm - App.razor - CascadingAuthenticationState, AuthorizeRouteView, NotAuthorized

OpenHabitTracker.Blazor.Server:
	- @page "/Error"
	- app.UseExceptionHandler("/Error");

Google Keep
	- title
	- pin
	- note
	- reminder
		- date
		- time
		- place
		- repeat
			- Does not repeat
			- Daily
			- Weekly
			- Monthly
			- Yearly
			- Custom:
				- Forever
				- Until a date
				- For a number of events
	- collaborator
	- background
	- (app) take photo
	- add image
	- archive
	- delete
	- add label
	- add drawing
	- (app) recording
	- make copy
	- show checkboxes
	- (app) send (share)
	- copy to Google Docs
	- version history
	- undo
	- redo
	- close
	- (app):
		- h1
		- h2
		- normal text
		- bold
		- italic
		- underline
		- clear (\) text (T) formatting

- filters are query parameters

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

copy Loop Habit Tracker
- History (done count grouped by week, month, quarter, year)
- Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
- Best streaks (from date - to date)
- Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

- drag & drop reorder
- keyboard navigation
- benchmark: method time & render time

OpenHabitTracker.Rest for SQL server for WebView

table Users
	User authentication
	... or ...
	Google, Microsoft, Apple login

- ASAP tasks
	- when, where, contact/company name, address, phone number, working hours, website, email
- date & time tasks

- don't use `event` to refresh everything on every change
- don't use `StateHasChanged()`
- don't do this: current screen changed -> save current screen to settings -> data changed -> refresh all

what is wrong with ididit:
	- I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them
	- show only highest priority overdue tasks

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

[X] Category must be set when creating a new note

virtualized container

method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance

---------------------------------------------------------------------------------------------------

- The modules' title bars look and act too similar to fields which confuses the experience to me - I think that's the one simple biggest UI fix I'd make. 
Make it a solid color and not part of the "add new" logic. 
https://imgur.com/a/kYlVFMq

- I feel like the modules should be collapsible? 
I don't quite understand what the box that contains the up/straight line/down arrow/no symbol is for (prioritizing and sorting I'm assuming) but I keep clicking on that thinking it will collapse the module.

- I'm a big fan of animations, and the simple ones are fairly easy with Blazor and CSS - even small or fast animations can make things feel much more intuitive