using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OpenHabitTracker.Localization;
using System.Globalization;

namespace OpenHabitTracker.UnitTests.Localization;

[TestFixture]
public class JsonStringLocalizerTests
{
    // The Web host renders every circuit on thread pool threads in parallel, through one singleton localizer.
    [Test]
    public void Indexer_CalledFromParallelThreads_DoesNotThrow()
    {
        IStringLocalizer localizer = new JsonStringLocalizerFactory(Options.Create(new LocalizationOptions { ResourcesPath = @"Localization\Resources" }))
            .Create(string.Empty, "OpenHabitTracker");

        string[] codes = Loc.Cultures.Keys.ToArray();
        string[] keys = ["Habits", "Notes", "Tasks", "Delete all data", "Settings", "Close", "Help"];

        // every language already loaded once, as on a server that has been running
        CultureInfo saved = CultureInfo.CurrentUICulture;

        foreach (string code in codes)
        {
            CultureInfo.CurrentUICulture = Loc.Cultures[code].Culture;
            _ = localizer["Habits"].Value;
        }

        CultureInfo.CurrentUICulture = saved;

        Exception? failure = null;

        Parallel.For(0, 16, new ParallelOptions { MaxDegreeOfParallelism = 16 }, worker =>
        {
            CultureInfo.CurrentUICulture = Loc.Cultures[codes[worker % codes.Length]].Culture;

            try
            {
                for (int i = 0; i < 200_000 && failure is null; i++)
                {
                    _ = localizer[keys[i % keys.Length]].Value;

                    if (i % 50 == 0)
                        _ = localizer[$"missing key {worker} {i % 997}"].Value;
                }
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
        });

        Assert.That(failure, Is.Null, failure?.ToString());
    }
}
