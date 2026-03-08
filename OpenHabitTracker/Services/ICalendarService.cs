namespace OpenHabitTracker.Services;

public interface ICalendarService
{
    DateTime GetCalendarDay(DateTime calendarStart, int day);
}
