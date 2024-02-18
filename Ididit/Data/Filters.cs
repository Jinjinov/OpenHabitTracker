namespace Ididit.Data;

public class Filters
{
    public string? SearchTerm { get; set; }

    public bool MatchCase { get; set; }

    public DateTime? DoneAtFilter { get; set; }

    public DateTime? PlannedAtFilter { get; set; }
}
