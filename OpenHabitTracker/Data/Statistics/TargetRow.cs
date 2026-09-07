namespace OpenHabitTracker.Data.Statistics;

// One row of the Target panel: what was done in this window against what the whole window asks for.
public class TargetRow
{
    public required StatisticsPeriod Period { get; init; }

    public required double Actual { get; init; }

    // Null when the habit has no target for its metric, which is a Time habit with no Duration.
    public required double? Target { get; init; }

    // How much of the window has elapsed, 0 to 1. The pace marker sits here.
    public required double Pace { get; init; }

    public double Fraction => Target is > 0 ? Actual / Target.Value : 0;
}
