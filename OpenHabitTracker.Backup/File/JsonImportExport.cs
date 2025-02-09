using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using System.Text.Json;

namespace OpenHabitTracker.Backup.File;

public class JsonImportExport(ClientSideData appData)
{
    private readonly ClientSideData _appData = appData;

    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public async Task<string> GetDataExportFileString()
    {
        UserImportExportData userData = await _appData.GetUserData();

        return JsonSerializer.Serialize(userData, _options);
    }

    public async Task ImportDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string content = await streamReader.ReadToEndAsync();

        UserImportExportData userData = JsonSerializer.Deserialize<UserImportExportData>(content, _options) ?? throw new InvalidDataException("Can't deserialize JSON");

        await _appData.SetUserData(userData);
    }
}
