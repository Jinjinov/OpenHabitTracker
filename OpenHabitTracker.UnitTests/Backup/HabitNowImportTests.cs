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

    private static (int, int, Period, Weekdays, int) Days(HabitModel habit) => (habit.RepeatCount, habit.RepeatInterval, habit.RepeatPeriod, habit.RepeatWeekdays, habit.RepeatMonthDays);

    [Test]
    public async Task WeekdaysAndDaysOfTheMonth_AreTheDaysOfTheHabit()
    {
        ImportedData data = await Read("backup-frequency-tracks.hn");

        Assert.Multiple(() =>
        {
            Assert.That(Days(Habit(data, "Read a book")), Is.EqualTo((3, 1, Period.Week, Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday, 0)));
            Assert.That(Days(Habit(data, "Pay bills")), Is.EqualTo((2, 1, Period.Month, Weekdays.None, (1 << 0) | (1 << 14))));
            Assert.That(Days(Habit(data, "Drink water")), Is.EqualTo((1, 1, Period.Day, Weekdays.None, 0)));
            Assert.That(Days(Habit(data, "Water plants")), Is.EqualTo((1, 3, Period.Day, Weekdays.None, 0)));
            Assert.That(Days(Habit(data, "Gym")), Is.EqualTo((3, 1, Period.Week, Weekdays.None, 0)));
        });
    }

    // Weekdays are java.util.Calendar numbers, 1 for Sunday to 7 for Saturday.
    [TestCase("01", "1 7", 2, Weekdays.Sunday | Weekdays.Saturday, 0)]
    [TestCase("01", "0 8 x 2", 1, Weekdays.Monday, 0)]
    [TestCase("01", "2 2", 1, Weekdays.Monday, 0)]
    [TestCase("01", "", 1, Weekdays.None, 0)]
    [TestCase("04", "0 32 31", 1, Weekdays.None, 1 << 30)]
    [TestCase("04", "15 15", 1, Weekdays.None, 1 << 14)]
    [TestCase("04", "", 1, Weekdays.None, 0)]
    public async Task Days_OutsideTheRealOnesAreLeftOutAndADayNamedTwiceIsOne(string repeat, string days, int count, Weekdays weekdays, int monthDays)
    {
        ImportedData data = await ReadText("1;1;Work;1", $"1;1;;Habit;1;0a1;B6a1;;0;{repeat};01;;{days};");

        HabitModel habit = data.Categories.SelectMany(x => x.Habits).Single();

        Assert.That((habit.RepeatCount, habit.RepeatWeekdays, habit.RepeatMonthDays), Is.EqualTo((count, weekdays, monthDays)));
    }

    [Test]
    public async Task Tracks_TheDaysAreThoseOfTheRunningTrack()
    {
        ImportedData data = await ReadText("1;1;Work;1",
            "70;-70;;Gym;1;0b1;B6a1;;0;00;01;;;|71;70;;Gym;1;0a1;B6a5;;0;01;01;;2 4;|" +
            "80;-80;;Swim;1;0b1;B6a1;;0;01;01;;2 4;|81;80;;Swim;1;0a1;B6a5;;0;00;01;;;");

        Assert.Multiple(() =>
        {
            Assert.That(Days(Habit(data, "Gym")), Is.EqualTo((2, 1, Period.Week, Weekdays.Monday | Weekdays.Wednesday, 0)));
            Assert.That(Days(Habit(data, "Swim")), Is.EqualTo((1, 1, Period.Day, Weekdays.None, 0)));
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

    // A backup written by hand in the .hn layout: categories, then activities, then entries.
    private async Task<ImportedData> ReadText(string categories, string activities, string entries = "")
    {
        await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes($"C202611081056331{{C{categories}{{H{activities}{{S{{X{entries}{{T"));

        return await _sut.ReadDataFile(stream);
    }

    [Test]
    public async Task Priority_EveryStepAndAMissingOne()
    {
        ImportedData data = await ReadText("1;1;Work;1",
            "1;1;;p0;1;1c0;B6aa;;0;0|2;2;;p1;1;1c1;B6aa;;0;0|3;3;;p2;1;1c2;B6aa;;0;0|4;4;;p3;1;1c3;B6aa;;0;0|" +
            "5;5;;p4;1;1c4;B6aa;;0;0|6;6;;p5;1;1c5;B6aa;;0;0|7;7;;p7;1;1c7;B6aa;;0;0|8;8;;none;1;1c;B6aa;;0;0");

        Assert.That(data.Categories.Single(x => x.Title == "Work").Tasks.Select(x => x.Priority), Is.EqualTo(new[]
        {
            Priority.None, Priority.VeryLow, Priority.Low, Priority.Medium, Priority.High, Priority.VeryHigh, Priority.VeryHigh, Priority.VeryLow
        }));
    }

    [Test]
    public async Task Frequency_CountsPerWeekOrMonthAndUnreadableParametersFallBackToDaily()
    {
        ImportedData data = await ReadText("1;1;Work;1",
            "1;1;;Weekly three;1;0a1;B6a1;;0;02;03;;;|2;2;;Monthly four;1;0a1;B6a1;;0;02;14;;;|3;3;;Broken count;1;0a1;B6a1;;0;02;1x;;;|" +
            "4;4;;Every x days;1;0a1;B6a1;;0;07;x;;;|5;5;;Every zero days;1;0a1;B6a1;;0;07;0;;;|6;6;;No days given;1;0a1;B6a1;;0;01;01;;;|" +
            "7;7;;Unknown repeat;1;0a1;B6a1;;0;09;01;;;");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(Habit(data, "Weekly three")), Is.EqualTo((3, 1, Period.Week)));
            Assert.That(Rule(Habit(data, "Monthly four")), Is.EqualTo((4, 1, Period.Month)));
            Assert.That(Rule(Habit(data, "Broken count")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(Habit(data, "Every x days")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(Habit(data, "Every zero days")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(Habit(data, "No days given")), Is.EqualTo((1, 1, Period.Week)));
            Assert.That(Rule(Habit(data, "Unknown repeat")), Is.EqualTo((1, 1, Period.Day)));
        });
    }

    [Test]
    public async Task Dates_ThatAreNotRealDays_LeaveTheTaskUnplanned()
    {
        ImportedData data = await ReadText("1;1;Work;1",
            "1;1;;Feb 31;1;1c1;B61v;;0;0|2;2;;Decade zero;1;1c1;@6a1;;0;0|3;3;;Too short;1;1c1;B6;;0;0|4;4;;Bad digit;1;1c1;B6w1;;0;0|" +
            "5;5;;Twenty ten;1;1c1;A9b5;;0;0|6;6;;Leap day;1;1c1;B81t;;0;0");

        Assert.Multiple(() =>
        {
            Assert.That(Task(data, "Feb 31").PlannedAt, Is.Null);
            Assert.That(Task(data, "Decade zero").PlannedAt, Is.Null);
            Assert.That(Task(data, "Too short").PlannedAt, Is.Null);
            Assert.That(Task(data, "Bad digit").PlannedAt, Is.Null);
            Assert.That(Task(data, "Twenty ten").PlannedDate, Is.EqualTo(new DateTime(2019, 12, 5)));
            Assert.That(Task(data, "Leap day").PlannedDate, Is.EqualTo(new DateTime(2028, 2, 29)));
        });
    }

    [Test]
    public async Task YesNoHabit_FailedEntriesWithAValueAndEntriesOnNoDayAreLeftOut()
    {
        ImportedData data = await ReadText("1;1;Work;1",
            "1;1;;Stretch;1;0a1;B6a1;;0;00;01;;;",
            "1;B6a2;fc|1;B6a3;fc;1|1;B6a4;fs|1;B6a5;fd|1;B6aw;fd|1;B6a6;fdWith a note");

        Assert.That(Habit(data, "Stretch").TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 5), Midday(2026, 11, 6) }));
    }

    [Test]
    public async Task Targets_ZeroQuantityIsOneAndATimerPastADayIsCapped()
    {
        ImportedData data = await ReadText("1;1;Work;1",
            "1;1;;Pushups;1;0a1;B6a1;;0;10;01;reps;;0|2;2;;Long timer;1;0a1;B6a1;;0;60;01;;;100000|" +
            "3;3;;Plain;1;0a1;B6a1;;0;00;01;ignored unit;;|4;4;;Timer no target;1;0a1;B6a1;;0;60;01;;;");

        Assert.Multiple(() =>
        {
            Assert.That(Habit(data, "Pushups (reps)").TargetQuantity, Is.EqualTo(1));
            Assert.That(Habit(data, "Long timer").Duration, Is.EqualTo(new TimeOnly(23, 59, 59)));
            Assert.That(Habit(data, "Plain").DisplayMetric, Is.EqualTo(DisplayMetric.Repetitions));
            Assert.That(Habit(data, "Timer no target").Duration, Is.Null);
            Assert.That(Habit(data, "Timer no target").DisplayMetric, Is.EqualTo(DisplayMetric.Time));
        });
    }

    [Test]
    public async Task Categories_ACustomNameIsKeptAndAnUnknownIdIsNoCategory()
    {
        ImportedData data = await ReadText("1;1;My own;1|broken",
            "1;1;;Mine;1;0a1;B6a1;;0;00;01;;;|2;2;;Lost;9;0a1;B6a1;;0;00;01;;;|3;3;;short");

        Assert.Multiple(() =>
        {
            Assert.That(CategoryOf(data, "Mine"), Is.EqualTo("My own"));
            Assert.That(CategoryOf(data, "Lost"), Is.Empty);
            Assert.That(data.Categories.SelectMany(x => x.Habits).Select(x => x.Title), Is.EquivalentTo(new[] { "Mine", "Lost" }));
        });
    }

    // Every track of an ended habit names the first, and none is the running one.
    [Test]
    public async Task Tracks_NoneRunning_TakesTheLatestTrackAndStartsAtTheFirst()
    {
        ImportedData data = await ReadText("1;1;Work;1",
            "61;-60;;Old name;1;0a1;B6a1;;0;00;01;;;|62;-60;;New name;1;0a1;B6a5;;0;07;02;;;",
            "61;B6a2;fd|62;B6a6;fd");

        HabitModel habit = data.Categories.SelectMany(x => x.Habits).Single();

        Assert.Multiple(() =>
        {
            Assert.That(habit.Title, Is.EqualTo("New name"));
            Assert.That(Rule(habit), Is.EqualTo((1, 2, Period.Day)));
            Assert.That(habit.StartAt, Is.EqualTo(new DateTime(2026, 11, 1)));
            Assert.That(habit.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 2), Midday(2026, 11, 6) }));
            Assert.That(habit.LastTimeDoneAt, Is.EqualTo(Midday(2026, 11, 6)));
        });
    }
}
