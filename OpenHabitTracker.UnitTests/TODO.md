# TODO:

## Unit Test Plan (NUnit + bUnit)

### Phase 0: Prerequisites — must be done before any test can compile

#### 0a. Extract service interfaces in OpenHabitTracker/Services/
Required for bUnit component tests — components must inject by interface so tests can substitute mocks.
Create one interface per service, exposing all public members:
- `IHabitService` 
- `INoteService`  
- `ITaskService`  
- `ICategoryService` 
- `IPriorityService` 
- `IItemService` 
- `ICalendarService` 
- `ITrashService` 
- `ISearchFilterService` 
- `IJsInterop` 

One interface per file, placed alongside the concrete class (`OpenHabitTracker/Services/IHabitService.cs` next to `HabitService.cs`, etc.).

Classes that do NOT need interfaces (verified from @inject scan):
- `ClientState` — injected almost everywhere; register real `ClientState` with mock `IDataAccess` in bUnit `TestContext.Services`
- `MarkdownToHtml` — injected in `NoteComponent`; register real instance (takes `MarkdownPipeline`, no side effects)
- `RemoteDataSync` — only in `Main.razor` layout, not in planned component tests
- `Examples` — only in `Data.razor` and `Main.razor`, not in planned component tests
- `ImportExportService` — only in `Backup.razor`, not in planned component tests

#### 0b. Update Startup.cs DI registrations
Change every `services.AddScoped<XService>()` to `services.AddScoped<IXService, XService>()`.
Also change `services.AddScoped<JsInterop>()` to `services.AddScoped<IJsInterop, JsInterop>()` in `OpenHabitTracker.Blazor/Startup.cs`.
This allows both Blazor apps and bUnit `TestContext.Services` to register mock implementations.

#### 0c. Update @inject in Blazor components
Replace concrete types with interface types in every component that injects a service or JsInterop:
- `HabitComponent.razor`, `Habits.razor` → `@inject IHabitService`
- `NoteComponent.razor`, `Notes.razor` → `@inject INoteService`
- `TaskComponent.razor`, `Tasks.razor` → `@inject ITaskService`
- `CategoryComponent.razor`, `Categories.razor` → `@inject ICategoryService`
- `CalendarComponent.razor` → `@inject ICalendarService`
- `NoteComponent.razor`, `Notes.razor`, `Habits.razor`, `Settings.razor`, `Main.razor` → `@inject IJsInterop`
- `ItemsComponent.razor`, `Search.razor` → `@inject ISearchFilterService`
- `Trash.razor` → `@inject ITrashService`
- `ItemsComponent.razor` → `@inject IItemService`
- `PriorityComponent.razor`, `Tasks.razor`, `Notes.razor`, `Habits.razor`, `Priorities.razor` → `@inject IPriorityService`

---

### Phase 1: NUnit service tests — pure C#, no browser, no renderer

Strategy: mock `IDataAccess` with NSubstitute → build real `ClientState` → pre-populate
`ClientState.Habits` / `Notes` / `Tasks` with in-memory data → call service method → assert.

Add file: OpenHabitTracker.UnitTests/TestData.cs — static factory to keep test construction DRY:

    internal static class TestData
    {
        internal static HabitModel Habit(long id = 1, string title = "Test", bool isDeleted = false,
            Priority priority = Priority.None, long categoryId = 0) =>
            new() { Id = id, Title = title, IsDeleted = isDeleted, Priority = priority, CategoryId = categoryId };
        internal static NoteModel Note(long id = 1, string title = "Test", string content = "", bool isDeleted = false) =>
            new() { Id = id, Title = title, Content = content, IsDeleted = isDeleted };
        internal static TaskModel Task(long id = 1, string title = "Test", bool isDeleted = false,
            DateTime? completedAt = null, DateTime? plannedAt = null) =>
            new() { Id = id, Title = title, IsDeleted = isDeleted, CompletedAt = completedAt, PlannedAt = plannedAt };
        internal static Dictionary<long, HabitModel> HabitDict(params HabitModel[] habits) =>
            habits.ToDictionary(h => h.Id);
        internal static Dictionary<long, NoteModel> NoteDict(params NoteModel[] notes) =>
            notes.ToDictionary(n => n.Id);
        internal static Dictionary<long, TaskModel> TaskDict(params TaskModel[] tasks) =>
            tasks.ToDictionary(t => t.Id);
        internal static CategoryModel Category(long id = 1, string title = "Test",
            List<NoteModel>? notes = null, List<TaskModel>? tasks = null, List<HabitModel>? habits = null) =>
            new() { Id = id, Title = title, Notes = notes, Tasks = tasks, Habits = habits };
        internal static Dictionary<long, CategoryModel> CategoryDict(params CategoryModel[] categories) =>
            categories.ToDictionary(c => c.Id);
    }

Verified: `clientState.Habits`, `clientState.Notes`, `clientState.Tasks` all have public setters
(delegate properties to `ClientData`) — direct assignment works as shown below.

SetUp boilerplate (shared across test classes):

    IDataAccess dataAccess = Substitute.For<IDataAccess>();
    dataAccess.DataLocation.Returns(DataLocation.Local);
    dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));
    MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    MarkdownToHtml markdownToHtml = new(pipeline);
    ClientState clientState = new(new[] { dataAccess }, markdownToHtml);

SettingsModel pitfalls — verified from source:
- `new SettingsModel()` defaults: `PriorityFilterDisplay = CheckBoxes`, all `ShowPriority = true`,
  `HiddenCategoryIds = []` — no items are actually filtered out. Safe default for most tests.
- `HideCompletedTasks = true` by default — completed tasks are HIDDEN in `GetTasks()`.
  Tests using tasks with `CompletedAt` set must explicitly set `clientState.Settings.HideCompletedTasks = false`.
- Default sort: habits → `Sort.SelectedRatio`, tasks → `Sort.PlannedAt`, notes → `Sort.Priority`.
  Tests asserting specific order must set `clientState.Settings.SortBy[ContentType.X]` explicitly.

NSubstitute null-return trap — any service method that does:

    if (await _clientState.DataAccess.GetHabit(id) is HabitEntity entity) { /* write-back */ }

... silently skips the write-back because NSubstitute returns null by default for `Task<T?>`.
Tests verifying DB write-backs must set up the return explicitly:

    dataAccess.GetHabit(habit.Id).Returns(Task.FromResult<HabitEntity?>(new HabitEntity { Id = habit.Id }));

Affected: MarkAsDone (GetTime, GetHabit), DeleteHabit (GetHabit), UpdateHabit (GetHabit),
Start/SetStartTime (GetTime/GetTask), DeleteNote (GetNote), DeleteTask (GetTask),
UpdateCategory (GetCategory), DeleteCategory (UpdateModel → GetHabit/GetNote/GetTask).
NOT affected: AddHabit/AddNote/AddTask (no Get* call before Add*), GetHabits/GetNotes/GetTasks (pure in-memory).

#### File: OpenHabitTracker.UnitTests/Services/HabitServiceTests.cs

    SearchFilterService searchFilterService = new();
    HabitService sut = new(clientState, searchFilterService);
    clientState.Habits = /* Dictionary<long, HabitModel> with test data */;

Tests:
- `GetHabits_NoFilter_ReturnsAllNonDeleted`
- `GetHabits_DeletedHabit_IsExcluded`
- `GetHabits_PriorityFilter_CheckBoxes_ExcludesHiddenPriority`
- `GetHabits_PriorityFilter_Dropdown_ReturnsOnlyMatchingPriority`
- `GetHabits_SearchTerm_CaseInsensitive_MatchesTitle`
- `GetHabits_SearchTerm_CaseSensitive_NoMatchOnDifferentCase`
- `GetHabits_SearchTerm_MatchesItemTitle`
- `GetHabits_DoneAtFilter_Before_ExcludesHabitsWithNoDoneBeforeDate`
- `GetHabits_DoneAtFilter_On_ReturnsHabitsDoneOnDate`
- `GetHabits_DoneAtFilter_After_ReturnsHabitsDoneAfterDate`
- `GetHabits_DoneAtFilter_NotOn_ExcludesHabitsDoneOnDate`
- `GetHabits_CategoryFilter_CheckBoxes_ExcludesHiddenCategory`
- `GetHabits_CategoryFilter_Dropdown_ReturnsOnlyMatchingCategory`
- `GetHabits_RatioFilter_ExcludesBelowMin`
- `GetHabits_SortByTitle_ReturnsAlphabetically`
- `GetHabits_SortByPriority_ReturnsHighestFirst`
- `AddHabit_SetsCreatedAtAndUpdatedAt`
- `AddHabit_AddsHabitToClientStateDictionary`
- `AddHabit_WhenNewHabitIsNull_DoesNothing`
- `AddHabit_ClearsNewHabitAfterAdd`
- `MarkAsDone_WhenLastTimeInProgress_CompletesIt_WithNow`
- `MarkAsDone_WhenLastTimeDone_AddsNewEntry`
- `MarkAsDone_WhenUncheckAllItemsOnHabitDoneEnabled_UnchecksAllItems`
- `Start_WhenNoTimeInProgress_AddsNewEntry`
- `Start_WhenLastTimeAlreadyInProgress_DoesNotAddSecondEntry`
- `DeleteHabit_SetsIsDeletedTrue`
- `DeleteHabit_AddsToTrash_WhenTrashIsAlreadyLoaded`
- `RemoveTimeDone_RemovesFromTimesDoneList`
- `RemoveTimeDone_UpdatesLastTimeDoneAtToMostRecentRemaining`

#### File: OpenHabitTracker.UnitTests/Services/NoteServiceTests.cs

    NoteService sut = new(clientState, searchFilterService, markdownToHtml);
    clientState.Notes = /* Dictionary<long, NoteModel> with test data */;

Tests:
- `GetNotes_NoFilter_ReturnsAllNonDeleted`
- `GetNotes_DeletedNote_IsExcluded`
- `GetNotes_PriorityFilter_CheckBoxes_ExcludesHiddenPriority`
- `GetNotes_PriorityFilter_Dropdown_ReturnsOnlyMatchingPriority`
- `GetNotes_SearchTerm_MatchesTitle`
- `GetNotes_SearchTerm_MatchesContent` — notes search Content field, not Items
- `GetNotes_CategoryFilter_CheckBoxes_ExcludesHiddenCategory`
- `GetNotes_CategoryFilter_Dropdown_ReturnsOnlyMatchingCategory`
- `GetNotes_SortByTitle_ReturnsAlphabetically`
- `GetNotes_SortByPriority_ReturnsHighestFirst`
- `AddNote_SetsTimestamps_AndAddsToClientState`
- `AddNote_SetsContentMarkdown_ViaMarkdownToHtml`
- `AddNote_ClearsNewNoteAfterAdd`
- `DeleteNote_SetsIsDeletedTrue`
- `DeleteNote_AddsToTrash_WhenTrashIsAlreadyLoaded`

#### File: OpenHabitTracker.UnitTests/Services/TaskServiceTests.cs

    TaskService sut = new(clientState, searchFilterService);
    clientState.Tasks = /* Dictionary<long, TaskModel> with test data */;
    clientState.Settings = new SettingsModel { HideCompletedTasks = false }; // override dangerous default

Note: TaskService.DoneAtFilter applies to task.CompletedAt directly (not TimesDone like HabitService).
Note: TaskService.MarkAsDone() TOGGLES — if already done (CompletedAt != null) it clears it back to null.
Note: HideCompletedTasks = true by default — most GetTasks tests must set it to false.

Tests:
- `GetTasks_NoFilter_ReturnsAllNonDeleted`
- `GetTasks_DeletedTask_IsExcluded`
- `GetTasks_HideCompletedTasks_True_ExcludesTasksWithCompletedAt` — unique to TaskService
- `GetTasks_HideCompletedTasks_False_IncludesCompletedTasks`
- `GetTasks_PriorityFilter_CheckBoxes_ExcludesHiddenPriority`
- `GetTasks_PriorityFilter_Dropdown_ReturnsOnlyMatchingPriority`
- `GetTasks_CategoryFilter_CheckBoxes_ExcludesHiddenCategory`
- `GetTasks_CategoryFilter_Dropdown_ReturnsOnlyMatchingCategory`
- `GetTasks_SearchTerm_MatchesTitle`
- `GetTasks_SearchTerm_MatchesItemTitle`
- `GetTasks_DoneAtFilter_Before` — filters on task.CompletedAt, not TimesDone
- `GetTasks_DoneAtFilter_On`
- `GetTasks_DoneAtFilter_After`
- `GetTasks_DoneAtFilter_NotOn`
- `GetTasks_PlannedAtFilter_Before`
- `GetTasks_PlannedAtFilter_On`
- `GetTasks_PlannedAtFilter_After`
- `GetTasks_PlannedAtFilter_NotOn`
- `GetTasks_SortByTitle_ReturnsAlphabetically`
- `GetTasks_SortByPlannedAt_ReturnsEarliestFirst`
- `AddTask_SetsTimestamps_AndAddsToClientState`
- `DeleteTask_SetsIsDeletedTrue_AndAddsToTrash`
- `Start_SetsStartedAt_AndClearsCompletedAt` — needs dataAccess.GetTask(...).Returns(...)
- `MarkAsDone_WhenNotDone_SetsCompletedAt` — needs dataAccess.GetTask(...).Returns(...)
- `MarkAsDone_WhenAlreadyDone_ClearsCompletedAt` — toggle behavior, needs GetTask setup
- `MarkAsDone_WhenNotDone_SetsAllItemsDoneAt` — needs dataAccess.GetItem(...).Returns(...)

#### File: OpenHabitTracker.UnitTests/Services/CategoryServiceTests.cs

    CategoryService sut = new(clientState);
    clientState.Categories = /* Dictionary<long, CategoryModel> with test data */;

CRITICAL: `DeleteCategory` reads `category.Notes`, `category.Tasks`, `category.Habits` from the
passed `CategoryModel` object — it does NOT look them up from `clientState.Notes/Tasks/Habits`.
Cascade tests must populate these navigation lists on the CategoryModel itself, e.g.:
    NoteModel note = TestData.Note();
    CategoryModel category = TestData.Category(id: 1, notes: [note]);
    clientState.Categories = TestData.CategoryDict(category);

Tests:
- `GetCategoryTitle_WhenFound_ReturnsTitle`
- `GetCategoryTitle_WhenNotFound_ReturnsIdAsString`
- `AddCategory_AddsToClientStateDictionary`
- `AddCategory_ResetsNewCategoryAfterAdd`
- `UpdateCategory_ChangesTitle`
- `UpdateCategory_ClearsSelectedCategory`
- `DeleteCategory_CascadesIsDeleted_AndCategoryId0_ToNotes`
- `DeleteCategory_CascadesIsDeleted_AndCategoryId0_ToTasks`
- `DeleteCategory_CascadesIsDeleted_AndCategoryId0_ToHabits`
- `DeleteCategory_RemovesFromClientStateDictionary`
- `DeleteCategory_RemovesIdFromHiddenCategoryIds_WhenPresent`
- `DeleteCategory_DoesNotCallUpdateSettings_WhenIdNotInHiddenList`

#### File: OpenHabitTracker.UnitTests/App/ClientStateTests.cs

Tests:
- `LoadHabits_CallsGetHabits_ExactlyOnce_EvenWhenCalledTwice`
- `LoadNotes_CallsGetNotes_ExactlyOnce_EvenWhenCalledTwice`
- `LoadTasks_CallsGetTasks_ExactlyOnce_EvenWhenCalledTwice`
- `LoadHabits_AssignsTimes_ToEachHabit_ByHabitId`
- `LoadPriorities_WhenNoneExist_AddsDefaultFivePriorities`
- `RefreshState_ClearsHabits_Notes_Tasks_Times_Items_Categories_Priorities_Trash`

---

### Phase 2: bUnit component tests — Blazor rendering (requires Phase 0c–0e complete)

Strategy: use `bunit.TestContext`, register mock `IXService` via `ctx.Services.AddScoped`,
render the component, find elements by CSS selector or text content, assert DOM and invoke events.

Note: `IGTourService` is from the `GTour` NuGet package (`GTour.Abstractions` namespace),
registered in `OpenHabitTracker.Blazor/Startup.cs` via `services.UseGTour()`.
Available transitively via the Phase 0a project reference to `OpenHabitTracker.Blazor` — no extra NuGet needed.
Add `@using GTour.Abstractions` (or a global using) in test files that reference `IGTourService`.

#### File: OpenHabitTracker.UnitTests/Components/HabitComponentTests.cs

HabitComponent injects: IHabitService, ClientState, IStringLocalizer, IGTourService

    IHabitService habitService = Substitute.For<IHabitService>();
    IDataAccess dataAccess = Substitute.For<IDataAccess>();
    dataAccess.DataLocation.Returns(DataLocation.Local);
    ClientState clientState = new(new[] { dataAccess }, markdownToHtml);
    ctx.Services.AddScoped(_ => habitService);
    ctx.Services.AddScoped(_ => clientState);
    ctx.Services.AddSingleton(Substitute.For<IStringLocalizer>());
    ctx.Services.AddSingleton(Substitute.For<IGTourService>());
    IRenderedComponent<HabitComponent> cut = ctx.RenderComponent<HabitComponent>(
        parameters => parameters.Add(p => p.Habit, testHabit));

Tests:
- `Renders_HabitTitle_InExpectedElement`
- `MarkAsDone_ButtonClick_InvokesHabitServiceMarkAsDone`
- `StartTimer_ButtonClick_InvokesHabitServiceStart`
- `DeleteButton_Click_InvokesHabitServiceDeleteHabit`

#### File: OpenHabitTracker.UnitTests/Components/NoteComponentTests.cs

NoteComponent injects: INoteService, IJsInterop, ClientState, IStringLocalizer, MarkdownToHtml, IGTourService
MarkdownToHtml is registered as a real instance (no interface needed — no JS calls, pure string transformation).

    INoteService noteService = Substitute.For<INoteService>();
    IJsInterop jsInterop = Substitute.For<IJsInterop>();
    IDataAccess dataAccess = Substitute.For<IDataAccess>();
    dataAccess.DataLocation.Returns(DataLocation.Local);
    ClientState clientState = new(new[] { dataAccess }, markdownToHtml);
    ctx.Services.AddScoped(_ => noteService);
    ctx.Services.AddScoped(_ => jsInterop);
    ctx.Services.AddScoped(_ => clientState);
    ctx.Services.AddScoped(_ => markdownToHtml); // real instance
    ctx.Services.AddSingleton(Substitute.For<IStringLocalizer>());
    ctx.Services.AddSingleton(Substitute.For<IGTourService>());

Tests:
- `Renders_NoteTitle`
- `Renders_ContentMarkdown_AsHtml_NotRawText`
- `EditButton_Click_ShowsEditForm`
- `DeleteButton_Click_InvokesNoteServiceDeleteNote`

#### File: OpenHabitTracker.UnitTests/Components/CalendarComponentTests.cs

CalendarComponent injects: ICalendarService, IHabitService, ClientState, IStringLocalizer

    ctx.Services.AddScoped(_ => Substitute.For<ICalendarService>());
    ctx.Services.AddScoped(_ => Substitute.For<IHabitService>());
    ctx.Services.AddScoped(_ => clientState);
    ctx.Services.AddSingleton(Substitute.For<IStringLocalizer>());

Tests:
- `WeekView_Renders_SevenDayCells`
- `NextWeek_ButtonClick_AdvancesCalendarBySevenDays`
- `PreviousWeek_ButtonClick_MovesCalendarBackSevenDays`

---

### Execution order

3. **Phase 0a** — extract service interfaces
4. **Phase 0b** — update Startup.cs registrations
5. **Phase 0c** — update @inject in Razor components
6. **Phase 1** — write NUnit service tests (can proceed service by service)
7. **Phase 2** — write bUnit component tests (requires Phases 0c–0e)
