#if DEBUG

using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.App;

// Debug builds load a year of habit history on top of the release examples.
// The release set is two days deep, which is enough for the lists and nothing else:
// the habit charts, the streaks and the weekly statistics all render empty against it.
// Debug only, because this is a developer tool, not a first run experience.
public partial class Examples
{
    private const int _historyDays = 365;

    private UserImportExportData GetDebugExamples(UserModel user, SettingsModel settings)
    {
        UserImportExportData userData = GetExamples(user, settings);

        DateTime now = DateTime.Now;

        string markdown =
            """
            Six habits, a year of completions each, generated deterministically - the same run gives the same set.

            - **Meditate** - near daily, for long streaks and a mostly complete calendar
            - **Read** - Time metric with a 30 minute duration, sessions on both sides of it
            - **Call parents** - every second day, the only one on the gap based streak path
            - **Run** - Quantity metric, three fixed weekdays, so Frequency has a shape
            - **Deep clean** - weekly, so its streaks are counted in weeks
            - **Back up photos** - monthly, sparse by day and dense by year
            """;

        userData.Categories.Add(new CategoryModel
        {
            UserId = user.Id,
            Title = "A year of history",
            Notes =
            [
                new()
                {
                    Title = "What this data is for",
                    Priority = Priority.Low,
                    Content = markdown,
                    ContentMarkdown = _markdownToHtml.GetMarkdown(markdown),
                    CreatedAt = now,
                    UpdatedAt = now,
                    Color = "bg-info-subtle"
                }
            ],
            Tasks =
            [
                new()
                {
                    Title = "Check every chart against the data",
                    Priority = Priority.Medium,
                    Items =
                    [
                        new() { Title = "Target - the marker sits where the pace is" },
                        new() { Title = "History - all five granularities" },
                        new() { Title = "Calendar - months line up, no breaks" },
                        new() { Title = "Best streaks - longest first" },
                        new() { Title = "Frequency - weekday bias is visible" }
                    ],
                    PlannedAt = now.Date.AddHours(18),
                    Duration = new TimeOnly(0, 30),
                    CreatedAt = now,
                    UpdatedAt = now,
                    Color = "bg-secondary-subtle"
                }
            ],
            Habits =
            [
                // Nearly every day, with the odd gap: long streaks and a mostly complete calendar.
                Habit("Meditate", salt: 11, percent: 92, color: "bg-primary-subtle",
                    repeatCount: 1, repeatInterval: 1, period: Period.Day,
                    metric: DisplayMetric.Repetitions, minMinutes: 10, spanMinutes: 15),

                // Three times a week, on the same three weekdays: the frequency panel has a shape.
                Habit("Run", salt: 23, percent: 85, color: "bg-secondary-subtle",
                    repeatCount: 3, repeatInterval: 1, period: Period.Week,
                    metric: DisplayMetric.Quantity, minMinutes: 25, spanMinutes: 35,
                    targetQuantity: 5, minQuantity: 3, spanQuantity: 7,
                    when: day => day.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday),

                // A time habit with a duration, and sessions on both sides of it,
                // so the calendar and the history chart show target met and target missed.
                Habit("Read", salt: 37, percent: 80, color: "bg-info-subtle",
                    repeatCount: 1, repeatInterval: 1, period: Period.Day,
                    metric: DisplayMetric.Time, minMinutes: 12, spanMinutes: 45,
                    duration: new TimeOnly(0, 30)),

                // Every second day: the only habit here that exercises the gap based streak
                // algorithm, since that path runs when RepeatInterval is greater than one.
                Habit("Call parents", salt: 53, percent: 48, color: "bg-primary-subtle",
                    repeatCount: 1, repeatInterval: 2, period: Period.Day,
                    metric: DisplayMetric.Repetitions, minMinutes: 8, spanMinutes: 30),

                // Weekly, so its streaks are counted in weeks rather than days.
                Habit("Deep clean", salt: 67, percent: 80, color: "bg-secondary-subtle",
                    repeatCount: 1, repeatInterval: 1, period: Period.Week,
                    metric: DisplayMetric.Time, minMinutes: 40, spanMinutes: 50,
                    duration: new TimeOnly(1, 0),
                    when: day => day.DayOfWeek == DayOfWeek.Saturday),

                // Monthly, so the history chart has a series that is sparse at day granularity
                // and dense at year granularity, and its streaks are counted in months.
                Habit("Back up photos", salt: 71, percent: 90, color: "bg-info-subtle",
                    repeatCount: 1, repeatInterval: 1, period: Period.Month,
                    metric: DisplayMetric.Repetitions, minMinutes: 15, spanMinutes: 20,
                    when: day => day.Day == 3)
            ]
        });

        return userData;
    }

    private static HabitModel Habit(string title, int salt, int percent, string color,
        int repeatCount, int repeatInterval, Period period, DisplayMetric metric,
        int minMinutes, int spanMinutes, TimeOnly? duration = null,
        long targetQuantity = 1, long minQuantity = 1, int spanQuantity = 1,
        Func<DateTime, bool>? when = null)
    {
        DateTime now = DateTime.Now;

        List<TimeModel> timesDone = History(salt, percent, minMinutes, spanMinutes, minQuantity, spanQuantity, when);

        return new HabitModel
        {
            Title = title,
            Priority = Priority.None,
            RepeatCount = repeatCount,
            RepeatInterval = repeatInterval,
            RepeatPeriod = period,
            DisplayMetric = metric,
            TargetQuantity = targetQuantity,
            Duration = duration,
            StartAt = now.Date.AddDays(-_historyDays),
            TimesDone = timesDone,
            LastTimeDoneAt = timesDone.LastOrDefault()?.CompletedAt,
            CreatedAt = now.Date.AddDays(-_historyDays),
            UpdatedAt = now,
            Color = color
        };
    }

    // Oldest first, every date relative to the run so the set never ages into a wall of overdue habits.
    private static List<TimeModel> History(int salt, int percent, int minMinutes, int spanMinutes,
        long minQuantity, int spanQuantity, Func<DateTime, bool>? when)
    {
        DateTime now = DateTime.Now;
        List<TimeModel> times = new();

        for (int daysAgo = _historyDays; daysAgo >= 0; daysAgo--)
        {
            DateTime day = now.Date.AddDays(-daysAgo);

            if (when is not null && !when(day))
                continue;

            if (!ExampleData.Includes(daysAgo, salt, percent))
                continue;

            DateTime startedAt = day.AddHours(7 + ExampleData.Spread(daysAgo, salt + 1, 12))
                                    .AddMinutes(ExampleData.Spread(daysAgo, salt + 2, 60));

            DateTime completedAt = startedAt.AddMinutes(minMinutes + ExampleData.Spread(daysAgo, salt + 3, spanMinutes));

            // A completion is never allowed into the future: the app refuses to record one, and a
            // habit whose last completion is ahead of the clock renders a negative elapsed time.
            if (completedAt > now)
                continue;

            times.Add(new TimeModel
            {
                StartedAt = startedAt,
                CompletedAt = completedAt,
                Quantity = minQuantity + ExampleData.Spread(daysAgo, salt + 4, spanQuantity)
            });
        }

        return times;
    }
}

#endif
