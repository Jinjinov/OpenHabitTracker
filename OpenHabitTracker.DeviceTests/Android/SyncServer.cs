using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OpenHabitTracker.DeviceTests.Android;

// The self-hosted server, published by Scripts/build-sync-server.ps1, run on this machine with an empty database
// and reached from the emulator through adb reverse on the same port.
public sealed class SyncServer : IDisposable
{
    public const int Port = 5299;

    // What the app on the device types as the address: adb reverse maps the device's port to this machine's.
    public static readonly string DeviceAddress = $"http://127.0.0.1:{Port}";

    public const string AdminName = "devicetests";
    public const string AdminPassword = "Device-Tests-1";

    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenHabitTracker.DeviceTests", "server");

    private readonly HttpClient _http = new() { BaseAddress = new Uri($"http://127.0.0.1:{Port}") };

    private Process? _process;

    public void Start(bool emptyDatabase)
    {
        Assert.That(File.Exists(Path.Combine(Folder, "OpenHT.dll")), Is.True, $"The server is not published in {Folder}; run Scripts/build-sync-server.ps1");

        // The server keeps its database beside its binaries.
        if (emptyDatabase && Directory.Exists(Path.Combine(Folder, ".OpenHabitTracker")))
            Directory.Delete(Path.Combine(Folder, ".OpenHabitTracker"), recursive: true);

        ProcessStartInfo start = new("dotnet", "OpenHT.dll")
        {
            WorkingDirectory = Folder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{Port}";
        start.Environment["AppSettings__UserName"] = AdminName;
        start.Environment["AppSettings__Email"] = "devicetests@localhost";
        start.Environment["AppSettings__Password"] = AdminPassword;
        start.Environment["AppSettings__JwtSecret"] = "device-tests-secret-that-is-long-enough-for-hmac";

        _process = Process.Start(start)!;
        _process.OutputDataReceived += (_, _) => { };
        _process.ErrorDataReceived += (_, _) => { };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        DateTime deadline = DateTime.Now.AddMinutes(2);
        while (DateTime.Now < deadline)
        {
            try
            {
                if (_http.GetAsync("/").GetAwaiter().GetResult().IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
            }

            Assert.That(_process.HasExited, Is.False, "The server exited during startup");

            // The interval between polls; a refused connection returns at once.
            Thread.Sleep(500);
        }

        Assert.Fail("The server did not answer within two minutes");
    }

    public void Stop()
    {
        if (_process is null || _process.HasExited)
            return;

        _process.Kill(entireProcessTree: true);
        _process.WaitForExit();
    }

    public string Token(string username, string password)
    {
        HttpResponseMessage response = _http.PostAsJsonAsync("/api/auth/jwt-token", new { username, password }).GetAwaiter().GetResult();
        Assert.That(response.IsSuccessStatusCode, Is.True, $"The server refused {username}: {response.StatusCode}");

        return Property(response.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult(), "jwtToken").GetString()!;
    }

    public void FailLogin(string username)
    {
        HttpResponseMessage response = _http.PostAsJsonAsync("/api/auth/jwt-token", new { username, password = "Not-The-Password-1" }).GetAwaiter().GetResult();
        Assert.That(response.IsSuccessStatusCode, Is.False, $"A wrong password logged {username} in");
    }

    // An account of its own per test keeps one test's data and lockout away from the next.
    public void CreateAccount(string username, string password)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/api/account/accounts")
        {
            Content = JsonContent.Create(new { username, email = $"{username}@localhost", password })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(AdminName, AdminPassword));

        HttpResponseMessage response = _http.SendAsync(request).GetAwaiter().GetResult();
        Assert.That(response.IsSuccessStatusCode, Is.True, $"The server did not create {username}: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");
    }

    public List<string> NoteTitles(string username, string password)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/api/dataaccess/notes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(username, password));

        HttpResponseMessage response = _http.SendAsync(request).GetAwaiter().GetResult();
        Assert.That(response.IsSuccessStatusCode, Is.True, $"The server did not list the notes: {response.StatusCode}");

        return response.Content.ReadFromJsonAsync<JsonElement>().GetAwaiter().GetResult()
            .EnumerateArray()
            .Select(note => Property(note, "title").GetString() ?? "")
            .ToList();
    }

    // The server's JSON casing is its own configuration, so names are matched without case.
    private static JsonElement Property(JsonElement element, string name)
    {
        return element.EnumerateObject().First(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    public void Dispose()
    {
        Stop();
        _http.Dispose();
    }
}
