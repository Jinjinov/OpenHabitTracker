using Ididit.Data;

namespace Ididit.Services;

public class CalendarService(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly string[] _days = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };

    private const long _ticksInDay = 864_000_000_000;
    private const long _ticksInWeek = 6_048_000_000_000;

    private DateTime _calendarStart;

    private long _calendarStartTicks;

    private readonly Dictionary<long, DateTime> _dateByTicks = new();

    private DayOfWeek _firstDayOfWeek;

    public DateTime FirstDayOfMonth { get; private set; }

    public async Task Initialize()
    {
        await _appData.InitializeSettings();

        SetCalendarStartByFirstDayOfMonth(DateTime.Today, _appData.Settings.StartOfWeek);
    }

    public string GetDayOfWeek(int dayIndex)
    {
        return _days[(dayIndex + (int)_firstDayOfWeek) % 7];
    }

    DateTime GetCalendarDay(int calendarWeek, int dayInWeek)
    {
        int days = calendarWeek * 7 + dayInWeek;

        return _calendarStart.AddDays(days);
    }

    public DateTime GetCalendarDayWithCaching(int calendarWeek, int dayInWeek)
    {
        long ticks = _calendarStartTicks + calendarWeek * _ticksInWeek + dayInWeek * _ticksInDay;

        if (!_dateByTicks.TryGetValue(ticks, out DateTime date))
        {
            date = new DateTime(ticks);
            _dateByTicks[ticks] = date;
        }

        return date;
    }

    DateTime GetFirstDayOfMonth(DateTime day)
    {
        return new DateTime(day.Year, day.Month, 1);
    }

    DateTime GetFirstDateOfWeek(DateTime day)
    {
        int diff = (7 + (day.DayOfWeek - _firstDayOfWeek)) % 7;
        return day.AddDays(-1 * diff).Date;
        //return day.AddDays((int)_firstDayOfWeek - (int)day.DayOfWeek);
    }

    void SetCalendarStartByFirstDayOfWeek(DateTime day, DayOfWeek firstDayOfWeek)
    {
        _firstDayOfWeek = firstDayOfWeek;
        FirstDayOfMonth = GetFirstDayOfMonth(day);
        _calendarStart = GetFirstDateOfWeek(day);
        _calendarStartTicks = _calendarStart.Ticks;
    }

    void SetCalendarStartByFirstDayOfMonth(DateTime day, DayOfWeek firstDayOfWeek)
    {
        _firstDayOfWeek = firstDayOfWeek;
        FirstDayOfMonth = GetFirstDayOfMonth(day);
        _calendarStart = GetFirstDateOfWeek(FirstDayOfMonth);
        _calendarStartTicks = _calendarStart.Ticks;
    }

    void SetCalendarStart(DateTime day)
    {
        FirstDayOfMonth = GetFirstDayOfMonth(day);
        _calendarStart = day;
        _firstDayOfWeek = day.DayOfWeek;
        _calendarStartTicks = _calendarStart.Ticks;
    }

    public void SetCalendarStartToNextMonth()
    {
        FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
        _calendarStart = GetFirstDateOfWeek(FirstDayOfMonth);
        _calendarStartTicks = _calendarStart.Ticks;
    }

    public void SetCalendarStartToPreviousMonth()
    {
        FirstDayOfMonth = FirstDayOfMonth.AddMonths(-1);
        _calendarStart = GetFirstDateOfWeek(FirstDayOfMonth);
        _calendarStartTicks = _calendarStart.Ticks;
    }

    public void SetCalendarStartToNextWeek()
    {
        _calendarStart = _calendarStart.AddDays(7);
        FirstDayOfMonth = GetFirstDayOfMonth(_calendarStart);
        if (_calendarStart.Day > 14)
            FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
        _calendarStartTicks = _calendarStart.Ticks;
    }

    public void SetCalendarStartToPreviousWeek()
    {
        _calendarStart = _calendarStart.AddDays(-7);
        FirstDayOfMonth = GetFirstDayOfMonth(_calendarStart);
        if (_calendarStart.Day > 14)
            FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
        _calendarStartTicks = _calendarStart.Ticks;
    }
}
