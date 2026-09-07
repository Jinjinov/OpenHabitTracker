namespace OpenHabitTracker.App;

// Shared mechanics for generating example history.
// Kept apart from the example content itself so the capture harness in the e2e project and the
// debug examples cannot drift on the two things that are easy to get wrong: determinism and dates.
internal static class ExampleData
{
    // Deterministic jitter - a rerun reproduces the set, but no two completions are identical.
    // Nobody meditates for exactly twenty minutes every single day.
    // It has to be a hash and not arithmetic on the day, in both directions:
    // a bare multiply-and-mod repeats one value for every day whenever the modulus divides the
    // multiplier, and a single shift still leaves consecutive days in runs of the same value on
    // small moduli.
    internal static int Spread(double daysAgo, int salt, int modulus)
    {
        ulong hash = (ulong)((long)(daysAgo * 16) + salt * 7919L) * 0x9E3779B97F4A7C15UL;
        hash ^= hash >> 29;
        hash *= 0xBF58476D1CE4E5B9UL;
        hash ^= hash >> 32;
        return (int)(hash % (ulong)modulus);
    }

    // Whether a given day is one the habit was done on, at roughly the given rate.
    internal static bool Includes(int daysAgo, int salt, int percent) => Spread(daysAgo, salt, 100) < percent;
}
