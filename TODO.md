# TODO:

Ididit.Google.Apis
Ididit.LocalStorage
https://learn.microsoft.com/en-us/ef/core/providers/sql-server/temporal-tables
using Markdig; Markdown.ToHtml
using HtmlAgilityPack; MarkSearchResultsInHtml

!!! title edit
	bool editTitle
	bool addNew
!! show Add New Note directly on top of the list
!! show Add New Task directly on top of the list
!! show Add New Habit directly on top of the list

! calendar in 1 row

search / filter / sort: (save filter in settings)
	filter by priority - multiple
	sort by priority
	filter by category - one
	sort by category
	filter by RepeatPeriod - multiple
	sort by RepeatPeriod
	filter by LastTimeDoneAt / elapsed time - range / relative range (to the interval)
	sort by LastTimeDoneAt / elapsed time

- host 24/7 on Raspberry Pi
	valid LAN IP
	test HDMI cable
	test fan
	always on
	install VS code
	install NET 8 sdk

---------------------------------------------------------------------------------------------------

calendar 7x7
	display month
	previous / next month

selected calendar day:
	show all (list)
	add
	remove

when habit is done, uncheck all habit items
when task is done, check all task items

when all habit items are done, habit is done
when all task items are done, task is done

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

Note Markdown
Tab in textarea

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
	habits calendar - own column?
	search, filter, sort

- read Settings from DB before Run()

- categories are filters
- filters are query parameters

- filter
- sort

- import
- export
- load examples
- delete all

- about
- help

---------------------------------------------------------------------------------------------------

- online sync - server with SQL
- calendar streak - load on demand
- average interval
- sub-tasks for exercises, chores, ...
- drag & drop reorder
- keyboard navigation
- benchmark: method time & render time
- Google Keep - copy all features
- don't list habits that i always do / never do - or find a solution for displaying them

essentials: (highest priority)
appointments: (very high priority)
ASAP tasks: (shopping, etc, ...)
goals: (high priority)
maintenance: (normal priority)

- note
- task
    - ASAP
    - date and time - appointment
- habit
    - repeating interval
    - always / every opportunity / occasion - NOTE ?

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

Make a concrete plan so that the habit is not just waiting to be done:
- for habits: split the day in 24 hours, like in calendar
- split the week in 7 days, like in Habits app

there is no point in accordian component - one action to open and one action to close is the same as next, previous - and it is not url route friendly 

- don't use `event` to refresh everything on every change
- don't use `StateHasChanged()`
- don't do this: current screen changed -> save current screen to settings -> data changed -> refresh all

- ReplaceTab in MemoEdit

- habits that i really want to do more frequently
	- chores, cleaning 
	- exercise, stretch 
	- grooming, hygiene 
	- meditation, singing 
	- reading, Spanish 
	- programming 
	- piano
- subtasks: cleaning, exercises

several exercises should not be several tasks
several cleaning chores should not be several tasks
- should be one task with interval and sub-tasks

- import google keep notes with all features
- organize notes
- category is mandatory
- auto sync & backup
- sub notes? paragraphs? sections? yes!
	- tree view? no!
	- accordion? no!
	- breadcrumbs with drop-down? yes!

are goals actually labels? yes!
hiking:
- category: sport
- goals: endurance, immune system, meditation
what i had as goals are actually subcategories 

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

use either checkbox [] or one time action to permanently create a habit

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

[X] Category must be set when creating a new note

copy Loop Habit Tracker
- History (done count grouped by week, month, quarter, year)
- Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
- Best straks (from date - to date)
- Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

virtualized container

method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance

---------------------------------------------------------------------------------------------------

text: ¡!
background shape: Circle
font: Miltonian
Regular 400 Normal
font size: 110
font color: #EEEEEE
background color: #333333