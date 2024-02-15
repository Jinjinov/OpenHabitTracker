namespace Ididit.Data.Models;

public class SettingsModel
{
    internal long Id { get; set; }

    public string StartPage { get; set; } = string.Empty;

    public string StartSidebar { get; set; } = string.Empty;

    public DayOfWeek FirstDayOfWeek { get; set; }

    public bool ShowItemList { get; set; }

    public bool ShowSmallCalendar { get; set; }

    public bool ShowLargeCalendar { get; set; }

    public bool ShowOnlyOverElapsedTimeToRepeatIntervalRatioMin { get; set; }

    public int ElapsedTimeToRepeatIntervalRatioMin { get; set; }

    public long SelectedCategoryId { get; set; }

    public Dictionary<InfoType, Sort> SortBy { get; set; } = new()
    {
        { InfoType.Note, Sort.Priority },
        { InfoType.Task, Sort.Priority },
        { InfoType.Habit, Sort.Priority }
    };

    public Dictionary<Priority, bool> ShowPriority { get; set; } = new()
    {
        { Priority.None, true },
        { Priority.VeryLow, true },
        { Priority.Low, true },
        { Priority.Medium, true },
        { Priority.High, true },
        { Priority.VeryHigh, true }
    };
}
