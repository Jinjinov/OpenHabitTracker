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

0.
prerequisite:
TODO:: research: high priority (DB migration)
save/load settings:
    - add string Name to Settings
    - add a way to create a new preset
    - add a way to load a preset
    - add a way to rename a preset
    - add a way to delete a preset
    - always load last used preset
        - add long SelectedSettingsId to Settings
        - load Settings[0], then Settings[Settings[0].SelectedSettingsId]

multiple saved settings, with optional "URL references a preset by name", otherwise the URL params overwrite the "URL settings" saved setting:

1.
TODO:: research: high priority
search/filter/sort query parameters in the URL - Web API

2.
TODO:: research: high priority
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

TODO:: research: normal priority (DB migration)
repeat:
    add `StartAt` to Habit ? some starting point for repeat interval (`PlannedAt` is more appropriate for tasks than habits)
        this solves a real problem: you create a habit with 1 week repeat interval on Monday, but you want to repeat the habit every Friday
    StartAt is optional

    a date picker is easy, but not very good - better would be:
    - weekly: which day in week
    - monthly: which day (or week/day - second monday) in month
    - yearly: which day (date) in year
        but this complicates ElapsedTime and is half way to the "exact repeating reminders, like Google Keep" task

    all "Overdue" logic must adapt to StartAt! - ElapsedTime is "DateTime.Now - CreatedAt" when LastTimeDoneAt is null

    is it worth it? yes, if `DateTime StartAt` can be reused in the "exact repeating reminders, like Google Keep" task

textarea Tabs
    - make markdown Tabs look the same as in textarea
        - currently Tabs are ignored in markdown (except under a "- list row")
        - if `DisplayNoteContentAsMarkdown` is `false`, tabs are already displayed properly with `style="white-space: pre-wrap;"`
        - there is no way to know if user is using tabs to create a code block

TODO:: research: high priority (DB migration)
Show only habits with ratio `over x%` / `under y%` - currently filter habits with urgency `over x%`, also add `under y%`
    how useful is it to see habits with urgency `under y%` if y is under 100?
    only real use case: you see all habits with ratio over 120% and then want to see only those with 100% - 120%

    see SelectedRatioMin and ShowOnlyOverSelectedRatioMin, add SelectedRatioMax - is ShowOnlyOverSelectedRatioMax needed?
    SelectedRatioMax is optional

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
