using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.Loop;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Backup;

[TestFixture]
public class LoopImportTests
{
    private LoopImport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _sut = new LoopImport(loc);
    }

    private static string Fixture(string path) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Loop", path);

    private async Task<ImportedData> Read(string path)
    {
        await using Stream stream = System.IO.File.OpenRead(Fixture(path));

        return await _sut.ReadDataFile(stream);
    }

    private static HabitModel Habit(ImportedData data, string title) => data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    private static DateTime Midday(int year, int month, int day) => new(year, month, day, 12, 0, 0);

    [Test]
    public async Task PhoneBackup_ArchivedHabit_GoesIntoAHiddenArchivedCategory()
    {
        ImportedData data = await Read("Phone/backup.db");

        CategoryModel archived = data.Categories.Single(x => x.Title == "Archived");

        Assert.Multiple(() =>
        {
            Assert.That(data.Categories.Single(x => x.Title == "").Habits.Select(x => x.Title),
                Is.EqualTo(new[] { "a (kilometrov)", "b", "Run (km)", "Water plants", "Gym", "Coffee (cups)" }));
            Assert.That(archived.Habits.Select(x => x.Title), Is.EqualTo(new[] { "c" }));
            Assert.That(data.Options.HiddenCategories, Is.EqualTo(new[] { archived }));
            Assert.That(data.Options.MatchCompletionsByDay, Is.True);
        });
    }

    [Test]
    public async Task PhoneBackup_NumericalHabit_KeepsItsTargetAndValuesWithoutTheSkip()
    {
        HabitModel run = Habit(await Read("Phone/backup.db"), "Run (km)");

        Assert.Multiple(() =>
        {
            Assert.That(run.DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(run.TargetQuantity, Is.EqualTo(2.5));
            Assert.That(run.Color, Is.EqualTo("bg-danger-subtle"));
            Assert.That(run.TimesDone!.Select(x => (x.StartedAt, x.CompletedAt, x.Quantity)),
                Is.EqualTo(new[] { (Midday(2026, 11, 6), (DateTime?)Midday(2026, 11, 6), 1.5), (Midday(2026, 11, 8), (DateTime?)Midday(2026, 11, 8), 3.2) }));
            Assert.That(run.LastTimeDoneAt, Is.EqualTo(Midday(2026, 11, 8)));
        });
    }

    [Test]
    public async Task PhoneBackup_AtMostHabit_ImportsItsLimitAsTheTarget()
    {
        HabitModel coffee = Habit(await Read("Phone/backup.db"), "Coffee (cups)");

        Assert.Multiple(() =>
        {
            Assert.That(coffee.TargetQuantity, Is.EqualTo(2));
            Assert.That(coffee.TimesDone!.Select(x => x.Quantity), Is.EqualTo(new[] { 1.0, 3.0 }));
        });
    }

    [Test]
    public async Task PhoneBackup_LargestStoredValue_IsReadInThousandths()
    {
        HabitModel a = Habit(await Read("Phone/backup.db"), "a (kilometrov)");

        Assert.Multiple(() =>
        {
            Assert.That(a.TargetQuantity, Is.EqualTo(999999999));
            Assert.That(a.TimesDone!.Select(x => x.Quantity), Is.EqualTo(new[] { 2147483.647, 4.0, 3.0, 2.0, 1.0 }));
        });
    }

    [Test]
    public async Task PhoneBackup_Frequencies_MapToCountsPerDaysOrWeeks()
    {
        ImportedData data = await Read("Phone/backup.db");

        HabitModel waterPlants = Habit(data, "Water plants");
        HabitModel gym = Habit(data, "Gym");
        HabitModel b = Habit(data, "b");

        Assert.Multiple(() =>
        {
            Assert.That((waterPlants.RepeatCount, waterPlants.RepeatInterval, waterPlants.RepeatPeriod), Is.EqualTo((1, 3, Period.Day)));
            Assert.That((gym.RepeatCount, gym.RepeatInterval, gym.RepeatPeriod), Is.EqualTo((3, 1, Period.Week)));
            Assert.That((b.RepeatCount, b.RepeatInterval, b.RepeatPeriod), Is.EqualTo((1, 1, Period.Day)));
        });
    }

    [Test]
    public async Task PhoneBackup_YesNoHabit_KeepsOnlyTheDaysItWasDone()
    {
        ImportedData data = await Read("Phone/backup.db");

        HabitModel waterPlants = Habit(data, "Water plants");

        Assert.Multiple(() =>
        {
            Assert.That(waterPlants.DisplayMetric, Is.EqualTo(DisplayMetric.Repetitions));
            Assert.That(waterPlants.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 5), Midday(2026, 11, 8) }));
            Assert.That(waterPlants.TimesDone!.Select(x => x.Quantity), Is.All.EqualTo(1.0));
        });
    }

    [Test]
    public async Task Schema12_EveryRowIsADoneDayAndColorsAreRgb()
    {
        ImportedData data = await Read("Repository/loop.db");

        List<HabitModel> habits = data.Categories.SelectMany(x => x.Habits).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(habits, Has.Count.EqualTo(9));
            Assert.That(habits.Sum(x => x.TimesDone!.Count), Is.EqualTo(164));
            Assert.That(habits.Select(x => x.Color).Distinct(), Is.EquivalentTo(new[] { "bg-info-subtle", "bg-warning-subtle" }));
            Assert.That(habits.All(x => x.DisplayMetric == DisplayMetric.Repetitions), Is.True);
            Assert.That((habits[0].RepeatCount, habits[0].RepeatInterval, habits[0].RepeatPeriod), Is.EqualTo((3, 1, Period.Week)));
            Assert.That((habits[2].RepeatCount, habits[2].RepeatInterval, habits[2].RepeatPeriod), Is.EqualTo((1, 5, Period.Day)));
            Assert.That(habits.SelectMany(x => x.TimesDone!).Min(x => x.StartedAt), Is.EqualTo(Midday(2016, 1, 1)));
            Assert.That(data.Options.HiddenCategories, Is.Empty);
        });
    }

    [Test]
    public async Task Schemas21And22_AreRead()
    {
        ImportedData schema21 = await Read("Repository/databases/021.db");
        ImportedData schema22 = await Read("Repository/databases/022.db");

        Assert.Multiple(() =>
        {
            Assert.That(schema21.Categories.Sum(x => x.Habits.Count), Is.EqualTo(1));
            Assert.That(schema21.Categories.SelectMany(x => x.Habits).Sum(x => x.TimesDone!.Count), Is.EqualTo(3));
            Assert.That(schema22.Categories.Sum(x => x.Habits.Count), Is.EqualTo(2));
            Assert.That(schema22.Categories.SelectMany(x => x.Habits).Sum(x => x.TimesDone!.Count), Is.EqualTo(1));
        });
    }
}
