using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.Habitica;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Backup;

// The fixture's account is at UTC+1 (timezoneOffset -60) with the day starting at midnight.
[TestFixture]
public class HabiticaImportTests
{
    private ImportedData _data = null!;

    [SetUp]
    public async Task SetUp()
    {
        await using Stream stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Habitica", "userdata.json"));

        _data = await new HabiticaImport().ReadDataFile(stream);
    }

    private HabitModel Habit(string title) => _data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    private TaskModel Task(string title) => _data.Categories.SelectMany(x => x.Tasks).Single(x => x.Title == title);

    private static DateTime Midday(int year, int month, int day) => new(year, month, day, 12, 0, 0);

    private static (int, int, Period) Rule(HabitModel habit) => (habit.RepeatCount, habit.RepeatInterval, habit.RepeatPeriod);

    [Test]
    public void TodosDailiesAndHabits_AreImportedAndTheRewardIsNot()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_data.Categories.SelectMany(x => x.Tasks).Select(x => x.Title),
                Is.EqualTo(new[] { "Join Habitica (Check me off!)", "Buy groceries", "Call mom" }));
            Assert.That(_data.Categories.SelectMany(x => x.Habits).Select(x => x.Title),
                Is.EqualTo(new[] { "Read a book", "Water plants", "Pay bills", "Add a task to Habitica", "Tap here to edit this into a bad habit you'd like to quit", "Drink water" }));
            Assert.That(_data.Categories.Select(x => x.Title), Is.EqualTo(new[] { "" }));
            Assert.That(_data.Options.MatchCompletionsByDay, Is.True);
        });
    }

    [Test]
    public void Todo_DueDateIsTheLocalDayAndTheChecklistBecomesItems()
    {
        TaskModel groceries = Task("Buy groceries");

        Assert.Multiple(() =>
        {
            Assert.That(groceries.PlannedDate, Is.EqualTo(new DateTime(2026, 11, 10)));
            Assert.That(groceries.IsPlannedDateOnly, Is.True);
            Assert.That(groceries.Items!.Select(x => (x.Title, x.DoneAt)), Is.EqualTo(new[] { ("Milk", (DateTime?)null), ("Bread", (DateTime?)null) }));
            Assert.That(groceries.CompletedAt, Is.Null);
        });
    }

    [Test]
    public void CompletedTodo_IsCompletedAtItsLocalTime()
    {
        Assert.That(Task("Call mom").CompletedAt, Is.EqualTo(new DateTime(2026, 11, 8, 11, 35, 44, 732)));
    }

    [Test]
    public void Dailies_MapTheirScheduleAndLocalStartDate()
    {
        HabitModel readABook = Habit("Read a book");
        HabitModel waterPlants = Habit("Water plants");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(readABook), Is.EqualTo((3, 1, Period.Week)));
            Assert.That(readABook.StartAt, Is.EqualTo(new DateTime(2026, 11, 8)));
            Assert.That(readABook.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Chapter one", "Chapter two" }));
            Assert.That(Rule(waterPlants), Is.EqualTo((1, 3, Period.Day)));
            Assert.That(waterPlants.StartAt, Is.EqualTo(new DateTime(2026, 11, 1)));
            Assert.That(Rule(Habit("Pay bills")), Is.EqualTo((1, 1, Period.Month)));
        });
    }

    [Test]
    public void Daily_CompletedHistoryEntry_IsACompletionOnItsLocalDay()
    {
        HabitModel payBills = Habit("Pay bills");

        Assert.Multiple(() =>
        {
            Assert.That(payBills.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 8) }));
            Assert.That(payBills.LastTimeDoneAt, Is.EqualTo(Midday(2026, 11, 8)));
            Assert.That(Habit("Read a book").TimesDone, Is.Empty);
        });
    }

    [Test]
    public void Habit_EveryUpPressIsACompletionAndDownPressesAreNot()
    {
        HabitModel drinkWater = Habit("Drink water");

        Assert.Multiple(() =>
        {
            Assert.That(Rule(drinkWater), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(drinkWater.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 8), Midday(2026, 11, 8) }));
            Assert.That(Habit("Tap here to edit this into a bad habit you'd like to quit").TimesDone, Is.Empty);
        });
    }
}
