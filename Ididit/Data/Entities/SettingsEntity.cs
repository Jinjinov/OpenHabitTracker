namespace Ididit.Data.Entities;

public class SettingsEntity
{
    public long Id { get; set; }

    public bool IsDarkMode { get; set; }

    public string StartPage { get; set; } = string.Empty;

    public string StartSidebar { get; set; } = string.Empty;

    public DayOfWeek FirstDayOfWeek { get; set; }

    public bool ShowItemList { get; set; }

    public bool ShowSmallCalendar { get; set; }

    public bool ShowLargeCalendar { get; set; }

    public bool InsertTabsInNoteContent { get; set; }

    public bool DisplayNoteContentAsMarkdown { get; set; }

    public bool ShowOnlyOverElapsedTimeToRepeatIntervalRatioMin { get; set; }

    public int ElapsedTimeToRepeatIntervalRatioMin { get; set; }

    public long SelectedCategoryId { get; set; }

    public Dictionary<ContentType, Sort> SortBy { get; set; } = new()
    {
        { ContentType.Note, Sort.Priority },
        { ContentType.Task, Sort.Priority },
        { ContentType.Habit, Sort.Priority }
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
