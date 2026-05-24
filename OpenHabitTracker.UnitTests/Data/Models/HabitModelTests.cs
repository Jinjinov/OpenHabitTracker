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

    // --- AddTimesDoneByDay new-day test ---

    [Test]
    public void AddTimesDoneByDay_ForNewDay_CreatesNewDayEntry()
    {
        HabitModel habit = new();
        DateTime existingDay = new(2025, 1, 1, 8, 0, 0);
        TimeModel existing = new() { StartedAt = existingDay };
        habit.TimesDone = [existing];
        habit.RefreshTimesDoneByDay();

        DateTime newDay = new(2025, 1, 5, 10, 0, 0);
        TimeModel newTime = new() { StartedAt = newDay };
        habit.AddTimesDoneByDay(newTime);

        Assert.That(habit.TimesDoneByDay, Contains.Key(newDay.Date));
        Assert.That(habit.TimesDoneByDay![newDay.Date], Has.Count.EqualTo(1));
    }

    // --- AddTimesDoneByDay null guard ---

    [Test]
    public void AddTimesDoneByDay_WhenTimesDoneByDayIsNull_DoesNotThrow()
    {
        HabitModel habit = new();
        habit.TimesDone = null;
        // TimesDoneByDay is null because RefreshTimesDoneByDay was not called

        TimeModel time = new() { StartedAt = new DateTime(2025, 1, 1) };

        Assert.DoesNotThrow(() => habit.AddTimesDoneByDay(time));
    }

    // --- RemoveTimesDoneByDay missing key ---

    [Test]
    public void RemoveTimesDoneByDay_WhenKeyNotInDict_DoesNotThrow()
    {
        HabitModel habit = new();
        DateTime day = new(2025, 1, 1, 8, 0, 0);
        habit.TimesDone = [new TimeModel { StartedAt = day }];
        habit.RefreshTimesDoneByDay();

        TimeModel notPresent = new() { StartedAt = new DateTime(2025, 6, 1) };

        Assert.DoesNotThrow(() => habit.RemoveTimesDoneByDay(notPresent));
    }

    // --- RefreshTimesDoneByDay with null TimesDone ---

    [Test]
    public void RefreshTimesDoneByDay_WhenTimesDoneIsNull_SetsTimesDoneByDayToNull()
    {
        HabitModel habit = new();
        habit.TimesDone = null;

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.TimesDoneByDay, Is.Null);
    }

    // --- RepeatCountReached with RepeatCount=0 ---

    [Test]
    public void RepeatCountReached_WhenRepeatCountIsZero_ReturnsTrueEvenWithNoTimesDone()
    {
        HabitModel habit = new() { RepeatCount = 0, RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [];

        Assert.That(habit.RepeatCountReached(DateTime.Now), Is.True);
    }

    // --- RepeatCountReached upper boundary ---

    [Test]
    public void RepeatCountReached_WhenDoneExactlyAtUpperBoundary_ReturnsTrue()
    {
        HabitModel habit = new() { RepeatCount = 1, RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime date = new(2025, 6, 1, 12, 0, 0);
        DateTime intervalEnd = date + habit.GetRepeatInterval(); // exactly at upper boundary
        habit.TimesDone = [new TimeModel { StartedAt = intervalEnd }];

        Assert.That(habit.RepeatCountReached(date), Is.True);
    }

    // --- GetRatio ElapsedToDesired with zero RepeatInterval ---

    [Test]
    public void GetRatio_ElapsedToDesired_WhenRepeatIntervalIsZero_ReturnsPositiveInfinity()
    {
        HabitModel habit = new() { RepeatInterval = 0, RepeatPeriod = Period.Day };
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();

        double ratio = habit.GetRatio(Ratio.ElapsedToDesired);

        Assert.That(ratio, Is.EqualTo(double.PositiveInfinity));
    }

    // --- GetRatio invalid value ---

    [Test]
    public void GetRatio_InvalidRatio_ThrowsArgumentOutOfRangeException()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [];
        habit.RefreshTimesDoneByDay();

        Assert.Throws<ArgumentOutOfRangeException>(() => habit.GetRatio((Ratio)99));
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

    // --- ElapsedTime with StartAt tests ---

    [Test]
    public void ElapsedTime_WhenNeverDone_AndStartAtSet_IsTimeSinceStartAt()
    {
        DateTime startAt = DateTime.Now.AddDays(-3);
        HabitModel habit = new() { CreatedAt = DateTime.Now.AddDays(-10), StartAt = startAt, LastTimeDoneAt = null };

        TimeSpan elapsed = habit.ElapsedTime;

        Assert.That(elapsed, Is.EqualTo(DateTime.Now - startAt).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void ElapsedTime_WhenNeverDone_AndStartAtInFuture_IsZero()
    {
        DateTime startAt = DateTime.Now.AddDays(5);
        HabitModel habit = new() { CreatedAt = DateTime.Now.AddDays(-10), StartAt = startAt, LastTimeDoneAt = null };

        TimeSpan elapsed = habit.ElapsedTime;

        Assert.That(elapsed, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void ElapsedTime_WhenDone_StartAtIsIgnored()
    {
        DateTime lastDone = DateTime.Now.AddDays(-1);
        DateTime startAt = DateTime.Now.AddDays(-10);
        HabitModel habit = new() { CreatedAt = DateTime.Now.AddDays(-20), StartAt = startAt, LastTimeDoneAt = lastDone };

        TimeSpan elapsed = habit.ElapsedTime;

        Assert.That(elapsed, Is.EqualTo(DateTime.Now - lastDone).Within(TimeSpan.FromSeconds(1)));
    }

    // --- CurrentStreak / BestStreak (calendar-window, RepeatInterval=1) ---

    [Test]
    public void ComputeStreaks_Daily_NoCompletions_BothNull()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak, Is.Null);
        Assert.That(habit.BestStreak, Is.Null);
    }

    [Test]
    public void ComputeStreaks_Daily_DoneToday_CurrentStreak1()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [new TimeModel { StartedAt = DateTime.Today }];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(1));
    }

    [Test]
    public void ComputeStreaks_Daily_DoneYesterdayOnly_CurrentStreak1()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [new TimeModel { StartedAt = DateTime.Today.AddDays(-1) }];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(1));
    }

    [Test]
    public void ComputeStreaks_Daily_DoneTodayAndYesterday_CurrentStreak2()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(2));
    }

    [Test]
    public void ComputeStreaks_Daily_GapYesterdayBreaksStreak_CurrentStreak1()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(1));
    }

    [Test]
    public void ComputeStreaks_Daily_BestStreakLongerThanCurrent()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-6) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-5) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-4) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.BestStreak?.Count, Is.EqualTo(3));
        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(1));
    }

    // --- CurrentStreak / BestStreak (gap-based, RepeatInterval=3, Period=Day) ---

    [Test]
    public void ComputeStreaks_GapBased_DoneToday_CurrentStreak1()
    {
        HabitModel habit = new() { RepeatInterval = 3, RepeatPeriod = Period.Day };
        habit.TimesDone = [new TimeModel { StartedAt = DateTime.Today }];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(1));
    }

    [Test]
    public void ComputeStreaks_GapBased_LastDone4DaysAgo_CurrentStreakNull()
    {
        HabitModel habit = new() { RepeatInterval = 3, RepeatPeriod = Period.Day };
        habit.TimesDone = [new TimeModel { StartedAt = DateTime.Today.AddDays(-4) }];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak, Is.Null);
    }

    [Test]
    public void ComputeStreaks_GapBased_TwoCompletionsWithinGap_CurrentStreak2()
    {
        HabitModel habit = new() { RepeatInterval = 3, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(2));
    }

    [Test]
    public void ComputeStreaks_GapBased_BestStreak3_CurrentStreak1()
    {
        HabitModel habit = new() { RepeatInterval = 3, RepeatPeriod = Period.Day };
        // completions at -10, -7, -4 days form a run of 3 (each gap = 3 days ≤ maxGap=3)
        // then gap from -4 to today = 4 days > maxGap → separate run of 1 today
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-10) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-7) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-4) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.BestStreak?.Count, Is.EqualTo(3));
        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(1));
    }

    [Test]
    public void ComputeStreaks_Daily_5ConsecutiveDays_CurrentStreak5()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-4) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-3) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(5));
        Assert.That(habit.BestStreak?.Count, Is.EqualTo(5));
    }

    [Test]
    public void ComputeStreaks_Daily_GapInMiddle_CurrentStreakIsTrailingRun()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        // done -5, -4, -3, gap on -2, done -1 and today → current streak = 2
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-5) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-4) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-3) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(2));
        Assert.That(habit.BestStreak?.Count, Is.EqualTo(3));
    }

    [Test]
    public void ComputeStreaks_Daily_RepeatCount2_AllDaysMeetCount_CurrentStreak3()
    {
        HabitModel habit = new() { RepeatCount = 2, RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2).AddHours(8) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2).AddHours(20) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1).AddHours(9) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1).AddHours(21) },
            new TimeModel { StartedAt = DateTime.Today.AddHours(10) },
            new TimeModel { StartedAt = DateTime.Today.AddHours(22) },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(3));
    }

    [Test]
    public void ComputeStreaks_Daily_RepeatCount2_OnlyOnePerDay_CurrentStreakNull()
    {
        HabitModel habit = new() { RepeatCount = 2, RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak, Is.Null);
    }

    [Test]
    public void ComputeStreaks_Daily_RepeatCount0_TreatedAs1_CurrentStreak1()
    {
        HabitModel habit = new() { RepeatCount = 0, RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone = [new TimeModel { StartedAt = DateTime.Today }];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(1));
    }

    [Test]
    public void ComputeStreaks_Daily_SingleCompletion_BestStreakFromEqualsTo()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime done = DateTime.Today;
        habit.TimesDone = [new TimeModel { StartedAt = done }];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.BestStreak?.Count, Is.EqualTo(1));
        Assert.That(habit.BestStreak?.From, Is.EqualTo(done));
        Assert.That(habit.BestStreak?.To, Is.EqualTo(done));
    }

    [Test]
    public void ComputeStreaks_Daily_BestStreakFromAndTo_MatchFirstAndLastCompletionInRun()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        DateTime first = DateTime.Today.AddDays(-6);
        DateTime last = DateTime.Today.AddDays(-2);
        habit.TimesDone =
        [
            new TimeModel { StartedAt = first },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-5) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-4) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-3) },
            new TimeModel { StartedAt = last },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.BestStreak?.Count, Is.EqualTo(5));
        Assert.That(habit.BestStreak?.From, Is.EqualTo(first));
        Assert.That(habit.BestStreak?.To, Is.EqualTo(last));
    }

    [Test]
    public void ComputeStreaks_Daily_DoneYesterdayAndDayBefore_NotToday_CurrentStreak2()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1) },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(2));
    }

    [Test]
    public void ComputeStreaks_Weekly_3ConsecutiveWeeks_CurrentStreak3()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Week };
        // Pick a Wednesday 2, 9, and 16 days ago — each lands in a distinct Mon–Sun bucket.
        // We go back far enough that all three are in separate past weeks regardless of today's day-of-week.
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1 + 7) % 7);
        habit.TimesDone =
        [
            new TimeModel { StartedAt = thisMonday.AddDays(-14).AddDays(2) }, // 2 weeks ago
            new TimeModel { StartedAt = thisMonday.AddDays(-7).AddDays(2) },  // last week
            new TimeModel { StartedAt = thisMonday.AddDays(2) },              // this week
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(3));
    }

    [Test]
    public void ComputeStreaks_Weekly_GapBreaksStreak_CurrentStreak2()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Week };
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1 + 7) % 7);
        habit.TimesDone =
        [
            new TimeModel { StartedAt = thisMonday.AddDays(-21).AddDays(2) }, // 3 weeks ago — skipped in streak
            new TimeModel { StartedAt = thisMonday.AddDays(-7).AddDays(2) },  // last week
            new TimeModel { StartedAt = thisMonday.AddDays(2) },              // this week
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(2));
    }

    [Test]
    public void ComputeStreaks_Monthly_3ConsecutiveMonths_CurrentStreak3()
    {
        HabitModel habit = new() { RepeatInterval = 1, RepeatPeriod = Period.Month };
        DateTime thisMonthStart = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        habit.TimesDone =
        [
            new TimeModel { StartedAt = thisMonthStart.AddMonths(-2).AddDays(14) },
            new TimeModel { StartedAt = thisMonthStart.AddMonths(-1).AddDays(14) },
            new TimeModel { StartedAt = thisMonthStart.AddDays(14) },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(3));
    }

    [Test]
    public void ComputeStreaks_GapBased_3CompletionsAt2DayIntervals_CurrentStreak3()
    {
        HabitModel habit = new() { RepeatInterval = 3, RepeatPeriod = Period.Day };
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-4) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-2) },
            new TimeModel { StartedAt = DateTime.Today },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(3));
    }

    [Test]
    public void ComputeStreaks_GapBased_GapBreaksMidRun_CurrentStreakIsTrailingRun()
    {
        HabitModel habit = new() { RepeatInterval = 3, RepeatPeriod = Period.Day };
        // -10 to -5: gap of 5 days > 3 → break; -5, -3, -1 are within 3 days of each other
        habit.TimesDone =
        [
            new TimeModel { StartedAt = DateTime.Today.AddDays(-10) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-5) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-3) },
            new TimeModel { StartedAt = DateTime.Today.AddDays(-1) },
        ];

        habit.RefreshTimesDoneByDay();

        Assert.That(habit.CurrentStreak?.Count, Is.EqualTo(3));
        Assert.That(habit.BestStreak?.Count, Is.EqualTo(3));
    }
}
