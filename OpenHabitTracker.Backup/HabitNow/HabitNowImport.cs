using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.Globalization;

namespace OpenHabitTracker.Backup.HabitNow;

// Reads the .hn file that HabitNow writes with "Create backup file": one line of text, in sections that open
// with { and a letter, records separated by | and fields by ;.
// Descriptions, task notes and entry notes have no place in this app and are left out, and so are the colors,
// which belong to HabitNow's categories.
public class HabitNowImport(IStringLocalizer loc)
{
    // The built-in categories are stored as keys, which HabitNow shows in the user's language.
    private static readonly Dictionary<string, string> _builtInCategories = new()
    {
        { "00cn_quit", "Quit" },
        { "00cn_sports", "Sports" },
        { "00cn_nutrition", "Nutrition" },
        { "00cn_health", "Health" },
        { "00cn_study", "Study" },
        { "00cn_work", "Work" },
        { "00cn_home", "Household" },
        { "00cn_art", "Art" },
        { "00cn_entertainment", "Entertainment" },
        { "00cn_other", "Other" },
        { "00cn_task", "Task" },
        { "00cn_meditation", "Meditation" },
        { "00cn_social", "Social" },
        { "00cn_finance", "Finance" },
        { "00cn_outdoor", "Outdoor" },
    };

    private readonly IStringLocalizer _loc = loc;

    public async Task<ImportedData> ReadDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string text = (await streamReader.ReadToEndAsync()).Trim();

        Dictionary<char, string[]> sections = text.Split('{')
            .Skip(1)
            .Where(x => x.Length > 0)
            .GroupBy(x => x[0])
            .ToDictionary(g => g.Key, g => g.First()[1..].Split('|', StringSplitOptions.RemoveEmptyEntries));

        Dictionary<string, string> categoryNames = sections.GetValueOrDefault('C', [])
            .Select(x => x.Split(';'))
            .Where(x => x.Length >= 3)
            .ToDictionary(x => x[0], x => CategoryName(x[2]));

        List<string[]> activities = sections.GetValueOrDefault('H', []).Select(x => x.Split(';')).Where(x => x.Length >= 10).ToList();

        ILookup<string, string[]> entriesByActivityId = sections.GetValueOrDefault('X', []).Select(x => x.Split(';')).Where(x => x.Length >= 3).ToLookup(x => x[0]);

        DateTime now = DateTime.Now;

        CategoryModel uncategorized = new();
        CategoryModel? archived = null;
        Dictionary<string, CategoryModel> categoriesById = [];
        List<CategoryModel> categories = [uncategorized];

        CategoryModel CategoryOf(string categoryId)
        {
            if (!categoryNames.TryGetValue(categoryId, out string? name))
                return uncategorized;

            if (!categoriesById.TryGetValue(categoryId, out CategoryModel? category))
            {
                category = new() { Title = name };

                categoriesById[categoryId] = category;
                categories.Add(category);
            }

            return category;
        }

        // A frequency change ends a habit's track and starts a new one, and every track names the first, so the
        // tracks of one habit import as one habit with the rule of the track still running.
        foreach (IGrouping<string, string[]> tracks in activities.Where(x => x[5].StartsWith('0')).GroupBy(x => x[1].TrimStart('-')))
        {
            string[] current = tracks.FirstOrDefault(x => !x[1].StartsWith('-')) ?? tracks.OrderBy(x => ToDate(x[6], 0)).Last();

            HabitModel habit = ToHabit(current, tracks.Min(x => ToDate(x[6], 0)), now);

            habit.TimesDone = tracks
                .SelectMany(track => entriesByActivityId[track[0]])
                .Select(entry => ToCompletion(entry, habit.DisplayMetric))
                .OfType<TimeModel>()
                .OrderBy(x => x.StartedAt)
                .ToList();

            habit.LastTimeDoneAt = habit.TimesDone.LastOrDefault()?.CompletedAt;

            if (StateOf(current) == 'b')
            {
                if (archived is null)
                {
                    archived = new() { Title = _loc["Archived"] };

                    categories.Add(archived);
                }

                archived.Habits.Add(habit);
            }
            else
            {
                CategoryOf(current[4]).Habits.Add(habit);
            }
        }

        foreach (string[] activity in activities.Where(x => x[5].StartsWith('1')))
        {
            CategoryOf(activity[4]).Tasks.Add(ToTask(activity, now));
        }

        MergeOptions options = new()
        {
            MatchCompletionsByDay = true,
            HiddenCategories = archived is null ? [] : [archived]
        };

        return new ImportedData(categories, options);
    }

    private string CategoryName(string name)
    {
        return _builtInCategories.TryGetValue(name, out string? english) ? _loc[english] : name;
    }

    private static HabitModel ToHabit(string[] fields, DateTime? startAt, DateTime now)
    {
        char evaluation = fields[9].Length > 0 ? fields[9][0] : '0';
        char repeat = fields[9].Length > 1 ? fields[9][1] : '0';

        string unit = fields.Length > 11 ? fields[11].Trim() : string.Empty;
        double target = fields.Length > 13 && double.TryParse(fields[13], NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0;

        (int repeatCount, int repeatInterval, Period repeatPeriod) = Frequency(repeat, fields[10], fields.Length > 12 ? fields[12] : string.Empty);

        HabitModel habit = new()
        {
            Title = unit.Length > 0 && evaluation == '1' ? $"{fields[3].Trim()} ({unit})" : fields[3].Trim(),
            Priority = ToPriority(fields[5]),
            RepeatCount = repeatCount,
            RepeatInterval = repeatInterval,
            RepeatPeriod = repeatPeriod,
            StartAt = startAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (evaluation == '1')
        {
            habit.DisplayMetric = DisplayMetric.Quantity;
            habit.TargetQuantity = target > 0 ? target : 1;
        }
        else if (evaluation == '6')
        {
            // A timer's target is in seconds, and the time a habit should take is its duration.
            habit.DisplayMetric = DisplayMetric.Time;
            habit.Duration = target > 0 ? TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(Math.Min(target, TimeSpan.FromDays(1).TotalSeconds - 1))) : null;
        }

        return habit;
    }

    // Every day and every N days map exactly, weekdays and days per week become a count per week, and days
    // of the month and days per month a count per month, because a repeat rule cannot name days.
    private static (int RepeatCount, int RepeatInterval, Period RepeatPeriod) Frequency(char repeat, string parameter, string days)
    {
        int dayCount = Math.Max(1, days.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

        return repeat switch
        {
            '1' => (dayCount, 1, Period.Week),
            '2' when parameter.Length > 1 && int.TryParse(parameter.AsSpan(1), out int count) => (Math.Max(1, count), 1, parameter[0] == '1' ? Period.Month : Period.Week),
            '4' => (dayCount, 1, Period.Month),
            '7' when int.TryParse(parameter, out int interval) => (1, Math.Max(1, interval), Period.Day),
            _ => (1, 1, Period.Day)
        };
    }

    // Done entries are completions, and so are failed entries that carry a value, so 1.5 km of a 2.5 km target
    // shows as short of it. Pending and skipped entries, and failed ones without a value, are left out.
    private static TimeModel? ToCompletion(string[] entry, DisplayMetric displayMetric)
    {
        if (ToDate(entry[1], 0) is not DateTime date)
            return null;

        string status = entry[2].Length >= 2 ? entry[2][..2] : entry[2];

        double? value = entry.Length > 3 && double.TryParse(entry[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? parsed : null;

        bool isCompletion = status == "fd" || (status == "fc" && value is not null && displayMetric != DisplayMetric.Repetitions);

        if (!isCompletion)
            return null;

        DateTime midday = Midday(date);

        return displayMetric switch
        {
            DisplayMetric.Quantity => new TimeModel { StartedAt = midday, CompletedAt = midday, Quantity = value ?? 1 },
            DisplayMetric.Time => new TimeModel { StartedAt = midday, CompletedAt = midday.AddSeconds(value ?? 0) },
            _ => new TimeModel { StartedAt = midday, CompletedAt = midday }
        };
    }

    private static TaskModel ToTask(string[] fields, DateTime now)
    {
        TaskModel task = new()
        {
            Title = fields[3].Trim(),
            Priority = ToPriority(fields[5]),
            CreatedAt = now,
            UpdatedAt = now
        };

        // A task has its due date, and once completed its completion date after it.
        if (ToDate(fields[6], 0) is DateTime due)
            task.PlannedAt = due.Date + TaskModel.DateOnlySentinel;

        if (StateOf(fields) == 'd' && ToDate(fields[6], 4) is DateTime completed)
            task.CompletedAt = Midday(completed);

        return task;
    }

    // The kind and the state are one character each, then the priority in decimal, 1 by default.
    private static char StateOf(string[] fields)
    {
        return fields[5].Length > 1 ? fields[5][1] : 'a';
    }

    private static Priority ToPriority(string kindStatePriority)
    {
        int priority = kindStatePriority.Length > 2 && int.TryParse(kindStatePriority.AsSpan(2), out int parsed) ? parsed : 1;

        return priority switch
        {
            <= 0 => Priority.None,
            1 => Priority.VeryLow,
            2 => Priority.Low,
            3 => Priority.Medium,
            4 => Priority.High,
            _ => Priority.VeryHigh
        };
    }

    // A date is four characters: the decade as a letter from B for the 2020s, the year in it, then the month
    // from zero and the day, each one base-32 digit.
    private static DateTime? ToDate(string dates, int start)
    {
        if (dates.Length < start + 4)
            return null;

        int decade = dates[start] - 'A' + 1;
        int year = dates[start + 1] - '0';
        int month = Base32(dates[start + 2]) + 1;
        int day = Base32(dates[start + 3]);

        if (decade < 1 || year is < 0 or > 9 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(2000 + decade * 10 + year, month))
            return null;

        return new DateTime(2000 + decade * 10 + year, month, day);
    }

    private static int Base32(char digit)
    {
        return digit switch
        {
            >= '0' and <= '9' => digit - '0',
            >= 'a' and <= 'v' => digit - 'a' + 10,
            _ => -1
        };
    }

    // A completion that only has a day is recorded at midday, because midnight belongs ambiguously to two days.
    private static DateTime Midday(DateTime day)
    {
        return day.Date.AddHours(12);
    }
}
