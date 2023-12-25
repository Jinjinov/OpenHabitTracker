# TODO:

1. online sync (c# backend with sql)
2. new hosting provider - ASP net core
3. make it very simple
	- what the first version had
	- but only the essential things
	- fix the problems of the first version
		- priority
		- never done
		- speed - blazor profiler

essentials: (highest priority)
appointments: (very high priority)
ASAP tasks: (shopping, etc, ...)
goals: (high priority)
maintenance: (normal priority)

- for habits: split the day in 24 hours, like in callendar
- split the week in 7 days, like in Habits app

tabs - routes
category - route friendly
there is no point in accordian component - it is not url route friendly
child of (task, habit, note) doesn't need a route
categories are filters
filters are query parameters 

- don't use `event` to refresh everything on every change
- don't use `StateHasChanged()`
- don't do this: current screen changed -> save current screen to settings -> data changed -> refresh all

load on demand - List = null / List = new()
GetByIndex<TKey, TEntity>(TKey lowerBound, TKey upperBound, string dbIndex, bool isRange)
lowerBound = parent id value
upperBound = null
dbIndex = parent id name
isRange = false
- [ ] task - "done times list" should load on demand - on Task done - on show Task details

- should be one task with interval and sub-tasks

- missing Trash
- [ ] restore deleted Goals from Trash

color coded priorities

Google Keep import = first class citizen
- created at
- edited at

- [ ] benchmark method performance

- ReplaceTab in MemoEdit

class Note, class Task, class RepeatingTask

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

- ASAP tasks
	- subtasks but not really?
	- more: where, when, address, email, phone
- date & time tasks? really? google calendar
	- the only reason would be details

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
	- tasks that don't need a reminder
	- tasks with interval longer than 7 days

	- too many options / settings

	- I'm not doing the critical tasks
	- no consequences (interval doesn't change)
	- show only highest priority overdue tasks 
	- refresh is too slow

do NOT add tasks that you ALWAYS do
do NOT add tasks that you NEVER do

task is either ASAP or habit
use either checkbox or one time action to permanently create a habit

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes 

copy Google Keep - don't truncate
category - in your face, route friendly 
[X] Category must be set when creating a new note
- Category (tree view dropbox --> breadcrums)
- Title
- Note

copy Loop Habit Tracker

there is no point in accordian component - one action to open and one action to close is the same as next, previous - and it is not url route friendly 

keep it simple, stupid!

blazor united
asp net core
sql server
markdown notes that don't truncate
tabs - routes
load on demand
virtualized container
method trace logging - performance 

https://learn.microsoft.com/en-us/aspnet/core/blazor/performance