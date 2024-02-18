namespace Ididit.Data;

public class Filters
{
    public string? SearchTerm { get; set; }

    public bool MatchCase { get; set; }

    public DateTime? DoneAtFilter { get; set; }

    public DateCompare DoneAtCompare { get; set; } = DateCompare.On;

    public DateTime? PlannedAtFilter { get; set; }

    public DateCompare PlannedAtCompare { get; set; } = DateCompare.On;
}
