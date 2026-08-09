using System.Text.Json;

namespace OpenHabitTracker.EndToEndTests.Screenshot;

// Seed data for the store captures - imported through the Data sidebar, same path as a JSON backup.
// Every date is relative to the run, so the set never ages into a wall of overdue habits.
// The shape matches a real export: UserImportExportData { Settings, Categories[] { Notes, Tasks, Habits } }.
// Settings is partial on purpose - properties left out keep SettingsModel's own defaults.

public static class SeedData
{
    private static readonly DateTime Now = DateTime.Now;

    private static string Date(double daysAgo, int hour = 9, int minute = 0) =>
        Now.Date.AddDays(-daysAgo).AddHours(hour).AddMinutes(minute).ToString("yyyy-MM-ddTHH:mm:sszzz");

    private static object Item(string title, double? doneDaysAgo = null) =>
        new { Title = title, DoneAt = doneDaysAgo is null ? null : Date(doneDaysAgo.Value) };

    // Deterministic jitter - a rerun reproduces the set, but no two completions are identical.
    // Nobody drinks exactly eight glasses or practises for exactly thirty minutes every single day.
    // It has to be a hash and not arithmetic on the day, in both directions:
    // a bare multiply-and-mod repeats one value for every day whenever the modulus divides the
    // multiplier (which is how a 45 minute habit came out at exactly 53 minutes in every session),
    // and a single shift still leaves consecutive days in runs of the same value on small moduli.
    private static int Spread(double daysAgo, int salt, int modulus)
    {
        ulong hash = (ulong)((long)(daysAgo * 16) + salt * 7919L) * 0x9E3779B97F4A7C15UL;
        hash ^= hash >> 29;
        hash *= 0xBF58476D1CE4E5B9UL;
        hash ^= hash >> 32;
        return (int)(hash % (ulong)modulus);
    }

    private static object[] Times(int targetMinutes, int targetQuantity, double[] daysAgo) =>
        daysAgo.Select(day =>
        {
            DateTime start = Now.Date.AddDays(-day)
                .AddHours(6 + Spread(day, 3, 4))
                .AddMinutes(5 * Spread(day, 7, 12));

            int minutes = Math.Max(5, targetMinutes - targetMinutes / 3 + Spread(day, 5, targetMinutes * 2 / 3 + 1));
            int quantity = targetQuantity == 1 ? 1 : Math.Max(1, targetQuantity - 3 + Spread(day, 11, 5));

            return (object)new
            {
                StartedAt = start.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                CompletedAt = start.AddMinutes(minutes).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Quantity = quantity
            };
        }).ToArray();

    private static object Note(string title, string content, string color, int priority, double createdDaysAgo) =>
        new
        {
            Content = content,
            Title = title,
            Color = color,
            Priority = priority,
            IsDeleted = false,
            CreatedAt = Date(createdDaysAgo),
            UpdatedAt = Date(createdDaysAgo)
        };

    // The session is generated from the task's own Duration, the same way a habit's history is
    // generated from targetMinutes, so a 30 minute task can never show a two hour session.
    private static object Task(string title, string color, int priority, double plannedInDays, string duration,
        object[] items, double? completedDaysAgo = null)
    {
        int targetMinutes = (int)TimeSpan.Parse(duration).TotalMinutes;
        double day = completedDaysAgo ?? 0;

        DateTime start = Now.Date.AddDays(-day).AddHours(6 + Spread(day, 13, 4)).AddMinutes(5 * Spread(day, 19, 12));
        int minutes = Math.Max(5, targetMinutes - targetMinutes / 3 + Spread(day, 23, targetMinutes * 2 / 3 + 1));

        return new
        {
            PlannedAt = Date(-plannedInDays, 8 + Spread(plannedInDays, 29, 9), 5 * Spread(plannedInDays, 31, 12)),
            StartedAt = completedDaysAgo is null ? null : start.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            CompletedAt = completedDaysAgo is null ? null : start.AddMinutes(minutes).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            Items = items,
            Duration = duration,
            Title = title,
            Color = color,
            Priority = priority,
            IsDeleted = false,
            CreatedAt = Date(20),
            UpdatedAt = Date(1)
        };
    }

    // Duration and the completion history are generated together from targetMinutes,
    // so the day cells can never show a time the habit does not claim to take.
    private static object Habit(string title, string color, int priority, int repeatCount, int repeatInterval,
        int repeatPeriod, int displayMetric, int targetQuantity, int targetMinutes, double[] daysAgo, object[] items)
    {
        object[] times = Times(targetMinutes, targetQuantity, daysAgo);

        return new
        {
            RepeatCount = repeatCount,
            RepeatInterval = repeatInterval,
            RepeatPeriod = repeatPeriod,
            DisplayMetric = displayMetric,
            TargetQuantity = targetQuantity,
            LastTimeDoneAt = ((dynamic)times[0]).CompletedAt,
            StartAt = Date(56),
            TimesDone = times,
            Items = items,
            Duration = TimeSpan.FromMinutes(targetMinutes).ToString(@"hh\:mm\:ss"),
            Title = title,
            Color = color,
            Priority = priority,
            IsDeleted = false,
            CreatedAt = Date(56),
            UpdatedAt = Date(0)
        };
    }

    private static object Category(string title, object[] notes, object[] tasks, object[] habits) =>
        new { UserId = 0, Title = title, IsCollapsed = false, CompletionRule = 0, Notes = notes, Tasks = tasks, Habits = habits };

    // QuerySection: Search, FilterByDate, FilterByCategory, FilterByPriority, FilterByStatus, Sort.
    // true means folded. Open sections read as depth on a wide frame and as clutter on a phone,
    // where they push the priority filter off the bottom.
    private static readonly Dictionary<int, bool> AllOpen = new()
    {
        [0] = false, [1] = false, [2] = false, [3] = false, [4] = false, [5] = false
    };

    private static readonly Dictionary<int, bool> PhoneFolded = new()
    {
        [0] = false, [1] = true, [2] = true, [3] = false, [4] = true, [5] = true
    };

    public static string Write(string path, bool foldForPhone = false)
    {
        object data = new
        {
            Settings = new
            {
                ShowColor = false,          // a text dropdown - frame is better spent on the item
                HideCompletedTasks = false, // finished work stays visible
                FoldSection = foldForPhone ? PhoneFolded : AllOpen
            },
            // Checklist items are deliberately rare - two habits and two tasks carry one,
            // 3 to 5 entries each. Items expand under their parent and swallow the list otherwise.
            Categories = new object[]
            {
                Category("Health",
                    [
                        Note("Morning routine", "Before the phone, in this order:\n\n1. a glass of water\n2. five minutes of stretching\n3. out the door\n\n> The order matters more than the length. Skipping step three still counts as a win if one and two happened.", "bg-success-subtle", 3, 12)
                    ],
                    [
                        Task("Book the physiotherapy appointment", "bg-success-subtle", 4, 1, "00:15:00", []),
                        Task("Plan next week's meals", "bg-success-subtle", 2, 3, "00:45:00",
                            [Item("Check what is already in the freezer", 1), Item("Pick three dinners"), Item("Write the shopping list"), Item("Order the delivery slot")]),
                        Task("Replace the running shoes", "bg-success-subtle", 1, 9, "01:00:00", [], 2)
                    ],
                    [
                        Habit("Morning run", "bg-primary-subtle", 4, 3, 1, 1, 0, 1, 35,
                            [1, 3, 5, 8, 10, 12, 15, 17, 19, 22, 24, 26],
                            [Item("Warm up"), Item("Easy five kilometres"), Item("Cool down")]),
                        Habit("Drink water", "bg-primary-subtle", 3, 1, 1, 0, 2, 8, 5,
                            [0, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12], []),
                        Habit("Strength training", "bg-primary-subtle", 3, 2, 1, 1, 1, 1, 45,
                            [2, 5, 9, 12, 16, 19, 23, 26], []),
                        Habit("Lights out before midnight", "bg-primary-subtle", 2, 1, 1, 0, 0, 1, 5,
                            [0, 1, 2, 4, 5, 6, 8, 9, 11, 12, 13], [])
                    ]),
                Category("Work",
                    [
                        Note("Standup", "**Yesterday** - finished the import, opened the pull request\n\n**Today** - review comments, start the migration notes\n\n**Blocked** - still waiting on staging credentials", "bg-info-subtle", 3, 1),
                        Note("Release checklist", "- [x] bump the version\n- [x] tag the commit\n- [ ] upload the build\n- [ ] write the changelog\n- [ ] announce it", "bg-info-subtle", 4, 5),
                        Note("Retro - what to raise", "Went well\n: the release cadence held for six weeks\n\nDid not\n: three hotfixes in one week, all from the same file\n\n`git log --stat` on that file before the next planning session.", "bg-info-subtle", 2, 9)
                    ],
                    [
                        Task("Prepare the quarterly report", "bg-info-subtle", 5, 2, "02:00:00",
                            [Item("Collect the numbers", 1), Item("Draft the summary"), Item("Review with the team"), Item("Send it before Friday")]),
                        Task("Refactor the sync service", "bg-info-subtle", 3, 6, "03:00:00", []),
                        Task("Reply to the support mail", "bg-info-subtle", 2, 0, "00:30:00", [], 0),
                        Task("Renew the domain", "bg-info-subtle", 4, 5, "00:20:00", [])
                    ],
                    [
                        Habit("Inbox to zero", "bg-info-subtle", 3, 1, 1, 0, 1, 1, 20,
                            [0, 1, 2, 3, 6, 7, 8, 9, 10, 13, 14], []),
                        Habit("Code review", "bg-info-subtle", 4, 1, 1, 0, 0, 1, 45,
                            [0, 1, 2, 3, 4, 7, 8, 9, 11, 14, 16], []),
                        Habit("Deep work block", "bg-info-subtle", 4, 1, 1, 0, 1, 1, 90,
                            [0, 1, 3, 4, 7, 8, 10, 11, 14, 15], []),
                        Habit("Weekly review", "bg-info-subtle", 2, 1, 1, 1, 1, 1, 30,
                            [2, 9, 16, 23, 30, 37],
                            [Item("Close what is finished"), Item("Empty the inbox"), Item("Plan next week")])
                    ]),
                Category("Learning",
                    [
                        Note("Atomic Habits - what stuck", "Systems beat goals. The goal is the direction, the system is what happens on a Tuesday.\n\n| cue | habit |\n| --- | --- |\n| coffee brewing | twenty pages |\n| alarm goes off | shoes on |\n| laptop closes | journal |\n\nMake it *obvious*, *attractive*, *easy*, *satisfying* - in that order when something stops working.", "bg-primary-subtle", 3, 8),
                        Note("Spanish - phrases worth keeping", "- *de vez en cuando* - from time to time\n- *a lo mejor* - maybe, but hopeful\n- *ya que* - since, given that\n- *me da igual* - I do not mind either way", "bg-primary-subtle", 1, 15)
                    ],
                    [
                        Task("Finish the algorithms course", "bg-primary-subtle", 3, 14, "10:00:00", []),
                        Task("Write up the migration notes", "bg-primary-subtle", 2, 8, "01:30:00", [])
                    ],
                    [
                        Habit("Read 20 pages", "bg-secondary-subtle", 3, 1, 1, 0, 1, 1, 30,
                            [0, 1, 2, 3, 5, 6, 7, 10, 12, 13, 15, 18], []),
                        Habit("Practice Spanish", "bg-secondary-subtle", 2, 1, 1, 0, 1, 1, 20,
                            [1, 2, 4, 5, 8, 9, 12, 15], []),
                        Habit("Piano", "bg-secondary-subtle", 2, 3, 1, 1, 1, 1, 40,
                            [2, 4, 6, 9, 11, 13, 16, 18], [])
                    ]),
                Category("Home",
                    [
                        Note("Packing list", "Carry on only.\n\n- passport, wallet, keys\n- charger and the short cable\n- running shoes\n- one book, not three", "bg-warning-subtle", 2, 20)
                    ],
                    [
                        Task("Fix the dripping kitchen tap", "bg-warning-subtle", 4, 4, "01:30:00", []),
                        Task("Clear out the storage room", "bg-warning-subtle", 1, 21, "04:00:00", [])
                    ],
                    [
                        Habit("Water the plants", "bg-secondary-subtle", 2, 1, 1, 1, 0, 1, 10,
                            [3, 10, 17, 24, 31], []),
                        Habit("Clear the desk", "bg-secondary-subtle", 1, 1, 1, 0, 0, 1, 5,
                            [0, 2, 3, 6, 8, 9, 12, 14], [])
                    ]),
                Category("Personal",
                    [
                        Note("Gift ideas", "- climbing shoes, half a size up\n- the hand grinder, not the electric one\n- tickets for the November concert", "bg-secondary-subtle", 1, 30)
                    ],
                    [
                        Task("Renew the passport", "bg-secondary-subtle", 5, 10, "02:00:00", []),
                        Task("Book the October flights", "bg-secondary-subtle", 3, 12, "00:45:00", [], 4)
                    ],
                    [
                        Habit("Call family", "bg-secondary-subtle", 3, 1, 1, 1, 0, 1, 25,
                            [4, 11, 18, 25], []),
                        Habit("Journal", "bg-secondary-subtle", 2, 1, 1, 0, 1, 1, 15,
                            [0, 1, 3, 4, 5, 8, 10, 11, 14, 16, 17], [])
                    ])
            }
        };

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return path;
    }
}
