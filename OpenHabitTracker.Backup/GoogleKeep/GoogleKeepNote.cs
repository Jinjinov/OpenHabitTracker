using System.Text.Json.Serialization;

namespace OpenHabitTracker.Backup.GoogleKeep;

public class Annotation
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class Label
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class ListContent
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("isChecked")]
    public bool IsChecked { get; set; }
}

public class GoogleKeepNote
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("isTrashed")]
    public bool IsTrashed { get; set; }

    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("annotations")]
    public List<Annotation> Annotations { get; set; } = [];

    [JsonPropertyName("listContent")]
    public List<ListContent> ListContent { get; set; } = [];

    [JsonPropertyName("textContent")]
    public string TextContent { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("userEditedTimestampUsec")]
    public long UserEditedTimestampUsec { get; set; }

    [JsonPropertyName("createdTimestampUsec")]
    public long CreatedTimestampUsec { get; set; }

    [JsonPropertyName("labels")]
    public List<Label> Labels { get; set; } = [];
}
