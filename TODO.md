# TODO:

!!! blazor lifecycle methods swallow exceptions !!! -> remove `protected override async Task OnInitializedAsync()`

Ididit.Google.Apis
Ididit.LocalStorage
https://learn.microsoft.com/en-us/ef/core/providers/sql-server/temporal-tables
using Markdig; Markdown.ToHtml
using HtmlAgilityPack; MarkSearchResultsInHtml

??? add TimeOnly? Duration to TaskModel ???
??? Task CompletedAt / Habit LastTimeDoneAt --> DateTime? DoneAt ???



refresh Notes, Tasks, Habits on filter change:
	event += StateHasChanged
	CascadingValue / CascadingParameter
	NavigationManager with Route Parameters - GetUriWithQueryParameter / GetUriWithQueryParameters
	https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-8.0#query-strings
	var newUri = NavigationManager.GetUriWithQueryParameter("filter", filter);
	NavigationManager.NavigateTo(newUri);
		[SupplyParameterFromQuery]
		public string Filter { get; set; }
			Navigation.GetUriWithQueryParameters(
			new Dictionary<string, object?>
			{
				["full name"] = "Morena Baccarin",
				["ping"] = new int?[] { 35, 16, null, 87, 240 }
			})



- host 24/7 on Raspberry Pi
	valid LAN IP
	test HDMI cable
	test fan
	always on
	install VS code
	install NET 8 sdk

---------------------------------------------------------------------------------------------------

two changing columns: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/dynamiccomponent?view=aspnetcore-8.0#example
	in first column: - RenderFragment / DynamicComponent - in a dropdown - this solves the problem with "Back"
		Settings
		Data
		Backup
		Search
		Menu == dropdown / select - dropdown is just an action menu, select displays the chosen option
		Categories
		Priorities
		Help
		Trash
	in second column: - NavigationManager.NavigateTo
		Home == NavigationManager.NavigateTo
		Notes
		Tasks
		Habits
		About

<div class="dropdown">
  <button class="btn btn-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false">
    Dropdown button
  </button>
  <ul class="dropdown-menu">
    <li><a class="dropdown-item" href="#">Action</a></li>
    <li><a class="dropdown-item" href="#">Another action</a></li>
    <li><hr class="dropdown-divider"></li>
    <li><a class="dropdown-item" href="#">Something else here</a></li>
  </ul>
</div>

<div class="dropdown">
  <button class="btn btn-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false">
    Dropdown
  </button>
  <ul class="dropdown-menu">
    <li><button class="dropdown-item" type="button">Action</button></li>
    <li><button class="dropdown-item" type="button">Another action</button></li>
    <li><hr class="dropdown-divider"></li>
    <li><button class="dropdown-item" type="button">Something else here</button></li>
  </ul>
</div>

add Search box
add Filter by done date (Task CompletedAt / Habit LastTimeDoneAt)

on calendar use yellow color for partially done (brush teeth 1x of 2x per day)

selected calendar day:
	show all (list)
	add
	remove

when habit is done, uncheck all habit items
when task is done, check all task items

when all habit items are done, habit is done
when all task items are done, task is done

---------------------------------------------------------------------------------------------------

textarea tabs

textarea highlight
highlight search results

Note Markdown
make regular markdown text look the same as in textarea

- common:
	- Router
		- Ididit.Blazor - Routes.razor
		- Ididit.Blazor.Wasm - App.razor - CascadingAuthenticationState, AuthorizeRouteView, NotAuthorized

markdown
google keep
google drive
localization
dark / light theme
search, filter, sort
help
about

EF Core: use DbContextFactory

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

import Google Keep repeating reminder
import Google Keep h1, h2, bold, italic, underline

pin razor page to Home - in a column - max 4 columns ?

- settings
	- CSS
		- theme
		- font size
		- background color
		- Note, Task, Habit background color
	- language
- localization https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-8.0
- https://github.com/xaviersolau/BlazorJsonLocalization

- Ididit.Blazor.Server:
	- @page "/Error"
	- app.UseExceptionHandler("/Error");

columns:
	search, filter, sort

- read Settings from DB before Run()

- categories are filters
- filters are query parameters

- filter
- sort

- about
- help

---------------------------------------------------------------------------------------------------

- online sync - server with SQL
- calendar streak
- average interval
- drag & drop reorder
- keyboard navigation
- benchmark: method time & render time
- Google Keep - copy all features
- don't list habits that i always do / never do - or find a solution for displaying them

Ididit.Rest for SQL server for WebView
User authentication / table Users
... or ...
Google, Microsoft, Apple log in

Ididit.yyyyMMddHHmmss.json
	Google Drive
	OneDrive
	iCloud
	Dropbox - NO!

essentials: (highest priority)
appointments: (very high priority)
ASAP tasks: (shopping, etc, ...)
goals: (high priority)
maintenance: (normal priority)

- ASAP tasks
	- with subtasks but not really?
	- more: where, when, address, email, phone
- date & time tasks? do i really need this? why not google calendar
	- the only reason would be details

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

showing DesiredInterval is a bad idea - no consequences (interval doesn't change) - show AverageInterval

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
