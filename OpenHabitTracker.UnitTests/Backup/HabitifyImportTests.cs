using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.Habitify;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Backup;

// The free plan allows three habits and logging only today, so the fixture holds one day of history.
[TestFixture]
public class HabitifyImportTests
{
    private ImportedData _data = null!;

    [SetUp]
    public async Task SetUp()
    {
        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        await using Stream stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Habitify", "backup.db"));

        _data = await new HabitifyImport(loc).ReadDataFile(stream);
    }

    private HabitModel Habit(string title) => _data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    private static DateTime Midday(int year, int month, int day) => new(year, month, day, 12, 0, 0);

    private static (int, int, Period) Rule(HabitModel habit) => (habit.RepeatCount, habit.RepeatInterval, habit.RepeatPeriod);

    [Test]
    public void Habits_AreImportedWithTrimmedTitlesAndTheirSchedule()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_data.Categories.Single().Habits.Select(x => x.Title), Is.EqualTo(new[] { "Drink water", "Read", "Run (km)" }));
            Assert.That(Rule(Habit("Drink water")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(Habit("Read")), Is.EqualTo((3, 1, Period.Week)));
            Assert.That(Rule(Habit("Run (km)")), Is.EqualTo((1, 2, Period.Day)));
            Assert.That(Habit("Read").StartAt, Is.EqualTo(new DateTime(2026, 11, 1)));
            Assert.That(Habit("Read").Color, Is.EqualTo("bg-primary-subtle"));
            Assert.That(_data.Options.MatchCompletionsByDay, Is.True);
        });
    }

    [Test]
    public void WeekdayHabit_HasItsDays()
    {
        Assert.Multiple(() =>
        {
            Assert.That((Habit("Read").RepeatCount, Habit("Read").RepeatWeekdays), Is.EqualTo((3, Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday)));
            Assert.That(Habit("Drink water").RepeatWeekdays, Is.EqualTo(Weekdays.None));
            Assert.That(Habit("Run (km)").RepeatWeekdays, Is.EqualTo(Weekdays.None));
        });
    }

    [Test]
    public async Task Weekdays_AllNamesAndOnesHabitifyMayAddLater()
    {
        ImportedData data = await ReadBackup(
            [
                "'a', '{\"name\":\"All\",\"regularly\":\"weekDays-mon,tue,wed,thu,fri,sat,sun\"}', NULL, 1",
                "'b', '{\"name\":\"Unknown beside\",\"regularly\":\"weekDays-mon,xyz\"}', NULL, 2",
                "'c', '{\"name\":\"Only unknown\",\"regularly\":\"weekDays-xyz\"}', NULL, 3",
                "'d', '{\"name\":\"Upper case\",\"regularly\":\"weekDays-MON,FRI\"}', NULL, 4"
            ],
            [],
            []);

        Assert.Multiple(() =>
        {
            Assert.That((HabitIn(data, "All").RepeatCount, HabitIn(data, "All").RepeatWeekdays), Is.EqualTo((7, (Weekdays)127)));
            Assert.That((HabitIn(data, "Unknown beside").RepeatCount, HabitIn(data, "Unknown beside").RepeatWeekdays), Is.EqualTo((1, Weekdays.Monday)));
            Assert.That((HabitIn(data, "Only unknown").RepeatCount, HabitIn(data, "Only unknown").RepeatWeekdays), Is.EqualTo((1, Weekdays.None)));
            Assert.That((HabitIn(data, "Upper case").RepeatCount, HabitIn(data, "Upper case").RepeatWeekdays), Is.EqualTo((2, Weekdays.Monday | Weekdays.Friday)));
        });
    }

    [Test]
    public void RepetitionGoal_IsAHabitThatIsDone()
    {
        HabitModel drinkWater = Habit("Drink water");

        Assert.Multiple(() =>
        {
            Assert.That(drinkWater.DisplayMetric, Is.EqualTo(DisplayMetric.Repetitions));
            Assert.That(drinkWater.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 8) }));
            Assert.That(Habit("Read").TimesDone, Is.Empty);
        });
    }

    [Test]
    public void LogInTheBaseUnit_IsConvertedToTheGoalsUnit()
    {
        HabitModel run = Habit("Run (km)");

        Assert.Multiple(() =>
        {
            Assert.That(run.DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(run.TargetQuantity, Is.EqualTo(5));
            Assert.That(run.TimesDone!.Select(x => (x.StartedAt, x.Quantity)), Is.EqualTo(new[] { (Midday(2026, 11, 8), 5.0) }));
            Assert.That(run.LastTimeDoneAt, Is.EqualTo(Midday(2026, 11, 8)));
        });
    }

    // A backup with the three tables the importer reads, written row by row.
    private static async Task<ImportedData> ReadBackup(string[] habits, string[] goals, string[] logs)
    {
        string path = Path.Combine(Path.GetTempPath(), $"habitify-{Guid.NewGuid():N}.db");

        try
        {
            using (Microsoft.Data.Sqlite.SqliteConnection connection = new($"Data Source={path};Pooling=False"))
            {
                connection.Open();

                using Microsoft.Data.Sqlite.SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    "CREATE TABLE habits (id TEXT, root_data TEXT, start_date TEXT, created_at INTEGER);" +
                    "CREATE TABLE habit_goals (habit_id TEXT, value REAL, unit_symbol TEXT, created_at INTEGER);" +
                    "CREATE TABLE habit_logs (habit_id TEXT, start_day TEXT, value REAL, unit_symbol TEXT);" +
                    string.Concat(habits.Select(x => $"INSERT INTO habits VALUES ({x});")) +
                    string.Concat(goals.Select(x => $"INSERT INTO habit_goals VALUES ({x});")) +
                    string.Concat(logs.Select(x => $"INSERT INTO habit_logs VALUES ({x});"));
                command.ExecuteNonQuery();
            }

            IStringLocalizer loc = Substitute.For<IStringLocalizer>();
            loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

            await using MemoryStream stream = new(await File.ReadAllBytesAsync(path));

            return await new HabitifyImport(loc).ReadDataFile(stream);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static HabitModel HabitIn(ImportedData data, string title) => data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    [Test]
    public async Task Goals_TheNewestWinsAndAMissingUnitIsRepetitions()
    {
        ImportedData data = await ReadBackup(
            ["'a', '{\"name\":\"Water\"}', NULL, 1", "'b', '{\"name\":\"Pushups\"}', '2026-02-30', 2", "'c', '{\"name\":\"Tea\"}', 'soon', 3"],
            ["'a', 2, 'L', 1", "'a', 3, 'L', 2", "'b', 20, NULL, 1", "'c', 2, 'cups', 1"],
            []);

        Assert.Multiple(() =>
        {
            Assert.That(HabitIn(data, "Water (L)").TargetQuantity, Is.EqualTo(3));
            Assert.That(HabitIn(data, "Pushups").DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(HabitIn(data, "Pushups").TargetQuantity, Is.EqualTo(20));
            Assert.That(HabitIn(data, "Tea (cups)").TargetQuantity, Is.EqualTo(2));
            Assert.That(HabitIn(data, "Water (L)").StartAt, Is.Null);
            Assert.That(HabitIn(data, "Pushups").StartAt, Is.Null);
            Assert.That(HabitIn(data, "Tea (cups)").StartAt, Is.Null);
        });
    }

    [Test]
    public async Task Logs_AreConvertedToTheGoalUnitAndEmptyOrUnreadableOnesAreLeftOut()
    {
        ImportedData data = await ReadBackup(
            ["'run', '{\"name\":\"Run\"}', '2026-11-01', 1", "'walk', '{\"name\":\"Walk\"}', '2026-11-01', 2", "'sip', '{\"name\":\"Sip\"}', '2026-11-01', 3", "'done', '{\"name\":\"Done\"}', '2026-11-01', 4"],
            ["'run', 5, 'kM', 1", "'walk', 2, 'mi', 1", "'sip', 1, 'cups', 1"],
            [
                "'run', '2026-11-02', 1500, 'm'", "'run', '2026-11-03', 0, 'm'", "'run', 'yesterday', 900, 'm'", "'run', NULL, 900, 'm'",
                "'walk', '2026-11-02', 1000, 'm'",
                "'sip', '2026-11-02', 3, 'mL'",
                "'done', '2026-11-04', NULL, NULL", "'done', '2026-11-05', 2, 'rep'"
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(HabitIn(data, "Run (km)").TimesDone!.Select(x => (x.StartedAt, x.Quantity)), Is.EqualTo(new[] { (Midday(2026, 11, 2), 1.5) }));
            Assert.That(HabitIn(data, "Walk (mi)").TimesDone!.Single().Quantity, Is.EqualTo(1000 / 1609.344).Within(1e-9));
            Assert.That(HabitIn(data, "Sip (cups)").TimesDone!.Single().Quantity, Is.EqualTo(3));
            Assert.That(HabitIn(data, "Done").TimesDone!.Select(x => (x.StartedAt, x.Quantity)), Is.EqualTo(new[] { (Midday(2026, 11, 4), 1.0), (Midday(2026, 11, 5), 1.0) }));
            Assert.That(HabitIn(data, "Done").LastTimeDoneAt, Is.EqualTo(Midday(2026, 11, 5)));
        });
    }

    [Test]
    public async Task Schedules_UnreadableOnesFallBackToDaily()
    {
        ImportedData data = await ReadBackup(
            [
                "'a', '{\"name\":\"No days\",\"regularly\":\"weekDays-\"}', NULL, 1",
                "'b', '{\"name\":\"Bad interval\",\"regularly\":\"dayInterval-x\"}', NULL, 2",
                "'c', '{\"name\":\"Zero interval\",\"regularly\":\"dayInterval-0\"}', NULL, 3",
                "'d', '{\"name\":\"Other\",\"regularly\":\"monthDays-1\"}', NULL, 4",
                "'e', '{\"name\":\"No rule\",\"regularly\":5}', NULL, 5"
            ],
            [],
            []);

        Assert.Multiple(() =>
        {
            Assert.That(Rule(HabitIn(data, "No days")), Is.EqualTo((1, 1, Period.Week)));
            Assert.That(Rule(HabitIn(data, "Bad interval")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(HabitIn(data, "Zero interval")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(HabitIn(data, "Other")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(HabitIn(data, "No rule")), Is.EqualTo((1, 1, Period.Day)));
        });
    }

    [Test]
    public async Task ArchivedHabit_GoesIntoTheHiddenArchivedCategoryAndAColorlessOneKeepsTheDefault()
    {
        ImportedData data = await ReadBackup(
            ["'a', '{\"name\":\"Old\",\"isArchived\":true}', NULL, 1", "'b', '{\"name\":\"Current\",\"isArchived\":false,\"accentColor\":\"not a color\"}', NULL, 2"],
            [],
            []);

        CategoryModel archived = data.Categories.Single(x => x.Title == "Archived");

        Assert.Multiple(() =>
        {
            Assert.That(data.Categories.Select(x => x.Title), Is.EqualTo(new[] { "", "Archived" }));
            Assert.That(archived.Habits.Select(x => x.Title), Is.EqualTo(new[] { "Old" }));
            Assert.That(data.Options.HiddenCategories, Is.EqualTo(new[] { archived }));
            Assert.That(HabitIn(data, "Current").Color, Is.EqualTo(new HabitModel().Color));
        });
    }
}
