using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenHabitTracker.Backup.GoogleKeep;

public class GoogleTaskLists
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<GoogleTaskList> Items { get; set; } = [];

    // Takeout names the Tasks folder in the language of the account, so Tasks.json is found by its kind.
    internal static bool IsTaskLists(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("kind", out JsonElement kind)
                && kind.ValueKind == JsonValueKind.String
                && kind.GetString() == "tasks#taskLists";
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public class GoogleTaskList
{
    [JsonPropertyName("items")]
    public List<GoogleTask> Items { get; set; } = [];

    [JsonPropertyName("recurrences")]
    public List<GoogleTaskRecurrence> Recurrences { get; set; } = [];
}

public class GoogleTask
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("scheduled_time")]
    public List<ScheduledTime> ScheduledTime { get; set; } = [];

    [JsonPropertyName("task_recurrence_id")]
    public string? TaskRecurrenceId { get; set; }
}

public class ScheduledTime
{
    [JsonPropertyName("current")]
    public bool Current { get; set; }

    [JsonPropertyName("start")]
    public DateTime Start { get; set; }
}

public class GoogleTaskRecurrence
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("schedule")]
    public Schedule Schedule { get; set; } = new();
}

public class Schedule
{
    [JsonPropertyName("interval")]
    public Interval Interval { get; set; } = new();
}

// Exactly one of daily, weekly, monthly and yearly is set.
public class Interval
{
    [JsonPropertyName("interval_multiplier")]
    public int IntervalMultiplier { get; set; } = 1;

    [JsonPropertyName("daily")]
    public JsonElement? Daily { get; set; }

    [JsonPropertyName("weekly")]
    public WeeklyInterval? Weekly { get; set; }

    [JsonPropertyName("monthly")]
    public JsonElement? Monthly { get; set; }

    [JsonPropertyName("yearly")]
    public JsonElement? Yearly { get; set; }
}

public class WeeklyInterval
{
    [JsonPropertyName("day_of_week")]
    public List<string> DayOfWeek { get; set; } = [];
}
