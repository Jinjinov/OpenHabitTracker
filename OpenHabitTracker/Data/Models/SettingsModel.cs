namespace OpenHabitTracker.Data.Models;

public class SettingsModel
{
    internal long Id { get; set; }

    public long UserId { get; set; }

    public bool IsDarkMode { get; set; } = true;

    public string Theme { get; set; } = "default";

    public string StartPage { get; set; } = string.Empty;

    public string StartSidebar { get; set; } = string.Empty;

    public string Culture { get; set; } = "en";

    public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;

    public Ratio SelectedRatio { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;

    public bool ShowHelp { get; set; } = true;

    public bool UncheckAllItemsOnHabitDone { get; set; } = false;

    public bool ShowItemList { get; set; } = true;

    public bool ShowSmallCalendar { get; set; } = true;

    public bool ShowLargeCalendar { get; set; } = true;

    public bool ShowColor { get; set; } = false;

    public bool ShowCreatedUpdated { get; set; } = false;

    public bool InsertTabsInNoteContent { get; set; } = true;

    public bool DisplayNoteContentAsMarkdown { get; set; } = true;

    public bool ShowOnlyOverSelectedRatioMin { get; set; }

    public int SelectedRatioMin { get; set; } = 50;

    public int HorizontalMargin { get; set; } = 1;

    public int VerticalMargin { get; set; } = 1;

    public List<long> HiddenCategoryIds { get; set; } = [];

    public Dictionary<Priority, bool> ShowPriority { get; set; } = new()
    {
        { Priority.None, true },
        { Priority.VeryLow, true },
        { Priority.Low, true },
        { Priority.Medium, true },
        { Priority.High, true },
        { Priority.VeryHigh, true }
    };

    public Dictionary<ContentType, Sort> SortBy { get; set; } = new()
    {
        { ContentType.Note, Sort.Priority },
        { ContentType.Task, Sort.Priority },
        { ContentType.Habit, Sort.Priority }
    };
}
