using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.Loop;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.IO.Compression;

namespace OpenHabitTracker.UnitTests.Backup;

[TestFixture]
public class LoopCsvImportTests
{
    private LoopCsvImport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _sut = new LoopCsvImport(loc);
    }

    private static string Fixture(string path) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Loop", path);

    private async Task<ImportedData> Read(string path)
    {
        await using Stream stream = System.IO.File.OpenRead(Fixture(path));

        return await _sut.ReadDataFile(stream);
    }

    // Loop's own export is a folder in its repository, so it is zipped the way the app writes it.
    private async Task<ImportedData> ReadRepositoryExport()
    {
        MemoryStream zip = new();

        ZipFile.CreateFromDirectory(Fixture("Repository/csv_export"), zip);

        zip.Position = 0;

        return await _sut.ReadDataFile(zip);
    }

    private static HabitModel Habit(ImportedData data, string title) => data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    private static DateTime Midday(int year, int month, int day) => new(year, month, day, 12, 0, 0);

    [Test]
    public async Task PhoneExport_ReadsWhatTheDatabaseOfTheSameHabitsHolds()
    {
        ImportedData csv = await Read("Phone/export.zip");

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        await using Stream database = System.IO.File.OpenRead(Fixture("Phone/backup.db"));

        ImportedData backup = await new LoopImport(loc).ReadDataFile(database);

        static List<string> Shape(ImportedData data) => data.Categories
            .SelectMany(category => category.Habits.Select(habit =>
                $"{category.Title}/{habit.Title} {habit.Color} {habit.RepeatCount}/{habit.RepeatInterval} {habit.RepeatPeriod} {habit.DisplayMetric} {habit.TargetQuantity} " +
                string.Join(" ", habit.TimesDone!.Select(x => $"{x.StartedAt:yyyy-MM-dd HH:mm}={x.Quantity}"))))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(Shape(csv), Is.EqualTo(Shape(backup)));
            Assert.That(csv.Options.MatchCompletionsByDay, Is.True);
            Assert.That(csv.Options.HiddenCategories.Select(x => x.Title), Is.EqualTo(new[] { "Archived" }));
        });
    }

    [Test]
    public async Task PhoneExport_FillsInDaysAndSkipsAreLeftOut()
    {
        HabitModel waterPlants = Habit(await Read("Phone/export.zip"), "Water plants");

        Assert.That(waterPlants.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 5), Midday(2026, 11, 8) }));
    }

    [Test]
    public async Task RepositoryExport_HabitsGoIntoTheirCategories()
    {
        ImportedData data = await ReadRepositoryExport();

        Assert.Multiple(() =>
        {
            Assert.That(data.Categories.Select(x => (x.Title, string.Join("|", x.Habits.Select(h => h.Title)))),
                Is.EqualTo(new[] { ("", "Meditate|Wake up early|Foo, Bar|Night run"), ("Books", "Reading"), ("Health, Wellness", "Stretch") }));
            Assert.That(data.Options.HiddenCategories, Is.Empty);
        });
    }

    [Test]
    public async Task RepositoryExport_FolderNamedWithoutTheCommaIsFoundByPosition()
    {
        ImportedData data = await ReadRepositoryExport();

        HabitModel fooBar = Habit(data, "Foo, Bar");
        HabitModel wakeUpEarly = Habit(data, "Wake up early");

        Assert.Multiple(() =>
        {
            Assert.That(fooBar.TimesDone, Has.Count.EqualTo(1));
            Assert.That(fooBar.Color, Is.EqualTo("bg-primary-subtle"));
            Assert.That((wakeUpEarly.RepeatCount, wakeUpEarly.RepeatInterval, wakeUpEarly.RepeatPeriod), Is.EqualTo((2, 3, Period.Day)));
            Assert.That(wakeUpEarly.Color, Is.EqualTo("bg-info-subtle"));
        });
    }

    [Test]
    public async Task TableHabitExport_ArchivedAtMostAndFrequencies()
    {
        ImportedData data = await Read("TableHabit/loop_habits_csv_v2.3.1.zip");

        HabitModel valueHabit = Habit(data, "value habit (unit)");
        HabitModel boolHabit = Habit(data, "bool habit");

        Assert.Multiple(() =>
        {
            Assert.That(data.Categories.Single(x => x.Title == "Archived").Habits.Select(x => x.Title), Is.EqualTo(new[] { "someone" }));
            Assert.That(valueHabit.DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(valueHabit.TargetQuantity, Is.EqualTo(12));
            Assert.That((valueHabit.RepeatCount, valueHabit.RepeatInterval, valueHabit.RepeatPeriod), Is.EqualTo((1, 1, Period.Month)));
            Assert.That(valueHabit.TimesDone!.Select(x => x.Quantity), Is.EqualTo(new[] { 30.0, 3.0, 12.0 }));
            Assert.That((boolHabit.RepeatCount, boolHabit.RepeatInterval, boolHabit.RepeatPeriod), Is.EqualTo((3, 14, Period.Day)));
            Assert.That(boolHabit.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 7, 2), Midday(2026, 7, 4) }));
            Assert.That(boolHabit.LastTimeDoneAt, Is.EqualTo(Midday(2026, 7, 4)));
        });
    }
}
