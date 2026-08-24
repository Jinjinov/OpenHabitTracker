namespace OpenHabitTracker.SelfTest;

/// <summary>
/// Runs the checks a host registers and reports the result on stdout.
/// The point is to run inside the packaged app, in whatever sandbox it was installed into,
/// so it must never create a window or need a user gesture.
/// </summary>
public static class SelfTestRunner
{
    public const string Argument = "--self-test";

    public static bool IsRequested(IEnumerable<string> args) => args.Contains(Argument);

    /// <summary>
    /// For entry points that are handed no arguments of their own (WinForms, Wpf, Maui).
    /// Always false on Android and iOS, which start the app without a command line.
    /// </summary>
    public static bool IsRequested() => IsRequested(Environment.GetCommandLineArgs());

    /// <summary>
    /// For entry points that cannot await. The work goes to the thread pool on purpose:
    /// Wpf and Maui already have a dispatcher SynchronizationContext installed by the time their
    /// startup handler runs, so blocking on a check that awaits anything would deadlock the
    /// very thread the continuation needs.
    /// </summary>
    public static int RunSync(IEnumerable<SelfTestCheck> checks, TextWriter output) =>
        Task.Run(() => Run(checks, output)).GetAwaiter().GetResult();

    public static async Task<int> Run(IEnumerable<SelfTestCheck> checks, TextWriter output)
    {
        int total = 0;
        int failed = 0;

        foreach (SelfTestCheck check in checks)
        {
            total++;

            try
            {
                string detail = await check.Run();

                output.WriteLine(string.IsNullOrEmpty(detail) ? $"PASS {check.Name}" : $"PASS {check.Name} - {detail}");
            }
            catch (Exception exception)
            {
                failed++;

                output.WriteLine($"FAIL {check.Name} - {exception.Message}");
            }
        }

        output.WriteLine($"{total - failed}/{total} passed");
        output.Flush();

        return failed == 0 ? 0 : 1;
    }
}
