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

    private static (int, int, Period, Weekdays, int, MonthWeeks, Months) Days(HabitModel habit) =>
        (habit.RepeatCount, habit.RepeatInterval, habit.RepeatPeriod, habit.RepeatWeekdays, habit.RepeatMonthDays, habit.RepeatMonthWeeks, habit.RepeatMonths);

    // "Pay bills" is due on the 8th, the day its daysOfMonth names, which is when Habitica's shouldDo makes it due.
    [Test]
    public void Dailies_HaveTheDaysTheyAreDueOn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Days(Habit("Read a book")), Is.EqualTo((3, 1, Period.Week, Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(Habit("Pay bills")), Is.EqualTo((1, 1, Period.Month, Weekdays.None, 1 << 7, MonthWeeks.None, Months.None)));
            Assert.That(Days(Habit("Water plants")), Is.EqualTo((1, 3, Period.Day, Weekdays.None, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(Habit("Drink water")), Is.EqualTo((1, 1, Period.Day, Weekdays.None, 0, MonthWeeks.None, Months.None)));
        });
    }

    [TestCase("m", Weekdays.Monday)]
    [TestCase("t", Weekdays.Tuesday)]
    [TestCase("w", Weekdays.Wednesday)]
    [TestCase("th", Weekdays.Thursday)]
    [TestCase("f", Weekdays.Friday)]
    [TestCase("s", Weekdays.Saturday)]
    [TestCase("su", Weekdays.Sunday)]
    public async Task Weekly_EachWeekday(string key, Weekdays weekday)
    {
        ImportedData data = await ReadJson($$"""{ "tasks": { "dailys": [ { "text": "d", "frequency": "weekly", "repeat": { "{{key}}": true } } ] } }""");

        Assert.That(Days(HabitIn(data, "d")), Is.EqualTo((1, 1, Period.Week, weekday, 0, MonthWeeks.None, Months.None)));
    }

    [Test]
    public async Task Schedules_ReadTheDaysAndKeepTheCountOfARuleWithoutThem()
    {
        ImportedData data = await ReadJson("""
            { "tasks": { "dailys": [
                { "text": "all seven", "frequency": "weekly", "repeat": { "m": true, "t": true, "w": true, "th": true, "f": true, "s": true, "su": true } },
                { "text": "not true", "frequency": "weekly", "repeat": { "m": "yes", "w": true } },
                { "text": "every two weeks", "frequency": "weekly", "everyX": 2, "repeat": { "m": true, "w": true } },
                { "text": "no repeat", "frequency": "weekly" },
                { "text": "first and last", "frequency": "monthly", "daysOfMonth": [ 1, 31 ] },
                { "text": "outside", "frequency": "monthly", "daysOfMonth": [ 0, 32, "x" ] },
                { "text": "every week", "frequency": "monthly", "weeksOfMonth": [ 0, 1, 2, 3, 4 ], "repeat": { "su": true } },
                { "text": "weeks outside", "frequency": "monthly", "weeksOfMonth": [ 5, -1 ], "repeat": { "su": true } },
                { "text": "both", "frequency": "monthly", "daysOfMonth": [ 1, 2, 3 ], "weeksOfMonth": [ 2 ], "repeat": { "s": true } },
                { "text": "week without weekday", "frequency": "monthly", "weeksOfMonth": [ 1 ], "repeat": { "m": false } },
                { "text": "no start", "frequency": "yearly" } ] } }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(Days(HabitIn(data, "all seven")), Is.EqualTo((7, 1, Period.Week, (Weekdays)127, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(HabitIn(data, "not true")), Is.EqualTo((1, 1, Period.Week, Weekdays.Wednesday, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(HabitIn(data, "every two weeks")), Is.EqualTo((2, 2, Period.Week, Weekdays.Monday | Weekdays.Wednesday, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(HabitIn(data, "no repeat")), Is.EqualTo((7, 1, Period.Week, Weekdays.None, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(HabitIn(data, "first and last")), Is.EqualTo((2, 1, Period.Month, Weekdays.None, (1 << 0) | (1 << 30), MonthWeeks.None, Months.None)));
            Assert.That(Days(HabitIn(data, "outside")), Is.EqualTo((3, 1, Period.Month, Weekdays.None, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(HabitIn(data, "every week")), Is.EqualTo((5, 1, Period.Month, Weekdays.Sunday, 0, (MonthWeeks)31, Months.None)));
            Assert.That(Days(HabitIn(data, "weeks outside")), Is.EqualTo((2, 1, Period.Month, Weekdays.Sunday, 0, MonthWeeks.None, Months.None)));
            Assert.That(Days(HabitIn(data, "both")), Is.EqualTo((1, 1, Period.Month, Weekdays.Saturday, 0, MonthWeeks.Third, Months.None)));
            Assert.That(Days(HabitIn(data, "week without weekday")), Is.EqualTo((1, 1, Period.Month, Weekdays.None, 0, MonthWeeks.Second, Months.None)));
            Assert.That(Days(HabitIn(data, "no start")), Is.EqualTo((1, 1, Period.Year, Weekdays.None, 0, MonthWeeks.None, Months.None)));
        });
    }

    // The start is the UTC instant of local midnight, so at UTC+2 the 13th at 23:00 UTC is the 14th.
    [Test]
    public async Task Yearly_IsTheLocalMonthAndDayOfTheStart()
    {
        ImportedData data = await ReadJson("""
            { "preferences": { "timezoneOffset": -120 },
              "tasks": { "dailys": [
                { "text": "leap day", "frequency": "yearly", "startDate": "2028-02-28T22:00:00.000Z" },
                { "text": "pi day", "frequency": "yearly", "everyX": 2, "startDate": "2026-03-13T23:00:00.000Z" } ] } }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(Days(HabitIn(data, "leap day")), Is.EqualTo((1, 1, Period.Year, Weekdays.None, 1 << 28, MonthWeeks.None, Months.February)));
            Assert.That(Days(HabitIn(data, "pi day")), Is.EqualTo((1, 2, Period.Year, Weekdays.None, 1 << 13, MonthWeeks.None, Months.March)));
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

    private static async Task<ImportedData> ReadJson(string json)
    {
        await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(json));

        return await new HabiticaImport().ReadDataFile(stream);
    }

    private static HabitModel HabitIn(ImportedData data, string title) => data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    [Test]
    public async Task ApiWrapper_TheDataPropertyIsRead()
    {
        ImportedData data = await ReadJson("""{ "data": { "tasks": { "todos": [ { "text": " Wrapped " } ] } } }""");

        Assert.That(data.Categories.Single().Tasks.Single().Title, Is.EqualTo("Wrapped"));
    }

    [Test]
    public void NoTasks_IsUnreadable()
    {
        Assert.ThrowsAsync<InvalidDataException>(() => ReadJson("""{ "preferences": {} }"""));
    }

    // At UTC+2 with the day starting at 04:00, 01:00 UTC on the 9th is 03:00 local, still the 8th.
    [Test]
    public async Task DayStart_ACompletionBeforeTheDayStartsCountsForThePreviousDay()
    {
        ImportedData data = await ReadJson("""
            { "preferences": { "timezoneOffset": -120, "dayStart": 4 },
              "tasks": { "habits": [ { "text": "Late", "history": [ { "date": "2026-11-09T01:00:00.000Z", "scoredUp": 1, "scoredDown": 0 }, { "date": "2026-11-09T03:00:00.000Z", "scoredUp": 2 } ] } ] } }
            """);

        Assert.That(HabitIn(data, "Late").TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 8), Midday(2026, 11, 9), Midday(2026, 11, 9) }));
    }

    [Test]
    public async Task Tags_TheFirstKnownTagIsTheCategoryAndTasksShareIt()
    {
        ImportedData data = await ReadJson("""
            { "tags": [ { "id": "t1", "name": "Work" }, { "id": "t2", "name": "Home" }, { "name": "no id" } ],
              "tasks": { "todos": [ { "text": "a", "tags": [ "missing", "t2", "t1" ] }, { "text": "b", "tags": [ "t2" ] }, { "text": "c", "tags": [] }, { "text": "d", "tags": "t1" } ] } }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(data.Categories.Select(x => x.Title), Is.EqualTo(new[] { "", "Home" }));
            Assert.That(data.Categories[1].Tasks.Select(x => x.Title), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(data.Categories[0].Tasks.Select(x => x.Title), Is.EqualTo(new[] { "c", "d" }));
        });
    }

    [Test]
    public async Task DailySchedules_BecomeACountPerWeekMonthOrYear()
    {
        ImportedData data = await ReadJson("""
            { "tasks": { "dailys": [
                { "text": "weekdays", "frequency": "weekly", "everyX": 2, "repeat": { "m": true, "t": false, "w": true, "th": false, "f": true, "s": false, "su": false } },
                { "text": "no repeat", "frequency": "weekly" },
                { "text": "none on", "frequency": "weekly", "repeat": { "m": false } },
                { "text": "month days", "frequency": "monthly", "daysOfMonth": [ 1, 15 ] },
                { "text": "month weeks", "frequency": "monthly", "weeksOfMonth": [ 0, 2 ], "repeat": { "s": true, "su": true } },
                { "text": "yearly", "frequency": "yearly", "everyX": 3 },
                { "text": "daily zero", "frequency": "daily", "everyX": 0 },
                { "text": "unknown", "frequency": "hourly" } ] } }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(Rule(HabitIn(data, "weekdays")), Is.EqualTo((3, 2, Period.Week)));
            Assert.That(Rule(HabitIn(data, "no repeat")), Is.EqualTo((7, 1, Period.Week)));
            Assert.That(Rule(HabitIn(data, "none on")), Is.EqualTo((1, 1, Period.Week)));
            Assert.That(Rule(HabitIn(data, "month days")), Is.EqualTo((2, 1, Period.Month)));
            Assert.That(Rule(HabitIn(data, "month weeks")), Is.EqualTo((4, 1, Period.Month)));
            Assert.That(Rule(HabitIn(data, "yearly")), Is.EqualTo((1, 3, Period.Year)));
            Assert.That(Rule(HabitIn(data, "daily zero")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(HabitIn(data, "unknown")), Is.EqualTo((1, 1, Period.Day)));
        });
    }

    // JsonElement.TryGetInt32 throws on a string instead of returning false, and ImportExportService
    // does not turn an InvalidOperationException into "The file could not be read".
    [Test]
    public async Task NumbersWrittenAsText_FallBackToTheDefaults()
    {
        ImportedData data = await ReadJson("""
            { "preferences": { "timezoneOffset": "-60", "dayStart": "4" },
              "tasks": { "dailys": [ { "text": "Daily", "frequency": "daily", "everyX": "2" } ],
                         "habits": [ { "text": "Counter", "history": [ { "date": "2026-11-09T10:00:00.000Z", "scoredUp": "1" } ] } ] } }
            """);

        Assert.Multiple(() =>
        {
            Assert.That(Rule(HabitIn(data, "Daily")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(HabitIn(data, "Counter").TimesDone, Is.Empty);
        });
    }

    [Test]
    public async Task Daily_HistoryInMillisecondsAndOnlyCompletedEntriesCount()
    {
        // 1762603200000 is 2025-11-08T12:00:00Z.
        ImportedData data = await ReadJson("""
            { "tasks": { "dailys": [ { "text": "Stretch", "history": [ { "date": 1762603200000, "completed": true }, { "date": 1762689600000, "completed": false }, { "date": "not a date", "completed": true }, { "completed": true } ] } ] } }
            """);

        Assert.That(HabitIn(data, "Stretch").TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2025, 11, 8) }));
    }

    [Test]
    public async Task Checklists_ATodoKeepsWhichItemsAreDoneADailyDoesNot()
    {
        ImportedData data = await ReadJson("""
            { "tasks": {
                "todos": [ { "text": "Pack", "checklist": [ { "text": "Socks", "completed": true }, { "text": "Shirt", "completed": false } ] } ],
                "dailys": [ { "text": "Routine", "checklist": [ { "text": "Teeth", "completed": true } ] } ] } }
            """);

        List<ItemModel> packItems = data.Categories.Single().Tasks.Single().Items!;

        Assert.Multiple(() =>
        {
            Assert.That(packItems.Select(x => x.Title), Is.EqualTo(new[] { "Socks", "Shirt" }));
            Assert.That(packItems[0].DoneAt, Is.Not.Null);
            Assert.That(packItems[1].DoneAt, Is.Null);
            Assert.That(HabitIn(data, "Routine").Items!.Select(x => (x.Title, x.DoneAt)), Is.EqualTo(new[] { ("Teeth", (DateTime?)null) }));
        });
    }

    // The last change of the todo stands in for the missing dateCompleted.
    [Test]
    public async Task Todo_CompletedWithoutDateCompleted_ImportsAsCompletedAtItsLastChange()
    {
        ImportedData data = await ReadJson("""
            { "tasks": { "todos": [ { "text": "Pack", "completed": true, "updatedAt": "2026-01-03T10:00:00.000Z", "checklist": [ { "text": "Socks", "completed": true } ] } ] } }
            """);

        TaskModel pack = data.Categories.Single().Tasks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(pack.CompletedAt, Is.EqualTo(new DateTime(2026, 1, 3, 10, 0, 0)));
            Assert.That(pack.Items!.Single().DoneAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task Dates_ComeFromTheFileAndFallBackToTheImportTime()
    {
        DateTime before = DateTime.Now;

        ImportedData data = await ReadJson("""
            { "tasks": { "todos": [
                { "text": "dated", "createdAt": "2026-01-02T10:00:00.000Z", "updatedAt": "2026-01-03T10:00:00.000Z" },
                { "text": "undated", "createdAt": 5 } ] } }
            """);

        List<TaskModel> tasks = data.Categories.Single().Tasks;

        Assert.Multiple(() =>
        {
            Assert.That(tasks[0].CreatedAt, Is.EqualTo(new DateTime(2026, 1, 2, 10, 0, 0)));
            Assert.That(tasks[0].UpdatedAt, Is.EqualTo(new DateTime(2026, 1, 3, 10, 0, 0)));
            Assert.That(tasks[1].CreatedAt, Is.EqualTo(new DateTime(1970, 1, 1, 0, 0, 0, 5)));
            Assert.That(tasks[1].UpdatedAt, Is.InRange(before, DateTime.Now));
        });
    }
}
