using OpenHabitTracker.Data;
using System.Text.Json;

namespace OpenHabitTracker.Backup.File;

public class JsonImportExport(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public async Task<string> GetDataExportFileString()
    {
        UserData userData = await _appData.GetUserData();

        return JsonSerializer.Serialize(userData, _options);
    }

    public async Task ImportDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string content = await streamReader.ReadToEndAsync();

        UserData userData = JsonSerializer.Deserialize<UserData>(content, _options) ?? throw new InvalidDataException("Can't deserialize JSON");

        await _appData.SetUserData(userData);
    }
}
