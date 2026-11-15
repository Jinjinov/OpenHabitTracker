using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data.Models;

[TestFixture]
public class ItemsModelTests
{
    [Test]
    public void DurationHourAndMinute_NoDuration_ReadZero()
    {
        TaskModel task = new();

        Assert.Multiple(() =>
        {
            Assert.That(task.DurationHour, Is.EqualTo(0));
            Assert.That(task.DurationMinute, Is.EqualTo(0));
        });
    }

    [Test]
    public void DurationHourThenMinute_BuildTheDuration()
    {
        HabitModel habit = new();

        habit.DurationHour = 1;
        habit.DurationMinute = 30;

        Assert.That(habit.Duration, Is.EqualTo(new TimeOnly(1, 30)));
    }

    [Test]
    public void DurationMinute_KeepsTheHour()
    {
        TaskModel task = new() { Duration = new TimeOnly(2, 15) };

        task.DurationMinute = 45;

        Assert.Multiple(() =>
        {
            Assert.That(task.Duration, Is.EqualTo(new TimeOnly(2, 45)));
            Assert.That(task.DurationHour, Is.EqualTo(2));
        });
    }

    [Test]
    public void DurationHourAndMinuteSetToZero_ClearTheDuration()
    {
        TaskModel task = new() { Duration = new TimeOnly(0, 20) };

        task.DurationMinute = 0;

        Assert.That(task.Duration, Is.Null);
    }

    [Test]
    public void DurationHourSetToZero_WithMinutesLeft_KeepsTheMinutes()
    {
        TaskModel task = new() { Duration = new TimeOnly(1, 20) };

        task.DurationHour = 0;

        Assert.That(task.Duration, Is.EqualTo(new TimeOnly(0, 20)));
    }
}
