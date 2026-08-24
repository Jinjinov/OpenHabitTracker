using OpenHabitTracker.App;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;

namespace OpenHabitTracker.SelfTest;

/// <summary>
/// Checks that are the same on every host, so a host only picks the ones that apply to it.
/// </summary>
public static class SelfTestChecks
{
    private const string ReachableHost = "openhabittracker.net";

    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The checks every host runs. A host adds its own on top rather than repeating these.
    /// </summary>
    public static IEnumerable<SelfTestCheck> Standard(string dataDirectory) =>
    [
        DataDirectory(dataDirectory),
        UserDirectory(dataDirectory),
        Network(),
        Localization(),
        TimeZone()
    ];

    /// <summary>
    /// The checks that only mean something where there is a window and a crash handler,
    /// so Photino, WinForms, Wpf and Maui run these on top of <see cref="Standard"/>.
    /// </summary>
    public static IEnumerable<SelfTestCheck> Desktop(string dataDirectory) =>
    [
        .. Standard(dataDirectory),
        CrashLog(dataDirectory),
        WindowGeometry(dataDirectory)
    ];

    /// <summary>
    /// The crash handler's own write path, exercised through the same call the handler makes,
    /// so a directory it cannot write to is found before a crash needs it rather than after.
    /// A probe name is used because the real Error.log is evidence and must not be overwritten.
    /// </summary>
    public static SelfTestCheck CrashLog(string dataDirectory) => new("crash log", () =>
    {
        string fileName = $"selftest-{Guid.NewGuid():N}.log";
        string message = $"self test {DateTime.Now:O}";

        App.CrashLog.Write(dataDirectory, message, fileName);

        try
        {
            string? read = App.CrashLog.Read(dataDirectory, fileName);

            if (read != message)
                throw new InvalidOperationException(read is null ? "the log was not written" : "the log was written but read back different");
        }
        finally
        {
            File.Delete(Path.Combine(dataDirectory, fileName));
        }

        return Task.FromResult(Path.Combine(Path.GetFullPath(dataDirectory), App.CrashLog.FileName));
    });

    /// <summary>
    /// Window geometry survives a save and load through the real serializer.
    /// Both sides swallow their own exceptions on purpose, so a broken round-trip is silent at
    /// runtime and only a comparison of the values proves anything.
    /// </summary>
    public static SelfTestCheck WindowGeometry(string dataDirectory) => new("window geometry", () =>
    {
        string path = Path.Combine(dataDirectory, $"selftest-{Guid.NewGuid():N}.yaml");

        WindowSettings written = new() { X = 11, Y = 22, Width = 1033, Height = 744 };

        written.Save(path);

        try
        {
            WindowSettings? read = WindowSettings.Load(path) ?? throw new InvalidOperationException("saved geometry did not load back");

            if (read.X != written.X || read.Y != written.Y || read.Width != written.Width || read.Height != written.Height)
                throw new InvalidOperationException($"loaded {read.X},{read.Y} {read.Width}x{read.Height} after saving {written.X},{written.Y} {written.Width}x{written.Height}");
        }
        finally
        {
            File.Delete(path);
        }

        return Task.FromResult(Path.Combine(Path.GetFullPath(dataDirectory), WindowSettings.FileName));
    });

    /// <summary>
    /// The resolved data directory exists and a file written there can be read back and removed.
    /// The reported path is half the value: a sandbox that redirects the write shows up in it.
    /// </summary>
    public static SelfTestCheck DataDirectory(string path) => new("data directory", async () =>
    {
        await WriteReadDelete(path);

        return Path.GetFullPath(path);
    });

    /// <summary>
    /// A file written to the user's Documents folder lands in the real one.
    /// Without filesystem permission a Flatpak silently redirects this into its own private tree
    /// and a Snap without the home plug refuses the write outright, which is the whole point:
    /// export reports success either way, so only the resulting path proves anything.
    /// </summary>
    public static SelfTestCheck UserDirectory(string dataDirectory) => new("user directory", async () =>
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (string.IsNullOrEmpty(documents))
            throw new InvalidOperationException("no Documents folder is defined");

        Directory.CreateDirectory(documents);

        await WriteReadDelete(documents);

        string full = Path.GetFullPath(documents);

        if (full.StartsWith(Path.GetFullPath(dataDirectory), StringComparison.Ordinal))
            throw new InvalidOperationException($"redirected into the app's own data directory: {full}");

        if (Environment.GetEnvironmentVariable("FLATPAK_ID") is not null && full.Contains("/.var/app/", StringComparison.Ordinal))
            throw new InvalidOperationException($"redirected into the Flatpak sandbox: {full}");

        return full;
    });

    /// <summary>
    /// The process can open a socket and complete one request.
    /// A Flatpak without --share=network has no network namespace at all, so sync cannot work
    /// however correct the app is.
    /// </summary>
    public static SelfTestCheck Network() => new("network", async () =>
    {
        using TcpClient client = new();

        await client.ConnectAsync(ReachableHost, 443).WaitAsync(NetworkTimeout);

        using HttpClient http = new() { Timeout = NetworkTimeout };

        using HttpResponseMessage response = await http.GetAsync($"https://{ReachableHost}", HttpCompletionOption.ResponseHeadersRead);

        return $"{ReachableHost} {(int)response.StatusCode}";
    });

    /// <summary>
    /// Every localization resource is present, parses, and carries the same keys as English.
    /// Per-host packaging is what breaks this: a file named to look culture-specific ends up in a
    /// satellite assembly instead of the one the loader reads.
    /// </summary>
    public static SelfTestCheck Localization() => new("localization", () =>
    {
        Assembly assembly = typeof(SelfTestChecks).Assembly;

        const string prefix = "OpenHabitTracker.Localization.Resources.";

        Dictionary<string, HashSet<string>> byResource = [];

        foreach (string name in assembly.GetManifestResourceNames().Where(x => x.StartsWith(prefix, StringComparison.Ordinal) && x.EndsWith(".json", StringComparison.Ordinal)))
        {
            using Stream stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"{name} cannot be opened");

            using JsonDocument document = JsonDocument.Parse(stream);

            byResource[name[prefix.Length..^".json".Length]] = [.. document.RootElement.EnumerateObject().Select(x => x.Name)];
        }

        const string tourPrefix = "GuidedTourComponent-";

        List<string> languages = [.. byResource.Keys.Where(x => !x.StartsWith(tourPrefix, StringComparison.Ordinal))];
        List<string> tours = [.. byResource.Keys.Where(x => x.StartsWith(tourPrefix, StringComparison.Ordinal)).Select(x => x[tourPrefix.Length..])];

        if (languages.Count == 0)
            throw new InvalidOperationException("no localization resources are embedded in this build");

        List<string> missingTour = [.. languages.Except(tours).Order()];

        if (missingTour.Count > 0)
            throw new InvalidOperationException($"no guided tour resource for {string.Join(", ", missingTour)}");

        if (!byResource.TryGetValue("en", out HashSet<string>? english))
            throw new InvalidOperationException("the English resource is missing");

        List<string> incomplete = [.. languages.Where(x => !byResource[x].SetEquals(english)).Order()];

        if (incomplete.Count > 0)
            throw new InvalidOperationException($"key set differs from English in {string.Join(", ", incomplete)}");

        return Task.FromResult($"{languages.Count} languages, {english.Count} keys");
    });

    /// <summary>
    /// No assertion is possible here, because only the user knows which zone is right.
    /// Reading it is the test: a container defaulting to UTC moves every habit's day boundary.
    /// </summary>
    public static SelfTestCheck TimeZone() => new("time zone", () =>
        Task.FromResult($"{TimeZoneInfo.Local.Id}, now {DateTime.Now:yyyy-MM-dd HH:mm}, day starts {DateTime.Today:yyyy-MM-dd HH:mm}"));

    private static async Task WriteReadDelete(string directory)
    {
        string probe = Path.Combine(directory, $"selftest-{Guid.NewGuid():N}.tmp");

        await File.WriteAllTextAsync(probe, nameof(SelfTestChecks));

        try
        {
            string read = await File.ReadAllTextAsync(probe);

            if (read != nameof(SelfTestChecks))
                throw new InvalidOperationException($"read back {read.Length} chars, expected {nameof(SelfTestChecks).Length}");
        }
        finally
        {
            File.Delete(probe);
        }
    }
}
