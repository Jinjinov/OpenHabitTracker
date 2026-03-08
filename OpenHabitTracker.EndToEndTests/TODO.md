# TODO:

## End-to-End Test Plan (NUnit + Microsoft.Playwright)

### Why e2e tests — what bUnit cannot catch

bUnit tests inject mock services and never execute real JS interop. This means a regression like
calling `JsInterop.SetLang()` inside `OnInitializedAsync()` on a Blazor SSR prerender will:
- **PASS in bUnit** — the mock `IJsInterop` silently succeeds.
- **CRASH in e2e** — the real app throws `InvalidOperationException: JavaScript interop calls cannot
  be issued at this time.`

E2e tests run the full app (Blazor Server, Blazor WebAssembly, or Blazor.Web) in a real browser.
They are the only test layer that can catch SSR lifecycle bugs, JS interop misuse, and broken
navigation routing.

Existing tests (`VideoTests.cs`, `LoadExamplesVideoTests.cs`) record marketing videos, not
assertions. They do not fail on bugs — they just record whatever the app shows. The tests planned
here are assertion-based.

---

### Phase 0: Prerequisites — must be done before any assertion test can compile

#### 0a. App must be running at http://localhost before tests execute

The test project has no way to start the app. The developer must start the target app manually
(e.g. `OpenHabitTracker.Blazor.Web`) and keep it running during the test run.

Document this constraint in a `README.md` or at the top of each test fixture:

    // Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
    // dotnet run --project OpenHabitTracker.Blazor.Web --configuration Release

Reason: Playwright tests assert against a live app. Tests that run against a stale build will NOT
detect JS fixes or markup changes made since the last server restart.

#### 0b. Create a shared base class: BaseTest.cs

Extract common setup and helpers used by all functional test fixtures.
The video tests use `PlaywrightTest` directly — do NOT modify them.
New functional tests inherit from `BaseTest`.

    namespace OpenHabitTracker.EndToEndTests;

    public abstract class BaseTest : PlaywrightTest
    {
        protected const string BaseUrl = "http://localhost";

        protected IBrowser Browser = null!;
        protected IBrowserContext Context = null!;
        protected IPage Page = null!;

        [SetUp]
        public async Task BaseSetUp()
        {
            Browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                IgnoreHTTPSErrors = true
            });
            Page = await Context.NewPageAsync();
        }

        [TearDown]
        public async Task BaseTearDown()
        {
            await Context.CloseAsync();
            await Browser.CloseAsync();
        }

        protected async Task GotoAsync(string path = "")
        {
            await Page.GotoAsync(BaseUrl + "/" + path.TrimStart('/'));
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(500); // OnAfterRenderAsync completes after NetworkIdle
        }
    }

Note: `Headless = true` in functional tests (unlike `false` in video tests). No FFmpeg recording.

Note: `WaitForTimeoutAsync(500)` after `NetworkIdle` is needed because Blazor's `OnAfterRenderAsync`
fires client-side after the initial HTTP response — `NetworkIdle` alone does not guarantee it ran.

Note: **localStorage is automatically empty in every test.** `Browser.NewContextAsync()` creates a
fully isolated browser context — no cookies, no localStorage, no session storage carry over from
previous test runs or other tests. This is why the video tests call `LoadExamples` every time.
There is no need for a `ClearAppDataAsync` helper — the fresh `Context` created in `BaseSetUp`
already guarantees a clean state.

#### 0c. Element locator strategy

`data-*` attributes (e.g. `data-main-step-1`) are owned by the GTour guided tour library.
**Do NOT add new `data-*` attributes for tests** — the tour library scans for them and extra
attributes would interfere with tour step discovery.

Tests may reuse existing `data-*` attributes where they already target the right element.
For all other elements, use locators that are already present in the markup, in priority order:

1. `id` — most stable; use when the element is unique on the page (e.g. `#closeSidebar`, `#main-content`)
   - If an element has no `id` but is unique (non-repeating) and suitable for one, **ask the user
     before adding an `id`** — ids are neutral (not owned by any library) and may be added, but
     only with explicit approval.
2. `data-*` tour attribute — stable and language-independent; reuse where it already exists.
   **Never add new `data-*` attributes.**
3. More complex structural locator — when the element repeats and has no id, combine role, CSS
   class, position (`.First`, `.Nth()`), or parent/child relationships to narrow it down
4. `aria-label` — **avoid**: values are localized via `@Loc[...]` and will break if language changes
5. Visible text / `HasText` filter — **avoid for assertions**: also localized and fragile

Existing attributes already usable by tests (do not need to be added):
- `data-main-step-1` — menu toggle button
- `data-main-step-2` — Home nav link
- `data-main-step-3` — Notes nav link
- `data-main-step-4` — Tasks nav link
- `data-main-step-5` — Habits nav link
- `data-main-step-6` — Search toggle button
- `data-notes-step-2` — first note title button
- `data-notes-step-7` — Close button in note detail
- `data-tasks-step-2` — first task title button
- `data-tasks-step-10` — Close button in task detail
- `data-habits-step-2` — first habit title button
- `data-habits-step-11` — Close button in habit detail
- `data-data-step-1` — Load examples button in Data sidebar
- `data-search-step-1` — search input field
- `data-search-step-3` — clear search term button
- `id="closeSidebar"` — sidebar close button

---

### Phase 1: Smoke tests — app starts, no JS errors, pages load

#### File: OpenHabitTracker.EndToEndTests/Smoke/StartupTests.cs

Purpose: Catch SSR lifecycle regressions (JS interop called during prerender) and routing errors.
These tests fail immediately on any of the bugs that unit tests cannot detect.

Strategy: navigate to each page, verify no unhandled JS errors were thrown, and verify a key
landmark element is present in the DOM.

    [TestFixture]
    public class StartupTests : BaseTest
    {
        // Attach a JS error listener before GotoAsync to catch SSR exceptions that surface as
        // unhandled JS errors in the browser console.
        private List<string> _jsErrors = [];

        [SetUp]
        public void AttachJsErrorListener()
        {
            Page.PageError += (_, error) => _jsErrors.Add(error);
        }

        private void AssertNoJsErrors()
        {
            Assert.That(_jsErrors, Is.Empty,
                $"JavaScript errors detected: {string.Join("; ", _jsErrors)}");
        }
    }

Tests:
- `HomePage_Loads_WithoutJsErrors`
  - Navigate to `/`, wait for NetworkIdle
  - Assert no JS page errors
  - Assert `nav[aria-label]` is present (main nav rendered)
- `NotesPage_Loads_WithoutJsErrors`
  - Navigate to `/notes`, assert no JS errors, assert `main#main-content` is visible
- `TasksPage_Loads_WithoutJsErrors`
  - Navigate to `/tasks`, assert no JS errors, assert `main#main-content` is visible
- `HabitsPage_Loads_WithoutJsErrors`
  - Navigate to `/habits`, assert no JS errors, assert `main#main-content` is visible
- `HomePage_AfterReload_StillLoads_WithoutJsErrors`
  - Navigate to `/`, reload, assert no JS errors — catches bugs only triggered on re-render

---

### Phase 2: Navigation tests

#### File: OpenHabitTracker.EndToEndTests/Navigation/NavigationTests.cs

Purpose: Verify nav links route to the correct pages and the URL matches.

    [TestFixture]
    public class NavigationTests : BaseTest
    {
    }

Tests:
- `NotesNavLink_Click_NavigatesToNotesPage`
  - Click `[data-main-step-3]`
  - Assert `Page.Url` ends with `/notes`
  - Assert page heading or landmark for Notes page is visible
- `TasksNavLink_Click_NavigatesToTasksPage`
  - Click `[data-main-step-4]`
  - Assert `Page.Url` ends with `/tasks`
- `HabitsNavLink_Click_NavigatesToHabitsPage`
  - Click `[data-main-step-5]`
  - Assert `Page.Url` ends with `/habits`
- `HomeNavLink_Click_NavigatesToHomePage`
  - Navigate to `/notes`, click `[data-main-step-2]`
  - Assert `Page.Url` ends with `/` (no path)
- `BrowserBack_AfterNavigation_ReturnsToPreviousPage`
  - Navigate to `/notes`, navigate to `/tasks`, click `Page.GoBackAsync()`
  - Assert URL ends with `/notes`

---

### Phase 3: CRUD tests

Strategy: each test gets a fresh empty app state automatically (new `BrowserContext` = empty
localStorage). Add data via UI interactions (or via the app's "Load examples" shortcut for read
tests). Assert DOM state after each operation.

#### File: OpenHabitTracker.EndToEndTests/CRUD/NoteTests.cs

Locators to investigate before writing: find the "Add note" button (`aria-label` or role+text),
note title input (`name`, `placeholder`, or `aria-label`), and delete button (`aria-label`).
Read the Razor source to identify what attributes already exist before writing any locator.

    [TestFixture]
    public class NoteTests : BaseTest
    {
        [SetUp]
        public async Task SetUp() => await GotoAsync("notes");
    }

Tests:
- `AddNote_TypeTitle_NoteAppearsInList`
  - Click add note button, type title "My Note", confirm
  - Assert a list item with text "My Note" is visible
- `AddNote_EmptyTitle_NoteIsNotAdded`
  - Click add note button, leave title empty, confirm
  - Assert no new note appears
- `DeleteNote_AfterAdd_NoteDisappearsFromList`
  - Add a note "Delete Me"
  - Click delete button on the note
  - Assert no list item with text "Delete Me" is visible
- `DeleteNote_MovesToTrash`
  - Add note "Trashed Note", delete it
  - Open Trash sidebar (via menu → Trash)
  - Assert "Trashed Note" appears in trash list
- `EditNote_ChangesTitle_NewTitleVisible`
  - Add note "Original"
  - Open edit, change title to "Updated"
  - Assert "Updated" is visible, "Original" is not

#### File: OpenHabitTracker.EndToEndTests/CRUD/TaskTests.cs

    [TestFixture]
    public class TaskTests : BaseTest
    {
        [SetUp]
        public async Task SetUp() => await GotoAsync("tasks");
    }

Tests:
- `AddTask_TypeTitle_TaskAppearsInList`
- `AddTask_EmptyTitle_TaskIsNotAdded`
- `MarkTaskAsDone_CheckboxClick_TaskMarkedComplete`
  - After marking done, assert the task has visual completed state
  - If `HideCompletedTasks = true` (app default), assert task disappears from list
- `MarkTaskAsDone_ThenUnmark_TaskShowsIncomplete`
  - Mark done, unmark — assert incomplete state restored (toggle behavior)
- `DeleteTask_AfterAdd_TaskDisappearsFromList`
- `DeleteTask_MovesToTrash`

#### File: OpenHabitTracker.EndToEndTests/CRUD/HabitTests.cs

    [TestFixture]
    public class HabitTests : BaseTest
    {
        [SetUp]
        public async Task SetUp() => await GotoAsync("habits");
    }

Tests:
- `AddHabit_TypeTitle_HabitAppearsInList`
- `AddHabit_EmptyTitle_HabitIsNotAdded`
- `MarkHabitAsDone_ButtonClick_DoneCountIncreases`
  - Click "Mark as done" on a habit
  - Assert the done count in the habit row increased by 1
- `DeleteHabit_AfterAdd_HabitDisappearsFromList`
- `DeleteHabit_MovesToTrash`

---

### Phase 4: Settings persistence tests

Purpose: Verify that settings saved in one page load survive a browser reload.
localStorage persists within the same `BrowserContext` across navigations — the reload stays in the
same context, so settings written in step 1 are still there in step 2.

#### File: OpenHabitTracker.EndToEndTests/Settings/SettingsPersistenceTests.cs

    [TestFixture]
    public class SettingsPersistenceTests : BaseTest
    {
        [SetUp]
        public async Task SetUp() => await GotoAsync();
    }

Tests:
- `DarkMode_Toggle_AppliesImmediately`
  - Open Settings sidebar, toggle dark mode
  - Assert `html[data-bs-theme="dark"]` attribute is present (Bootstrap dark mode attribute)
- `DarkMode_Toggle_PersistedAfterReload`
  - Toggle dark mode ON, reload page
  - Assert `html[data-bs-theme="dark"]` is still present after reload
- `Language_Change_AppliesImmediately`
  - Open Settings sidebar, change language to one that changes visible text (e.g. "de")
  - Assert a translated nav label is now in German (e.g. aria-label="Notizen")
- `Theme_Change_AppliesColorVariable`
  - Change theme in Settings sidebar to a non-default theme
  - Assert the Bootstrap theme CSS variable changed (via `Page.EvaluateAsync` or visible change)

---

### Phase 5: Search and filter tests

#### File: OpenHabitTracker.EndToEndTests/Search/SearchTests.cs

    [TestFixture]
    public class SearchTests : BaseTest
    {
        [SetUp]
        public async Task SetUp()
        {
            await GotoAsync("notes");
            await LoadExamplesViaUiAsync();
        }

        private async Task LoadExamplesViaUiAsync()
        {
            // open menu, click Data, click Load Examples, close sidebar
            // (extract the same logic as LoadExamplesVideoTests.LoadExamples)
        }
    }

Tests:
- `SearchTerm_MatchingTitle_ShowsOnlyMatchingNotes`
  - Open search sidebar, type term that matches one example note
  - Assert only matching note(s) are visible in the main list
- `SearchTerm_NoMatch_ShowsEmptyList`
  - Type a term that matches nothing
  - Assert no note items are visible
- `SearchTerm_Clear_ShowsAllNotes`
  - Type a term, click clear button `[data-search-step-3]`
  - Assert all notes are visible again
- `CategoryFilter_HideCategory_ExcludesItsItems`
  - Open search/filter, uncheck a category
  - Assert items from that category disappear from the list

---

### Phase 6: Trash and restore tests

#### File: OpenHabitTracker.EndToEndTests/Trash/TrashTests.cs

    [TestFixture]
    public class TrashTests : BaseTest
    {
        [SetUp]
        public async Task SetUp() => await GotoAsync("notes");
    }

Tests:
- `DeletedNote_AppearsInTrash`
  - Add note "Trashed", delete it, open Trash sidebar
  - Assert "Trashed" appears in trash list
- `RestoreNote_FromTrash_ReappearsInNotesList`
  - Add note "Restore Me", delete it, open Trash
  - Click Restore on "Restore Me"
  - Close trash, navigate to /notes
  - Assert "Restore Me" is in notes list again
- `EmptyTrash_ClearsAllDeletedItems`
  - Add and delete two notes
  - Open Trash, click "Empty Trash"
  - Assert trash list is empty

---

### Execution order

1. **Phase 0b** — create `BaseTest.cs` with shared setup and `GotoAsync` helper
2. **Phase 1** — write smoke tests (highest priority — SSR regression detection)
3. **Phase 2** — write navigation tests
4. **Phase 4** — write settings persistence tests (independent of CRUD)
5. **Phase 3** — write CRUD tests (read Razor source to find existing locators before writing each test)
6. **Phase 5** — write search/filter tests
7. **Phase 6** — write trash/restore tests
