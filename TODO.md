# TODO:

save file:
- wasm
- desktop
- mobile

using Microsoft.AspNetCore.Components.Forms; InputFile / download
using Microsoft.Maui.Storage; IFilePicker / namespace CommunityToolkit.Maui.Storage; interface IFileSaver
using Microsoft.Win32; OpenFileDialog / SaveFileDialog
using System.Windows.Forms; OpenFileDialog / SaveFileDialog

load file:
- wasm
- desktop
- mobile

import file:
- import json
- import yaml
- import tsv

markdown

google keep

google drive

localization

dark / light theme

search, filter, sort

help

about

- Calendar
	- Habits.razor
	- DateTime.Now
	- List<TimeModel>? TimesDone
	- 7 row, one for each day of the week
	- 6 columns = one month - 4 full weeks = 28 - another 0/1/2/3 days can take max 2 weeks more
	- find the last monday of the previous month
		DateTime currentDate = DateTime.Now;
		DateTime lastDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
		DateTime lastMonday = lastDayOfMonth.AddDays((int)DayOfWeek.Monday - (int)lastDayOfMonth.DayOfWeek);
	- previous month DaysInMonth
	- this month DaysInMonth
	- next month until sunday - until max 14.
	- each day is a toggle button

- host 24/7 on Raspberry Pi
	valid LAN IP
	test HDMI cable
	test fan
	always on
	install VS code
	install NET 8 sdk

---------------------------------------------------------------------------------------------------

EF Core: use DbContextFactory

benchmark method time & render time

NO: display -> show details -> edit
YES: display -> show details & edit

Note Markdown
Tab in textarea

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

- common:
	- Routes.razor </Router>
		- Ididit.Blazor.Maui
		- Ididit.Blazor.Wasm
		- Ididit.Blazor
	- MainLayout.razor
		- Ididit.Blazor.Wasm
		- Ididit.Blazor
	- _Imports.razor
		- Ididit.Blazor.Maui
		- Ididit.Blazor.Wasm
		- Ididit.Blazor.Web
		- Ididit.Blazor

- Ididit.Blazor.Server:
	- @page "/Error"
	- app.UseExceptionHandler("/Error");

Blazor magic strings: blazor-error-ui , --blazor-load-percentage , --blazor-load-percentage-text , blazor-error-boundary , validation-errors , validation-message

https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/BootErrors.ts
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts#L230
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Web/ErrorBoundary.cs#L48
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationSummary.cs#L76
https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web/src/Forms/ValidationMessage.cs#L74

The following built-in Razor components are provided by the Blazor framework:

https://learn.microsoft.com/en-us/aspnet/core/blazor/components/built-in-components?view=aspnetcore-8.0

App
AntiforgeryToken	https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.antiforgerytoken?view=aspnetcore-8.0
Authentication		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.webassembly.authentication.remoteauthenticatorviewcore-1?view=aspnetcore-8.0
AuthorizeView		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.authorization.authorizeview?view=aspnetcore-8.0
CascadingValue		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.cascadingvalue-1?view=aspnetcore-8.0
DynamicComponent	https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.dynamiccomponent?view=aspnetcore-8.0
ErrorBoundary		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.errorboundary?view=aspnetcore-8.0
FocusOnNavigate		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.focusonnavigate?view=aspnetcore-8.0
HeadContent			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.headcontent?view=aspnetcore-8.0
HeadOutlet			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.headoutlet?view=aspnetcore-8.0
InputCheckbox		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputcheckbox?view=aspnetcore-8.0
InputDate			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputdate-1?view=aspnetcore-8.0
InputFile			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputfile?view=aspnetcore-8.0
InputNumber			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputnumber-1?view=aspnetcore-8.0
InputRadio			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputradio-1?view=aspnetcore-8.0
InputRadioGroup		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputradiogroup-1?view=aspnetcore-8.0
InputSelect			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputselect-1?view=aspnetcore-8.0
InputText			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputtext?view=aspnetcore-8.0
InputTextArea		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.forms.inputtextarea?view=aspnetcore-8.0
LayoutView			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.layoutview?view=aspnetcore-8.0
MainLayout
NavLink				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.navlink?view=aspnetcore-8.0
NavMenu
PageTitle			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.pagetitle?view=aspnetcore-8.0
QuickGrid			https://learn.microsoft.com/en-us/aspnet/core/blazor/components/quickgrid?view=aspnetcore-8.0
Router				https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routing.router?view=aspnetcore-8.0
RouteView			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.routeview?view=aspnetcore-8.0
SectionContent		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.sections.sectioncontent?view=aspnetcore-8.0
SectionOutlet		https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.sections.sectionoutlet?view=aspnetcore-8.0
Virtualize			https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.web.virtualization.virtualize-1?view=aspnetcore-8.0

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