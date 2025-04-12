using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OpenHabitTracker.CloudStorage.Desktop;

public class GoogleDriveService : IGoogleDriveService
{
    // If modifying these scopes, delete your previously saved "Ididit" folder
    private readonly string[] _scopes = { DriveService.Scope.DriveFile };

    private const string _applicationName = "ididit";

    private static GoogleClientSecrets? GetGoogleClientSecrets()
    {
        // D:\Jinjinov\Ididit\Ididit.WebView.Maui\bin\Debug\net6.0-windows10.0.19041.0\win10-x64\AppX\
        // D:\Jinjinov\Ididit\Ididit.WebView.Wpf\bin\Debug\net6.0-windows\
        // "/media/sf_Jinjinov/Ididit/Ididit.WebView.Photino/bin/Debug/net6.0/"
        // "/Users/Urban/Projects/Ididit/Ididit.WebView.Maui/bin/Debug/net6.0-maccatalyst/maccatalyst-x64/Ididit.WebView.Maui.app/Contents/MonoBundle"
        string baseDirectory = AppContext.BaseDirectory;

        /*
        baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        These days (.NET Core, .NET Standard 1.3+ or .NET Framework 4.6+) it's better to use AppContext.BaseDirectory rather than AppDomain.CurrentDomain.BaseDirectory. 
        Both are equivalent, but multiple AppDomains are no longer supported. https://learn.microsoft.com/en-us/dotnet/core/porting/net-framework-tech-unavailable

        // C:\WINDOWS\system32
        // D:\Jinjinov\Ididit\Ididit.WebView.Wpf\bin\Debug\net6.0-windows
        // "/media/sf_Jinjinov/Ididit"
        // "/Users/Urban/Projects/Ididit/Ididit.WebView.Maui/bin/Debug/net6.0-maccatalyst/maccatalyst-x64/Ididit.WebView.Maui.app"
        baseDirectory = Environment.CurrentDirectory;
        baseDirectory = Directory.GetCurrentDirectory(); // public static string GetCurrentDirectory() => Environment.CurrentDirectory;
        /**/

        while (!File.Exists(Path.Combine(baseDirectory, "credentials.json")))
        {
            string? parent = Directory.GetParent(baseDirectory)?.FullName; // macOS work-around

            if (parent == null)
                return null;
            else
                baseDirectory = parent;
        }

        string path = Path.Combine(baseDirectory, "credentials.json");

        GoogleClientSecrets googleClientSecrets;

        using (FileStream stream = new(path, FileMode.Open, FileAccess.Read))
        {
            googleClientSecrets = GoogleClientSecrets.FromStream(stream);
        }

        return googleClientSecrets;
    }

    public async Task<DriveService?> GetDriveService()
    {
        /*
        GoogleClientSecrets? googleClientSecrets = GetGoogleClientSecrets() ?? System.Text.Json.JsonSerializer.Deserialize<GoogleClientSecrets>(CredentialsJson);

        if (googleClientSecrets == null)
            return null;

        ClientSecrets clientSecrets = googleClientSecrets.Secrets;
        /**/

        // The folder "Ididit" stores the user's access and refresh tokens, and is created automatically when the authorization flow completes for the first time
        string credPath = "Ididit";

        GoogleDriveClientSecrets googleDriveClientSecrets = new();

        // https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth/OAuth2/LocalServerCodeReceiver.cs
        // throw new NotSupportedException($"Failed to launch browser with \"{authorizationUrl}\" for authorization; platform not supported.");

        UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            googleDriveClientSecrets.ClientSecrets,
            _scopes,
            "user",
            CancellationToken.None,
            new FileDataStore(credPath)); // Defines whether the folder parameter is absolute or relative to
                                            // Environment.SpecialFolder.ApplicationData on Windows,
                                            // C:\Users\<user>\AppData\Roaming\
                                            // or $HOME on Linux and MacOS.

        DriveService service = new(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _applicationName
        });

        return service;
    }

    // https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth/OAuth2/LocalServerCodeReceiver.cs#L530
    // https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth/OAuth2/LocalServerCodeReceiver.cs#L538
    // https://github.com/googleapis/google-api-dotnet-client/blob/main/Src/Support/Google.Apis.Auth/OAuth2/LocalServerCodeReceiver.cs#L543

    /*
        protected virtual bool OpenBrowser(string url)
        {
            // See https://github.com/dotnet/corefx/issues/10361
            // This is best-effort only, but should work most of the time.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // See https://stackoverflow.com/a/6040946/44360 for why this is required
                url = System.Text.RegularExpressions.Regex.Replace(url, @"(\\*)" + "\"", @"$1$1\" + "\"");
                url = System.Text.RegularExpressions.Regex.Replace(url, @"(\\+)$", @"$1$1");
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"") { CreateNoWindow = true });
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
                return true;
            }
            return false;
        }
    /**/
}
