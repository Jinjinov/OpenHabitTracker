using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data.Models;

[TestFixture]
public class TaskModelTests
{
    [Test]
    public void PlannedDate_SetWithNoExistingTime_StoresDateOnlySentinel()
    {
        TaskModel task = new();

        task.PlannedDate = new DateTime(2030, 1, 15);

        Assert.That(task.IsPlannedDateOnly, Is.True);
        Assert.That(task.PlannedTime, Is.Null);
        Assert.That(task.PlannedAt!.Value.Date, Is.EqualTo(new DateTime(2030, 1, 15)));
        Assert.That(task.PlannedAt!.Value.TimeOfDay, Is.EqualTo(TaskModel.DateOnlySentinel));
    }

    [Test]
    public void PlannedTime_SetWithNoDate_DefaultsDateToToday()
    {
        TaskModel task = new();

        task.PlannedTime = new TimeOnly(10, 0);

        Assert.That(task.IsPlannedDateOnly, Is.False);
        Assert.That(task.PlannedAt!.Value.Date, Is.EqualTo(DateTime.Today));
        Assert.That(task.PlannedTime, Is.EqualTo(new TimeOnly(10, 0)));
    }

    [Test]
    public void PlannedDate_ChangedWhileTimed_KeepsTime()
    {
        TaskModel task = new() { PlannedAt = new DateTime(2030, 1, 15, 10, 0, 0) };

        task.PlannedDate = new DateTime(2030, 2, 20);

        Assert.That(task.IsPlannedDateOnly, Is.False);
        Assert.That(task.PlannedAt, Is.EqualTo(new DateTime(2030, 2, 20, 10, 0, 0)));
    }

    [Test]
    public void PlannedDate_ChangedWhileDateOnly_StaysDateOnly()
    {
        TaskModel task = new();
        task.PlannedDate = new DateTime(2030, 1, 15);

        task.PlannedDate = new DateTime(2030, 2, 20);

        Assert.That(task.IsPlannedDateOnly, Is.True);
        Assert.That(task.PlannedAt!.Value.Date, Is.EqualTo(new DateTime(2030, 2, 20)));
    }

    [Test]
    public void PlannedTime_ClearedWhileTimed_BecomesDateOnlySameDate()
    {
        TaskModel task = new() { PlannedAt = new DateTime(2030, 1, 15, 10, 0, 0) };

        task.PlannedTime = null;

        Assert.That(task.IsPlannedDateOnly, Is.True);
        Assert.That(task.PlannedAt!.Value.Date, Is.EqualTo(new DateTime(2030, 1, 15)));
    }

    [Test]
    public void PlannedDate_ClearedToNull_ClearsPlannedAt()
    {
        TaskModel task = new() { PlannedAt = new DateTime(2030, 1, 15, 10, 0, 0) };

        task.PlannedDate = null;

        Assert.That(task.PlannedAt, Is.Null);
        Assert.That(task.PlannedTime, Is.Null);
        Assert.That(task.IsPlannedDateOnly, Is.False);
    }

    [Test]
    public void IsPlannedDateOnly_SentinelTime_DetectedAndHidesTime()
    {
        TaskModel task = new() { PlannedAt = new DateTime(2030, 1, 15) + TaskModel.DateOnlySentinel };

        Assert.That(task.IsPlannedDateOnly, Is.True);
        Assert.That(task.PlannedTime, Is.Null);
    }

    [Test]
    public void IsPlannedDateOnly_LatestPickableTime_IsNotDateOnly()
    {
        // The minute-granularity picker maxes out at 23:59:00 - it must read as a timed task.
        TaskModel task = new() { PlannedAt = new DateTime(2030, 1, 15, 23, 59, 0) };

        Assert.That(task.IsPlannedDateOnly, Is.False);
        Assert.That(task.PlannedTime, Is.EqualTo(new TimeOnly(23, 59)));
    }

    [Test]
    public void PlannedAt_Null_IsNotDateOnly()
    {
        TaskModel task = new();

        Assert.That(task.IsPlannedDateOnly, Is.False);
        Assert.That(task.PlannedDate, Is.Null);
        Assert.That(task.PlannedTime, Is.Null);
    }
}
