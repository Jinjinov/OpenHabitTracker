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
    - wire sub-collections from flat dicts        CORRECT  (ClientData.GetHabits(), ClientState.LoadNotes/LoadTasks/LoadHabits)
    - CRUD Add operations update dicts            CORRECT
    - per-instance loads register into dicts      CORRECT  (LoadTimesDone, Initialize, Start, AddTimeDone, AddItem, RemoveTimeDone, DeleteItem)
    - CategoryModel sub-lists wired at runtime    CORRECT  (LoadNotes/LoadTasks/LoadHabits + Add mutations + ChangeCategory)
    - DataAccess private to store                 MISSING  (exposed as public property, services use it directly)

public DataAccess:

    DataAccess is public on ClientState
        Services call _clientState.DataAccess directly for mutations (Add, Update, Remove)
        This is an enforcement problem — the invariant cannot be violated if DataAccess is private.

    consider making DataAccess private
        DataAccess is currently public on ClientState so services can reach it directly
        long-term: make it private, add explicit ClientState methods for every operation
        services call ClientState methods → ClientState calls DataAccess + updates dict
        this enforces the invariant at compile time, not by convention

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

if this is removed `// TODO:: remove temp fix (needed to get TimesDoneByDay, TotalTimeSpent, AverageTimeSpent, AverageInterval)`
and TimesDone are actually lazy loaded, there will be a bug:
    FilterHabits NotOn vs. Before/On/After inconsistency with null TimesDone:
    File: QueryExtensions.cs, FilterHabits DoneAtCompare block
    When a habit's TimesDone has not been lazy-loaded yet (is null), the four date comparisons behave differently:
        NotOn:          x.TimesDone?.Any(...) != true  →  null != true  →  true   →  habit INCLUDED
        Before/On/After: x.TimesDone?.Any(...) == true  →  null == true  →  false  →  habit EXCLUDED
    Users see different result sets depending on which habits have been selected 
    (and thus had their TimesDone loaded) in the current session.

---------------------------------------------------------------------------------------------------

1, 2, 3 must be done at the same time so there is one new DB migration, not three

0.
cross-component refresh when toggling collapse in Home.razor (all three pages embedded)
- toggling collapse in one embedded page does not refresh the others
- event Action? CategoryChanged approach was rejected — find a different solution

similar problem: editing a note/task/habit in HabitComponent/NoteComponent/TaskComponent (second column, not IsEmbedded) does not immediately refresh the title/content shown in the list in the parent page — same "child updates, parent doesn't know" pattern

1.
Category-grouped main list (togglable alternative view):
✓ applies to Notes, Tasks, and Habits pages
✓ controlled by a new ShowGroupedByCategory setting (bool, default false)
✓ replaces the current flat foreach in each page (nested foreach + GetHabitGroups/GetTaskGroups/GetNoteGroups)
✓ outer loop: foreach (CategoryModel category in CategoryService.Categories)
✓ items with no category appear in an "Uncategorized" bucket rendered at the bottom
✓ grouped view respects category filter display mode (CheckBoxes/RadioButtons use HiddenCategoryIds; SelectOptions uses SelectedCategoryId)
✓ inner loop: items filtered+sorted per category using FilterHabits/FilterTasks/FilterNotes(queryParameters)
✓ category header row (all three pages): category title, collapse/expand
- category header row (Habits only): and/or toggle button (see task 2), status color (see task 2)
✓ category header row (Habits only): LastTimeDoneAt (see task 3)
✓ cross-category sorting still works in flat view; grouped view sorts within each category
✓ inject ICategoryService into Habits.razor, Tasks.razor, Notes.razor
✓ all new UI strings use @Loc["..."] and added to en.json only — other 19 language JSON files still need translations
✓ persistence chain for ShowGroupedByCategory: SettingsModel, SettingsEntity, EntityToModel, ModelToEntity
✓ persistence chain for IsCollapsed: CategoryModel, CategoryEntity, EntityToModel, ModelToEntity
- EF migration (covers all 4 new fields: ShowGroupedByCategory, ShowLastTimeDone, IsCollapsed, CompletionRule):
  - run after all model/entity changes for tasks 1, 2, 3 are done:
    cd e:/Jinjinov/OpenHabitTracker && dotnet ef migrations add AddGroupedViewSettings --project OpenHabitTracker.EntityFrameworkCore --startup-project OpenHabitTracker.Blazor.Wasm
    cd e:/Jinjinov/OpenHabitTracker && dotnet ef migrations add AddGroupedViewSettings --project OpenHabitTracker.Blazor.Web --startup-project OpenHabitTracker.Blazor.Web
✓ Settings.razor: ShowGroupedByCategory checkbox added above "Show help"
✓ Settings.razor: ShowLastTimeDone directly below ShowGroupedByCategory, always visible
✓ Settings.razor: data-settings-step- attributes renumbered; guided tour texts added to GuidedTourComponent-en.json only — other 19 language JSON files still need translations
✓ new localization strings added to en.json: "Group by category", "Uncategorized", "Show last done time"

2.
add group "and / or" toggle:
- all habits/items done -> green (color) / "complete" (text)
- one habit/item done -> green (color) / "complete" (text)

Plan:
✓ add CompletionRule property to CategoryModel (enum CompletionRule { All, Any })
✓ full persistence chain: CategoryEntity, EntityToModel, ModelToEntity
- EF migration in both OpenHabitTracker.EntityFrameworkCore/Migrations/
  and OpenHabitTracker.Blazor.Web/Migrations/
- all new UI strings must use @Loc["..."] and add translations to all 20 language JSON files
- new localization strings (not yet added to any JSON file): "Mark complete when all habits done" / "Mark complete when any habit done"
- one display location: category header row in the grouped main list (Habits only)
  - and/or toggle button changes CompletionRule between All and Any
  - meaningful impact: color applied to the "Last done" text in the category header, driven by CompletionRule
  - CompletionRule.All: green = all done, orange = some done, red = none done
  - CompletionRule.Any: green = any done, red = none done (no orange — one is enough)
  - "done this week" = has at least one TimeDone with StartedAt >= weekStart (same as stats)
  - stats panel already reflects CompletionRule via "x out of y categories complete" — no toggle needed there (stats = read results, not change settings)

3.
LastDone date: for a group, for the items
✓ add date to habit item (already existed: ElapsedTime + ratio badge)
✓ add date to category (max LastTimeDoneAt shown in category header)
✓ add settings to show, hide this extra info (ShowLastTimeDone)

Plan:
✓ "last done for an item" already exists: HabitModel.LastTimeDoneAt (DateTime?)
✓ "last done for a category" = max(LastTimeDoneAt) across all habits in that category
✓ all new UI strings use @Loc["..."] and added to en.json only — other 19 language JSON files still need translations
✓ new localization strings added to en.json: "Last done", "Show last done time"
- two display locations, both optional and independent:

  A. Stats panel (second column, see task 4 plan):
✓    - show LastTimeDoneAt (most recent across all habits) — shown in HabitsStatisticsComponent per group

  B. Category-grouped main list (see task 1):
✓  - show LastTimeDoneAt in the category header row (hidden when collapsed)
✓  - controlled by ShowLastTimeDone setting (bool, default true)
✓  - persistence chain: SettingsModel, SettingsEntity, EntityToModel, ModelToEntity
     - EF migration: covered by task 1 migration above

  C. Per-habit in the flat main list:
✓  - already shown (ElapsedTime + ratio badge on each habit row)

4.
This week (xx.xx - yy.yy) statistics
✓ x out of y habits done
✓ x out of y groups are green (color) / "complete" (text) — "x out of y categories complete" in HabitsStatisticsComponent

Plan:
✓ implement as 3 reusable components: NotesStatisticsComponent, TasksStatisticsComponent, HabitsStatisticsComponent
✓ wide screens (>= 1280px): each component renders in the else branch inside the second column on its respective page when no item is selected
✓ mobile: each component renders if (!_showSecondColumn)
✓ inject ICategoryService into all 3 stats components
✓ respect ShowGroupedByCategory: GetHabitGroups/GetTaskGroups/GetNoteGroups in each component (flat = single group, grouped = per category)
✓ respect HiddenCategoryIds / SelectedCategoryId from Settings
✓ all new UI strings use @Loc["..."] and added to en.json only — other 19 language JSON files still need translations
✓ new localization strings added to en.json: "This week", "out of", "overdue", "Categories complete"

Habits stats:
✓ respect ShowGroupedByCategory
✓ category title (if ShowGroupedByCategory) | green/orange/red counts | LastTimeDoneAt of most recent habit
✓ "this week" aggregate at top: habits done at least once this week out of total; categories fully complete this week
- CompletionRule used for IsComplete() but and/or toggle UI not yet done (see task 2)

Tasks stats:
✓ respect ShowGroupedByCategory
✓ category title (if ShowGroupedByCategory) | total count | done count | overdue count | total time spent
✓ "this week" aggregate at top: tasks completed this week | tasks planned this week

Notes stats:
✓ respect ShowGroupedByCategory
✓ category title (if ShowGroupedByCategory) | total count | count per Priority
✓ CreatedAt / UpdatedAt of most recently created/updated note per group (shown in both flat and grouped)

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
