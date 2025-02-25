namespace OpenHabitTracker.Blazor.Components;

public class TourStep
{
    public required string Text { get; set; }
    public Func<bool> Show { get; set; } = () => true;
}
