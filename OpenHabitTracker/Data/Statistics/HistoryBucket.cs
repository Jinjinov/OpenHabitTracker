namespace OpenHabitTracker.Data.Statistics;

// One bar of the History chart.
public class HistoryBucket
{
    public required DateTime Start { get; init; }

    public required string Label { get; init; }

    public required double Value { get; init; }

    // Expected for this bucket alone, so a short first bucket and the still-running current one
    // are not measured against a full one. Null when the habit has no target for its metric.
    public required double? Expected { get; init; }
}
