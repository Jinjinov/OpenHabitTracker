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

!!! add Google Drive
    Blazor WASM -> Google Drive REST API
    Blazor Desktop -> Google Drive API
!!! add Blazor Server - OAuth REST, CRUD REST, SignalR for instant UI refresh on multiple devices
    Blazor Mobile -> Blazor Server
    Blazor Server -> Google Drive API
    https://app.openhabittracker.net/ -> https://pwa.openhabittracker.net/

---------------------------------------------------------------------------------------------------

only blazor server will offer online sync - one user per server https://app.openhabittracker.net
blazor server will require guid key on app.* - saved with Blazored.LocalStorage on client / browser
offer pwa.* for offline version https://pwa.openhabittracker.net
offer docker for own server
    Docker Hub : https://hub.docker.com
    GitHub Container Registry : https://github.com/Jinjinov/OpenHabitTracker/packages
    https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry

login will be with Google, Microsoft, Dropbox 
email will be unique user id
requires scope with permission to get email
store refresh token for each cloud provider

all client/server communication: (including the GUID key)
    blazor server: DI / SignalR
    blazor wasm, windows, linux, macOS, iOS, android: REST API endpoints

---------------------------------------------------------------------------------------------------

Example .env File:

    OWNER_NAME=Jinjinov
    SECRET_KEY=my-super-secret-key-1234

Example docker-compose.yml:

    version: '3.8'

    services:
      app:
        image: your-server-image
        environment:
          - OWNER_NAME=${OWNER_NAME}  # Use the name from the .env file
          - SECRET_KEY=${SECRET_KEY}  # Use the key from the .env file
        ports:
          - "5000:5000"

Server:

    public class ServerConfiguration
    {
        public string OwnerName { get; set; }
        public string SecretKey { get; set; }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Get the key from environment variables
            var ownerName = Environment.GetEnvironmentVariable("OWNER_NAME");
            var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");
            services.AddSingleton(new ServerConfiguration { OwnerName = ownerName, SecretKey = secretKey });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.MapPost("/auth", (ServerConfiguration config, [FromBody] string key) =>
            {
                if (key != config.SecretKey)
                {
                    return Results.Unauthorized("Invalid key.");
                }

                return Results.Ok("Authenticated.");
            });
        }
    }

Client:

    @inject HttpClient HttpClient
    @inject SecureStorage SecureStorage

    @code {
        private string _serverIp;
        private string _userKey;

        private HubConnection _hubConnection;

        private async Task ConnectToServer()
        {
            if (string.IsNullOrEmpty(_serverIp) || string.IsNullOrEmpty(_userKey))
            {
                // Prompt user to enter both IP and Key
                return;
            }

            // Authenticate with the server using the key
            var response = await HttpClient.PostAsJsonAsync($"http://{_serverIp}:5000/auth", _userKey);

            if (response.IsSuccessStatusCode)
            {
                // Key is valid, now proceed to connect to SignalR Hub
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"http://{_serverIp}:5000/chatHub")
                    .Build();

                await _hubConnection.StartAsync();
                // Proceed to chat or other operations
            }
            else
            {
                // Invalid key, show error message
                ShowErrorMessage("Invalid key or server IP.");
            }
        }
    }

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

Auto sync to external folder (that can be part of Google Drive, OneDrive, iCloud, Dropbox)

use MyDocuments instead of ApplicationData

foreach(folder in enum SpecialFolder) string path = Environment.GetFolderPath(folder); // Windows, Linux, macOS, iOS, Android

save and load without file dialog
sync: - save last loaded filename to DB, load if there is a new file / more recent

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

if (ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
{
    ActivityCompat.RequestPermissions(MainActivity.Instance, new string[] { Manifest.Permission.WriteExternalStorage }, 1);
}

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
}
if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
}
if (RuntimeInformation.IsOSPlatform(OSPlatform.Android))
{
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

Host 24/7 on Raspberry Pi

    Simply put if one of the devices is off a docker (always running server) could hold the data for when another device comes online.
    I have a service with the following parts.
    Mobile Client - Server (docker container) - cliënt / laptop / webapp etc. they all sync the encrypted data to the docker. If one dies it's on the others if the docker dies it's on the cliënts.
    The user sets a passphrase that works on all clients the server doesn't know anything except the size.
    A docker container is an easy way to host a service but also on someone's homelab if you have a fill in field for the server address the user could choose to do local sync and skip the internet and keep privacy.

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

- filters are query parameters

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

copy Loop Habit Tracker
- History (done count grouped by week, month, quarter, year)
- Calendar (continuous year calendar, no breaks in months: 7 days -> 7 rows (horizontal scroll) or 7 columns (vertical scroll))
- Best streaks (from date - to date)
- Frequency (by day of the week - continuous calendar, without dates, done count grouped by days of the week)

- drag & drop reorder
- keyboard navigation
- benchmark: method time & render time

- ASAP tasks
    - when, where, contact/company name, address, phone number, working hours, website, email
- date & time tasks

- don't use `event` to refresh everything on every change
- don't use `StateHasChanged()`
- don't do this: current screen changed -> save current screen to settings -> data changed -> refresh all

what is wrong: I'm not doing the critical tasks - because I see too many unimportant tasts that are overdue and I am satisfied with completing them

email: copy task list as HTML with checkboxes to clipboard
sms, message: copy task list with Unicode checkboxes

[X] Category must be set when creating a new note

virtualized container

method trace logging - benchmark method performance
https://learn.microsoft.com/en-us/aspnet/core/blazor/performance

---------------------------------------------------------------------------------------------------

- The modules' title bars look and act too similar to fields which confuses the experience to me - I think that's the one simple biggest UI fix I'd make. 
Make it a solid color and not part of the "add new" logic. 
https://imgur.com/a/kYlVFMq

- I feel like the modules should be collapsible? 
I don't quite understand what the box that contains the up/straight line/down arrow/no symbol is for (prioritizing and sorting I'm assuming) but I keep clicking on that thinking it will collapse the module.

- I'm a big fan of animations, and the simple ones are fairly easy with Blazor and CSS - even small or fast animations can make things feel much more intuitive
