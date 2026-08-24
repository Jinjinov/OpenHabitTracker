using System.Reflection;

namespace OpenHabitTracker.App;

// The single source is <Version> in Directory.Build.props, which the SDK turns into this attribute
// on every assembly in the solution. Nothing else in the app may carry the version as text.
public static class AppVersion
{
    public static string Current { get; } = Read();

    private static string Read()
    {
        string? informational = typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informational))
            return typeof(AppVersion).Assembly.GetName().Version?.ToString(3) ?? "";

        // The SDK appends "+<commit sha>" when the repository is available at build time.
        int plus = informational.IndexOf('+', StringComparison.Ordinal);

        return plus < 0 ? informational : informational[..plus];
    }
}
