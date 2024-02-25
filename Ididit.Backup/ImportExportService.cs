using Ididit.Backup.File;

namespace Ididit.Backup;

public class ImportExportService(JsonBackup jsonBackup, TsvBackup tsvBackup, YamlBackup yamlBackup, MarkdownBackup markdownBackup)
{
    private readonly JsonBackup _jsonBackup = jsonBackup;
    private readonly TsvBackup _tsvBackup = tsvBackup;
    private readonly YamlBackup _yamlBackup = yamlBackup;
    private readonly MarkdownBackup _markdownBackup = markdownBackup;

    public async Task<string> GetDataExportFileString(FileFormat fileFormat)
    {
        return fileFormat switch
        {
            FileFormat.Json => await _jsonBackup.GetDataExportFileString(),
            FileFormat.Tsv => await _tsvBackup.GetDataExportFileString(),
            FileFormat.Yaml => await _yamlBackup.GetDataExportFileString(),
            FileFormat.Md => await _markdownBackup.GetDataExportFileString(),
            _ => throw new ArgumentOutOfRangeException(nameof(fileFormat)),
        };
    }

    public async Task ImportDataFile(string filename, Stream stream)
    {
        if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            await _jsonBackup.ImportDataFile(stream);
        }
        else if (filename.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
        {
            await _tsvBackup.ImportDataFile(stream);
        }
        else if (filename.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
        {
            await _yamlBackup.ImportDataFile(stream);
        }
        else if (filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            await _markdownBackup.ImportDataFile(stream);
        }
    }
}
