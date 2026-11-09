using OpenHabitTracker.Backup.GoogleKeep;
using System.IO.Compression;
using System.Text.Json;

namespace OpenHabitTracker.Backup;

// The content decides the format; the file extension only breaks a tie between text formats.
public static class ImportFormatDetector
{
    private static readonly byte[] _zipSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly byte[] _sqliteSignature = "SQLite format 3\0"u8.ToArray();

    private static readonly ImportFormat[] _textFormats = [ImportFormat.Json, ImportFormat.Tsv, ImportFormat.Yaml, ImportFormat.Markdown];

    public static ImportFormat Detect(string filename, byte[] content)
    {
        if (content.AsSpan().StartsWith(_zipSignature))
        {
            if (IsLoopCsvExport(content))
                return ImportFormat.LoopCsv;

            return IsGoogleKeepTakeout(content) ? ImportFormat.GoogleKeep : ImportFormat.Unknown;
        }

        if (content.AsSpan().StartsWith(_sqliteSignature))
        {
            return IsLoopDatabase(content) ? ImportFormat.LoopDatabase : ImportFormat.Unknown;
        }

        using StreamReader streamReader = new(new MemoryStream(content));

        string text = streamReader.ReadToEnd();

        if (IsHabitNowBackup(text))
            return ImportFormat.HabitNow;

        if (IsHabiticaUserData(text))
            return ImportFormat.Habitica;

        string[] lines = text.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();

        ImportFormat hint = GetExtensionHint(filename);

        if (hint != ImportFormat.Unknown && Matches(hint, text, lines))
        {
            return hint;
        }

        return _textFormats.FirstOrDefault(format => Matches(format, text, lines), ImportFormat.Unknown);
    }

    private static ImportFormat GetExtensionHint(string filename)
    {
        return Path.GetExtension(filename).ToLowerInvariant() switch
        {
            ".json" => ImportFormat.Json,
            ".tsv" => ImportFormat.Tsv,
            ".yaml" or ".yml" => ImportFormat.Yaml,
            ".md" => ImportFormat.Markdown,
            _ => ImportFormat.Unknown
        };
    }

    private static bool Matches(ImportFormat format, string text, string[] lines)
    {
        return format switch
        {
            ImportFormat.Json => IsJsonBackup(text),
            ImportFormat.Tsv => IsTsvBackup(lines),
            ImportFormat.Yaml => IsYamlBackup(lines),
            ImportFormat.Markdown => lines.Any(line => line.StartsWith("# ")),
            _ => false
        };
    }

    private static bool IsJsonBackup(string text)
    {
        if (!text.TrimStart().StartsWith('{'))
            return false;

        try
        {
            using JsonDocument document = JsonDocument.Parse(text);

            return document.RootElement.ValueKind == JsonValueKind.Object && (document.RootElement.TryGetProperty("Settings", out _) || document.RootElement.TryGetProperty("Categories", out _));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsTsvBackup(string[] lines)
    {
        string[] header = lines[0].Split('\t');

        return header.Contains("ContentType") && header.Contains("Category") && header.Contains("Title");
    }

    private static bool IsYamlBackup(string[] lines)
    {
        string? firstKey = lines.FirstOrDefault(line => line.Length > 0 && line != "---" && !line.StartsWith('#'));

        return firstKey is not null && (firstKey.StartsWith("Settings:") || firstKey.StartsWith("Categories:"));
    }

    // SQLite keeps the CREATE TABLE statement of every table as plain text in the file, so the table and
    // column names can be found without opening the database.
    private static bool IsLoopDatabase(byte[] content)
    {
        return content.AsSpan().IndexOf("CREATE TABLE Repetitions"u8) >= 0 && content.AsSpan().IndexOf("freq_num"u8) >= 0;
    }

    // C, the backup time as yyyyMMddHHmmss, 1, and the categories section.
    private static bool IsHabitNowBackup(string text)
    {
        return text.Length >= 18 && text[0] == 'C' && !text.AsSpan(1, 15).ContainsAnyExceptInRange('0', '9') && text.AsSpan(16, 2).SequenceEqual("{C");
    }

    // The user data has its tasks by type, directly or, from the API, wrapped in "data".
    private static bool IsHabiticaUserData(string text)
    {
        if (!text.TrimStart().StartsWith('{'))
            return false;

        try
        {
            using JsonDocument document = JsonDocument.Parse(text);

            JsonElement root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object)
                root = data;

            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("tasks", out JsonElement tasks)
                && tasks.ValueKind == JsonValueKind.Object
                && tasks.TryGetProperty("dailys", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsLoopCsvExport(byte[] content)
    {
        try
        {
            using ZipArchive archive = new(new MemoryStream(content));

            return archive.GetEntry("Habits.csv") is not null && archive.GetEntry("Checkmarks.csv") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool IsGoogleKeepTakeout(byte[] content)
    {
        try
        {
            using ZipArchive archive = new(new MemoryStream(content));

            foreach (ZipArchiveEntry entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                using StreamReader streamReader = new(entry.Open());

                if (GoogleKeepNote.IsNote(streamReader.ReadToEnd()))
                    return true;
            }

            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
