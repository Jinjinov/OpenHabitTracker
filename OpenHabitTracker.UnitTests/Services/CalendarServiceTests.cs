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
}
