using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.Text.Json;

namespace OpenHabitTracker.Backup.Habitica;

// Reads the user data that habitica.com/export/userdata.json downloads.
// To-dos become tasks, dailies become habits with their schedule, and habits, which are counters with no schedule,
// become daily habits with their up presses as completions. Rewards, notes and difficulty, which only sets rewards
// in the game, are left out, and the first tag becomes the category.
public class HabiticaImport
{
    private static readonly string[] _weekdays = ["m", "t", "w", "th", "f", "s", "su"];

    public async Task<ImportedData> ReadDataFile(Stream stream)
    {
        using JsonDocument document = await JsonDocument.ParseAsync(stream);

        // The API wraps the same user data in "data".
        JsonElement user = document.RootElement.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object ? data : document.RootElement;

        // Every date is the UTC instant of a local time; timezoneOffset is in minutes, -60 for UTC+1, and a day
        // starts at dayStart hours.
        TimeSpan offset = TimeSpan.Zero;
        TimeSpan dayStart = TimeSpan.Zero;

        if (user.TryGetProperty("preferences", out JsonElement preferences))
        {
            if (preferences.TryGetProperty("timezoneOffset", out JsonElement timezoneOffset) && timezoneOffset.TryGetInt32(out int minutes))
                offset = TimeSpan.FromMinutes(-minutes);

            if (preferences.TryGetProperty("dayStart", out JsonElement start) && start.TryGetInt32(out int hours))
                dayStart = TimeSpan.FromHours(hours);
        }

        Dictionary<string, string> tagNames = [];

        if (user.TryGetProperty("tags", out JsonElement tags) && tags.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement tag in tags.EnumerateArray())
            {
                if (String(tag, "id") is string id && String(tag, "name") is string name)
                    tagNames[id] = name;
            }
        }

        DateTime now = DateTime.Now;

        CategoryModel uncategorized = new();
        Dictionary<string, CategoryModel> categoriesByTag = [];
        List<CategoryModel> categories = [uncategorized];

        CategoryModel CategoryOf(JsonElement task)
        {
            if (!task.TryGetProperty("tags", out JsonElement taskTags) || taskTags.ValueKind != JsonValueKind.Array)
                return uncategorized;

            string? tagId = taskTags.EnumerateArray().Select(x => x.GetString()).FirstOrDefault(x => x is not null && tagNames.ContainsKey(x));

            if (tagId is null)
                return uncategorized;

            if (!categoriesByTag.TryGetValue(tagId, out CategoryModel? category))
            {
                category = new() { Title = tagNames[tagId] };

                categoriesByTag[tagId] = category;
                categories.Add(category);
            }

            return category;
        }

        DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc + offset, DateTimeKind.Unspecified);

        DateTime LocalDay(DateTime utc) => (Local(utc) - dayStart).Date;

        if (!user.TryGetProperty("tasks", out JsonElement tasks))
            throw new InvalidDataException("The file has no tasks");

        foreach (JsonElement todo in Array(tasks, "todos"))
        {
            TaskModel task = new()
            {
                Title = (String(todo, "text") ?? string.Empty).Trim(),
                Items = Checklist(todo, now),
                CreatedAt = Date(todo, "createdAt") is DateTime created ? Local(created) : now,
                UpdatedAt = Date(todo, "updatedAt") is DateTime updated ? Local(updated) : now
            };

            // A due date is a day.
            if (Date(todo, "date") is DateTime due)
                task.PlannedAt = Local(due).Date + TaskModel.DateOnlySentinel;

            if (todo.TryGetProperty("completed", out JsonElement completed) && completed.ValueKind == JsonValueKind.True)
                task.CompletedAt = Date(todo, "dateCompleted") is DateTime dateCompleted ? Local(dateCompleted) : null;

            CategoryOf(todo).Tasks.Add(task);
        }

        foreach (JsonElement daily in Array(tasks, "dailys"))
        {
            (int repeatCount, int repeatInterval, Period repeatPeriod) = DailyFrequency(daily);

            HabitModel habit = NewHabit(daily, now, Local);

            habit.RepeatCount = repeatCount;
            habit.RepeatInterval = repeatInterval;
            habit.RepeatPeriod = repeatPeriod;
            habit.StartAt = Date(daily, "startDate") is DateTime startDate ? Local(startDate).Date : null;
            habit.Items = Checklist(daily, null);
            habit.TimesDone = History(daily)
                .Where(entry => entry.TryGetProperty("completed", out JsonElement done) && done.ValueKind == JsonValueKind.True)
                .Select(entry => Date(entry, "date"))
                .OfType<DateTime>()
                .Select(date => Completion(LocalDay(date)))
                .ToList();

            Finish(habit);

            CategoryOf(daily).Habits.Add(habit);
        }

        foreach (JsonElement counter in Array(tasks, "habits"))
        {
            HabitModel habit = NewHabit(counter, now, Local);

            // Each up press of a day is a completion of that day.
            habit.TimesDone = History(counter)
                .Where(entry => Date(entry, "date") is not null && entry.TryGetProperty("scoredUp", out JsonElement up) && up.TryGetInt32(out _))
                .SelectMany(entry => Enumerable.Repeat(LocalDay(Date(entry, "date")!.Value), entry.GetProperty("scoredUp").GetInt32()))
                .Select(Completion)
                .ToList();

            Finish(habit);

            CategoryOf(counter).Habits.Add(habit);
        }

        MergeOptions options = new() { MatchCompletionsByDay = true };

        return new ImportedData(categories, options);
    }

    private static HabitModel NewHabit(JsonElement task, DateTime now, Func<DateTime, DateTime> local)
    {
        return new HabitModel
        {
            Title = (String(task, "text") ?? string.Empty).Trim(),
            CreatedAt = Date(task, "createdAt") is DateTime created ? local(created) : now,
            UpdatedAt = Date(task, "updatedAt") is DateTime updated ? local(updated) : now
        };
    }

    private static void Finish(HabitModel habit)
    {
        habit.TimesDone = habit.TimesDone!.OrderBy(x => x.StartedAt).ToList();

        habit.LastTimeDoneAt = habit.TimesDone.LastOrDefault()?.CompletedAt;
    }

    // Weekdays become a count per week and days of the month a count per month, because a repeat rule cannot name days.
    private static (int RepeatCount, int RepeatInterval, Period RepeatPeriod) DailyFrequency(JsonElement daily)
    {
        int everyX = daily.TryGetProperty("everyX", out JsonElement every) && every.TryGetInt32(out int x) ? Math.Max(1, x) : 1;

        int weekdays = daily.TryGetProperty("repeat", out JsonElement repeat) && repeat.ValueKind == JsonValueKind.Object
            ? _weekdays.Count(day => repeat.TryGetProperty(day, out JsonElement on) && on.ValueKind == JsonValueKind.True)
            : 7;

        int daysOfMonth = Array(daily, "daysOfMonth").Count();
        int weeksOfMonth = Array(daily, "weeksOfMonth").Count();

        return String(daily, "frequency") switch
        {
            "weekly" => (Math.Max(1, weekdays), everyX, Period.Week),
            "monthly" => (Math.Max(1, daysOfMonth > 0 ? daysOfMonth : weeksOfMonth * weekdays), everyX, Period.Month),
            "yearly" => (1, everyX, Period.Year),
            _ => (1, everyX, Period.Day)
        };
    }

    private static List<ItemModel> Checklist(JsonElement task, DateTime? doneAt)
    {
        return Array(task, "checklist")
            .Select(item => new ItemModel
            {
                Title = (String(item, "text") ?? string.Empty).Trim(),
                DoneAt = doneAt is not null && item.TryGetProperty("completed", out JsonElement completed) && completed.ValueKind == JsonValueKind.True ? doneAt : null
            })
            .ToList();
    }

    // A completion that only has a day is recorded at midday, because midnight belongs ambiguously to two days.
    private static TimeModel Completion(DateTime day)
    {
        DateTime midday = day.Date.AddHours(12);

        return new TimeModel { StartedAt = midday, CompletedAt = midday };
    }

    private static IEnumerable<JsonElement> History(JsonElement task) => Array(task, "history");

    private static IEnumerable<JsonElement> Array(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement array) && array.ValueKind == JsonValueKind.Array ? array.EnumerateArray() : [];
    }

    private static string? String(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    // Dates are ISO strings, and history dates are milliseconds.
    private static DateTime? Date(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
            return null;

        if (value.ValueKind == JsonValueKind.String && value.TryGetDateTimeOffset(out DateTimeOffset date))
            return date.UtcDateTime;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long milliseconds))
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;

        return null;
    }
}
