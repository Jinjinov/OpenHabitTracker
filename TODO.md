# TODO:

- /add
- /update
- restore from trash
- AddServices()
- read Settings from DB before Run()
- common:
	- app.css
	- bootstrap.css
	- Routes.razor
	- MainLayout.razor
	- NavMenu.razor

- online sync - server with SQL
- no css / js downloads, everything is local
- calendar streak - load on demand
- average interval
- sub-tasks for exercises, chores, ...
- drag & drop reorder
- keyboard navigation
- benchmark: method time & render time
- Google Keep - copy all features
- don't list habits that i always do / never do - or find a solution for displaying them - HIGH / LOW priority - NEED / WANT to do importance

color coded priorities

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

categories are filters
filters are query parameters

there is no point in accordian component - one action to open and one action to close is the same as next, previous - and it is not url route friendly 

- don't use `event` to refresh everything on every change
- don't use `StateHasChanged()`
- don't do this: current screen changed -> save current screen to settings -> data changed -> refresh all

load on demand - List = null / List = new()
- [ ] task - "done times list" should load on demand - on Task done - on show Task details

- [ ] restore deleted Goals from Trash

- ReplaceTab in MemoEdit

- [ ] don't add Category/Goal until (name is set) / (Save button is clicked) - no need to undo adding empty objects = easy discard

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
- tasks and habits mixed
- too many tasks - should be one task with interval and sub-tasks
- missing Trash

- category tree -> breadcrumbs with dropdown

- tasks with very low importance
- tasks that i never do
- tasks that i always do - don't need a reminder
- tasks with interval longer than 7 days

- too many options / settings

- I'm not doing the critical tasks
- no consequences (interval doesn't change)
- show only highest priority overdue tasks 
- refresh is too slow

showing DesiredInterval is a bad idea - no consequences (interval doesn't change) - show AverageInterval

task is either ASAP or habit
use either checkbox [] or one time action to permanently create a habit

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes 

copy Google Keep - don't truncate
category - in your face, route friendly 
[X] Category must be set when creating a new note

copy Loop Habit Tracker
- History (done count grouped by week, month, quarter, year)
- Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
- Best straks (from date - to date)
- Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

virtualized container
method trace logging - performance 

- [ ] benchmark method performance

https://learn.microsoft.com/en-us/aspnet/core/blazor/performance