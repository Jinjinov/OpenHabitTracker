using OpenHabitTracker.Data;

namespace OpenHabitTracker.Dto;

public class QueryParameters
{
    public string? SearchTerm { get; set; }

    public bool MatchCase { get; set; }

    public DateTime? DoneAtFilter { get; set; }

    public DateCompare DoneAtCompare { get; set; } = DateCompare.On;

    public DateTime? PlannedAtFilter { get; set; }

    public DateCompare PlannedAtCompare { get; set; } = DateCompare.On;

    public bool HideCompletedTasks { get; set; }

    public bool ShowOnlyOverSelectedRatioMin { get; set; }

    public int SelectedRatioMin { get; set; } = 50;

    public Ratio SelectedRatio { get; set; }

    public FilterDisplay CategoryFilterDisplay { get; set; } = FilterDisplay.CheckBoxes;

    public FilterDisplay PriorityFilterDisplay { get; set; } = FilterDisplay.CheckBoxes;

    public long? SelectedCategoryId { get; set; }

    public Priority? SelectedPriority { get; set; }

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
        { ContentType.Task, Sort.PlannedAt },
        { ContentType.Habit, Sort.SelectedRatio }
    };
}
