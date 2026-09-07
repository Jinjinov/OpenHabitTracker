namespace OpenHabitTracker.Data.Statistics;

// One row of the Target panel: what was done in this window against what the whole window asks for.
public class TargetRow
{
    public required StatisticsPeriod Period { get; init; }

    public required double Actual { get; init; }

    // Null when the habit has no target for its metric, which is a Time habit with no Duration.
    public required double? Target { get; init; }

    // How much of the window should be done by now, 0 to 1, counted in whole repeat periods
    // rather than in elapsed time. A quarter asks a monthly habit for 1 during July, 2 during
    // August and 3 during September, so being exactly on the mark is never a surplus.
    public required double Pace { get; init; }

    // How many of the habit repeat periods this window spans.
    // Pace only carries information above 2: a window that is one period long is done or not done,
    // and marking a fraction of it says nothing that can be acted on.
    public required double Periods { get; init; }

    public double Fraction => Target is > 0 ? Actual / Target.Value : 0;
}
