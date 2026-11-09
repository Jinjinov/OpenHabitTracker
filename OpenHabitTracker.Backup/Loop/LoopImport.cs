using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Backup.Loop;

// Reads the database that Loop Habit Tracker writes with "Export full backup", in every schema version from 12 on.
// Descriptions, questions, reminders and entry notes have no place in this app and are left out.
public class LoopImport(IStringLocalizer loc)
{
    // Entry values from Loop's Entry.kt. A skip is stored as 3 on a numerical habit too.
    private const long YesManual = 2;
    private const long Skip = 3;

    private readonly IStringLocalizer _loc = loc;

    public async Task<ImportedData> ReadDataFile(Stream stream)
    {
        // SQLite opens files, not streams.
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        try
        {
            await using (FileStream file = System.IO.File.Create(path))
            {
                await stream.CopyToAsync(file);
            }

            return Read(path);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    private ImportedData Read(string path)
    {
        // Without pooling the file is closed with the connection, so it can be deleted.
        string connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();

        using SqliteConnection connection = new(connectionString);

        connection.Open();

        // Old schemas, such as 12, have neither numerical habits nor entry values.
        bool hasNumericalHabits = Columns(connection, "Habits").Contains("type");
        bool hasEntryValues = Columns(connection, "Repetitions").Contains("value");

        DateTime now = DateTime.Now;

        CategoryModel uncategorized = new();
        CategoryModel? archived = null;

        Dictionary<long, (HabitModel Habit, bool IsNumerical)> habitsByLoopId = [];

        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = hasNumericalHabits
                ? "SELECT id, name, color, archived, freq_num, freq_den, type, target_value, unit FROM Habits ORDER BY position"
                : "SELECT id, name, color, archived, freq_num, freq_den FROM Habits ORDER BY position";

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
                bool isNumerical = hasNumericalHabits && !reader.IsDBNull(6) && reader.GetInt64(6) == 1;
                string unit = isNumerical && !reader.IsDBNull(8) ? reader.GetString(8).Trim() : string.Empty;

                (int repeatCount, int repeatInterval, Period repeatPeriod) = LoopFormat.Frequency(GetInt64(reader, 4, 1), GetInt64(reader, 5, 1));

                HabitModel habit = new()
                {
                    Title = LoopFormat.Title(name, unit),
                    Color = Color(GetInt64(reader, 2, 0)),
                    RepeatCount = repeatCount,
                    RepeatInterval = repeatInterval,
                    RepeatPeriod = repeatPeriod,
                    DisplayMetric = isNumerical ? DisplayMetric.Quantity : DisplayMetric.Repetitions,
                    TargetQuantity = isNumerical ? reader.GetDouble(7) : 1,
                    TimesDone = [],
                    CreatedAt = now,
                    UpdatedAt = now
                };

                if (GetInt64(reader, 3, 0) != 0)
                {
                    archived ??= new() { Title = _loc["Archived"] };
                    archived.Habits.Add(habit);
                }
                else
                {
                    uncategorized.Habits.Add(habit);
                }

                habitsByLoopId[reader.GetInt64(0)] = (habit, isNumerical);
            }
        }

        using (SqliteCommand command = connection.CreateCommand())
        {
            // Before entry values, every row was a day the habit was done.
            command.CommandText = hasEntryValues
                ? "SELECT habit, timestamp, value FROM Repetitions ORDER BY timestamp"
                : "SELECT habit, timestamp FROM Repetitions ORDER BY timestamp";

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (reader.IsDBNull(0) || !habitsByLoopId.TryGetValue(reader.GetInt64(0), out (HabitModel Habit, bool IsNumerical) entry))
                    continue;

                long value = hasEntryValues ? GetInt64(reader, 2, YesManual) : YesManual;

                double quantity;

                if (entry.IsNumerical)
                {
                    if (value == Skip || value <= 0)
                        continue;

                    quantity = value / LoopFormat.NumericalScale;
                }
                else
                {
                    if (value != YesManual)
                        continue;

                    quantity = 1;
                }

                // The day is stored as UTC midnight in milliseconds.
                DateTime day = LoopFormat.Midday(DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(1)).UtcDateTime);

                entry.Habit.TimesDone!.Add(new TimeModel { StartedAt = day, CompletedAt = day, Quantity = quantity });

                entry.Habit.LastTimeDoneAt = day;
            }
        }

        List<CategoryModel> categories = [uncategorized];

        if (archived is not null)
            categories.Add(archived);

        MergeOptions options = new()
        {
            MatchCompletionsByDay = true,
            HiddenCategories = archived is null ? [] : [archived]
        };

        return new ImportedData(categories, options);
    }

    private static HashSet<string> Columns(SqliteConnection connection, string table)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = $"SELECT name FROM pragma_table_info('{table}')";

        using SqliteDataReader reader = command.ExecuteReader();

        HashSet<string> columns = new(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
            columns.Add(reader.GetString(0));

        return columns;
    }

    private static long GetInt64(SqliteDataReader reader, int ordinal, long fallback)
    {
        return reader.IsDBNull(ordinal) ? fallback : reader.GetInt64(ordinal);
    }

    // Old schemas, such as 12, store the color itself, later ones the palette index.
    private static string Color(long stored)
    {
        return LoopFormat.IsPaletteIndex(stored) ? LoopFormat.ColorOfPaletteIndex((int)stored) : LoopFormat.ColorOfRgb((int)(stored & 0xFFFFFF));
    }
}
