# TODO:

## Unit Test Plan (NUnit + bUnit)

### Prerequisites

#### a. Element locator strategy

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
