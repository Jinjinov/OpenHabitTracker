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

    public async Task ImportDataFile(Stream stream)
    {

    }
}
