using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.HabitNow;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Backup;

[TestFixture]
public class HabitNowImportTests
{
    private HabitNowImport _sut = null!;

    [SetUp]
    public void SetUp()
    {
        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _sut = new HabitNowImport(loc);
    }

    private async Task<ImportedData> Read(string file)
    {
        await using Stream stream = System.IO.File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "HabitNow", file));

        return await _sut.ReadDataFile(stream);
    }

    private static HabitModel Habit(ImportedData data, string title) => data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    private static TaskModel Task(ImportedData data, string title) => data.Categories.SelectMany(x => x.Tasks).Single(x => x.Title == title);

    private static string CategoryOf(ImportedData data, string title) =>
        data.Categories.Single(x => x.Habits.Any(h => h.Title == title) || x.Tasks.Any(t => t.Title == title)).Title;

    private static DateTime Midday(int year, int month, int day) => new(year, month, day, 12, 0, 0);

    private static (int, int, Period) Rule(HabitModel habit) => (habit.RepeatCount, habit.RepeatInterval, habit.RepeatPeriod);

    [Test]
    public async Task EveryRepeatType_MapsToACountPerDaysWeeksOrMonths()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(Habit(data, "Drink water")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(Habit(data, "Read a book")), Is.EqualTo((3, 1, Period.Week)));
            Assert.That(Rule(Habit(data, "Water plants")), Is.EqualTo((1, 3, Period.Day)));
            Assert.That(Rule(Habit(data, "Pay bills")), Is.EqualTo((2, 1, Period.Month)));
            Assert.That(Rule(Habit(data, "Gym")), Is.EqualTo((3, 1, Period.Week)));
        });
    }

    [Test]
    public async Task DaysPerMonth_IsACountPerMonth()
    {
        ImportedData data = await Read("backup-other-years.hn");

        Assert.That(Rule(Habit(data, "Gym")), Is.EqualTo((2, 1, Period.Month)));
    }

    [Test]
    public async Task FrequencyChange_ImportsOneHabitWithTheRunningTracksRuleAndAllEntries()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        List<HabitModel> gyms = data.Categories.SelectMany(x => x.Habits).Where(x => x.Title == "Gym").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(gyms, Has.Count.EqualTo(1));
            Assert.That(gyms[0].StartAt, Is.EqualTo(new DateTime(2026, 11, 1)));
            Assert.That(gyms[0].TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 8) }));
            Assert.That(CategoryOf(data, "Gym"), Is.EqualTo("Sports"));
        });
    }

    [Test]
    public async Task YesNoHabit_KeepsDoneDaysAndDropsPendingAndSkipped()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        Assert.Multiple(() =>
        {
            Assert.That(Habit(data, "Water plants").TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 4), Midday(2026, 11, 7) }));
            Assert.That(Habit(data, "Drink water").TimesDone, Has.Count.EqualTo(5));
            Assert.That(Habit(data, "Drink water").LastTimeDoneAt, Is.EqualTo(Midday(2026, 11, 8)));
        });
    }

    [Test]
    public async Task NumericHabit_KeepsFailedEntriesWithTheirValue()
    {
        HabitModel run = Habit(await Read("backup-frequency-tracks.hn"), "Run (km)");

        Assert.Multiple(() =>
        {
            Assert.That(run.DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(run.TargetQuantity, Is.EqualTo(2.5));
            Assert.That(run.TimesDone!.Select(x => (x.StartedAt, x.Quantity)),
                Is.EqualTo(new[] { (Midday(2026, 11, 2), 3.2), (Midday(2026, 11, 4), 1.5), (Midday(2026, 11, 7), 2.5) }));
        });
    }

    [Test]
    public async Task TimerHabit_TargetIsTheDurationAndSecondsAreTheTimeSpent()
    {
        HabitModel meditate = Habit(await Read("backup-frequency-tracks.hn"), "Meditate");

        Assert.Multiple(() =>
        {
            Assert.That(meditate.DisplayMetric, Is.EqualTo(DisplayMetric.Time));
            Assert.That(meditate.Duration, Is.EqualTo(new TimeOnly(0, 15)));
            Assert.That(meditate.TimesDone!.Select(x => x.CompletedAt - x.StartedAt),
                Is.EqualTo(new TimeSpan?[] { TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(15) }));
        });
    }

    [Test]
    public async Task ArchivedHabit_GoesIntoTheHiddenArchivedCategoryWithItsNoteDropped()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        CategoryModel archived = data.Categories.Single(x => x.Title == "Archived");

        Assert.Multiple(() =>
        {
            Assert.That(archived.Habits.Select(x => x.Title), Is.EqualTo(new[] { "Pay bills" }));
            Assert.That(archived.Habits[0].TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 1) }));
            Assert.That(data.Options.HiddenCategories, Is.EqualTo(new[] { archived }));
            Assert.That(data.Options.MatchCompletionsByDay, Is.True);
        });
    }

    [Test]
    public async Task Tasks_DueDateIsDateOnlyAndACompletedTaskIsCompletedOnItsDay()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        TaskModel groceries = Task(data, "Buy groceries");
        TaskModel callMom = Task(data, "Call mom");

        Assert.Multiple(() =>
        {
            Assert.That(groceries.PlannedDate, Is.EqualTo(new DateTime(2026, 11, 10)));
            Assert.That(groceries.IsPlannedDateOnly, Is.True);
            Assert.That(groceries.CompletedAt, Is.Null);
            Assert.That(CategoryOf(data, "Buy groceries"), Is.EqualTo("Finance"));
            Assert.That(callMom.PlannedDate, Is.EqualTo(new DateTime(2026, 11, 7)));
            Assert.That(callMom.CompletedAt, Is.EqualTo(Midday(2026, 11, 8)));
        });
    }

    [Test]
    public async Task Dates_InOtherYearsAndDecades()
    {
        ImportedData data = await Read("backup-other-years.hn");

        Assert.Multiple(() =>
        {
            Assert.That(Task(data, "Renew passport").PlannedDate, Is.EqualTo(new DateTime(2030, 12, 31)));
            Assert.That(Task(data, "Tax return").PlannedDate, Is.EqualTo(new DateTime(2025, 1, 5)));
        });
    }

    [Test]
    public async Task Priority_MapsInFixedSteps()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        Assert.Multiple(() =>
        {
            Assert.That(Habit(data, "Drink water").Priority, Is.EqualTo(Priority.VeryLow));
            Assert.That(Habit(data, "Read a book").Priority, Is.EqualTo(Priority.Medium));
        });
    }

    [Test]
    public async Task OnlyCategoriesThatHoldSomethingAreCreated()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        Assert.That(data.Categories.Select(x => x.Title),
            Is.EquivalentTo(new[] { "", "Health", "Study", "Sports", "Household", "Meditation", "Archived", "Finance", "Task" }));
    }
}
