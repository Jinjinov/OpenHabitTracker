namespace OpenHabitTracker.Services;

public class CalendarService
{
    private readonly Dictionary<long, DateTime> _dateByTicks = [];

    private const long _ticksInDay = 864_000_000_000;

    public static int GetDayOfWeek(DayOfWeek firstDayOfWeek, int dayIndex)
    {
        return (dayIndex + (int)firstDayOfWeek) % 7;
    }

    public DateTime GetCalendarDay(DateTime calendarStart, int day)
    {
        long ticks = calendarStart.Ticks + day * _ticksInDay;

        if (!_dateByTicks.TryGetValue(ticks, out DateTime date))
        {
            date = new DateTime(ticks);
            _dateByTicks[ticks] = date;
        }

        return date;
    }
}
