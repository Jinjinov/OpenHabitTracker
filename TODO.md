# TODO:

---------------------------------------------------------------------------------------------------

find out why `padding-left: 12px !important;` is needed on iOS - why `padding-left: env(safe-area-inset-left) !important;` doesn't work

---------------------------------------------------------------------------------------------------

    fix ClientState GetUserData() which calls InitializeContent()
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

this is a big problem - services use `_dataAccess` on their own, but `ClientState` is supposed to represent the current state - as the only source of truth
Ididit did not have this problem, `Repository` was the only class with `IDatabaseAccess` and represented the current state

---------------------------------------------------------------------------------------------------

1, 2, 3 must be done at the same time so there is one new DB migration, not three

1.
Category-grouped main list (togglable alternative view):
- applies to Notes, Tasks, and Habits pages
- controlled by a new ShowGroupedByCategory setting (bool, default false)
- replaces the current flat foreach in each page:
  - foreach (NoteModel note in NoteService.GetNotes())
  - foreach (TaskModel task in TaskService.GetTasks())
  - foreach (HabitModel habit in HabitService.GetHabits())
- outer loop: foreach (CategoryModel category in CategoryService.Categories)
- inner loop: items filtered+sorted per category (new GetNotes/GetTasks/GetHabits(categoryId)
  overload, NOT category.Notes/Tasks/Habits directly which are unfiltered and unsorted)
- category header row (all three pages): category title, collapse/expand (same unicode char as in Search)
- category header row (Habits only): also and/or toggle button (see task 2), status color,
  LastTimeDoneAt (see task 3)
- cross-category sorting still works in flat view; grouped view sorts within each category
- inject ICategoryService into Habits.razor, Tasks.razor, Notes.razor (not currently injected)
- all new UI strings (category header labels) must use @Loc["..."] and add translations to json — app has 20 languages
- persistence chain for ShowGroupedByCategory (bool, new SettingsModel/SettingsEntity field):
  - add to SettingsModel and SettingsEntity
  - add mapping in EntityToModel.cs and ModelToEntity.cs
  - EF migration in both OpenHabitTracker.EntityFrameworkCore/Migrations/
    and OpenHabitTracker.Blazor.Web/Migrations/
  - export/import: automatically included since full SettingsModel is serialized

2.
add group "and / or" toggle:
- all habits/items done -> green
- one habit/item done -> green

Plan:
- add CompletionRule property to CategoryModel (enum CompletionRule { All, Any })
- full persistence chain (CompletionRule is a new persisted field, unlike display-only settings):
  - add to CategoryEntity
  - add mapping in EntityToModel.cs and ModelToEntity.cs
  - EF migration in both OpenHabitTracker.EntityFrameworkCore/Migrations/
    and OpenHabitTracker.Blazor.Web/Migrations/
  - include in all export/import formats: JSON, YAML, TSV, Markdown (Google Keep is import-only)
- all new UI strings (toggle labels) must use @Loc["..."] and add translations to json — app has 20 languages
- two display locations, both optional and independent:

  A. Stats panel (second column, see task 4 plan):
     - show per-category toggle state alongside the green/orange/red aggregate
     - affects how the category's status color is computed in stats

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
- two display locations, both optional and independent:

  A. Stats panel (second column, see task 4 plan):
     - show LastTimeDoneAt (most recent across all habits)

  B. Category-grouped main list (see task 1):
     - show LastTimeDoneAt in the category header row
     - controlled by a new ShowLastTimeDone setting (bool, default true when
       ShowGroupedByCategory is true)
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
- x out of y habits done
- x out of y groups are green

Plan:
- implement as 3 reusable components: NotesStatisticsComponent, TasksStatisticsComponent, HabitsStatisticsComponent
  NOTE: this is separate from the existing ShowHabitStatistics setting, which shows per-habit
  detail stats (time spent, ratios, elapsed) inside HabitComponent when editing a single habit
- wide screens (>= 1280px): each component renders in the else branch inside the second column
  on its respective page when no item is selected (mutually exclusive with the edit component —
  stats disappear when you open a habit/task/note)
  second column already exists and is empty in this case - uncomment and implement the else branch
- mobile: each component renders if (!_showSecondColumn)
- inject ICategoryService into Habits.razor, Tasks.razor, Notes.razor
- respect ShowGroupedByCategory (see task 1) - iterate Notes, Tasks, Habits OR ClientData.Categories (already has .Habits/.Tasks/.Notes populated at runtime
  via ClientState), respect HiddenCategoryIds / SelectedCategoryId from Settings
- all new UI strings must use @Loc["..."] and add translations to json — app has 20 languages

Habits stats:
- respect ShowGroupedByCategory (see task 1)
- category title (if ShowGroupedByCategory) | habit count | green/orange/red counts (using existing
  GetRatio() + SelectedRatio logic) | LastTimeDoneAt of most recent habit
- "this week" aggregate at top: habits done at least once this week (TimesDone entries where
  StartedAt >= week start) out of total habits; categories fully green this week
- week boundaries: "SettingsModel.FirstDayOfWeek + 7 days" of current week
- data source: HabitModel.TimesDone (List<TimeModel>, already loaded after Initialize())
  TimeModel.StartedAt / CompletedAt; HabitModel.TotalTimeSpent / AverageTimeSpent already computed

Tasks stats:
- respect ShowGroupedByCategory (see task 1)
- category title (if ShowGroupedByCategory) | total count | done count (CompletedAt != null) |
  overdue count (PlannedAt < now && CompletedAt == null) | total time spent (sum of
  CompletedAt - StartedAt across completed tasks)
- "this week" aggregate at top: tasks completed this week | tasks planned this week

Notes stats:
- respect ShowGroupedByCategory (see task 1)
- category title (if ShowGroupedByCategory) | total count | count per Priority
- CreatedAt / UpdatedAt

5.
SUBMIT DESKTOP VIDEO (1920x1080) TO:
    - macOS App Store: upload MP4 in App Store Connect

SUBMIT MOBILE VIDEO (886x1920) TO:
    - iOS App Store: upload MP4 in App Store Connect

---------------------------------------------------------------------------------------------------

1.
1a Filter:
    - Priority + Category filter blocks — extract to extension methods on `IEnumerable<ContentModel>`; currently repeated 6× across `HabitService`, `NoteService`, `TaskService`, `ClientData`
    - `ClientData` mixes data bag with query/filter/sort logic (SRP) — 40-70 line query methods inside what should be a plain data container
    - Duplicate query logic across `HabitService`, `NoteService`, `TaskService` (SRP) — same filter/sort structure (priority, search, category, date, sort switch) maintained independently in all three
    - Plan to eliminate duplication:
      1. Create `Query/` folder with two files:
         - `Query/QueryParameters.cs` — move from `Dto/QueryParameters.cs`, update namespace to `OpenHabitTracker.Query`
         - `Query/QueryExtensions.cs` — new static class with extension methods `IEnumerable<NoteModel>.FilterNotes(QueryParameters)`, `IEnumerable<TaskModel>.FilterTasks(QueryParameters)`, `IEnumerable<HabitModel>.FilterHabits(QueryParameters)` — single authoritative filter+sort logic for each type
      2. `ClientData.GetNotes/GetTasks/GetHabits(QueryParameters)` keep only the `if (X is null) { /* lazy load */ }` block, then call `Notes.Values.FilterNotes(queryParameters)` / `Tasks.Values.FilterTasks(queryParameters)` / `Habits.Values.FilterHabits(queryParameters)`
      3. `NoteService.GetNotes()` / `TaskService.GetTasks()` / `HabitService.GetHabits()` build a `QueryParameters` from `_clientState.Settings` + `_searchFilterService`, then call `Notes!.FilterNotes(queryParameters)` / `Tasks` / `Habits` directly — no delegation to `ClientData`, same access pattern as today but without the duplicated filter/sort code
      4. "Category-grouped main list" feature (TODO item line 54) can use the same extension methods directly: `category.Notes.FilterNotes(queryParameters)`, `category.Tasks.FilterTasks(queryParameters)`, `category.Habits.FilterHabits(queryParameters)` — no async, no lazy load, data already in memory
1b QueryParameters:
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

call LoadTimesDone on Habit Initialize - sort needs it, every calendar needs it, ...
    save TotalTimeSpent
    save AverageInterval
    on Habit Initialize - load only last week (last X days, displayed in small calendar)
    call LoadTimesDone for large calendar

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
