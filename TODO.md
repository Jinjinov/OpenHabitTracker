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
InitializeItems and InitializeTimes have null checks and do not update data when called in GetUserData()
    both load data directly from DB with _dataAccess.GetTimes() and _dataAccess.GetItems()
    but LoadTimesDone also loads data with _dataAccess.GetTimes() - these are not the same objects as in InitializeTimes
    and ItemService.Initialize also loads data with _dataAccess.GetItems() - these are not the same objects as in InitializeItems
    user can add or remove Items and Times list but the code does not update Items and Times in the AppData
    so without temp fix, GetUserData() would return Items and Times that were loaded with Initialize()
remove these from class AppData:
    public Dictionary<long, TimeModel>? Times { get; set; }
    public Dictionary<long, ItemModel>? Items { get; set; }
or make sure that other services update them
this is a big problem - services use _dataAccess on their own, but AppData is supposed to represent the current state - as the only source of truth
Ididit did not have this problem, `Repository` was the only class with `IDatabaseAccess` and represented the current state

---------------------------------------------------------------------------------------------------

1.
CategoryId can't be null
    - add default category if there is none (same as settings)
    - add DefaultCategoryId to Settings
    - add migration that sets any null CategoryId to DefaultCategoryId

add class User with string Id, Name, Email, PasswordHash
    add UserId to Category
    add UserId to Settings
    UserId can't be null

2.
make every ...Id a required field in EF Core - Debug.Assert(Id != 0) before Add / Update

3.
refactor OpenHabitTracker
4.
run Jetbrains Rider code analysis
5.
add comments to methods
6.
add REST API endpoints for online data sync to Blazor Server - use them in Blazor Wasm, Photino, Wpf, WinForms, Maui

7.
add OAuth to Blazor Wasm, Photino, Wpf, WinForms, Blazor Server, Maui
    Google Drive
    Microsoft OneDrive
    Dropbox

8.
use Google, Microsoft, Dropbox OAuth for unique user id and login

9.
add backup to
    Google Drive
    Microsoft OneDrive
    Dropbox

10.
use DB in Blazor Server for multi user sync with add REST API endpoints

11.
write unit tests with Appium / bUnit

12.
Android: get permission to save SQLite DB in an external folder that can be part of Google Drive, OneDrive, iCloud, Dropbox

13.
deploy Blazor Server Docker image to Raspberry Pi 5 / Synology NAS DS224+

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

!!! add comments to methods - 1. for any open source contributor - 2. for GitHub Copilot

---------------------------------------------------------------------------------------------------

!!! run Jetbrains Rider code analysis

---------------------------------------------------------------------------------------------------

!!! refactor classes:

UserData -> UserImportExportData

AppData -> ClientSideData
    - hold state
    - load state
    - map to models
    - interact with _dataAccess
    - interact with _runtimeData
    - interact with _markdownPipeline
    - import, export / GetUserData, SetUserData
    - LoadExamples

IRuntimeData -> IClientSideRuntimeData

Initialize -> LoadSavedData

ClientSideState:
    public Dictionary<long, HabitModel>? Habits { get; set; }
    public Dictionary<long, NoteModel>? Notes { get; set; }
    public Dictionary<long, TaskModel>? Tasks { get; set; }
    public Dictionary<long, TimeModel>? Times { get; set; }
    public Dictionary<long, ItemModel>? Items { get; set; }
    public Dictionary<long, CategoryModel>? Categories { get; set; }
    public Dictionary<long, PriorityModel>? Priorities { get; set; }

---------------------------------------------------------------------------------------------------

Android:
    save SQLite DB in an external folder
    can be part of Google Drive, OneDrive, iCloud, Dropbox

AndroidManifest.xml
MANAGE_EXTERNAL_STORAGE
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />

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
    iCloud --- https://github.com/gachris/iCloud.Dav ---
    Dropbox https://www.nuget.org/packages/Dropbox.Api
        WASM authorisation - REST
        desktop authorisation - OpenHabitTracker.Google.Apis - using Google.Apis.Auth.OAuth2;
        mobile authorisation - `ASP.NET Core`

Nextcloud
    https://www.nuget.org/packages/NextcloudApi
ownCloud
    https://www.nuget.org/packages/bnoffer.owncloudsharp

Blazor Server / Web
    `ASP.NET Core`
    SQL Server
    version history: https://learn.microsoft.com/en-us/ef/core/providers/sql-server/temporal-tables
    table Users
    column UserId in every other table
    EF Core: use `DbContextFactory`

---------------------------------------------------------------------------------------------------

Host 24/7 on
    Raspberry Pi 5
    Synology NAS DS224+

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

when all habit items are done, habit is done
when all task items are done, task is done

content background:
    list all possible colors
    whole <div>, not just Title

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

benchmark method time & render time

read Settings from DB before Run() - !!! Transient / Scoped / Singleton !!!

??? Task `CompletedAt` / Habit `LastTimeDoneAt` --> `DateTime? DoneAt` ???

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

- filters are query parameters

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

copy Loop Habit Tracker
- History (done count grouped by week, month, quarter, year)
- Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
- Best streaks (from date - to date)
- Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

---------------------------------------------------------------------------------------------------

- drag & drop reorder
- keyboard navigation
- benchmark: method time & render time

- ASAP tasks: when, where, contact/company name, address, phone number, working hours, website, email

- don't use `event` to refresh everything on every change
- don't use `StateHasChanged()`
- don't do this: current screen changed -> save current screen to settings -> data changed -> refresh all

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

virtualized container

method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance

---------------------------------------------------------------------------------------------------

what is wrong: I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them
