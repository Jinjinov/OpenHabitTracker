using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.Globalization;
using System.Text.Json;

namespace OpenHabitTracker.Backup.Habitify;

// Reads the SQLite backup that Habitify writes, where each habit is its Firebase JSON in habits.root_data, its goal
// is in habit_goals and its logs are in habit_logs. Notes, skips and fails, reminders and areas, which the backup
// does not carry, are left out.
public class HabitifyImport(IStringLocalizer loc)
{
    // A log is in the base unit of its kind, 5 km as 5000 m, while the goal keeps the unit it was set in, so both
    // convert through the size of each unit in its base unit. Habitify stores some symbols in a casing nobody
    // writes, so each unit also has the way it is written. A unit missing here is left as it is.
    private static readonly Dictionary<string, (double Size, string Display)> _units = new()
    {
        { "m", (1, "m") }, { "kM", (1000, "km") }, { "mi", (1609.344, "mi") }, { "ft", (0.3048, "ft") },
        { "g", (1, "g") }, { "kg", (1000, "kg") }, { "lb", (453.59237, "lb") },
        { "mL", (1, "mL") }, { "L", (1000, "L") },
        { "sec", (1, "sec") }, { "min", (60, "min") }, { "hr", (3600, "hr") },
    };

    // A goal of one repetition a day is a habit that is simply done.
    private const string Repetitions = "rep";

    private readonly IStringLocalizer _loc = loc;

    public async Task<ImportedData> ReadDataFile(Stream stream)
    {
        using TemporaryDatabase database = await TemporaryDatabase.Open(stream);

        SqliteConnection connection = database.Connection;

        Dictionary<string, (double Value, string Unit)> goals = ReadGoals(connection);
        ILookup<string, (DateTime Day, double Value, string Unit)> logs = ReadLogs(connection);

        DateTime now = DateTime.Now;

        CategoryModel uncategorized = new();
        CategoryModel? archived = null;

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "SELECT id, root_data, start_date FROM habits ORDER BY created_at";

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            string id = reader.GetString(0);

            using JsonDocument document = JsonDocument.Parse(reader.GetString(1));

            JsonElement root = document.RootElement;

            (double goalValue, string goalUnit) = goals.GetValueOrDefault(id, (1, Repetitions));

            bool isQuantity = goalUnit != Repetitions || goalValue > 1;

            (int repeatCount, int repeatInterval, Period repeatPeriod) = Frequency(String(root, "regularly"));

            string name = (String(root, "name") ?? string.Empty).Trim();

            HabitModel habit = new()
            {
                Title = isQuantity && goalUnit != Repetitions ? $"{name} ({(_units.TryGetValue(goalUnit, out (double Size, string Display) unit) ? unit.Display : goalUnit)})" : name,
                RepeatCount = repeatCount,
                RepeatInterval = repeatInterval,
                RepeatPeriod = repeatPeriod,
                DisplayMetric = isQuantity ? DisplayMetric.Quantity : DisplayMetric.Repetitions,
                TargetQuantity = isQuantity ? goalValue : 1,
                StartAt = !reader.IsDBNull(2) && DateTime.TryParseExact(reader.GetString(2), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate) ? startDate : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            if (NearestColor.TryParseHex(String(root, "accentColor"), out string color))
                habit.Color = color;

            habit.TimesDone = logs[id]
                .Where(log => log.Value > 0)
                .Select(log => Completion(log.Day, isQuantity ? Convert(log.Value, log.Unit, goalUnit) : 1))
                .OrderBy(x => x.StartedAt)
                .ToList();

            habit.LastTimeDoneAt = habit.TimesDone.LastOrDefault()?.CompletedAt;

            if (root.TryGetProperty("isArchived", out JsonElement isArchived) && isArchived.ValueKind == JsonValueKind.True)
            {
                archived ??= new() { Title = _loc["Archived"] };
                archived.Habits.Add(habit);
            }
            else
            {
                uncategorized.Habits.Add(habit);
            }
        }

        List<CategoryModel> categories = archived is null ? [uncategorized] : [uncategorized, archived];

        MergeOptions options = new()
        {
            MatchCompletionsByDay = true,
            HiddenCategories = archived is null ? [] : [archived]
        };

        return new ImportedData(categories, options);
    }

    // A goal can change, and the newest one is the habit's goal now.
    private static Dictionary<string, (double Value, string Unit)> ReadGoals(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "SELECT habit_id, value, unit_symbol FROM habit_goals ORDER BY created_at";

        using SqliteDataReader reader = command.ExecuteReader();

        Dictionary<string, (double Value, string Unit)> goals = [];

        while (reader.Read())
            goals[reader.GetString(0)] = (reader.GetDouble(1), reader.IsDBNull(2) ? Repetitions : reader.GetString(2));

        return goals;
    }

    // start_day is the local day of the log.
    private static ILookup<string, (DateTime Day, double Value, string Unit)> ReadLogs(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "SELECT habit_id, start_day, value, unit_symbol FROM habit_logs WHERE start_day IS NOT NULL";

        using SqliteDataReader reader = command.ExecuteReader();

        List<(string HabitId, DateTime Day, double Value, string Unit)> logs = [];

        while (reader.Read())
        {
            if (!DateTime.TryParseExact(reader.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day))
                continue;

            logs.Add((reader.GetString(0), day, reader.IsDBNull(2) ? 1 : reader.GetDouble(2), reader.IsDBNull(3) ? Repetitions : reader.GetString(3)));
        }

        return logs.ToLookup(x => x.HabitId, x => (x.Day, x.Value, x.Unit));
    }

    private static double Convert(double value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit || !_units.TryGetValue(fromUnit, out (double Size, string Display) from) || !_units.TryGetValue(toUnit, out (double Size, string Display) to))
            return value;

        return value * from.Size / to.Size;
    }

    // Weekdays become a count per week, because a repeat rule cannot name days.
    private static (int RepeatCount, int RepeatInterval, Period RepeatPeriod) Frequency(string? regularly)
    {
        if (regularly is not null && regularly.StartsWith("weekDays-"))
            return (Math.Max(1, regularly["weekDays-".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries).Length), 1, Period.Week);

        if (regularly is not null && regularly.StartsWith("dayInterval-") && int.TryParse(regularly.AsSpan("dayInterval-".Length), out int days))
            return (1, Math.Max(1, days), Period.Day);

        return (1, 1, Period.Day);
    }

    // A completion that only has a day is recorded at midday, because midnight belongs ambiguously to two days.
    private static TimeModel Completion(DateTime day, double quantity)
    {
        DateTime midday = day.Date.AddHours(12);

        return new TimeModel { StartedAt = midday, CompletedAt = midday, Quantity = quantity };
    }

    private static string? String(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
