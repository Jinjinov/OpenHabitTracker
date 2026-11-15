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

    private static JsonStringLocalizerFactory Factory() =>
        new(Options.Create(new LocalizationOptions { ResourcesPath = @"Localization\Resources" }));

    private static IStringLocalizer Shared() => Factory().Create(string.Empty, "OpenHabitTracker");

    [TestCase("en", "Habits")]
    [TestCase("sl", "Navade")]
    [TestCase("de", "Gewohnheiten")]
    public void Indexer_ReadsTheFileOfTheUiCulture(string culture, string expected)
    {
        CultureInfo saved = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);

            LocalizedString habits = Shared()["Habits"];

            Assert.Multiple(() =>
            {
                Assert.That(habits.Name, Is.EqualTo("Habits"));
                Assert.That(habits.Value, Is.EqualTo(expected));
            });
        }
        finally
        {
            CultureInfo.CurrentUICulture = saved;
        }
    }

    [Test]
    [SetUICulture("ru")]
    public void Indexer_LanguageTheAppDoesNotShip_FallsBackToEnglish()
    {
        Assert.That(Shared()["Habits"].Value, Is.EqualTo("Habits"));
    }

    [Test]
    [SetUICulture("de-AT")]
    public void Indexer_RegionalCulture_UsesItsLanguage()
    {
        Assert.That(Shared()["Habits"].Value, Is.EqualTo("Gewohnheiten"));
    }

    [Test]
    [SetUICulture("en")]
    public void Indexer_MissingKey_ShowsTheMarkerInDebugAndTheKeyInRelease()
    {
        LocalizedString missing = Shared()["no such key in any file"];

        Assert.That(missing.Value, Is.EqualTo("❗ no such key in any file ❗").Or.EqualTo("no such key in any file"));
    }

    [Test]
    [SetUICulture("en")]
    public void Indexer_WithArguments_FormatsTheTranslation()
    {
        Assert.That(Shared()["Done out of total", 3, 5].Value, Is.EqualTo("3 out of 5 done"));
    }

    [Test]
    [SetUICulture("sl")]
    public void Indexer_WithArgumentsAndAMissingKey_FormatsTheKey()
    {
        Assert.That(Shared()["missing {0}", 7].Value, Is.EqualTo("❗ missing 7 ❗").Or.EqualTo("missing 7"));
    }

    [Test]
    [SetUICulture("en")]
    public void GetAllStrings_ListsTheLoadedLanguage()
    {
        IStringLocalizer localizer = Shared();
        _ = localizer["Habits"];

        List<LocalizedString> all = localizer.GetAllStrings(true).ToList();

        Assert.That(all.Single(x => x.Name == "Notes").Value, Is.EqualTo("Notes"));
    }

    [Test]
    [SetUICulture("en")]
    public void Create_ForATypeWithItsOwnFile_ReadsThatFile()
    {
        IStringLocalizer localizer = Factory().Create(typeof(OpenHabitTracker.Blazor.Components.GuidedTourComponent));

        Assert.Multiple(() =>
        {
            Assert.That(localizer["Open Notes."].Value, Is.EqualTo("Open Notes."));
            Assert.That(localizer["Habits"].Value, Is.EqualTo("❗ Habits ❗").Or.EqualTo("Habits"));
        });
    }

    [Test]
    [SetUICulture("en")]
    public void Create_ForATypeWithoutItsOwnFile_ReadsTheSharedFile()
    {
        IStringLocalizer localizer = Factory().Create(typeof(JsonStringLocalizerTests));

        Assert.That(localizer["Habits"].Value, Is.EqualTo("Habits"));
    }

    [Test]
    public void Create_SameTypeOrNameTwice_ReturnsTheCachedLocalizer()
    {
        JsonStringLocalizerFactory factory = Factory();

        IStringLocalizer firstByType = factory.Create(typeof(JsonStringLocalizerTests));
        IStringLocalizer firstByName = factory.Create(string.Empty, "OpenHabitTracker");

        Assert.Multiple(() =>
        {
            Assert.That(factory.Create(typeof(JsonStringLocalizerTests)), Is.SameAs(firstByType));
            Assert.That(factory.Create(string.Empty, "OpenHabitTracker"), Is.SameAs(firstByName));
        });
    }

    [Test]
    public void Create_UnknownAssembly_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Factory().Create("Base", "No.Such.Assembly"));
    }

    // Every language file is read by the same keys, so a key missing from one shows the marker in that language only.
    private static IEnumerable<TestCaseData> LanguageFiles()
    {
        string resources = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenHabitTracker", "Localization", "Resources");

        foreach (string prefix in new[] { "", "GuidedTourComponent-" })
        {
            foreach (string code in Loc.Cultures.Keys.Where(x => x != "en"))
                yield return new TestCaseData(Path.Combine(resources, $"{prefix}en.json"), Path.Combine(resources, $"{prefix}{code}.json")).SetArgDisplayNames($"{prefix}{code}");
        }
    }

    private static Dictionary<string, string> ReadLanguageFile(string path) =>
        System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;

    [TestCaseSource(nameof(LanguageFiles))]
    public void LanguageFile_HasExactlyTheEnglishKeys(string englishPath, string languagePath)
    {
        Dictionary<string, string> english = ReadLanguageFile(englishPath);
        Dictionary<string, string> language = ReadLanguageFile(languagePath);

        Assert.Multiple(() =>
        {
            Assert.That(english.Keys.Except(language.Keys), Is.Empty, "missing");
            Assert.That(language.Keys.Except(english.Keys), Is.Empty, "extra");
        });
    }

    // string.Format throws when a translation names an argument the English text does not pass.
    [TestCaseSource(nameof(LanguageFiles))]
    public void LanguageFile_UsesTheSamePlaceholdersAsEnglish(string englishPath, string languagePath)
    {
        Dictionary<string, string> english = ReadLanguageFile(englishPath);
        Dictionary<string, string> language = ReadLanguageFile(languagePath);

        List<string> mismatched = english
            .Where(x => language.ContainsKey(x.Key))
            .Where(x => !Placeholders(x.Value).SetEquals(Placeholders(language[x.Key])))
            .Select(x => x.Key)
            .ToList();

        Assert.That(mismatched, Is.Empty);
    }

    private static HashSet<string> Placeholders(string text) =>
        System.Text.RegularExpressions.Regex.Matches(text, @"\{\d+[^}]*\}").Select(x => x.Value).ToHashSet();
}
