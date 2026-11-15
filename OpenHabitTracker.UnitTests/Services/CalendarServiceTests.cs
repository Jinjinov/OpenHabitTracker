using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class CalendarServiceTests
{
    private CalendarService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new();
    }

    // --- GetDayOfWeek tests ---

    [TestCase(DayOfWeek.Sunday, 0, 0)]   // Sun first, index 0 → Sunday (0)
    [TestCase(DayOfWeek.Sunday, 1, 1)]   // Sun first, index 1 → Monday (1)
    [TestCase(DayOfWeek.Sunday, 6, 6)]   // Sun first, index 6 → Saturday (6)
    [TestCase(DayOfWeek.Monday, 0, 1)]   // Mon first, index 0 → Monday (1)
    [TestCase(DayOfWeek.Monday, 6, 0)]   // Mon first, index 6 → Sunday (0) — wrap-around
    [TestCase(DayOfWeek.Saturday, 0, 6)] // Sat first, index 0 → Saturday (6)
    [TestCase(DayOfWeek.Saturday, 1, 0)] // Sat first, index 1 → Sunday (0) — wrap-around
    public void GetDayOfWeek_ReturnsCorrectDayOfWeekValue(DayOfWeek firstDayOfWeek, int dayIndex, int expected)
    {
        int result = CalendarService.GetDayOfWeek(firstDayOfWeek, dayIndex);

        Assert.That(result, Is.EqualTo(expected));
    }

    // --- GetCalendarDay tests ---

    [Test]
    public void GetCalendarDay_Day0_ReturnsCalendarStart()
    {
        DateTime start = new(2025, 1, 1);

        DateTime result = _sut.GetCalendarDay(start, 0);

        Assert.That(result, Is.EqualTo(new DateTime(2025, 1, 1)));
    }

    [Test]
    public void GetCalendarDay_Day1_ReturnsNextDay()
    {
        DateTime start = new(2025, 1, 1);

        DateTime result = _sut.GetCalendarDay(start, 1);

        Assert.That(result, Is.EqualTo(new DateTime(2025, 1, 2)));
    }

    [Test]
    public void GetCalendarDay_Day7_ReturnsOneWeekLater()
    {
        DateTime start = new(2025, 3, 1);

        DateTime result = _sut.GetCalendarDay(start, 7);

        Assert.That(result, Is.EqualTo(new DateTime(2025, 3, 8)));
    }

    [Test]
    public void GetCalendarDay_SameArgs_ReturnsSameValue()
    {
        DateTime start = new(2025, 6, 1);

        DateTime first = _sut.GetCalendarDay(start, 3);
        DateTime second = _sut.GetCalendarDay(start, 3);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void GetCalendarDay_NegativeDay_ReturnsDateBeforeStart()
    {
        DateTime start = new(2025, 6, 15);

        DateTime result = _sut.GetCalendarDay(start, -7);

        Assert.That(result, Is.EqualTo(new DateTime(2025, 6, 8)));
    }

    [TestCase(2028, 2, 28, 1, 2028, 2, 29)]
    [TestCase(2028, 2, 28, 2, 2028, 3, 1)]
    [TestCase(2026, 12, 31, 1, 2027, 1, 1)]
    [TestCase(2026, 3, 28, 2, 2026, 3, 30)]
    [TestCase(2026, 10, 24, 2, 2026, 10, 26)]
    public void GetCalendarDay_AcrossLeapYearYearEndAndDaylightSavingChanges_StaysOnMidnight(int year, int month, int day, int offset, int expectedYear, int expectedMonth, int expectedDay)
    {
        DateTime result = _sut.GetCalendarDay(new DateTime(year, month, day), offset);

        Assert.That(result, Is.EqualTo(new DateTime(expectedYear, expectedMonth, expectedDay)));
        Assert.That(result.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void GetCalendarDay_StartWithATime_KeepsTheTime()
    {
        DateTime result = _sut.GetCalendarDay(new DateTime(2026, 1, 1, 13, 5, 0), 3);

        Assert.That(result, Is.EqualTo(new DateTime(2026, 1, 4, 13, 5, 0)));
    }
}
