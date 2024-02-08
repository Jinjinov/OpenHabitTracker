namespace Ididit.Services;

public class CalendarService
{
    private readonly string[] _days = { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };

    private readonly Dictionary<long, DateTime> _dateByTicks = new();

    private const long _ticksInDay = 864_000_000_000;
    private const long _ticksInWeek = 6_048_000_000_000;

    public string GetDayOfWeek(DayOfWeek firstDayOfWeek, int dayIndex)
    {
        return _days[(dayIndex + (int)firstDayOfWeek) % 7];
    }

    public DateTime GetCalendarDay(DateTime calendarStart, int calendarWeek, int dayInWeek)
    {
        long ticks = calendarStart.Ticks + calendarWeek * _ticksInWeek + dayInWeek * _ticksInDay;

        if (!_dateByTicks.TryGetValue(ticks, out DateTime date))
        {
            date = new DateTime(ticks);
            _dateByTicks[ticks] = date;
        }

        return date;
    }
}
