# TODO:

---------------------------------------------------------------------------------------------------

find out why `padding-left: 12px !important;` is needed on iOS - why `padding-left: env(safe-area-inset-left) !important;` doesn't work

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
    - wire sub-collections from flat dicts        CORRECT  in ClientData.GetHabits(), partial in ClientState.LoadHabits()
    - CRUD Add operations update dicts            CORRECT
    - DataAccess private to store                 MISSING  (exposed as public property, services use it directly)
    - per-instance loads register into dicts      MISSING  (orphaned objects — see bug section below)
    - CategoryModel sub-lists wired at runtime    MISSING  (only in GetUserData() for export)

    Three violations, all surgical fixes — the architecture is sound, the invariant just isn't enforced consistently.

---------------------------------------------------------------------------------------------------

ClientState dict sync:
    fix `ClientState.GetUserData()` which calls InitializeContent()
    search for `// TODO:: remove temp fix`
        `InitializeItems` and `InitializeTimes` have null checks and do not update data when called in GetUserData()
            both load data directly from DB with `_dataAccess.GetTimes()` and `_dataAccess.GetItems()`
            but HabitService.LoadTimesDone also loads data with `_dataAccess.GetTimes(habit.Id)` - these are not the same objects as in `InitializeTimes`
            and ItemService.Initialize also loads data with `_dataAccess.GetItems(items.Id)` - these are not the same objects as in `InitializeItems`
        user can add or remove Items and Times list
            `DataAccess.AddItem(item);` / `DataAccess.UpdateItem(item);`
            `DataAccess.AddTime(timeEntity);` / `DataAccess.UpdateTime(timeEntity);`
            the code does not update Items and Times in the ClientState
        so without temp fix, GetUserData() would return Items and Times that were loaded with Initialize()
    NO!!! - either remove these from class ClientState: - NO!!!
        public Dictionary<long, TimeModel>? Times { get; set; }
        public Dictionary<long, ItemModel>? Items { get; set; }
    or
        make sure that other services update them !!!
        1.
            `ToEntity` already exist
            add `ToModel` and use it for every `Model`
                models need other models to initialize their `List<>` properties
                    List<CategoryModel> Categories
                        List<NoteModel>? Notes
                        List<TaskModel>? Tasks
                            List<ItemModel>? Items
                        List<HabitModel>? Habits
                            List<ItemModel>? Items
                            List<TimeModel>? TimesDone
                provide `ClientData` as input
                    - if `Model` is not found in the `Dictionary` then use `_dataAccess`
                    - add it to `Dictionary` in `ClientData`
        2.
            make sure that loading an `Entity` with `DataAccess` and creating a `Model` results in storing the `Model` in a `Dictionary` in `ClientData`
            check for every `new.*Model`
        3.
            make sure that every `DataAccess.Add` and `DataAccess.Update` and `DataAccess.Remove` also updates `Dictionary<long, Model>` in `ClientData`
            private `DataAccess` in `ClientData`

to remove `// TODO:: remove temp fix` and keep habit ratio in UI:
call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
    save TotalTimeSpent
    save AverageInterval
    on Habit Initialize - load only last week (last X days, displayed in small calendar)
    call LoadTimesDone for large calendar            

this is a problem:
    - services use `_dataAccess` on their own, but `ClientState` is supposed to represent the current state as the only source of truth
    - but only `ClientState.GetUserData()` suffers from it and a `temp fix` is in place
Ididit did not have this problem, `Repository` was the only class with `IDatabaseAccess` and represented the current state

new findings (discovered while planning Category-grouped main list):
    - CategoryModel.Notes/Tasks/Habits are also never populated at runtime (same root cause)
      only populated in GetUserData() as a one-off for export — same pattern as Times/Items
    - ItemService.Initialize() lazy-loads Items per-instance via _dataAccess.GetItems(items.Id)
      but does NOT store them back into ClientState.Items — orphaned objects, not in the dict
      same problem as HabitService.LoadTimesDone → _dataAccess.GetTimes(habit.Id)
    - HabitService.RemoveTimeDone() removes from habit.TimesDone but NOT from ClientState.Times
    - ItemService.DeleteItem() removes from items.Items but NOT from ClientState.Items
    - CategoryService.DeleteCategory() cascade is completely broken:
      iterates category.Notes/Tasks/Habits to mark children IsDeleted=true,
      but those lists are always null at runtime — children are never marked deleted,
      silently left in ClientState.Notes/Tasks/Habits as live items with a dangling CategoryId
    - these issues (Times, Items, CategoryModel lists) must all be solved together
      solving only CategoryModel.Notes/Tasks/Habits (for grouped view) while leaving Times/Items
      broken would be inconsistent and pull on the same thread without finishing it

---------------------------------------------------------------------------------------------------

Dict sync fix: detailed problem description and plan

    ROOT CAUSE:
        The identity map invariant is not enforced:
        "every Model that exists in memory must be the canonical instance stored in its ClientState dict"
        Two things break this invariant:

        A. DataAccess is public on ClientState
           Services call _clientState.DataAccess directly for mutations (Add, Update, Remove)
           without always updating the corresponding dict entry.
           This is an enforcement problem — the invariant cannot be violated if DataAccess is private.

        B. Per-instance lazy loads in services create objects outside the dicts
           HabitService.LoadTimesDone(habit):
               loads TimeEntity list → creates new TimeModel objects → assigns to habit.TimesDone
               but NEVER adds them to ClientState.Times
               → those TimeModel instances are orphaned (not in the dict)
           ItemService.Initialize(items):
               loads ItemEntity list → creates new ItemModel objects → assigns to items.Items
               but NEVER adds them to ClientState.Items
               → those ItemModel instances are orphaned (not in the dict)

        C. Remove operations in services don't update dicts
           HabitService.RemoveTimeDone(habit, timeModel):
               removes from habit.TimesDone, calls DataAccess.RemoveTime
               but NEVER removes from ClientState.Times
           ItemService.DeleteItem(items, item):
               removes from items.Items, calls DataAccess.RemoveItem
               but NEVER removes from ClientState.Items

        D. CategoryModel.Notes/Tasks/Habits are never wired at runtime
           ClientState.LoadNotes/LoadTasks/LoadHabits never populate CategoryModel sub-lists
           Only GetUserData() does it, as a one-off for export
           → CategoryService.DeleteCategory() cascade is completely broken:
             it iterates category.Notes/Tasks/Habits to mark children IsDeleted,
             but those lists are always null → children are never marked deleted,
             silently left as live items in ClientState dicts with a dangling CategoryId
           WHY they were left null: populating them at runtime requires every mutation
           (AddNote, DeleteNote, RestoreNote, category change, etc.) to dual-write:
           update the flat dict AND update the category sub-list.
           WHY they SHOULD be populated: the mutations that need dual-write are a closed set
           (3 types × handful of operations, written once). The consumers are an open set —
           every future feature that works with categories (grouped view, CompletionRule,
           LastTimeDone, stats, cascade delete) gets correct grouped data for free.

           Option B - (query flat dicts by CategoryId in every consumer) - REJECTED
           scatters the same filtering logic across every consumer and makes DeleteCategory depend on ClientState.

           DECISION:
           Option A - populate CategoryModel.Notes/Tasks/Habits at runtime and maintain them in every service mutation.

    LAZY LOADING IS KEPT:
        Per-instance lazy loads in services stay exactly as they are — triggered by user interaction,
        loading only what is needed. The fix is not to remove lazy loading but to enforce the invariant:
        "if you loaded it, register it in the dict"
        The dict does not need to be complete — it just needs to contain everything that HAS been loaded.

        WHY Times lazy loading is critical:
        In Ididit (predecessor app), all TimesDone were loaded upfront. After 5 years of daily use
        with ~50 habits done 1-2× per day, the Times table grew to ~180,000 records and became slow.
        Per-habit lazy loading of Times was introduced specifically to solve this.
        Items are small (handful per habit/task) and not a performance concern.

        The temp fix in LoadHabits() calls LoadTimes() (bulk load of ALL times) — this is the WRONG
        direction and reintroduces the exact performance problem lazy loading was meant to solve.
        However, removing it is NOT part of Steps 1-4. It is a separate, larger task (see below)
        because the habit list UI depends on AverageInterval and TotalTimeSpent being computed
        from TimesDone — without them the ratio badges show division-by-zero results.

        SEPARATE TASK — remove temp fix (prerequisite: persist aggregates to DB):
            HabitModel computes TotalTimeSpent and AverageInterval in OnTimesDoneChanged()
            from the full TimesDone list. The habit list renders these via GetRatio() for the
            ratio badge and elapsed time display. Without TimesDone loaded, they are TimeSpan.Zero
            and ElapsedTimeToAverageIntervalRatio / AverageIntervalToRepeatIntervalRatio divide by zero.
            The // TODO:: save it? comments in HabitModel.cs already identify the solution:
            - add TotalTimeSpent and AverageInterval as persisted fields on HabitEntity
            - compute and save them on every AddTimeDone, RemoveTimeDone, UpdateTimeDone
            - once persisted, LoadHabits() can render the full list without loading any Times
            - then remove LoadTimes() and the TimesDone wiring from LoadHabits() (remove temp fix)
            - load only last N days of Times at startup for the small calendar display
            - load full Times per-habit on selection for the large calendar
            This requires a DB migration and is tracked separately from Steps 1-4.

            CAUTION — misleading comment in source code:
            ClientState.GetUserData() has `Times = null; // TODO:: remove temp fix` at the line
            that nulls Times before LoadTimes(). That line must NOT be removed — nulling Times
            before LoadTimes() is correct export behavior (forces full reload so partially-lazy-loaded
            Times don't produce an incomplete export). Only the temp fix lines inside LoadHabits()
            should be removed. The same pattern applies to Items: GetUserData() should also null Items
            before LoadItems() (currently missing — see Step 4).

    PLAN:

        Step 1 — wire CategoryModel sub-lists at runtime (fix D)
            IMPORTANT: initialize all category sub-lists to empty new() in LoadCategories(),
                after creating CategoryModel objects from the DB result:
                foreach category in Categories.Values: category.Notes = new(); category.Tasks = new(); category.Habits = new();
                place in LoadCategories() because all three Load methods call LoadCategories() first —
                the lists are guaranteed initialized before any per-item wiring loop runs.
                this handles first use of the app (DB empty) — without this, category.Notes/Tasks/Habits
                would stay null for categories that have no items, causing null ref in grouped view
            ALTERNATIVE: instead of the initialization loop above, change CategoryModel to use List<T> = new()
                instead of List<T>? — all null checks/guards in backup/import/service code would then
                become unnecessary and could be removed
            in ClientState.LoadNotes(): after loading, for each NoteModel,
                use TryGetValue — skip if CategoryId == 0 (uncategorized, no CategoryModel in dict)
                skip if IsDeleted — deleted items belong only in the flat dict (for trash view),
                    NOT in category sub-lists (runtime view = active items only)
                    export gets deleted items from the flat dicts directly (see Step 4), not from sub-lists
                add it to category.Notes
            in ClientState.LoadTasks(): same for TaskModel → category.Tasks
            in ClientState.LoadHabits(): same for HabitModel → category.Habits
            this fixes DeleteCategory cascade — category.Notes/Tasks/Habits will be populated
            IMPORTANT: CategoryId == 0 guard is required everywhere in Step 1 and in all mutations.
            Items with CategoryId == 0 have no CategoryModel in the dict — TryGetValue, not indexer.
            also maintain sub-lists in every service mutation:
                NoteService.AddNote()       → category.Notes.Add(note)
                NoteService.DeleteNote()    → category.Notes.Remove(note)
                TrashService.Restore(NoteModel)  → category.Notes.Add(note)
                TaskService.AddTask()       → category.Tasks.Add(task)
                TaskService.DeleteTask()    → category.Tasks.Remove(task)
                TrashService.Restore(TaskModel)  → category.Tasks.Add(task)
                HabitService.AddHabit()     → category.Habits.Add(habit)
                HabitService.DeleteHabit()  → category.Habits.Remove(habit)
                TrashService.Restore(HabitModel) → category.Habits.Add(habit)
                category change (any type)  → remove from old category list, add to new
            CategoryService.DeleteCategory() cascade — after Step 1, category.Notes/Tasks/Habits are
            populated, so DeleteCategory will iterate them correctly. But it must also remove each child
            from the flat ClientState dicts (ClientState.Notes, ClientState.Tasks, ClientState.Habits),
            not just mark IsDeleted on the model.

        Step 2 — register per-instance lazy load results into dicts (fix B)
            lazy loading stays — just add dict registration after each lazy load:
            in HabitService.LoadTimesDone(habit): after assigning habit.TimesDone,
                initialize ClientState.Times if null, then:
                foreach (TimeModel time in habit.TimesDone)
                    _clientState.Times[time.Id] = time;
            in ItemService.Initialize(items): after assigning items.Items,
                initialize ClientState.Items if null, then:
                foreach (ItemModel item in items.Items)
                    _clientState.Items[item.Id] = item;

        Step 3 — keep Remove operations in sync (fix C)
            in HabitService.RemoveTimeDone: after DataAccess.RemoveTime,
                also _clientState.Times?.Remove(timeModel.Id)
            in ItemService.DeleteItem: after DataAccess.RemoveItem,
                also _clientState.Items?.Remove(item.Id)

        Step 4 — fix GetUserData() and SetUserData() for category sub-lists
            GetUserData() assigns category.Notes/Tasks/Habits directly on live runtime CategoryModel
            objects (same instances in ClientState.Categories dict). Currently harmless because those
            lists are null at runtime. After Step 1 they won't be null — GetUserData() would overwrite
            the maintained runtime lists with export-format lists.
            Fix: in GetUserData(), do NOT assign to category.Notes/Tasks/Habits on runtime objects.
            Keep building the export hierarchy from the flat dicts (as currently done with
            notesByCategoryId, tasksByCategoryId, habitsByCategoryId) — this correctly includes
            deleted items in the export (full backup). Store results in local variables only,
            never assign back to the live CategoryModel instances.
            NOTE: runtime category sub-lists intentionally EXCLUDE deleted items (DeleteNote removes
            them from category.Notes). Export must include deleted items for full backup/restore.
            These are two different views of the data — do not conflate them.

            SetUserData() adds models to flat dicts but never wires them to category sub-lists.
            After an import, the flat dicts are correct but category sub-lists would be stale.
            Fix: after SetUserData() adds models to the dicts, also wire them to their category
            sub-lists (same logic as Step 1 — for each imported note/task/habit, skip if IsDeleted,
            skip if CategoryId == 0, then add to category list).
            NOTE: exported data includes deleted items (full backup) — the IsDeleted skip is required
            here for the same reason as in Step 1: category sub-lists are the runtime view (active only).
            IMPORTANT: before the wiring loop, initialize category sub-lists the same way as in
            LoadCategories(): foreach category in Categories.Values: category.Notes = new(); category.Tasks = new(); category.Habits = new();
            SetUserData() does NOT call LoadCategories() — it builds dicts directly from imported data,
            so the initialization loop from LoadCategories() does NOT run. Without this, category sub-lists
            would be null and the wiring loop would crash on category.Notes.Add(note).
            If the ALTERNATIVE (List<T> = new() in CategoryModel) is chosen, this initialization is unnecessary.

            Also fix GetUserData() for Items (same pattern as existing Times fix):
            GetUserData() nulls Times before LoadTimes() to force full reload for export.
            Do the same for Items:
                Items = null;
                await LoadItems();
            NOTE: LoadItems() already exists in ClientState.cs line 232 — same null-guard pattern as LoadTimes()
            without this, if Items was partially populated by lazy loads, GetUserData() would
            export incomplete item data

        Step 5 — consider making DataAccess private (fix A, long-term)
            DataAccess is currently public on ClientState so services can reach it directly
            long-term: make it private, add explicit ClientState methods for every operation
            services call ClientState methods → ClientState calls DataAccess + updates dict
            this enforces the invariant at compile time, not by convention
            NOTE: this is the largest change — Steps 1-4 are safe to do first

        Order: Steps 1-4 address different root causes and have non-overlapping code changes,
               but Step 4 MUST be deployed together with Step 1 — Step 4's GetUserData fix exists
               because of Step 1: once Step 1 wires category sub-lists at runtime, GetUserData()
               would overwrite them on the next export if Step 4 is not in place.
               Step 5 is a larger refactor, do separately after 1-4 are verified.

---------------------------------------------------------------------------------------------------

1, 2, 3 must be done at the same time so there is one new DB migration, not three
(the "remove temp fix" task also requires a DB migration — adding TotalTimeSpent and AverageInterval to HabitEntity — but that is a separate migration done independently of tasks 1/2/3)

0.
prerequisite for task 1 (avoids duplicating row HTML between flat and grouped loops):
- extract HabitRowComponent from the habit row block in Habits.razor
- extract TaskRowComponent from the task row block in Tasks.razor
- extract NoteRowComponent from the note row block in Notes.razor
- both the flat loop and the grouped-view category loop use the same row component

1.
Category-grouped main list (togglable alternative view):
prerequisite: Dict sync fix Steps 1-4 must be done first — Task 1 uses category.Notes/Tasks/Habits
  populated at runtime (Step 1); without it, all category sub-lists are null and grouped view is broken
- applies to Notes, Tasks, and Habits pages
- controlled by a new ShowGroupedByCategory setting (bool, default false)
- replaces the current flat foreach in each page:
  - foreach (NoteModel note in NoteService.GetNotes())
  - foreach (TaskModel task in TaskService.GetTasks())
  - foreach (HabitModel habit in HabitService.GetHabits())
- outer loop: foreach (CategoryModel category in CategoryService.Categories)
- items with no category (CategoryId == 0, the default long value) appear in an "Uncategorized" bucket rendered at the bottom
- grouped view respects HiddenCategoryIds and SelectedCategoryId from Settings (same as flat view)
- inner loop: items filtered+sorted per category
    `QueryParameters queryParameters = _searchFilterService.GetQueryParameters(_clientState.Settings);`
    CategoryModel.Notes/Tasks/Habits are populated at runtime (Option A — see Dict sync fix plan):
              `category.Notes.FilterNotes(queryParameters)`
              `category.Tasks.FilterTasks(queryParameters)`
              `category.Habits.FilterHabits(queryParameters)`
    Uncategorized bucket: CategoryId == 0 items have no CategoryModel in the dict (see Step 1 guard).
    Render as a hardcoded bucket AFTER the category loop using flat dict with .Where(x => x.CategoryId == 0):
              `ClientState.Notes.Values.Where(x => x.CategoryId == 0).FilterNotes(queryParameters)`
              `ClientState.Tasks.Values.Where(x => x.CategoryId == 0).FilterTasks(queryParameters)`
              `ClientState.Habits.Values.Where(x => x.CategoryId == 0).FilterHabits(queryParameters)`
    This is the one place where Option B is used — it is an exception, not a contradiction of Option A.
- category header row (all three pages): category title, collapse/expand (same unicode char as in Search)
- category header row (Habits only): also and/or toggle button (see task 2), status color
  (green/orange/red, computed solely from CompletionRule — see task 2), LastTimeDoneAt (see task 3)
- cross-category sorting still works in flat view; grouped view sorts within each category
- inject ICategoryService into Habits.razor, Tasks.razor, Notes.razor (not currently injected)
- all new UI strings (category header labels) must use @Loc["..."] and add translations to json — app has 20 languages
- persistence chain for ShowGroupedByCategory (bool, new SettingsModel/SettingsEntity field):
  - add to SettingsModel and SettingsEntity
  - add mapping in EntityToModel.cs and ModelToEntity.cs
  - EF migration in both OpenHabitTracker.EntityFrameworkCore/Migrations/
    and OpenHabitTracker.Blazor.Web/Migrations/
  - export/import: automatically included since full SettingsModel is serialized
- new localization string: "Group by category"

2.
add group "and / or" toggle:
- all habits/items done -> green (color) / "complete" (text)
- one habit/item done -> green (color) / "complete" (text)

Plan:
- add CompletionRule property to CategoryModel (enum CompletionRule { All, Any })
- full persistence chain (CompletionRule is a new persisted field, unlike display-only settings):
  - add to CategoryEntity
  - add mapping in EntityToModel.cs and ModelToEntity.cs
  - EF migration in both OpenHabitTracker.EntityFrameworkCore/Migrations/
    and OpenHabitTracker.Blazor.Web/Migrations/
  - include in all export/import formats: JSON, YAML, TSV, Markdown (Google Keep is import-only)
- all new UI strings must use @Loc["..."] and add translations to json — app has 20 languages
- new localization strings: "Mark complete when all habits done" / "Mark complete when any habit done"
- two display locations, both optional and independent:

  A. Stats panel (second column, see task 4 plan):
     - show per-category CompletionRule state alongside the green/orange/red aggregate (read-only display for now)
     - CompletionRule is the sole input for computing each category's status color in stats

  B. Category-grouped main list (see task 1):
     - and/or toggle button in the category header row

3.
LastDone date: for a group, for the items
- add date to habit item
- add date to category
add settings to show, hide this extra info

Plan:
- "last done for an item" already exists: HabitModel.LastTimeDoneAt (DateTime?)
- "last done for a category" = max(LastTimeDoneAt) across all habits in that category
- all new UI strings must use @Loc["..."] and add translations to json — app has 20 languages
- new localization strings: "Last done", "Show last done"
- two display locations, both optional and independent:

  A. Stats panel (second column, see task 4 plan):
     - show LastTimeDoneAt (most recent across all habits)

  B. Category-grouped main list (see task 1):
     - show LastTimeDoneAt in the category header row
     - controlled by a new ShowLastTimeDone setting (bool, default true)
     - persistence chain for ShowLastTimeDone (bool, new SettingsModel/SettingsEntity field):
       - add to SettingsModel and SettingsEntity
       - add mapping in EntityToModel.cs and ModelToEntity.cs
       - EF migration in both OpenHabitTracker.EntityFrameworkCore/Migrations/
         and OpenHabitTracker.Blazor.Web/Migrations/
       - export/import: automatically included since full SettingsModel is serialized

  C. Per-habit in the flat main list:
     - already shown (ElapsedTime + ratio badge on each habit row)
     - no change needed

4.
This week (xx.xx - yy.yy) statistics
✓ x out of y habits done
- x out of y groups are green (color) / "complete" (text)

Plan:
✓ implement as 3 reusable components: NotesStatisticsComponent, TasksStatisticsComponent, HabitsStatisticsComponent
✓ wide screens (>= 1280px): each component renders in the else branch inside the second column on its respective page when no item is selected
✓ mobile: each component renders if (!_showSecondColumn)
- inject ICategoryService into Habits.razor, Tasks.razor, Notes.razor
- respect ShowGroupedByCategory (see task 1)
    - false: iterate Notes, Tasks, Habits
    - true: iterate CategoryService.Categories, use category.Notes/Tasks/Habits (populated at runtime — see Dict sync fix plan, Option A decision)
  respect HiddenCategoryIds / SelectedCategoryId from Settings
✓ all new UI strings must use @Loc["..."] and add translations to json — app has 20 languages
✓ new localization strings: "This week", "out of" added; "overdue", "complete" pending (requires task 1)

Habits stats:
- respect ShowGroupedByCategory (see task 1)
- category title (if ShowGroupedByCategory) | habit count | green/orange/red counts (using existing GetRatio() + SelectedRatio logic) | LastTimeDoneAt of most recent habit
✓ "this week" aggregate at top: habits done at least once this week out of total habits; categories fully complete this week (flat list only, no per-category grouping)

Tasks stats:
- respect ShowGroupedByCategory (see task 1)
- category title (if ShowGroupedByCategory) | total count | done count (CompletedAt != null) | overdue count (PlannedAt < now && CompletedAt == null) | total time spent (sum of CompletedAt - StartedAt across completed tasks)
✓ total count | done count (CompletedAt != null) — flat list only, no per-category grouping
- "this week" aggregate at top: tasks completed this week | tasks planned this week

Notes stats:
- respect ShowGroupedByCategory (see task 1)
- category title (if ShowGroupedByCategory) | total count | count per Priority
✓ total count | count per Priority — flat list only, no per-category grouping
- CreatedAt / UpdatedAt

---------------------------------------------------------------------------------------------------

1.
QueryParameters:
    - `ClientData.GetHabits/GetNotes/GetTasks` each have a TODO: "first filter with queryParameters, then use _dataAccess"
    - Currently all records are loaded into memory first, then filtered in C# — the intent is to push filters down to the data layer
    - `_dataAccess` calls would receive query parameters (search term, category, priority, date range) and return only matching records
    - This would eliminate the need for large in-memory filter blocks and reduce data transferred from the data source

2.
exact repeating reminders, like Google Keep

3.
drag & drop reorder - manual sort - 1000000 sort index
- sort categories?
- sort items?

---------------------------------------------------------------------------------------------------

4.
upgrade to .NET 10

5.
upgrade NuGet versions

---------------------------------------------------------------------------------------------------

1.
search/filter/sort query parameters in the URL - Web API

2.
search/filter/sort query parameters in the URL - Blazor

---------------------------------------------------------------------------------------------------

3.
refresh local if remote has changed:

set `_lastRefreshAt = DateTime.UtcNow;` on local changes, so a local change won't trigger an update of the local UI

---------------------------------------------------------------------------------------------------

4.
Data.razor -> "Online sync" -> "Log in"
Sync between `DataLocation.Local` and `DataLocation.Remote` in `ClientState.SetDataLocation()`
method to copy one db context to another

    public void CopyData(DbContext source, DbContext destination)
    {
        foreach (var entityType in source.Model.GetEntityTypes())
        {
            var sourceSet = source.Set(entityType.ClrType);
            var destinationSet = destination.Set(entityType.ClrType);
            // Retrieve all records without tracking.
            var data = sourceSet.AsNoTracking().ToList();
            // Add records to the destination context.
            destinationSet.AddRange(data);
        }
        destination.SaveChanges();
    }

    using (var sqliteContext = new MyDbContext(sqliteOptions))
    using (var sqlServerContext = new MyDbContext(sqlServerOptions))
    {
        // Copy data from SQLite to SQL Server.
        CopyData(sqliteContext, sqlServerContext);
    }

    // Or to copy in the opposite direction:
    using (var sqliteContext = new MyDbContext(sqliteOptions))
    using (var sqlServerContext = new MyDbContext(sqlServerOptions))
    {
        // Copy data from SQL Server to SQLite.
        CopyData(sqlServerContext, sqliteContext);
    }

---------------------------------------------------------------------------------------------------

5.
make every ...Id a required field in EF Core - Debug.Assert(Id != 0) before Add / Update

---------------------------------------------------------------------------------------------------

now:
- 1. Google
- 2. Microsoft
- 3. Apple

later:
- 1. Nextcloud
- 2. Dropbox
- 3. Box

3 ways to login to google drive:
- HTTP REST (wasm, web)
- NuGet packages: google drive api, ... (windows, macos, linux)
- MAUI WebAuthenticator + server backend https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/communication/authentication

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-9.0

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/?view=aspnetcore-9.0

https://github.com/dotnet/maui-samples/tree/main/9.0/PlatformIntegration/PlatformIntegrationDemos

https://github.com/dotnet/maui/blob/main/src/Essentials/samples/Sample.Server.WebAuthenticator/Startup.cs#L33-L64

https://github.com/dotnet/maui/blob/main/src/Essentials/samples/Sample.Server.WebAuthenticator/Controllers/MobileAuthController.cs

6.
add OAuth to Blazor Wasm, Photino, Wpf, WinForms, Blazor Server, Maui
    Google Drive
    Microsoft OneDrive
    Dropbox
    Box
    Nextcloud

7.
use Google, Microsoft, Dropbox OAuth for unique user id and login

8.
add backup to
    Google Drive
    Microsoft OneDrive
    Dropbox
    Box
    Nextcloud

9.
use DB in Blazor Server for multi user sync with REST API endpoints

---------------------------------------------------------------------------------------------------

10.
Android:
    save SQLite DB in an external folder
    can be part of Google Drive, OneDrive, iCloud, Dropbox

AndroidManifest.xml
MANAGE_EXTERNAL_STORAGE

    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

Android: get permission to save SQLite DB in an external folder that can be part of Google Drive, OneDrive, iCloud, Dropbox

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

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // LocalApplicationData, ApplicationData, UserProfile, Personal, MyDocuments, Desktop, DesktopDirectory
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // LocalApplicationData, ApplicationData, UserProfile, Personal, MyDocuments, Desktop, DesktopDirectory
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Android))
    {
        if (ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
        {
            ActivityCompat.RequestPermissions(MainActivity.Instance, new string[] { Manifest.Permission.WriteExternalStorage }, 1);
        }
        path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "MyAppFolder");
        return Path.Combine(Android.OS.Environment.ExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath, "MyAppFolder");
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.iOS))
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

---------------------------------------------------------------------------------------------------

add Google Drive
    Blazor WASM -> Google Drive REST API
    Blazor Desktop -> Google Drive API
add Blazor Server - OAuth REST, CRUD REST, SignalR for instant UI refresh on multiple devices
    Blazor Mobile -> Blazor Server
    Blazor Server -> Google Drive API

login will be with Google, Microsoft, Dropbox - requires scope with permission to get email
email will be unique user id
store the refresh token for each cloud provider

---------------------------------------------------------------------------------------------------

    <PackageReference Include="AspNet.Security.OAuth.Dropbox" Version="9.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="9.0.1" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.MicrosoftAccount" Version="9.0.1" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="9.0.1" />

---------------------------------------------------------------------------------------------------

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.Google;
    using System.Security.Claims;
    using System.Text.Json;

    namespace OpenHabitTracker.Blazor.Web;

    public static class AuthenticationSetup
    {
        public static IServiceCollection AddAuthenticationProviders(this IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme; // Default for external providers
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromDays(14); // Remember login for 14 days
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = "Your-Google-Client-Id";
                options.ClientSecret = "Your-Google-Client-Secret";
                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.Events.OnCreatingTicket = async context =>
                {
                    var identity = context.Principal.Identity as ClaimsIdentity;
                    var email = context.Principal.FindFirst(ClaimTypes.Email)?.Value;
                    var name = context.Principal.FindFirst(ClaimTypes.Name)?.Value;
                    identity.AddClaim(new Claim("email", email ?? string.Empty));
                    identity.AddClaim(new Claim("name", name ?? string.Empty));
                    // Save tokens for later API calls if needed
                    var tokens = JsonSerializer.Serialize(context.Properties.GetTokens());
                    identity.AddClaim(new Claim("tokens", tokens));
                };
            })
            .AddMicrosoftAccount(options =>
            {
                options.ClientId = "Your-OneDrive-Client-Id";
                options.ClientSecret = "Your-OneDrive-Client-Secret";
                options.SaveTokens = true;
                options.Scope.Add("email");
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Events.OnCreatingTicket = async context =>
                {
                    var identity = context.Principal.Identity as ClaimsIdentity;
                    var email = context.Principal.FindFirst(ClaimTypes.Email)?.Value;
                    var name = context.Principal.FindFirst(ClaimTypes.Name)?.Value;
                    identity.AddClaim(new Claim("email", email ?? string.Empty));
                    identity.AddClaim(new Claim("name", name ?? string.Empty));
                    // Save tokens for later API calls
                    var tokens = JsonSerializer.Serialize(context.Properties.GetTokens());
                    identity.AddClaim(new Claim("tokens", tokens));
                };
            })
            .AddDropbox(options =>
            {
                options.ClientId = "Your-Dropbox-Client-Id";
                options.ClientSecret = "Your-Dropbox-Client-Secret";
                options.SaveTokens = true;
                options.Events.OnCreatingTicket = async context =>
                {
                    var identity = context.Principal.Identity as ClaimsIdentity;
                    // Dropbox doesn't return email in default scopes, so fetch additional data if needed
                    var userInfoResponse = await context.Backchannel.GetAsync("https://api.dropboxapi.com/2/users/get_current_account");
                    if (userInfoResponse.IsSuccessStatusCode)
                    {
                        var userInfo = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync());
                        var email = userInfo.RootElement.GetProperty("email").GetString();
                        var name = userInfo.RootElement.GetProperty("name").GetProperty("display_name").GetString();
                        identity.AddClaim(new Claim("email", email ?? string.Empty));
                        identity.AddClaim(new Claim("name", name ?? string.Empty));
                    }
                };
            })
            .AddOpenIdConnect("iCloud", options =>
            {
                options.Authority = "https://appleid.apple.com";
                options.ClientId = "Your-Apple-Client-Id";
                options.ClientSecret = "Your-Apple-Client-Secret"; // Use JWT-based client secret as per Apple guidelines
                options.ResponseType = "code";
                options.Scope.Add("email");
                options.Scope.Add("name");
                options.SaveTokens = true;
                options.Events.OnTokenValidated = context =>
                {
                    var identity = context.Principal.Identity as ClaimsIdentity;
                    var email = context.Principal.FindFirst(ClaimTypes.Email)?.Value;
                    var name = context.Principal.FindFirst("name")?.Value;
                    identity.AddClaim(new Claim("email", email ?? string.Empty));
                    identity.AddClaim(new Claim("name", name ?? string.Empty));
                    return Task.CompletedTask;
                };
            });
            return services;
        }
    }

---------------------------------------------------------------------------------------------------

setup Authentication

    <!--<script src="_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js"></script>-->
    @* <CascadingAuthenticationState> *@
    @* </CascadingAuthenticationState> *@
    move LoginDisplay / @NavBarFragment.GetNavBarFragment() to Backup
    appsettings.json
    appsettings.Development.json

https://github.com/openiddict/openiddict-core

https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers

Backup
    Google Drive https://www.nuget.org/packages/Google.Apis.Drive.v3
    OneDrive https://www.nuget.org/packages/Microsoft.Graph
    NO! --- iCloud --- https://github.com/gachris/iCloud.Dav --- NO! --- only official support is for Swift and Objective-C
    Dropbox https://www.nuget.org/packages/Dropbox.Api
        WASM authorisation - REST
        desktop authorisation - OpenHabitTracker.Google.Apis - using Google.Apis.Auth.OAuth2;
        mobile authorisation - `ASP.NET Core`

Box
    https://www.nuget.org/packages/Box.Sdk.Gen
Nextcloud
    https://www.nuget.org/packages/NextcloudApi
NO! --- ownCloud --- NO! --- proprietary features in enterprise version
    https://www.nuget.org/packages/bnoffer.owncloudsharp

Blazor Server / Web
    `ASP.NET Core`
    SQL Server
    version history: https://learn.microsoft.com/en-us/ef/core/providers/sql-server/temporal-tables
    table Users
    column UserId in every other table
    EF Core: use `DbContextFactory`

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

when all habit items are done, habit is done
when all task items are done, task is done

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

read Settings from DB before Run() - !!! Transient / Scoped / Singleton !!! - Scoped instances before and after Run() are not the same

unify into one property ??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ???

---------------------------------------------------------------------------------------------------

easy for AI ?

common `Router`
    OpenHabitTracker.Blazor - Routes.razor
    OpenHabitTracker.Blazor.Wasm - App.razor - CascadingAuthenticationState, AuthorizeRouteView, NotAuthorized

easy for AI ?

OpenHabitTracker.Blazor.Server:
    - @page "/Error"
    - app.UseExceptionHandler("/Error");

---------------------------------------------------------------------------------------------------

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

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

copy Loop Habit Tracker
- History (done count grouped by week, month, quarter, year)
- Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
- Best streaks (from date - to date)
- Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

---------------------------------------------------------------------------------------------------

ASAP tasks: when, where, contact/company name, address, phone number, working hours, website, email

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

---------------------------------------------------------------------------------------------------

virtualized container

benchmark: method time & render time
method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance

---------------------------------------------------------------------------------------------------

bUnit - Always invisible (in-memory, no browser/device)
Playwright - Configurable — headless (invisible) or headed (visible)
Appium - Always visible — real device or emulator screen

write Appium integration tests:

The only things Appium could uniquely cover that Playwright can't:
    App launch on device — does the MAUI shell start without crashing on Android/iOS/Windows?
    Android back button — does pressing the hardware back button behave correctly (navigate back vs. exit)?
    iOS swipe-back gesture — does swiping from the left edge navigate back?
    App lifecycle — does the app resume correctly after being backgrounded (e.g., data not lost after switching apps)?
    Permissions dialogs — if you ever add camera/storage/notification permissions, Appium can tap "Allow" on the native OS dialog.

https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/uitest-appium-nunit/
https://github.com/dotnet/maui-samples/tree/main/8.0/UITesting/BasicAppiumNunitSample

https://devblogs.microsoft.com/dotnet/dotnet-maui-ui-testing-appium/

---------------------------------------------------------------------------------------------------

accessibility: Silent operations give no screen reader feedback (WCAG 4.1.3):
    - note save, habit marked done, item deleted — screen reader users hear nothing
    - success feedback: aria-live="polite" (role="status") region in Main.razor, write brief status text after operations
    - error feedback: role="alert" (implies aria-live="assertive") for validation errors — interrupts immediately
    PLAN:
    Step A — shared StatusService (OpenHabitTracker.Blazor/StatusService.cs):
    - Add a Scoped service: public class StatusService { public string Message { get; private set; } public event Action? OnChange; public void Set(string msg) { Message = msg; OnChange?.Invoke(); } public void Clear() { Message = string.Empty; OnChange?.Invoke(); } }
    - Register in DI: builder.Services.AddScoped<StatusService>();
    Step B — live region in Main.razor:
    - Add <div role="status" aria-live="polite" aria-atomic="true" class="visually-hidden">@StatusService.Message</div> at the bottom of the layout (inside <main> or just before </body>)
    - Subscribe to StatusService.OnChange in OnInitialized; call StateHasChanged in the handler
    - Auto-clear after 3 seconds: use a CancellationTokenSource, cancel previous timer before starting a new one
    Step C — call StatusService.Set() after each silent operation:
    - HabitComponent.razor: after MarkAsDone → StatusService.Set(Loc["Habit marked as done"])
    - NoteComponent.razor: after Save → StatusService.Set(Loc["Note saved"])
    - HabitComponent/NoteComponent/TaskComponent.razor: after Delete → StatusService.Set(Loc["Item deleted"])
    - ItemsComponent.razor: after item checkbox toggled → StatusService.Set(Loc["Item checked"] / Loc["Item unchecked"])
    Step D — validation errors (role="alert"):
    - Where form validation messages are shown, wrap in <div role="alert">...</div> (role="alert" implies aria-live="assertive" so no extra attribute needed)
    - Existing ValidationMessage components can be wrapped; no changes to the validation logic itself

---------------------------------------------------------------------------------------------------

add comments to methods - 1. for any open source contributor - 2. for GitHub Copilot

deploy Blazor Server Docker image to Raspberry Pi 5 / Synology NAS DS224+

---------------------------------------------------------------------------------------------------

what is wrong: I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them
