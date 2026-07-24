using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.UnitTests.Query;

[TestFixture]
public class QueryExtensionsRelativeRangeTests
{
    private static readonly DateTime Today = DateTime.Today;

    // --- Planned range on tasks ---

    [Test]
    public void FilterTasks_PlannedRange_IncludesOnlyTasksWithinBounds()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, plannedAt: Today.AddDays(-10)),
            TestData.Task(2, plannedAt: Today.AddDays(-5)),
            TestData.Task(3, plannedAt: Today),
            TestData.Task(4, plannedAt: Today.AddDays(5)),
            TestData.Task(5, plannedAt: Today.AddDays(10)),
            TestData.Task(6, plannedAt: null),
        ];

        QueryParameters qp = new() { PlannedRangeStart = Today.AddDays(-7), PlannedRangeEnd = Today.AddDays(7) };

        List<long> result = tasks.FilterTasks(qp).Select(t => t.Id).ToList();

        Assert.That(result, Is.EquivalentTo(new long[] { 2, 3, 4 }));
    }

    [Test]
    public void FilterTasks_PlannedRange_OpenUpperBound_IncludesEverythingFromStart()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, plannedAt: Today.AddDays(-1)),
            TestData.Task(2, plannedAt: Today),
            TestData.Task(3, plannedAt: Today.AddDays(100)),
        ];

        QueryParameters qp = new() { PlannedRangeStart = Today, PlannedRangeEnd = null };

        List<long> result = tasks.FilterTasks(qp).Select(t => t.Id).ToList();

        Assert.That(result, Is.EquivalentTo(new long[] { 2, 3 }));
    }

    [Test]
    public void FilterTasks_PlannedDateOnlySentinel_FallsInsideWholeDayRange()
    {
        // Issue 23 interaction: the date-only sentinel time must not push the task out of its day.
        List<TaskModel> tasks =
        [
            TestData.Task(1, plannedAt: Today.AddDays(3) + TaskModel.DateOnlySentinel),
        ];

        QueryParameters qp = new() { PlannedRangeStart = Today.AddDays(3), PlannedRangeEnd = Today.AddDays(3) };

        List<long> result = tasks.FilterTasks(qp).Select(t => t.Id).ToList();

        Assert.That(result, Is.EquivalentTo(new long[] { 1 }));
    }

    // --- Done range on tasks ---

    [Test]
    public void FilterTasks_DoneRange_IncludesOnlyTasksCompletedWithinBounds()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, completedAt: Today.AddDays(-10)),
            TestData.Task(2, completedAt: Today.AddDays(-2)),
            TestData.Task(3, completedAt: Today),
            TestData.Task(4, completedAt: null),
        ];

        QueryParameters qp = new() { DoneRangeStart = Today.AddDays(-7), DoneRangeEnd = Today, ShowDoneInRange = true };

        List<long> result = tasks.FilterTasks(qp).Select(t => t.Id).ToList();

        Assert.That(result, Is.EquivalentTo(new long[] { 2, 3 }));
    }

    // --- Done range on habits ---

    [Test]
    public void FilterHabits_DoneRange_IncludesHabitWithCompletionInWindow()
    {
        HabitModel inRange = TestData.Habit(1);
        inRange.TimesDone = [new TimeModel { Id = 1, HabitId = 1, CompletedAt = Today.AddDays(-2) }];

        HabitModel outOfRange = TestData.Habit(2);
        outOfRange.TimesDone = [new TimeModel { Id = 2, HabitId = 2, CompletedAt = Today.AddDays(-20) }];

        List<HabitModel> habits = [inRange, outOfRange];

        QueryParameters qp = new() { DoneRangeStart = Today.AddDays(-7), DoneRangeEnd = Today.AddDays(7), ShowDoneInRange = true };
        qp.SortBy[ContentType.Habit] = Sort.Title;

        List<long> result = habits.FilterHabits(qp).Select(h => h.Id).ToList();

        Assert.That(result, Is.EquivalentTo(new long[] { 1 }));
    }

    [Test]
    public void FilterHabits_DoneRange_ExcludesHabitWhoseCompletionsStraddleButMissWindow()
    {
        // One completion before the window and one after must not count as "in range":
        // a single completion has to fall inside, which is why both bounds are one Any.
        HabitModel straddling = TestData.Habit(1);
        straddling.TimesDone =
        [
            new TimeModel { Id = 1, HabitId = 1, CompletedAt = Today.AddDays(-20) },
            new TimeModel { Id = 2, HabitId = 1, CompletedAt = Today.AddDays(20) },
        ];

        List<HabitModel> habits = [straddling];

        QueryParameters qp = new() { DoneRangeStart = Today.AddDays(-7), DoneRangeEnd = Today.AddDays(7), ShowDoneInRange = true };
        qp.SortBy[ContentType.Habit] = Sort.Title;

        List<long> result = habits.FilterHabits(qp).Select(h => h.Id).ToList();

        Assert.That(result, Is.Empty);
    }

    // --- ShowDoneInRange off (Not done) ---

    [Test]
    public void FilterTasks_ShowDoneInRangeOff_IncludesNotCompletedInWindowAndNeverCompleted()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, completedAt: Today.AddDays(-2)),   // completed in window -> excluded
            TestData.Task(2, completedAt: Today.AddDays(-20)),  // completed outside window -> included
            TestData.Task(3, completedAt: null),                // never completed -> included
        ];

        QueryParameters qp = new() { DoneRangeStart = Today.AddDays(-7), DoneRangeEnd = Today, ShowDoneInRange = false };

        List<long> result = tasks.FilterTasks(qp).Select(t => t.Id).ToList();

        Assert.That(result, Is.EquivalentTo(new long[] { 2, 3 }));
    }

    [Test]
    public void FilterHabits_ShowDoneInRangeOff_IncludesHabitsWithNoCompletionInWindowAndNeverDone()
    {
        HabitModel doneInWindow = TestData.Habit(1);
        doneInWindow.TimesDone = [new TimeModel { Id = 1, HabitId = 1, CompletedAt = Today.AddDays(-2) }]; // excluded

        HabitModel doneOutside = TestData.Habit(2);
        doneOutside.TimesDone = [new TimeModel { Id = 2, HabitId = 2, CompletedAt = Today.AddDays(-20) }]; // included

        HabitModel neverDone = TestData.Habit(3); // TimesDone null -> included

        List<HabitModel> habits = [doneInWindow, doneOutside, neverDone];

        QueryParameters qp = new() { DoneRangeStart = Today.AddDays(-7), DoneRangeEnd = Today.AddDays(7), ShowDoneInRange = false };
        qp.SortBy[ContentType.Habit] = Sort.Title;

        List<long> result = habits.FilterHabits(qp).Select(h => h.Id).ToList();

        Assert.That(result, Is.EquivalentTo(new long[] { 2, 3 }));
    }
}
