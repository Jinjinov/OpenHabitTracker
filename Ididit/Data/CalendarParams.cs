namespace Ididit.Data;

public class CalendarParams
{
    public DateTime CalendarStart { get; set; }

    public DateTime FirstDayOfMonth { get; set; }

    private static DateTime GetFirstDayOfMonth(DateTime day)
    {
        return new DateTime(day.Year, day.Month, 1);
    }

    private static DateTime GetFirstDayOfWeek(DayOfWeek firstDayOfWeek, DateTime day)
    {
        int diff = -((7 + (day.DayOfWeek - firstDayOfWeek)) % 7);
        return day.AddDays(diff).Date;
    }

    public void SetCalendarStartByDaysFromToday(int days)
    {
        FirstDayOfMonth = GetFirstDayOfMonth(DateTime.Today);
        CalendarStart = DateTime.Today.AddDays(days);
    }

    public void SetCalendarStartByFirstDayOfWeek(DayOfWeek firstDayOfWeek, DateTime day)
    {
        FirstDayOfMonth = GetFirstDayOfMonth(day);
        CalendarStart = GetFirstDayOfWeek(firstDayOfWeek, day);
    }

    public void SetCalendarStartByFirstDayOfMonth(DayOfWeek firstDayOfWeek, DateTime day)
    {
        FirstDayOfMonth = GetFirstDayOfMonth(day);
        CalendarStart = GetFirstDayOfWeek(firstDayOfWeek, FirstDayOfMonth);
    }

    public void SetCalendarStartToNextMonth(DayOfWeek firstDayOfWeek)
    {
        FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
        CalendarStart = GetFirstDayOfWeek(firstDayOfWeek, FirstDayOfMonth);
    }

    public void SetCalendarStartToPreviousMonth(DayOfWeek firstDayOfWeek)
    {
        FirstDayOfMonth = FirstDayOfMonth.AddMonths(-1);
        CalendarStart = GetFirstDayOfWeek(firstDayOfWeek, FirstDayOfMonth);
    }

    public void SetCalendarStartToNextWeek()
    {
        CalendarStart = CalendarStart.AddDays(7);
        FirstDayOfMonth = GetFirstDayOfMonth(CalendarStart);
        if (CalendarStart.Day > 14)
            FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
    }

    public void SetCalendarStartToPreviousWeek()
    {
        CalendarStart = CalendarStart.AddDays(-7);
        FirstDayOfMonth = GetFirstDayOfMonth(CalendarStart);
        if (CalendarStart.Day > 14)
            FirstDayOfMonth = FirstDayOfMonth.AddMonths(1);
    }
}
