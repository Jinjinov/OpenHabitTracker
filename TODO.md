# TODO:

---------------------------------------------------------------------------------------------------

find out why `padding-left: 12px !important;` is needed on iOS - why `padding-left: env(safe-area-inset-left) !important;` doesn't work

---------------------------------------------------------------------------------------------------

to lazy load Times / TimesDone and remove `// TODO:: remove temp fix` and keep habit ratio in UI:
call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
    save TotalTimeSpent
    save AverageInterval
    on Habit Initialize - load only last week (last X days, displayed in small calendar)
    call LoadTimesDone for large calendar

LAZY LOADING:
    Per-instance lazy loads in services stay exactly as they are — triggered by user interaction,
    loading only what is needed. Loaded objects are registered in the dict.
    WHY Times lazy loading is critical:
    In Ididit (predecessor app), all TimesDone were loaded upfront. After 5 years of daily use
    with ~50 habits done 1-2× per day, the Times table grew to ~180,000 records and became slow.
    Per-habit lazy loading of Times was introduced specifically to solve this.
    Items are small (handful per habit/task) and not a performance concern.
    The temp fix in LoadHabits() calls LoadTimes() (bulk load of ALL times) — this is the WRONG
    direction and reintroduces the exact performance problem lazy loading was meant to solve.
    the habit list UI depends on AverageInterval and TotalTimeSpent being computed
    from TimesDone — without them the ratio badges show division-by-zero results.

TASK — remove temp fix (prerequisite: persist aggregates to DB):
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
    This requires a DB migration

if you fix this: `// TODO:: remove temp fix (needed to get TimesDoneByDay, TotalTimeSpent, AverageTimeSpent, AverageInterval)`
and TimesDone are actually lazy loaded, there will be a bug:
    FilterHabits NotOn vs. Before/On/After inconsistency with null TimesDone:
    File: QueryExtensions.cs, FilterHabits DoneAtCompare block
    When a habit's TimesDone has not been lazy-loaded yet (is null), the four date comparisons behave differently:
        NotOn:          x.TimesDone?.Any(...) != true  →  null != true  →  true   →  habit INCLUDED
        Before/On/After: x.TimesDone?.Any(...) == true  →  null == true  →  false  →  habit EXCLUDED
    Users see different result sets depending on which habits have been selected 
    (and thus had their TimesDone loaded) in the current session.

---------------------------------------------------------------------------------------------------

StateHasChanged() in Habits.razor — double-render and missing resize handling:

WHY the StateHasChanged() exists (OnAfterRenderAsync, firstRender || dynamicComponentVisibilityChanged):
    Blazor renders the component → DOM appears in browser → JS can now measure the element.
    JsInterop.GetElementDimensions(columnRef) runs and sets columnWidth.
    But the already-rendered template skipped the habits list because `columnWidth == 0`
    (the guard `@if (HabitService.Habits is not null && columnWidth != 0)` on line 108).
    StateHasChanged() forces a second render with the real columnWidth so CalendarComponent
    receives a non-zero ColumnWidth and computes `daysInRow = ColumnWidth / 50` correctly.
    The double-render is structurally unavoidable with this approach — it is the honest
    consequence of needing a browser measurement before the first meaningful render.
    The StateHasChanged() is NOT a hack; it is the correct response to that constraint.

THE REAL GAP — window resize is not handled:
    columnWidth is measured once on firstRender and again when the sidebar opens/closes
    (tracked via dynamicComponentVisibilityChanged from the DynamicComponentType cascade).
    Window resize, browser zoom, and mobile orientation changes are NOT handled.
    After resize, columnWidth stays stale — the calendar shows the wrong number of day cells,
    overflowing or leaving dead space. This is a real bug, most visible on desktop and MAUI/WPF/WinForms.
    Main.razor also only calls GetWindowDimensions() on firstRender — _windowDimensions is equally stale.

WHY the TODO alternatives don't work:
    @media for each day:
        CSS media queries can show/hide elements but cannot change how many <button> elements
        Blazor renders. You would need to pre-render all possible daysInRow counts and hide
        the extras — wasteful DOM and still fragile.
    Get <body> dimensions once at startup, calculate column width from that:
        Main.razor already does GetWindowDimensions() on firstRender.
        Column width != window width: you would need to subtract sidebar (350px when open),
        Bootstrap column gaps, and HorizontalMargin padding. That math breaks every time layout changes.
    Move the measurement up the hierarchy, before Habits renders for the first time:
        Main.razor already blocks child rendering until _windowDimensions is set (the
        `@if (_windowDimensions is not null)` gate). If column width were measured there too,
        Habits.razor would have a non-zero columnWidth on its very first render — no double-render.
        The problem: columnRef lives inside Habits.razor, so Main.razor cannot hold a reference to it.
        You would have to measure the <main> container instead and cascade that width down — close
        but not the same as the column's clientWidth, which depends on sidebar state, Bootstrap
        column gaps, and HorizontalMargin padding. That approximation breaks every time layout changes.
    Don't render habits if columnWidth == 0:
        Already done — that is exactly what the guard on line 108 does.
        The guard is necessary and correct; it does not remove the need for StateHasChanged().

FIX — ResizeObserver:
    The browser ResizeObserver API fires whenever an observed element's size changes:
    on first observation, on window resize, on zoom, on sidebar open/close.

    No feedback loop risk: OnWidthChanged triggers a re-render which can change the column's
    height (more/fewer habit rows) but never its width — width is determined by the Bootstrap
    parent grid, not by content inside. ResizeObserver will not fire on its own callback's side effects.

    Three required safety measures (without these: crash on navigation-during-resize, flood on Blazor Server, crash on old iOS):

    1. Debounce + width-change guard — ResizeObserver fires up to 60x/sec during window drag-resize.
       On Blazor Server each invokeMethodAsync goes over SignalR, flooding the connection.
       Only invoke .NET if the width actually changed (columns snap to discrete pixel widths):

    2. IAsyncDisposable cleanup — if the user navigates away while a resize callback is in-flight,
       the component is disposed but invokeMethodAsync fires anyway → ObjectDisposedException on
       the .NET side and a JS error. Requires DisposeAsync to disconnect the observer and dispose
       the DotNetObjectReference. The try-catch in JS must use .catch(() => {}) not a synchronous
       try-catch block — invokeMethodAsync returns a Promise, so a sync catch doesn't catch async
       rejections and the ObjectDisposedException still surfaces as an unhandled promise rejection.

    3. ResizeObserver availability guard — undefined on iOS < 13.4 and old Android WebViews.
       Without the guard the JS call throws. With it, columnWidth stays 0 and habits don't render
       on those devices — same behavior as today, not worse.

    RISK — ElementReference validity during DisposeAsync:
        When DisposeAsync calls UnobserveElementWidth(columnRef), Blazor must resolve the
        ElementReference struct to the actual JS DOM element to do the Map lookup. If the DOM
        element has already been removed by the time DisposeAsync runs, Blazor cannot resolve it
        and the Map lookup returns undefined — the ResizeObserver is never disconnected and the
        DotNetObjectReference leaks. In practice Blazor runs DisposeAsync before removing the DOM,
        so this is likely safe — but the exact ordering is not guaranteed by the framework.

        Safe alternative: key the Map by a string ID instead of the element object. C# generates
        a GUID at observe-time, stores it as a field, passes it to both observe and unobserve.
        No DOM resolution needed in unobserve — works regardless of element lifetime.

    Add to jsInterop.js:

        const _resizeObservers = new Map();

        export function observeElementWidth(id, element, dotnetRef) {
            if (typeof ResizeObserver === 'undefined') return;  // 3. old iOS/Android guard
            if (_resizeObservers.has(id)) return;               // guard against double-observe
            let lastWidth = 0;
            const ro = new ResizeObserver(entries => {
                for (let entry of entries) {
                    const w = Math.round(entry.contentRect.width);
                    if (w !== lastWidth) {                      // 1. width-change guard
                        lastWidth = w;
                        dotnetRef.invokeMethodAsync('OnWidthChanged', w)  // 2. swallow navigation race
                            .catch(() => {});                   //    must be .catch, not try-catch (Promise)
                    }
                }
            });
            ro.observe(element);
            _resizeObservers.set(id, { ro, dotnetRef });
        }

        export function unobserveElementWidth(id) {             // string key — no DOM resolution needed
            const entry = _resizeObservers.get(id);
            if (entry) {
                entry.ro.disconnect();
                entry.dotnetRef.dispose();                      // 2. release .NET GC reference
                _resizeObservers.delete(id);
            }
        }

    Changes required (verified against current code):

    jsInterop.js — add observeElementWidth and unobserveElementWidth (see snippet above)

    IJsInterop.cs — add two method signatures:
        ValueTask ObserveElementWidth(string id, ElementReference element, DotNetObjectReference<Habits> dotnetRef);
        ValueTask UnobserveElementWidth(string id);
        (or use object for dotnetRef to keep the interface generic across pages)

    JsInterop.cs — add implementations matching the existing lazy-module pattern

    Habits.razor:
        - Add @implements IAsyncDisposable at the top of the file (required for Blazor to call
          DisposeAsync on component teardown — without this directive the method exists but is
          never invoked by the framework)
        - Do NOT add @inject IPreRenderService — OnAfterRenderAsync is never called during SSR
          prerendering (Microsoft docs: "OnAfterRender and OnAfterRenderAsync aren't called during
          the prerendering process"). The guard would be redundant. RuntimeClientData uses it
          because OnInitializedAsync IS called during prerendering — a different lifecycle method.
        - Add private DotNetObjectReference<Habits>? _dotNetRef;  (must store to dispose in DisposeAsync)
        - Add private readonly string _observerId = Guid.NewGuid().ToString();  (stable key for Map)
        - In OnAfterRenderAsync: replace the GetElementDimensions + StateHasChanged block with:
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JsInterop.ObserveElementWidth(_observerId, columnRef, _dotNetRef);
            }
        - Add [JSInvokable] public async Task OnWidthChanged(int width):
            [JSInvokable]
            public async Task OnWidthChanged(int width)
            {
                columnWidth = width;
                await InvokeAsync(StateHasChanged);
            }
          NOT void — void is wrong for two reasons:
          (a) On Blazor Server, [JSInvokable] callbacks arrive via SignalR, potentially off the
              component's synchronization context. InvokeAsync(StateHasChanged) is the safe pattern;
              calling StateHasChanged() directly can race.
          (b) void means the JS Promise returned by invokeMethodAsync resolves immediately without
              waiting for the C# work to complete — errors inside would become unhandled rejections
              that the .catch(() => {}) in observeElementWidth cannot reliably suppress.
              Returning Task lets Blazor marshal it as a Promise so the .catch is effective.
        - Implement IAsyncDisposable — dispose the DotNetObjectReference BEFORE the JS call so it
          always runs even if the JS call throws (e.g. JSDisconnectedException on circuit teardown):
            public async ValueTask DisposeAsync()
            {
                _dotNetRef?.Dispose();
                try { await JsInterop.UnobserveElementWidth(_observerId); } catch { }
            }
          The try-catch is needed because JsInterop.UnobserveElementWidth can throw if the JS
          runtime is already gone (browser tab closed, Blazor Server circuit disconnected).
          During normal in-app navigation JsInterop is alive (it is scoped to the circuit/session,
          not the component), so this only matters at app teardown — but at that point the JS
          context is gone anyway and the cleanup doesn't matter. The try-catch makes it safe and
          silent in all cases.
          NOTE: the JS unobserveElementWidth also calls dotnetRef.dispose() — this double-dispose
          is safe because DotNetObjectReference.Dispose() is idempotent.
        - Remove the dynamicComponentVisibilityChanged tracking — ResizeObserver fires on sidebar
          toggle automatically because the column reflows when the sidebar appears/disappears.
          This means removing:
            • the _dynamicComponentTypeName field declaration
            • the bool dynamicComponentVisibilityChanged local variable
            • the _dynamicComponentTypeName = DynamicComponentType?.Name assignment
            • the || dynamicComponentVisibilityChanged condition in the if statement

    Notes.razor — no changes needed: confirmed no columnRef, no columnWidth, no CalendarComponent
    Tasks.razor — no changes needed: confirmed no columnRef, no columnWidth, no CalendarComponent

    Home.razor embeds all three pages simultaneously (IsEmbedded=true). Each component instance
    has its own columnRef DOM element → own ResizeObserver entry in the Map → no conflict.
    The _resizeObservers.has(id) guard prevents double-observe if OnAfterRenderAsync fires
    more than once with firstRender=true (can happen in Blazor Server SSR with prerendering).

    Platform safety summary:
        WASM                      — safe, invokeMethodAsync is in-process, re-renders cheap
        Blazor Server             — safe with width-change guard (SignalR flood avoided) and
                                    InvokeAsync(StateHasChanged) (synchronization context safe)
        MAUI iOS                  — safe for iOS >= 13.4; guard handles older devices
        MAUI Android              — safe, Chrome WebView supports it since Android 5.0
        WinForms / WPF / Photino  — safe, WebView2 is Chromium; Photino Linux uses WebKit2GTK >= 2019

    The InvokeAsync(StateHasChanged) call stays in OnWidthChanged — it is still needed because
    the callback arrives from JS outside the Blazor render cycle. But it is now driven by real
    size changes, not a one-shot post-render measurement. The columnWidth != 0 guard on line 108
    also stays and becomes semantically correct: it means "no measurement received yet" rather
    than "first render hasn't completed the JS round-trip".

---------------------------------------------------------------------------------------------------

1.
QueryParameters:
    `ClientData.GetHabits/GetNotes/GetTasks` each have a TODO: "first filter with queryParameters, then use _dataAccess"
    Currently all records are loaded into memory first, then filtered in C# — the intent is to push filters down to the data layer
    `_dataAccess` calls would receive query parameters (search term, category, priority, date range) and return only matching records
    This would eliminate the need for large in-memory filter blocks and reduce data transferred from the data source

2.
exact repeating reminders, like Google Keep

3.
drag & drop reorder - manual sort - 1000000 sort index
- sort categories?
- sort items?

4.
upgrade to .NET 10

5.
upgrade NuGet versions

---------------------------------------------------------------------------------------------------

1.
prerequisite:
TODO:: research: normal priority (DB migration)
save/load settings:
    - add string Name to Settings
    - add a way to create a new preset
    - add a way to load a preset
    - add a way to rename a preset
    - add a way to delete a preset
    - always load last used preset
        - add long SelectedSettingsId to Settings
        - load Settings[0], then Settings[Settings[0].SelectedSettingsId]
    - add string? Name to SettingsEntity + SettingsModel (null = unnamed/default row)
    - add long SelectedSettingsId to SettingsEntity + SettingsModel (default = own Id = Settings[0].Id)
    - Settings[0] is the root row; load Settings[0], then load Settings[Settings[0].SelectedSettingsId]
    - ClientState.LoadSettings() currently takes settings[0] — extend to also load Settings[SelectedSettingsId]
    - ClientState.UpdateSettings() uses Settings.Id to fetch and update — no change needed
    - UI in Settings.razor: preset selector dropdown + icons for create (bi-plus) / rename (bi-pencil) / delete (bi-trash)
    - on create: AddSettings() new row, set Settings[0].SelectedSettingsId = new row Id, UpdateSettings()
    - on load: set Settings[0].SelectedSettingsId = selected row Id, UpdateSettings(), reload
    - on rename: set Name on selected row, UpdateSettings()
    - on delete: DeleteSettings() selected row, set Settings[0].SelectedSettingsId = Settings[0].Id, reload
    - "URL settings" row: reserved row with Name = "URL", overwritten on each URL param navigation

multiple saved settings, with optional "URL references a preset by name", otherwise the URL params overwrite the "URL settings" saved setting:

2.
TODO:: research: normal priority
search/filter/sort query parameters in the URL - Blazor
    - QueryParameters class already exists with all filter/sort fields
    - serialize QueryParameters to URL query string via NavigationManager
    - on page load: parse URL params, find or overwrite "URL settings" row, set SelectedSettingsId to it
    - if URL references a preset by name: find matching Name in Settings rows, set SelectedSettingsId to it

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
6.
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
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

when all habit items are done, habit is done automatically ??? pros & cons ?
when all task items are done, task is done automatically ??? pros & cons ?

repeat:
    - weekly: which day in week
    - monthly: which day (or week/day - second monday) in month
    - yearly: which day (date) in year

textarea Tabs
    - make markdown Tabs look the same as in textarea
        - currently Tabs are ignored in markdown (except under a "- list row")
        - if `DisplayNoteContentAsMarkdown` is `false`, tabs are already displayed properly with `style="white-space: pre-wrap;"`
        - there is no way to know if user is using tabs to create a code block

horizontal calendar with vertical weeks

---------------------------------------------------------------------------------------------------

read Settings from DB before Run() - NO!!! - !!! Transient / Scoped / Singleton !!! - Scoped instances before and after Run() are not the same

unify into one property ??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ??? NO!!!

---------------------------------------------------------------------------------------------------

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

TODO:: research: low priority - large feature
copy Loop Habit Tracker UI - all required data is already in the DB
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
    the codebase would need to be stable first; comments on moving targets are maintenance burden

deploy Blazor Server Docker image to Raspberry Pi 5 / Synology NAS DS224+

---------------------------------------------------------------------------------------------------

what is wrong: I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them
