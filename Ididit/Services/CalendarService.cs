using Ididit.Data;

namespace Ididit.Services;

public class CalendarService(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly string[] _days = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };

    private readonly Dictionary<long, DateTime> _dateByTicks = new();

    private const long _ticksInDay = 864_000_000_000;
    private const long _ticksInWeek = 6_048_000_000_000;

    private DateTime _calendarStart;

    public DateTime FirstDayOfMonth { get; private set; }

    public async Task Initialize()
    {
        await _appData.InitializeSettings();

        SetCalendarStartByFirstDayOfMonth(DateTime.Today);
    }

    public string GetDayOfWeek(int dayIndex)
    {
        return _days[(dayIndex + (int)_appData.Settings.FirstDayOfWeek) % 7];
    }

    DateTime GetCalendarDay(int calendarWeek, int dayInWeek)
    {
        int days = calendarWeek * 7 + dayInWeek;

        return _calendarStart.AddDays(days);
    }

    public DateTime GetCalendarDayWithCaching(int calendarWeek, int dayInWeek)
    {
        long ticks = _calendarStart.Ticks + calendarWeek * _ticksInWeek + dayInWeek * _ticksInDay;

        if (!_dateByTicks.TryGetValue(ticks, out DateTime date))
        {
            date = new DateTime(ticks);
            _dateByTicks[ticks] = date;
        }

        return date;
    }

    private DateTime GetFirstDayOfMonth(DateTime day)
    {
        return new DateTime(day.Year, day.Month, 1);
    }

    private DateTime GetFirstDateOfWeek(DateTime day)
    {
        int diff = -((7 + (day.DayOfWeek - _appData.Settings.FirstDayOfWeek)) % 7);
        return day.AddDays(diff).Date;
    }

    void SetCalendarStartByFirstDayOfWeek(DateTime day)
    {
        FirstDayOfMonth = GetFirstDayOfMonth(day);
        _calendarStart = GetFirstDateOfWeek(day);
    }

    void SetCalendarStartByFirstDayOfMonth(DateTime day)
    {
        FirstDayOfMonth = GetFirstDayOfMonth(day);
        _calendarStart = GetFirstDateOfWeek(FirstDayOfMonth);
    }

    public void SetCalendarStartToNextMonth()
    {
        FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
        _calendarStart = GetFirstDateOfWeek(FirstDayOfMonth);
    }

    public void SetCalendarStartToPreviousMonth()
    {
        FirstDayOfMonth = FirstDayOfMonth.AddMonths(-1);
        _calendarStart = GetFirstDateOfWeek(FirstDayOfMonth);
    }

    public void SetCalendarStartToNextWeek()
    {
        _calendarStart = _calendarStart.AddDays(7);
        FirstDayOfMonth = GetFirstDayOfMonth(_calendarStart);
        if (_calendarStart.Day > 14)
            FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
    }

    public void SetCalendarStartToPreviousWeek()
    {
        _calendarStart = _calendarStart.AddDays(-7);
        FirstDayOfMonth = GetFirstDayOfMonth(_calendarStart);
        if (_calendarStart.Day > 14)
            FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
    }
}
