# TODO:

---------------------------------------------------------------------------------------------------

find out why `padding-left: 12px !important;` is needed on iOS - why `padding-left: env(safe-area-inset-left) !important;` doesn't work

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

accessibility:

    1. Arrow keys for Menu sidebar (ARIA menu pattern):
       - Tab enters the menu, Up/Down arrows move between items, Tab exits

       PLAN:
       File: OpenHabitTracker.Blazor/Pages/Menu.razor
       - Add role="menu" to the outer <div class="list-group"> container
       - Add role="menuitem" and tabindex="-1" to every <button class="list-group-item"> (all except the first get tabindex="-1"; the first gets tabindex="0" so Tab enters the menu)
       - Add @onkeydown="HandleKeyDown" to the container div
       - Add @ref attributes to each button: store them in ElementReference[] _items
       - Track int _focusedIndex = 0
       - In HandleKeyDown:
           ArrowDown / ArrowRight → _focusedIndex = (_focusedIndex + 1) % _items.Length; await JsInterop.FocusElement(_items[_focusedIndex]);
           ArrowUp / ArrowLeft   → _focusedIndex = (_focusedIndex - 1 + _items.Length) % _items.Length; await JsInterop.FocusElement(_items[_focusedIndex]);
           Home                  → _focusedIndex = 0; await JsInterop.FocusElement(_items[0]);
           End                   → _focusedIndex = _items.Length - 1; await JsInterop.FocusElement(_items[^1]);
           Tab / Escape          → e.StopPropagation() is NOT needed — let Tab bubble so focus exits naturally
       - Update tabindex on each button dynamically: tabindex="@(i == _focusedIndex ? 0 : -1)"
       - On menu item click: update _focusedIndex to match clicked item
       - Add @inject IJsInterop JsInterop

    2. Silent operations give no screen reader feedback (WCAG 4.1.3):
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

    3. Focus management (currently missing):
       - sidebar opens → move focus to first element inside sidebar
       - sidebar closes → return focus to the button that opened it (menu or search)
       - note/task/habit edit closes → return focus to the list item that was opened

       PLAN:
       File: OpenHabitTracker.Blazor/Layout/Main.razor

       Step A — sidebar focus on open:
       - Main.razor already renders sidebar content in a <div @onkeydown="HandleSidebarKeyDown">
       - After setting _dynamicComponentType (i.e., opening the sidebar), call await JsInterop.FocusElement() on the first focusable element inside the sidebar div
       - Use a JS helper: add focusFirstIn(selector) to jsInterop.js that does container.querySelector('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])').focus()
       - Add ValueTask FocusFirstIn(string cssSelector) to IJsInterop.cs and JsInterop.cs
       - After OnAfterRenderAsync detects sidebar just opened, call await JsInterop.FocusFirstIn("#sidebar-container")
       - Add id="sidebar-container" to the sidebar div

       Step B — restore focus to opener button on sidebar close:
       - Main.razor has two opener buttons: the menu toggle button and the search toggle button
       - Add @ref="_menuButtonRef" and @ref="_searchButtonRef" on those two buttons
       - Track which one was last clicked: ElementReference _lastOpenerRef
       - Set _lastOpenerRef = _menuButtonRef (or _searchButtonRef) in the click handler before opening the sidebar
       - On close (when _dynamicComponentType is set to null), call await JsInterop.FocusElement(_lastOpenerRef) after StateHasChanged

       Step C — restore focus to list item on edit close:
       - In Habits.razor / Notes.razor / Tasks.razor, each list item that can be opened for editing has a button or row
       - Add @ref on the "open edit" button for each item (use a Dictionary<long, ElementReference> _itemRefs keyed by item Id)
       - When edit closes (CloseSelected callback fires), call await JsInterop.FocusElement(_itemRefs[_selectedId]) where _selectedId is the id of the item that was open

    4. Calendar arrow key navigation (roving tabindex):
       - currently Tab through every day cell (up to 42 presses for month view)
       - only one cell has tabindex="0" at a time, arrow keys move between cells, Tab exits grid
        Home/End in calendar grid:
       - Home → first day of the week, End → last day of the week
        Page Up/Page Down in calendar:
       - Page Up → previous month, Page Down → next month
       - add `role="grid"` / `role="row"` / `role="gridcell"` / `role="columnheader"` to grid divs

       PLAN:
       File: OpenHabitTracker.Blazor/Components/CalendarComponent.razor

       Step A — ARIA grid roles:
       - Outermost calendar container: add role="grid" aria-label="@Loc["Calendar"]"
       - Each week row div: add role="row"
       - Each day-of-week header cell: add role="columnheader" scope="col"
       - Each day button: change from <button> to a <div role="gridcell"> wrapping a <button> (or keep <button> and add role="gridcell" directly — gridcell on the button is acceptable and simpler)

       Step B — roving tabindex state:
       - Add int _activeDayIndex = 0 (index into the flat list of displayed day cells, 0–41 for month view)
       - Add ElementReference[] _dayCellRefs with a slot per rendered day
       - In the day cell loop: tabindex="@(i == _activeDayIndex ? 0 : -1)" and @ref="@_dayCellRefs[i]"
       - On click of a day cell: _activeDayIndex = i; (focus is already there by click)

       Step C — keyboard handler on the grid container:
       - Add @onkeydown="HandleGridKeyDown" @onkeydown:preventDefault="@_preventDefaultOnGrid" to the role="grid" div
       - bool _preventDefaultOnGrid — set true only for handled keys to avoid blocking Tab
       - In HandleGridKeyDown:
           ArrowRight  → move _activeDayIndex += 1; if overflows, advance to next month and set _activeDayIndex = 0
           ArrowLeft   → move _activeDayIndex -= 1; if underflows, go to previous month and set _activeDayIndex = last cell
           ArrowDown   → _activeDayIndex += 7 (next week); if overflows, go to next month
           ArrowUp     → _activeDayIndex -= 7 (prev week); if underflows, go to previous month
           Home        → _activeDayIndex = index of first day of current week (round down to nearest multiple of 7)
           End         → _activeDayIndex = index of last day of current week (round up to nearest multiple of 7, minus 1)
           PageDown    → call existing next-month navigation; keep same day-of-month if possible, else clamp to last day
           PageUp      → call existing prev-month navigation; same clamping
           Tab         → do NOT prevent default; let browser handle Tab to exit the grid naturally
       - After _activeDayIndex changes: await JsInterop.FocusElement(_dayCellRefs[_activeDayIndex])

       Step D — aria-label on each day cell button:
       - Add aria-label="@dateTime.ToString("dddd, MMMM d", culture) — @timesDone @Loc["times done"]" to each day cell button so screen readers announce the full date and completion count

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
Android: get permission to save SQLite DB in an external folder that can be part of Google Drive, OneDrive, iCloud, Dropbox

12.
deploy Blazor Server Docker image to Raspberry Pi 5 / Synology NAS DS224+

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

what is wrong: I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them
