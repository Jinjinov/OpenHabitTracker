# TODO:

!!! write unit tests: https://bunit.dev/ https://github.com/bUnit-dev/bUnit

https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/uitest-appium-nunit/
https://github.com/dotnet/maui-samples/tree/main/8.0/UITesting/BasicAppiumNunitSample

https://devblogs.microsoft.com/dotnet/dotnet-maui-ui-testing-appium/
https://github.com/jfversluis/Template.Maui.UITesting

find out why `padding-left: 12px !important;` is needed on iOS - try: `padding-left: env(safe-area-inset-left) !important;`

!!! Multiple form field elements in the same form have the same id attribute value.
!!! This might prevent the browser from correctly autofilling the form.
!!! To fix this issue, use unique id attribute values for each form field.

fix AppData GetUserData() - remove these from class AppData:
	public Dictionary<long, TimeModel>? Times { get; set; }
	public Dictionary<long, ItemModel>? Items { get; set; }

---------------------------------------------------------------------------------------------------

!!!
!!! close menu on navigation !!!

-The modules' title bars look and act too similar to fields which confuses the experience to me - I think that's the one simple biggest UI fix I'd make. 
Make it a solid color and not part of the "add new" logic. 
https://imgur.com/a/kYlVFMq

!!! - Cancel besides every Save !!!
!!!
In the upper right, some modules you have an X to close the module, in some you have a trash can which deletes. 
I found myself clicking on the trash just wanting to close or cancel. 
Have clear consistency with the save, close and trash icons.

-I feel like the modules should be collapsible? 
I don't quite understand what the box that contains the up/straight line/down arrow/no symbol is for (prioritizing and sorting I'm assuming) but I keep clicking on that thinking it will collapse the module.

-I'm a big fan of animations, and the simple ones are fairly easy with Blazor and CSS - even small or fast animations can make things feel much more intuitive

---------------------------------------------------------------------------------------------------

1. !!!

Snap: Preinstalled on Ubuntu and derivatives, available for other distros but not preinstalled.
	https://snapcraft.io/docs/dotnet-apps
	https://snapcraft.io/docs/dotnet-plugin

	https://snapcraft.io/docs/registering-your-app-name
	https://snapcraft.io/account
	https://snapcraft.io/docs/pre-built-apps

	https://snapcraft.io/snaps
	https://dashboard.snapcraft.io/register-snap/
	https://dashboard.snapcraft.io/register-snap-feedback/openhabittracker/

Flatpak: Preinstalled on Fedora, available for other distros but not preinstalled.
	https://github.com/flathub/org.freedesktop.Sdk.Extension.dotnet8
	https://docs.flatpak.org/en/latest/dotnet.html
	https://flatpak.org/setup/Ubuntu
	https://github.com/flatpak/flatpak-builder-tools
	https://github.com/flatpak/flatpak-builder-tools/tree/master/dotnet

	flatpak-builder build-dir --user --install-deps-from=flathub --download-only net.openhabittracker.app.yaml

	python3 flatpak-dotnet-generator.py --dotnet 8 --freedesktop 23.08 nuget-sources.json OpenHabitTracker/OpenHabitTracker.Blazor.Photino/OpenHabitTracker.Blazor.Photino.csproj

	flatpak-builder build-dir --user --force-clean --install --repo=repo net.openhabittracker.app.yaml

	error: 'net.openhabittracker' is not a valid application name: Names must contain at least 2 periods

	flatpak run net.openhabittracker.app

	https://flathub.org/
	https://github.com/flathub/flathub

---------------------------------------------------------------------------------------------------

2. !!!

edit times done
	task: edit date and time 
	- started at = now - duration 
	- completed at = now
	habit:
	- list all for the selected day
	- edit time only 

---------------------------------------------------------------------------------------------------

!!! add `@using Microsoft.Extensions.Logging` @inject ILogger Logger !!!

---------------------------------------------------------------------------------------------------

catch unhandled exceptions:
	AppDomain.CurrentDomain.UnhandledException
	TaskScheduler.UnobservedTaskException

---------------------------------------------------------------------------------------------------

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

replace all `@inject AppData AppData` with appropriate services

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
		desktop authorisation - Ididit.Google.Apis - using Google.Apis.Auth.OAuth2;
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

task and habit:
	list all times done
	edit time done
	delete time done

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

call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
	save TotalTimeSpent
	save AverageInterval
	on Habit Initialize - load only last week (last X days, displayed in small calendar)
	call LoadTimesDone for large calendar

benchmark method time & render time

read Settings from DB before Run() - !!! Transient / Scoped / Singleton !!!

??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ???

common `Router`
	Ididit.Blazor - Routes.razor
	Ididit.Blazor.Wasm - App.razor - CascadingAuthenticationState, AuthorizeRouteView, NotAuthorized

Ididit.Blazor.Server:
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

Ididit.Rest for SQL server for WebView

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
