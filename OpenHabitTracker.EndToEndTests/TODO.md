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

---

### Playwright Quirks

#### WaitForLoadStateAsync(NetworkIdle) — FIXED

Does nothing in Blazor WASM — all SPA navigation, IndexedDB reads, and component renders are
client-side with no network traffic. Legitimate use is only after GotoAsync() for the initial
WASM download from IIS. All other occurrences have been replaced with Expect(...).ToBeVisibleAsync()
assertions or removed entirely.

#### WaitForTimeoutAsync — FIXED

Blind sleep with no knowledge of app state — always flaky. Replaced with Expect(...) assertions
that actually wait for observable state changes.

#### Browser errors do not fail tests automatically — FIXED

Playwright tests only fail if an assertion fails or the test code throws. Browser-side crashes —
JS exceptions, Blazor rendering failures, console errors — are completely invisible unless you
explicitly wire them up.

Blazor WASM does NOT throw rendering exceptions to window.onerror (which Playwright captures
as Page.PageError). Blazor catches them internally and writes to console.error, then shows
#blazor-error-ui. All three handlers are wired up in BaseTest.BaseSetUp/BaseTearDown:

    Page.PageError += (_, error) => _browserErrors.Add($"PageError: {error}");
    Page.Console += (_, msg) => { if (msg.Type == "error") _browserErrors.Add($"Console: {msg.Text}"); };
    Page.RequestFailed += (_, request) => _browserErrors.Add($"RequestFailed: {request.Url}");

BaseTearDown asserts #blazor-error-ui is hidden, then fails the test if any browser error occurred.

Why only WASM is affected and not MAUI/WPF/Photino: Blazor Hybrid's WebView JS bridge
serializes an unset ElementReference as null in JS (so guards like `if (element &&` work).
Blazor WASM passes the raw { __internalId: "" } object, which is truthy but not a DOM element,
so the guard passes and any subsequent DOM method call (e.g. element.addEventListener) crashes.

#### CountAsync() is not a wait

`await Page.Locator("...").CountAsync()` returns immediately with whatever count exists at
that millisecond. If the DOM is still updating, the result is wrong and the test is flaky.
Always use `Expect(...).ToHaveCountAsync(n)` instead — it retries until the condition is met
or times out.

#### IsVisibleAsync() is not a wait

`await Page.Locator("...").IsVisibleAsync()` is also a snapshot — it does not retry.
Use `Expect(...).ToBeVisibleAsync()` when you need to wait for an element to appear.
Only use IsVisibleAsync() when you want the current state right now (e.g. an if-branch).

#### Page.RequestFailed — FIXED

If a .wasm or .js resource fails to load, Blazor silently degrades and tests may still pass
because their assertions coincidentally match something in the partially loaded DOM.
Wired up in BaseTest.BaseSetUp alongside PageError/Console to catch broken resource loads.

#### #blazor-error-ui — FIXED

When Blazor crashes hard (unrecoverable error), it makes #blazor-error-ui visible.
Asserted hidden in BaseTest.BaseTearDown as an additional signal on top of the console.error handler:

    await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();

