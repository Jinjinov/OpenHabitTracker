using System.IO.Compression;
using System.Text.Json;

namespace OpenHabitTracker.Backup;

// The content decides the format; the file extension only breaks a tie between text formats.
public static class ImportFormatDetector
{
    private static readonly byte[] _zipSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly ImportFormat[] _textFormats = [ImportFormat.Json, ImportFormat.Tsv, ImportFormat.Yaml, ImportFormat.Markdown];

    public static ImportFormat Detect(string filename, byte[] content)
    {
        if (content.AsSpan().StartsWith(_zipSignature))
        {
            return IsGoogleKeepTakeout(content) ? ImportFormat.GoogleKeep : ImportFormat.Unknown;
        }

        using StreamReader streamReader = new(new MemoryStream(content));

        string text = streamReader.ReadToEnd();
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

    private static bool IsGoogleKeepTakeout(byte[] content)
    {
        try
        {
            using ZipArchive archive = new(new MemoryStream(content));

            foreach (ZipArchiveEntry entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                using StreamReader streamReader = new(entry.Open());

                if (IsGoogleKeepNote(streamReader.ReadToEnd()))
                    return true;
            }

            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool IsGoogleKeepNote(string text)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);

            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("userEditedTimestampUsec", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
