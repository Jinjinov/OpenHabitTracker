using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using System.Text.Json;

namespace OpenHabitTracker.Backup.File;

public class JsonImportExport(ClientState clientState)
{
    private readonly ClientState _clientState = clientState;

    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public async Task<string> GetDataExportFileString()
    {
        UserImportExportData userData = await _clientState.GetUserData();

        return JsonSerializer.Serialize(userData, _options);
    }

    public async Task ImportDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string content = await streamReader.ReadToEndAsync();

        UserImportExportData userData = JsonSerializer.Deserialize<UserImportExportData>(content, _options) ?? throw new InvalidDataException("Can't deserialize JSON");

        await _clientState.SetUserData(userData);
    }
}
