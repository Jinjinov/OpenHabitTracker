using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data.Models;

[TestFixture]
public class SettingsModelRangeTests
{
    private static readonly DateTime Today = new(2026, 7, 24);

    [Test]
    public void ResolvePlannedRange_BothNull_ReturnsNoBounds()
    {
        SettingsModel settings = new();

        (DateTime? start, DateTime? end) = settings.ResolvePlannedRange(Today);

        Assert.That(start, Is.Null);
        Assert.That(end, Is.Null);
    }

    [Test]
    public void ResolvePlannedRange_PastRange_ResolvesRelativeToToday()
    {
        SettingsModel settings = new() { PlannedFromDayOffset = -7, PlannedToDayOffset = -3 };

        (DateTime? start, DateTime? end) = settings.ResolvePlannedRange(Today);

        Assert.That(start, Is.EqualTo(new DateTime(2026, 7, 17)));
        Assert.That(end, Is.EqualTo(new DateTime(2026, 7, 21)));
    }

    [Test]
    public void ResolvePlannedRange_ZeroToZero_IsTodayOnly()
    {
        SettingsModel settings = new() { PlannedFromDayOffset = 0, PlannedToDayOffset = 0 };

        (DateTime? start, DateTime? end) = settings.ResolvePlannedRange(Today);

        Assert.That(start, Is.EqualTo(Today));
        Assert.That(end, Is.EqualTo(Today));
    }

    [Test]
    public void ResolvePlannedRange_SpanningToday_ResolvesBothSides()
    {
        SettingsModel settings = new() { PlannedFromDayOffset = -7, PlannedToDayOffset = 7 };

        (DateTime? start, DateTime? end) = settings.ResolvePlannedRange(Today);

        Assert.That(start, Is.EqualTo(new DateTime(2026, 7, 17)));
        Assert.That(end, Is.EqualTo(new DateTime(2026, 7, 31)));
    }

    [Test]
    public void ResolvePlannedRange_OpenLowerBound_OnlyEndResolved()
    {
        SettingsModel settings = new() { PlannedFromDayOffset = null, PlannedToDayOffset = 7 };

        (DateTime? start, DateTime? end) = settings.ResolvePlannedRange(Today);

        Assert.That(start, Is.Null);
        Assert.That(end, Is.EqualTo(new DateTime(2026, 7, 31)));
    }

    [Test]
    public void ResolvePlannedRange_OpenUpperBound_OnlyStartResolved()
    {
        SettingsModel settings = new() { PlannedFromDayOffset = -7, PlannedToDayOffset = null };

        (DateTime? start, DateTime? end) = settings.ResolvePlannedRange(Today);

        Assert.That(start, Is.EqualTo(new DateTime(2026, 7, 17)));
        Assert.That(end, Is.Null);
    }

    [Test]
    public void ResolvePlannedRange_IgnoresTimeOfDay()
    {
        SettingsModel settings = new() { PlannedFromDayOffset = 0, PlannedToDayOffset = 1 };

        (DateTime? start, DateTime? end) = settings.ResolvePlannedRange(Today.AddHours(15).AddMinutes(42));

        Assert.That(start, Is.EqualTo(Today));
        Assert.That(end, Is.EqualTo(Today.AddDays(1)));
    }

    [Test]
    public void ResolveDoneRange_ReadsDoneOffsets()
    {
        SettingsModel settings = new()
        {
            PlannedFromDayOffset = 100,
            PlannedToDayOffset = 200,
            DoneFromDayOffset = -5,
            DoneToDayOffset = 5,
        };

        (DateTime? start, DateTime? end) = settings.ResolveDoneRange(Today);

        Assert.That(start, Is.EqualTo(new DateTime(2026, 7, 19)));
        Assert.That(end, Is.EqualTo(new DateTime(2026, 7, 29)));
    }

    [Test]
    public void ResolveRange_ExtremeOffsets_ClampsInsteadOfOverflowing()
    {
        SettingsModel settings = new() { PlannedFromDayOffset = int.MinValue, PlannedToDayOffset = int.MaxValue };

        (DateTime? Start, DateTime? End) range = default;
        Assert.DoesNotThrow(() => range = settings.ResolvePlannedRange(Today));

        Assert.That(range.Start, Is.EqualTo(Today.AddDays(-SettingsModel.MaxDayOffset)));
        Assert.That(range.End, Is.EqualTo(Today.AddDays(SettingsModel.MaxDayOffset)));
    }
}
