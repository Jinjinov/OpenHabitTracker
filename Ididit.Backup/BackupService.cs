using Ididit.Backup.Drive;

namespace Ididit.Backup;

public class BackupService(JsonBackup jsonBackup, TsvBackup tsvBackup, YamlBackup yamlBackup)
{
    private readonly JsonBackup _jsonBackup = jsonBackup;
    private readonly TsvBackup _tsvBackup = tsvBackup;
    private readonly YamlBackup _yamlBackup = yamlBackup;

    public async Task<string> GetDataExportFileString(DataFormat dataFormat)
    {
        return dataFormat switch
        {
            DataFormat.Json => await _jsonBackup.GetDataExportFileString(),
            DataFormat.Tsv => await _tsvBackup.GetDataExportFileString(),
            DataFormat.Yaml => await _yamlBackup.GetDataExportFileString(),
            _ => throw new ArgumentOutOfRangeException(nameof(dataFormat)),
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
    }
}
