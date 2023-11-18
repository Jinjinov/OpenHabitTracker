# TODO:

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