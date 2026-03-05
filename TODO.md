# TODO:

write unit tests: https://bunit.dev/ https://github.com/bUnit-dev/bUnit

https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/uitest-appium-nunit/
https://github.com/dotnet/maui-samples/tree/main/8.0/UITesting/BasicAppiumNunitSample

https://devblogs.microsoft.com/dotnet/dotnet-maui-ui-testing-appium/
https://github.com/jfversluis/Template.Maui.UITesting

---------------------------------------------------------------------------------------------------

find out why `padding-left: 12px !important;` is needed on iOS - try: `padding-left: env(safe-area-inset-left) !important;`

---------------------------------------------------------------------------------------------------

    fix AppData GetUserData() which calls InitializeContent()
    search for `// TODO:: remove temp fix`
        `InitializeItems` and `InitializeTimes` have null checks and do not update data when called in GetUserData()
            both load data directly from DB with `_dataAccess.GetTimes()` and `_dataAccess.GetItems()`
            but HabitService.LoadTimesDone also loads data with `_dataAccess.GetTimes(habit.Id)` - these are not the same objects as in `InitializeTimes`
            and ItemService.Initialize also loads data with `_dataAccess.GetItems(items.Id)` - these are not the same objects as in `InitializeItems`
        user can add or remove Items and Times list
            `DataAccess.AddItem(item);` / `DataAccess.UpdateItem(item);`
            `DataAccess.AddTime(timeEntity);` / `DataAccess.UpdateTime(timeEntity);`
            the code does not update Items and Times in the AppData
        so without temp fix, GetUserData() would return Items and Times that were loaded with Initialize()
    NO!!! - either remove these from class AppData: - NO!!!
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

this is a big problem - services use `_dataAccess` on their own, but `AppData` is supposed to represent the current state - as the only source of truth
Ididit did not have this problem, `Repository` was the only class with `IDatabaseAccess` and represented the current state

---------------------------------------------------------------------------------------------------

- [ ] `HabitModel` + `TaskModel` — extract identical `Duration`, `DurationProxy`, `DurationHour`, `DurationMinute` into a shared base class (e.g. `DurationModel : ItemsModel`)
- [ ] `TrashService.RestoreAll()` — replace duplicated type-switch with a loop calling `Restore(model)` (use `.ToList()` to snapshot before iterating)
- [ ] Priority + Category filter blocks — extract to extension methods on `IEnumerable<ContentModel>`; currently repeated 6× across `HabitService`, `NoteService`, `TaskService`, `ClientData`
- [ ] `CalendarParams.SetCalendarStartToNextWeek` + `SetCalendarStartToPreviousWeek` — extract to a private `ShiftCalendarByWeek(int days)` helper; only difference is `+7` vs `-7`

---------------------------------------------------------------------------------------------------

write unit tests:

Extract interfaces: INoteService, ITaskService, IHabitService, IPriorityService, ICategoryService, IItemService, ICalendarService, ITrashService

    OpenHabitTracker.UnitTests - general test coverage (bUnit component tests, Appium native app tests)

    why NUnit over xUnit?
    - NUnit has [SetUp] / [TearDown] - fit for browser (Playwright) and device (Appium) lifecycle
    - xUnit has better native DI 
        - more useful for ASP.NET integration tests
        - not relevant here because bUnit has its own DI via TestContext.Services
    - NUnit has more built-in assertions than xUnit - no need for FluentAssertions or Shouldly

    why not just use Playwright for everything?
    - Playwright = real browser, full app, slow (seconds per test)
    - bUnit = renders one Blazor component in isolation, no browser, milliseconds per test
    - Appium = controls the native MAUI app on iOS/Android/Windows - Playwright cannot do this

    use bUnit when a behavior is too deep or tedious to reach through the browser UI
    use Appium when you need to verify the native app specifically (platform behavior)

---------------------------------------------------------------------------------------------------

## Unit Test Plan (NUnit + bUnit)

### Phase 0: Prerequisites — must be done before any test can compile

#### 0a. Add project references to OpenHabitTracker.UnitTests.csproj
- Add `<ProjectReference Include="..\OpenHabitTracker\OpenHabitTracker.csproj" />`
- Add `<ProjectReference Include="..\OpenHabitTracker.Blazor\OpenHabitTracker.Blazor.csproj" />`
  (needed for bUnit component rendering tests)

#### 0b. Add NSubstitute to OpenHabitTracker.UnitTests.csproj
- Add `<PackageReference Include="NSubstitute" Version="5.3.0" />`
- Add the following (catches misuse at compile time — `IncludeAssets` makes it analyzer-only, `PrivateAssets` hides it from consumers):
  ```xml
  <PackageReference Include="NSubstitute.Analyzers.CSharp" Version="1.0.17">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  ```
- Used to mock `IDataAccess` (already an interface) without a real database
- `MarkdownToHtml` does not need mocking — instantiate directly with a real `MarkdownPipeline`

#### 0c. Extract service interfaces in OpenHabitTracker/Services/
Required for bUnit component tests — components must inject by interface so tests can substitute mocks.
Create one interface per service, exposing all public members:
- `IHabitService` — `GetHabits()`, `AddHabit()`, `UpdateHabit()`, `DeleteHabit()`, `MarkAsDone()`, `Start()`, `SetStartTime()`, `AddTimeDone()`, `RemoveTimeDone()`, `UpdateTimeDone()`, `LoadTimesDone()`, `SetSelectedHabit()`, `Initialize()`, `Habits`, `SelectedHabit`, `NewHabit`
- `INoteService`  — `GetNotes()`, `AddNote()`, `UpdateNote()`, `DeleteNote()`, `SetSelectedNote()`, `Initialize()`, `Notes`, `SelectedNote`, `NewNote`
- `ITaskService`  — `GetTasks()`, `AddTask()`, `UpdateTask()`, `DeleteTask()`, `Start()`, `SetStartTime()`, `MarkAsDone()`, `SetSelectedTask()`, `Initialize()`, `Tasks`, `SelectedTask`, `NewTask`
- `ICategoryService` — `GetCategoryTitle()`, `AddCategory()`, `UpdateCategory()`, `DeleteCategory()`, `SetSelectedCategory()`, `Initialize()`, `Categories`, `SelectedCategory`, `NewCategory`
- `IPriorityService` — expose full public surface
- `IItemService` — expose full public surface
- `ICalendarService` — expose full public surface
- `ITrashService` — expose full public surface
- `ISearchFilterService` — `SearchTerm`, `MatchCase`, `DoneAtFilter`, `DoneAtCompare`, `PlannedAtFilter`, `PlannedAtCompare`, `MarkSearchResults(string text)`, `MarkSearchResultsInHtml(string text)`
- `IJsInterop` — `ConsoleLog()`, `SetMode()`, `SetLang()`, `SetTheme()`, `FocusElement()`, `SetElementProperty()`, `GetElementProperty<T>()`, `GetWindowDimensions()`, `GetElementDimensions()`, `SaveAsUTF8()`, `SetCalculateAutoHeight()`, `HandleTabKey()`
  File: `OpenHabitTracker.Blazor/IJsInterop.cs` (different project — `JsInterop` is `sealed` and in `OpenHabitTracker.Blazor/`)
  `JsInterop` must also implement `IAsyncDisposable` — add `: IJsInterop, IAsyncDisposable` to the class declaration

One interface per file, placed alongside the concrete class (`OpenHabitTracker/Services/IHabitService.cs` next to `HabitService.cs`, etc.).

Classes that do NOT need interfaces (verified from @inject scan):
- `ClientState` — injected almost everywhere; register real `ClientState` with mock `IDataAccess` in bUnit `TestContext.Services`
- `MarkdownToHtml` — injected in `NoteComponent`; register real instance (takes `MarkdownPipeline`, no side effects)
- `RemoteDataSync` — only in `Main.razor` layout, not in planned component tests
- `Examples` — only in `Data.razor` and `Main.razor`, not in planned component tests
- `ImportExportService` — only in `Backup.razor`, not in planned component tests

#### 0d. Update Startup.cs DI registrations
Change every `services.AddScoped<XService>()` to `services.AddScoped<IXService, XService>()`.
Also change `services.AddScoped<JsInterop>()` to `services.AddScoped<IJsInterop, JsInterop>()` in `OpenHabitTracker.Blazor/Startup.cs`.
This allows both Blazor apps and bUnit `TestContext.Services` to register mock implementations.

#### 0e. Update @inject in Blazor components
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

1. **Phase 0a** — add project references (compile gate for everything else)
2. **Phase 0b** — add NSubstitute package
3. **Phase 0c** — extract service interfaces
4. **Phase 0d** — update Startup.cs registrations
5. **Phase 0e** — update @inject in Razor components
6. **Phase 1** — write NUnit service tests (can proceed service by service)
7. **Phase 2** — write bUnit component tests (requires Phases 0c–0e)

---------------------------------------------------------------------------------------------------

accessibility:

    1. Arrow keys for Menu sidebar (ARIA menu pattern):
       - Tab enters the menu, Up/Down arrows move between items, Tab exits

    2. Silent operations give no screen reader feedback (WCAG 4.1.3):
       - note save, habit marked done, item deleted — screen reader users hear nothing
       - success feedback: aria-live="polite" (role="status") region in Main.razor, write brief status text after operations
       - error feedback: role="alert" (implies aria-live="assertive") for validation errors — interrupts immediately

    3. Focus management (currently missing):
       - sidebar opens → move focus to first element inside sidebar
       - sidebar closes → return focus to the button that opened it (menu or search)
       - note/task/habit edit closes → return focus to the list item that was opened

    4. Calendar arrow key navigation (roving tabindex):
       - currently Tab through every day cell (up to 42 presses for month view)
       - only one cell has tabindex="0" at a time, arrow keys move between cells, Tab exits grid
        Home/End in calendar grid:
       - Home → first day of the week, End → last day of the week
        Page Up/Page Down in calendar:
       - Page Up → previous month, Page Down → next month
       - add `role="grid"` / `role="row"` / `role="gridcell"` / `role="columnheader"` to grid divs

---------------------------------------------------------------------------------------------------

add French, Portuguese, Italian, Japanese in OpenHabitTracker\OpenHabitTracker\Localization\Resources

---------------------------------------------------------------------------------------------------

1.
desktop: https://youtu.be/qsC7lX3yZ-A
sidebar: https://youtu.be/dq1OmpjBBNk
mobile:  https://youtu.be/zYAg-PXe7FI https://youtube.com/shorts/zYAg-PXe7FI

SUBMIT DESKTOP VIDEO (1920x1080) TO:
    - Microsoft Store: upload MP4 in Partner Center
        also upload a 1920x1080 PNG "Super Hero Art" image 
        (required for trailer to appear at top of listing)
    - macOS App Store: upload MP4 in App Store Connect
    - Snap Store: paste desktop YouTube URL in Snap developer dashboard

SUBMIT MOBILE VIDEO (886x1920) TO:
    - iOS App Store: upload MP4 in App Store Connect
    - Google Play: paste mobile YouTube URL in Play Console

2.
exact repeating reminders, like Google Keep

3.
drag & drop reorder - manual sort - 1000000 sort index
- sort categories?
- sort items?

---------------------------------------------------------------------------------------------------

4.
add group "and / or" toggle:
- all habits/items done -> green
- one habit/item done -> green

5.
LastDone date: for a group, for the items
- add date to habit item
- add date to category
- all of the above
add settings to show, hide this extra info

6.
This week (xx.xx - yy.yy) statistics 
- x out of y habits done
- x out of y groups are green

---------------------------------------------------------------------------------------------------

7.
upgrade to .NET 10

8.
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
add comments to methods - 1. for any open source contributor - 2. for GitHub Copilot

6.
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

7.
add OAuth to Blazor Wasm, Photino, Wpf, WinForms, Blazor Server, Maui
    Google Drive
    Microsoft OneDrive
    Dropbox
    Box
    Nextcloud

8.
use Google, Microsoft, Dropbox OAuth for unique user id and login

9.
add backup to
    Google Drive
    Microsoft OneDrive
    Dropbox
    Box
    Nextcloud

10.
use DB in Blazor Server for multi user sync with REST API endpoints

---------------------------------------------------------------------------------------------------

11.
write unit tests with Appium / bUnit

12.
Android: get permission to save SQLite DB in an external folder that can be part of Google Drive, OneDrive, iCloud, Dropbox

13.
deploy Blazor Server Docker image to Raspberry Pi 5 / Synology NAS DS224+

14.
Flatpak: test on Raspberry Pi

---------------------------------------------------------------------------------------------------

Android:
    save SQLite DB in an external folder
    can be part of Google Drive, OneDrive, iCloud, Dropbox

AndroidManifest.xml
MANAGE_EXTERNAL_STORAGE

    <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
    <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

---------------------------------------------------------------------------------------------------

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

replace all `@inject AppData AppData` with appropriate services

call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
    save TotalTimeSpent
    save AverageInterval
    on Habit Initialize - load only last week (last X days, displayed in small calendar)
    call LoadTimesDone for large calendar

read Settings from DB before Run() - !!! Transient / Scoped / Singleton !!! - Scoped instances before and after Run() are not the same

unify into one property ??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ???

common `Router`
    OpenHabitTracker.Blazor - Routes.razor
    OpenHabitTracker.Blazor.Wasm - App.razor - CascadingAuthenticationState, AuthorizeRouteView, NotAuthorized

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

keyboard navigation

ASAP tasks: when, where, contact/company name, address, phone number, working hours, website, email

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

virtualized container

benchmark: method time & render time
method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance

---------------------------------------------------------------------------------------------------

what is wrong: I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them
