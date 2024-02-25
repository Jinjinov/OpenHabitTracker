using Ididit.Backup.File;
using Ididit.Backup.GoogleKeep;

namespace Ididit.Backup;

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

    public async Task ImportDataFile(string filename, Stream stream)
    {
        if (filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            await _jsonImportExport.ImportDataFile(stream);
        }
        else if (filename.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
        {
            await _tsvImportExport.ImportDataFile(stream);
        }
        else if (filename.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
        {
            await _yamlImportExport.ImportDataFile(stream);
        }
        else if (filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            await _markdownImportExport.ImportDataFile(stream);
        }
        else if (filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            await _googleKeepImport.ImportDataFile(stream);
        }
    }
}
