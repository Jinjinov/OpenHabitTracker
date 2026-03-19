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

### Prerequisites

#### a. App must be running at http://localhost before tests execute

The test project has no way to start the app. The developer must start the target app manually
(e.g. `OpenHabitTracker.Blazor.Web`) and keep it running during the test run.

Document this constraint in a `README.md` or at the top of each test fixture:

    // Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
    // dotnet run --project OpenHabitTracker.Blazor.Web --configuration Release

Reason: Playwright tests assert against a live app. Tests that run against a stale build will NOT
detect JS fixes or markup changes made since the last server restart.

#### b. Element locator strategy

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

### WaitForLoadStateAsync(NetworkIdle)

Used ~114 times in these tests but does nothing in Blazor WASM — all SPA navigation, IndexedDB reads, and component renders are client-side with no network traffic. 
Legitimate uses are few (e.g. after GotoAsync() for the initial WASM download from IIS). 
The rest should be replaced with Expect(...).ToBeVisibleAsync() assertions that actually wait for visible state changes, or removed entirely.

