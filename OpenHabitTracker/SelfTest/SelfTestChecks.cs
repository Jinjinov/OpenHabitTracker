namespace OpenHabitTracker.SelfTest;

/// <summary>
/// Checks that are the same on every host, so a host only picks the ones that apply to it.
/// </summary>
public static class SelfTestChecks
{
    /// <summary>
    /// The resolved data directory exists and a file written there can be read back and removed.
    /// The reported path is half the value: a sandbox that redirects the write shows up in it.
    /// </summary>
    public static SelfTestCheck DataDirectory(string path) => new("data directory", async () =>
    {
        string probe = Path.Combine(path, $"selftest-{Guid.NewGuid():N}.tmp");

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

        return Path.GetFullPath(path);
    });
}
