using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data.Models;

[TestFixture]
public class HabitModelTests
{
    // --- GetRepeatInterval tests ---

    [Test]
    public void GetRepeatInterval_Day_ReturnsCorrectTimeSpan()
    {
        HabitModel habit = new() { RepeatInterval = 2, RepeatPeriod = Period.Day };
        Assert.That(habit.GetRepeatInterval(), Is.EqualTo(TimeSpan.FromDays(2)));
    }

    [Test]
    public void GetRepeatInterval_Week_ReturnsCorrectTimeSpan()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Week };
        Assert.That(habit.GetRepeatInterval(), Is.EqualTo(TimeSpan.FromDays(7)));
    }

    [Test]
    public void GetRepeatInterval_Month_ReturnsCorrectTimeSpan()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Month };
        Assert.That(habit.GetRepeatInterval(), Is.EqualTo(TimeSpan.FromDays(30)));
    }

    [Test]
    public void GetRepeatInterval_Year_ReturnsCorrectTimeSpan()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Year };
        Assert.That(habit.GetRepeatInterval(), Is.EqualTo(TimeSpan.FromDays(365)));
    }

    // --- RefreshTimesDoneByDay tests ---

    [Test]
    public void RefreshTimesDoneByDay_GroupsEntriesByStartedAtDate()
    {
        HabitModel habit = new();
        DateTime day1 = new(2025, 1, 1, 8, 0, 0);
        DateTime day2 = new(2025, 1, 2, 9, 0, 0);
        habit.TimesDone =
        [
            new TimeModel { StartedAt = day1 },
            new TimeModel { StartedAt = day1.AddHours(4) },
            new TimeModel { StartedAt = day2 }
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.TimesDoneByDay, Has.Count.EqualTo(2));
        Assert.That(habit.TimesDoneByDay![day1.Date], Has.Count.EqualTo(2));
        Assert.That(habit.TimesDoneByDay![day2.Date], Has.Count.EqualTo(1));
    }

    [Test]
    public void RefreshTimesDoneByDay_WhenTimesDoneIsEmpty_ProducesEmptyDict()
    {
        HabitModel habit = new() { TimesDone = [] };

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.TimesDoneByDay, Is.Not.Null);
        Assert.That(habit.TimesDoneByDay, Is.Empty);
    }

    // --- RepeatCountReached tests ---

    [Test]
    public void RepeatCountReached_WhenEnoughTimesDoneInInterval_ReturnsTrue()
    {
        HabitModel habit = new() { RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime now = new(2025, 6, 1, 12, 0, 0);
        habit.TimesDone = [new TimeModel { StartedAt = now }];

        Assert.That(habit.RepeatCountReached(now), Is.True);
    }

    [Test]
    public void RepeatCountReached_WhenNotEnoughTimesDoneInInterval_ReturnsFalse()
    {
        HabitModel habit = new() { RepeatCount = 2, RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime now = new(2025, 6, 1, 12, 0, 0);
        habit.TimesDone = [new TimeModel { StartedAt = now }]; // only 1, need 2

        Assert.That(habit.RepeatCountReached(now), Is.False);
    }

    [Test]
    public void RepeatCountReached_WhenTimesDoneIsNull_ReturnsFalse()
    {
        HabitModel habit = new() { RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = null;

        Assert.That(habit.RepeatCountReached(DateTime.Now), Is.False);
    }

    // --- GetRatio tests ---

    [Test]
    public void GetRatio_ElapsedToDesired_DoesNotThrow()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();

        Assert.DoesNotThrow(() => habit.GetRatio(Ratio.ElapsedToDesired));
    }

    [Test]
    public void GetRatio_ElapsedToAverage_DoesNotThrow()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime t1 = DateTime.Now.AddDays(-2);
        DateTime t2 = DateTime.Now.AddDays(-1);
        habit.TimesDone = [new TimeModel { StartedAt = t1 }, new TimeModel { StartedAt = t2 }];
        habit.LastTimeDoneAt = t2;
        habit.RefreshTimesDoneByDay();

        double ratio = habit.GetRatio(Ratio.ElapsedToAverage);

        Assert.That(ratio, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void GetRatio_AverageToDesired_DoesNotThrow()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime t1 = DateTime.Now.AddDays(-2);
        DateTime t2 = DateTime.Now.AddDays(-1);
        habit.TimesDone = [new TimeModel { StartedAt = t1 }, new TimeModel { StartedAt = t2 }];
        habit.RefreshTimesDoneByDay();

        Assert.DoesNotThrow(() => habit.GetRatio(Ratio.AverageToDesired));
    }

    // --- AddTimesDoneByDay / RemoveTimesDoneByDay tests ---

    [Test]
    public void AddTimesDoneByDay_AddsToExistingDay()
    {
        HabitModel habit = new();
        DateTime day = new(2025, 1, 1, 8, 0, 0);
        TimeModel existing = new() { StartedAt = day };
        habit.TimesDone = [existing];
        habit.RefreshTimesDoneByDay();

        TimeModel newTime = new() { StartedAt = day.AddHours(2) };
        habit.AddTimesDoneByDay(newTime);

        Assert.That(habit.TimesDoneByDay![day.Date], Has.Count.EqualTo(2));
    }

    [Test]
    public void RemoveTimesDoneByDay_RemovesFromDay()
    {
        HabitModel habit = new();
        DateTime day = new(2025, 1, 1, 8, 0, 0);
        TimeModel time = new() { StartedAt = day };
        habit.TimesDone = [time];
        habit.RefreshTimesDoneByDay();

        habit.RemoveTimesDoneByDay(time);

        Assert.That(habit.TimesDoneByDay![day.Date], Is.Empty);
    }

    // --- OnTimesDoneChanged tests ---

    [Test]
    public void OnTimesDoneChanged_ZeroEntries_SetsAllAveragesToZero()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.AverageInterval, Is.EqualTo(TimeSpan.Zero));
        Assert.That(habit.TotalTimeSpent, Is.EqualTo(TimeSpan.Zero));
        Assert.That(habit.AverageTimeSpent, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void OnTimesDoneChanged_OneEntry_SetsAverageIntervalToRepeatInterval()
    {
        HabitModel habit = new() { RepeatInterval = 3, RepeatPeriod = Period.Day };
        habit.TimesDone = [new TimeModel { StartedAt = DateTime.Now }];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.AverageInterval, Is.EqualTo(habit.GetRepeatInterval()));
    }

    [Test]
    public void OnTimesDoneChanged_MultipleEntries_ComputesCorrectAverageIntervalAndTotalTimeSpent()
    {
        DateTime t1 = new(2025, 1, 1, 8, 0, 0);
        DateTime t2 = new(2025, 1, 3, 8, 0, 0); // 2 days after t1
        DateTime t3 = new(2025, 1, 7, 8, 0, 0); // 4 days after t2
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = t1, CompletedAt = t1.AddHours(1) },
            new TimeModel { StartedAt = t2, CompletedAt = t2.AddHours(2) },
            new TimeModel { StartedAt = t3, CompletedAt = t3.AddHours(3) },
        ];

        habit.RefreshTimesDoneByDay();

        // average interval = (2 days + 4 days) / 2 = 3 days
        Assert.That(habit.AverageInterval, Is.EqualTo(TimeSpan.FromDays(3)));
        // total time spent = 1h + 2h + 3h = 6h
        Assert.That(habit.TotalTimeSpent, Is.EqualTo(TimeSpan.FromHours(6)));
    }

    // --- GetRatio edge-case tests ---

    [Test]
    public void GetRatio_ElapsedToAverage_WhenNoTimesDone_ReturnsPositiveInfinity()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day, CreatedAt = DateTime.Now.AddDays(-1) };
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay(); // AverageInterval = TimeSpan.Zero

        double ratio = habit.GetRatio(Ratio.ElapsedToAverage);

        Assert.That(ratio, Is.EqualTo(double.PositiveInfinity));
    }

    [Test]
    public void GetRatio_AverageToDesired_WhenNoTimesDone_ReturnsZero()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay(); // AverageInterval = TimeSpan.Zero

        double ratio = habit.GetRatio(Ratio.AverageToDesired);

        Assert.That(ratio, Is.EqualTo(0.0));
    }

    // --- RepeatCountReached boundary test ---

    [Test]
    public void RepeatCountReached_WhenDoneExactlyAtWindowBoundary_ReturnsTrue()
    {
        HabitModel habit = new() { RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime date = new(2025, 6, 1, 12, 0, 0);
        DateTime intervalStart = date - habit.GetRepeatInterval(); // exactly at lower boundary
        habit.TimesDone = [new TimeModel { StartedAt = intervalStart }];

        Assert.That(habit.RepeatCountReached(date), Is.True);
    }

    // --- OnTimesDoneChanged in-progress time test ---

    [Test]
    public void OnTimesDoneChanged_WithInProgressTime_TotalTimeSpentIgnoresInProgressTime()
    {
        DateTime t1 = new(2025, 1, 1, 8, 0, 0);
        DateTime t2 = new(2025, 1, 2, 8, 0, 0);
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = t1, CompletedAt = t1.AddHours(1) }, // 1 hour
            new TimeModel { StartedAt = t2 },                                // in-progress, contributes 0
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.TotalTimeSpent, Is.EqualTo(TimeSpan.FromHours(1)));
    }

    // --- ElapsedTime tests ---

    [Test]
    public void ElapsedTime_WhenNeverDone_IsTimeSinceCreatedAt()
    {
        DateTime createdAt = DateTime.Now.AddDays(-5);
        HabitModel habit = new() { CreatedAt = createdAt, LastTimeDoneAt = null };

        TimeSpan elapsed = habit.ElapsedTime;

        Assert.That(elapsed, Is.EqualTo(DateTime.Now - createdAt).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void ElapsedTime_WhenDone_IsTimeSinceLastTimeDoneAt()
    {
        DateTime lastDone = DateTime.Now.AddDays(-2);
        HabitModel habit = new() { LastTimeDoneAt = lastDone };

        TimeSpan elapsed = habit.ElapsedTime;

        Assert.That(elapsed, Is.EqualTo(DateTime.Now - lastDone).Within(TimeSpan.FromSeconds(1)));
    }
}
