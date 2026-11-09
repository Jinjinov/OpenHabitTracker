using CsvHelper;
using Microsoft.Data.Sqlite;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup.File;
using OpenHabitTracker.Backup.GoogleKeep;
using OpenHabitTracker.Backup.Habitica;
using OpenHabitTracker.Backup.HabitNow;
using OpenHabitTracker.Backup.Loop;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.Text.Json;
using YamlDotNet.Core;

namespace OpenHabitTracker.Backup;

public class ImportExportService(ClientState clientState, JsonImportExport jsonImportExport, TsvImportExport tsvImportExport, YamlImportExport yamlImportExport, MarkdownImportExport markdownImportExport, GoogleKeepImport googleKeepImport, LoopImport loopImport, LoopCsvImport loopCsvImport, HabitNowImport habitNowImport, HabiticaImport habiticaImport)
{
    private readonly ClientState _clientState = clientState;
    private readonly JsonImportExport _jsonImportExport = jsonImportExport;
    private readonly TsvImportExport _tsvImportExport = tsvImportExport;
    private readonly YamlImportExport _yamlImportExport = yamlImportExport;
    private readonly MarkdownImportExport _markdownImportExport = markdownImportExport;
    private readonly GoogleKeepImport _googleKeepImport = googleKeepImport;
    private readonly LoopImport _loopImport = loopImport;
    private readonly LoopCsvImport _loopCsvImport = loopCsvImport;
    private readonly HabitNowImport _habitNowImport = habitNowImport;
    private readonly HabiticaImport _habiticaImport = habiticaImport;

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

    // Reads the file and counts what an import would add, without writing anything.
    public async Task<(ImportResult Result, PendingImport? PendingImport)> ReadDataFile(string filename, Stream stream)
    {
        using MemoryStream memoryStream = new();

        await stream.CopyToAsync(memoryStream);

        ImportFormat importFormat = ImportFormatDetector.Detect(filename, memoryStream.ToArray());

        memoryStream.Position = 0;

        List<CategoryModel> categories;
        SettingsModel? settings = null;
        MergeOptions options = new();

        // Wasm ships no SQLite, so a database can only be read on the other hosts.
        if (importFormat == ImportFormat.LoopDatabase && OperatingSystem.IsBrowser())
            return (ImportResult.UnsupportedFormat, null);

        // Only the parse errors of the file formats are caught, so any other failure still surfaces.
        try
        {
            switch (importFormat)
            {
                case ImportFormat.Json:
                    UserImportExportData jsonData = await _jsonImportExport.ReadDataFile(memoryStream);
                    categories = jsonData.Categories;
                    settings = jsonData.Settings;
                    break;
                case ImportFormat.Tsv:
                    categories = await _tsvImportExport.ReadDataFile(memoryStream);
                    break;
                case ImportFormat.Yaml:
                    UserImportExportData yamlData = await _yamlImportExport.ReadDataFile(memoryStream);
                    categories = yamlData.Categories;
                    settings = yamlData.Settings;
                    break;
                case ImportFormat.Markdown:
                    categories = await _markdownImportExport.ReadDataFile(memoryStream);
                    break;
                case ImportFormat.GoogleKeep:
                    categories = await _googleKeepImport.ReadDataFile(memoryStream);
                    break;
                case ImportFormat.LoopDatabase:
                    (categories, options) = await _loopImport.ReadDataFile(memoryStream);
                    break;
                case ImportFormat.LoopCsv:
                    (categories, options) = await _loopCsvImport.ReadDataFile(memoryStream);
                    break;
                case ImportFormat.HabitNow:
                    (categories, options) = await _habitNowImport.ReadDataFile(memoryStream);
                    break;
                case ImportFormat.Habitica:
                    (categories, options) = await _habiticaImport.ReadDataFile(memoryStream);
                    break;
                default:
                    return (ImportResult.UnsupportedFormat, null);
            }
        }
        catch (Exception exception) when (exception is JsonException or YamlException or CsvHelperException or InvalidDataException or SqliteException)
        {
            return (ImportResult.UnreadableContent, null);
        }

        UserDataMerge merge = await _clientState.MergeUserData(categories, options);

        PendingImport pendingImport = new()
        {
            Format = importFormat,
            Filename = filename,
            Categories = categories,
            Options = options,
            Settings = settings,
            Found = merge.Found,
            New = merge.New
        };

        return (ImportResult.Read, pendingImport);
    }

    // The data can change while the import waits, so what is new is matched again here.
    public async Task Import(PendingImport pendingImport)
    {
        if (pendingImport.Settings is not null)
            await _clientState.ReplaceSettings(pendingImport.Settings);

        UserDataMerge merge = await _clientState.MergeUserData(pendingImport.Categories, pendingImport.Options);

        await _clientState.AddUserData(merge);
    }
}
