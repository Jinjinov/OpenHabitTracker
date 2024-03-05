# TODO:

!!! Blazor lifecycle methods swallow exceptions !!! -> remove `protected override async Task OnInitializedAsync()`

!!! Blazor @inject swallows constructor exceptions !!! -> add `@using Microsoft.Extensions.Logging` @inject ILogger Logger

Ididit.Google.Apis
Ididit.LocalStorage
version history: https://learn.microsoft.com/en-us/ef/core/providers/sql-server/temporal-tables

??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ???

call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
	save TotalTimeSpent
	save AverageInterval
	on Habit Initialize - load only last week (last X days, displayed in small calendar)
	call LoadTimesDone for large calendar

Photino can't open web links in external browser
	https://github.com/tryphotino/photino.Blazor/issues/111
	https://github.com/tryphotino/photino.Blazor/pull/113

find out why `padding-left: 12px !important;` is needed on iOS

---------------------------------------------------------------------------------------------------

move LoginDisplay / @NavBarFragment.GetNavBarFragment() to Backup

! search & replace Maui colors:
	512BD4
	2B0B98
	appicon.svg
	appiconfg.svg
	splash.svg
	dotnet_bot.svg

! prepare for release (Microsoft Store, Google Play, Apple Store)
	app name
	app icon
	app background color
	app entitlements
	app metadata

! Website

! Register the new domain

! Rename the project & repository

---------------------------------------------------------------------------------------------------

Backup
	Google Drive
	OneDrive
	iCloud
	Dropbox
		WASM authorisation - REST
		desktop authorisation - using Google.Apis.Auth.OAuth2;
		mobile authorisation - `ASP.NET Core`

Blazor Server / Web
	`ASP.NET Core`
	SQL Server
	version history
	table Users
	column UserId in every other table
	EF Core: use `DbContextFactory`

Host 24/7 on Raspberry Pi

---------------------------------------------------------------------------------------------------

repeat:
	add `StartAt` / `PlannedAt` to Habit ? some starting point for repeat interval
	weekly: which day in week
	monthly: which day (or week/day - second monday) in month
	yearly: which day (date) in year

textarea Tabs
	make markdown Tabs look the same as in textarea
	insert Tabs in multiple rows

Show only habits with ratio `over` / `under`

when habit is done, uncheck all habit items
when task is done, check all task items
when all habit items are done, habit is done
when all task items are done, task is done

horizontal calendar with vertical weeks

---------------------------------------------------------------------------------------------------

common `Router`
	Ididit.Blazor - Routes.razor
	Ididit.Blazor.Wasm - App.razor - CascadingAuthenticationState, AuthorizeRouteView, NotAuthorized

benchmark method time & render time

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

- Ididit.Blazor.Server:
	- @page "/Error"
	- app.UseExceptionHandler("/Error");

- read Settings from DB before Run()

- filters are query parameters

---------------------------------------------------------------------------------------------------

- online sync - server with SQL
- calendar streak
- drag & drop reorder
- keyboard navigation
- benchmark: method time & render time
- Google Keep - copy all features
- don't list habits that i always do / never do - or find a solution for displaying them

Ididit.Rest for SQL server for WebView

table Users
	User authentication
	... or ...
	Google, Microsoft, Apple log in

essentials: (highest priority)
appointments: (very high priority)
ASAP tasks: (shopping, etc, ...)
goals: (high priority)
maintenance: (normal priority)

- ASAP tasks
	- where, when, address, email, phone
	- when, where, contact/company name, address, phone number, working hours, website, email
- date & time tasks

1. online sync (c# backend with sql)
2. new hosting provider - ASP net core
3. make it very simple
	- what the first version had
	- but only the essential things
	- fix the problems of the first version
		- priority
		- never done
		- speed - blazor profiler

- don't use `event` to refresh everything on every change
- don't use `StateHasChanged()`
- don't do this: current screen changed -> save current screen to settings -> data changed -> refresh all

- import google keep notes with all features
- organize notes
- category is mandatory
- auto sync & backup

what is wrong with ididit:

	- one in all, jack of all trades, master of none
	- too many tasks - should be one task with interval and sub-tasks

	- category tree -> breadcrumbs with dropdown

	- tasks with very low importance
	- tasks that i never do
	- tasks that i always do - don't need a reminder
	- tasks with interval longer than 7 days

	- too many options / settings

	- I'm not doing the critical tasks
	- show only highest priority overdue tasks

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

[X] Category must be set when creating a new note

copy Loop Habit Tracker
- History (done count grouped by week, month, quarter, year)
- Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
- Best streaks (from date - to date)
- Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

virtualized container

method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance
