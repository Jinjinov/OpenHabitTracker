using CsvHelper;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;
using System.Text.Json;
using YamlDotNet.Core;

namespace OpenHabitTracker.Backup;

public class ImportExportService(JsonImportExport jsonImportExport, TsvImportExport tsvImportExport, YamlImportExport yamlImportExport, MarkdownImportExport markdownImportExport, GoogleKeepImport googleKeepImport)
{
    private readonly JsonImportExport _jsonImportExport = jsonImportExport;
    private readonly TsvImportExport _tsvImportExport = tsvImportExport;
    private readonly YamlImportExport _yamlImportExport = yamlImportExport;
    private readonly MarkdownImportExport _markdownImportExport = markdownImportExport;
    private readonly GoogleKeepImport _googleKeepImport = googleKeepImport;

    public async Task<string> GetDataExportFileString(FileFormat fileFormat)
    {
        return fileFormat switch
        {
            FileFormat.Json => await _jsonImportExport.GetDataExportFileString(),
            FileFormat.Tsv => await _tsvImportExport.GetDataExportFileString(),
            FileFormat.Yaml => await _yamlImportExport.GetDataExportFileString(),
            FileFormat.Md => await _markdownImportExport.GetDataExportFileString(),
            _ => throw new ArgumentOutOfRangeException(nameof(fileFormat)),
        };
    }

    public async Task<ImportResult> ImportDataFile(string filename, Stream stream)
    {
        using MemoryStream memoryStream = new();

        await stream.CopyToAsync(memoryStream);

        ImportFormat importFormat = ImportFormatDetector.Detect(filename, memoryStream.ToArray());

        memoryStream.Position = 0;

        // Only the parse errors of the file formats are caught, so a failure to save still surfaces.
        try
        {
            switch (importFormat)
            {
                case ImportFormat.Json:
                    await _jsonImportExport.ImportDataFile(memoryStream);
                    break;
                case ImportFormat.Tsv:
                    await _tsvImportExport.ImportDataFile(memoryStream);
                    break;
                case ImportFormat.Yaml:
                    await _yamlImportExport.ImportDataFile(memoryStream);
                    break;
                case ImportFormat.Markdown:
                    await _markdownImportExport.ImportDataFile(memoryStream);
                    break;
                case ImportFormat.GoogleKeep:
                    await _googleKeepImport.ImportDataFile(memoryStream);
                    break;
                default:
                    return ImportResult.UnsupportedFormat;
            }
        }
        catch (Exception exception) when (exception is JsonException or YamlException or CsvHelperException or InvalidDataException)
        {
            return ImportResult.UnreadableContent;
        }

        return ImportResult.Imported;
    }
}
