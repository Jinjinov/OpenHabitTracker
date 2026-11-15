using OpenHabitTracker.App;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class CalendarParamsTests
{
    // 2026-03-04 is a Wednesday.
    private static readonly DateTime _wednesday = new(2026, 3, 4, 17, 45, 0);

    [TestCase(DayOfWeek.Monday, 2026, 3, 2)]
    [TestCase(DayOfWeek.Sunday, 2026, 3, 1)]
    [TestCase(DayOfWeek.Saturday, 2026, 2, 28)]
    [TestCase(DayOfWeek.Wednesday, 2026, 3, 4)]
    [TestCase(DayOfWeek.Thursday, 2026, 2, 26)]
    public void GetFirstDayOfWeek_ReturnsTheMidnightOfTheWeekStart(DayOfWeek firstDayOfWeek, int year, int month, int day)
    {
        DateTime result = CalendarParams.GetFirstDayOfWeek(firstDayOfWeek, _wednesday);

        Assert.That(result, Is.EqualTo(new DateTime(year, month, day)));
    }

    [Test]
    public void GetFirstDayOfWeek_AcrossTheYearBoundary_ReturnsTheDayInThePreviousYear()
    {
        // 2027-01-01 is a Friday.
        DateTime result = CalendarParams.GetFirstDayOfWeek(DayOfWeek.Monday, new DateTime(2027, 1, 1));

        Assert.That(result, Is.EqualTo(new DateTime(2026, 12, 28)));
    }

    [Test]
    public void SetCalendarStartByFirstDayOfWeek_StartsOnTheWeekOfTheDayAndShowsItsMonth()
    {
        CalendarParams sut = new();

        sut.SetCalendarStartByFirstDayOfWeek(DayOfWeek.Monday, new DateTime(2026, 3, 1));

        Assert.Multiple(() =>
        {
            Assert.That(sut.CalendarStart, Is.EqualTo(new DateTime(2026, 2, 23)));
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(new DateTime(2026, 3, 1)));
        });
    }

    [TestCase(DayOfWeek.Monday, 2026, 2, 23)]
    [TestCase(DayOfWeek.Sunday, 2026, 3, 1)]
    public void SetCalendarStartByFirstDayOfMonth_StartsOnTheWeekOfTheFirstOfTheMonth(DayOfWeek firstDayOfWeek, int year, int month, int day)
    {
        CalendarParams sut = new();

        sut.SetCalendarStartByFirstDayOfMonth(firstDayOfWeek, new DateTime(2026, 3, 20));

        Assert.Multiple(() =>
        {
            Assert.That(sut.CalendarStart, Is.EqualTo(new DateTime(year, month, day)));
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(new DateTime(2026, 3, 1)));
        });
    }

    [Test]
    public void SetCalendarStartToNextMonth_FromDecember_MovesIntoJanuaryOfTheNextYear()
    {
        CalendarParams sut = new();
        sut.SetCalendarStartByFirstDayOfMonth(DayOfWeek.Monday, new DateTime(2026, 12, 10));

        sut.SetCalendarStartToNextMonth(DayOfWeek.Monday);

        Assert.Multiple(() =>
        {
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(new DateTime(2027, 1, 1)));
            Assert.That(sut.CalendarStart, Is.EqualTo(new DateTime(2026, 12, 28)));
        });
    }

    [Test]
    public void SetCalendarStartToPreviousMonth_FromJanuary_MovesIntoDecemberOfThePreviousYear()
    {
        CalendarParams sut = new();
        sut.SetCalendarStartByFirstDayOfMonth(DayOfWeek.Sunday, new DateTime(2027, 1, 15));

        sut.SetCalendarStartToPreviousMonth(DayOfWeek.Sunday);

        Assert.Multiple(() =>
        {
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(new DateTime(2026, 12, 1)));
            Assert.That(sut.CalendarStart, Is.EqualTo(new DateTime(2026, 11, 29)));
        });
    }

    [Test]
    public void SetCalendarStartToNextMonth_ThenPrevious_ReturnsToTheSameView()
    {
        CalendarParams sut = new();
        sut.SetCalendarStartByFirstDayOfMonth(DayOfWeek.Monday, new DateTime(2028, 2, 29));
        DateTime start = sut.CalendarStart;
        DateTime month = sut.FirstDayOfMonth;

        sut.SetCalendarStartToNextMonth(DayOfWeek.Monday);
        sut.SetCalendarStartToPreviousMonth(DayOfWeek.Monday);

        Assert.Multiple(() =>
        {
            Assert.That(sut.CalendarStart, Is.EqualTo(start));
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(month));
        });
    }

    // A grid that starts in the second half of a month shows mostly the next month.
    [TestCase(2026, 3, 7, 2026, 3, 14, 2026, 3, 1)]
    [TestCase(2026, 3, 8, 2026, 3, 15, 2026, 4, 1)]
    [TestCase(2026, 12, 20, 2026, 12, 27, 2027, 1, 1)]
    public void SetCalendarStartToNextWeek_MovesSevenDaysAndPicksTheMonthMostOfTheGridIsIn(
        int year, int month, int day, int startYear, int startMonth, int startDay, int monthYear, int monthMonth, int monthDay)
    {
        CalendarParams sut = new() { CalendarStart = new DateTime(year, month, day) };

        sut.SetCalendarStartToNextWeek();

        Assert.Multiple(() =>
        {
            Assert.That(sut.CalendarStart, Is.EqualTo(new DateTime(startYear, startMonth, startDay)));
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(new DateTime(monthYear, monthMonth, monthDay)));
        });
    }

    [Test]
    public void SetCalendarStartToPreviousWeek_IntoThePreviousMonth_MovesSevenDaysBack()
    {
        CalendarParams sut = new() { CalendarStart = new DateTime(2026, 3, 2) };

        sut.SetCalendarStartToPreviousWeek();

        Assert.Multiple(() =>
        {
            Assert.That(sut.CalendarStart, Is.EqualTo(new DateTime(2026, 2, 23)));
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(new DateTime(2026, 3, 1)));
        });
    }

    [Test]
    public void SetCalendarStartByDaysFromToday_StartsThatManyDaysFromTodayAndShowsThisMonth()
    {
        CalendarParams sut = new();

        DateTime before = DateTime.Today;
        sut.SetCalendarStartByDaysFromToday(-6);
        DateTime after = DateTime.Today;

        // the method reads the clock itself, so a run across midnight may resolve against either day
        Assert.Multiple(() =>
        {
            Assert.That(sut.CalendarStart, Is.EqualTo(before.AddDays(-6)).Or.EqualTo(after.AddDays(-6)));
            Assert.That(sut.FirstDayOfMonth, Is.EqualTo(new DateTime(before.Year, before.Month, 1)).Or.EqualTo(new DateTime(after.Year, after.Month, 1)));
        });
    }
}
