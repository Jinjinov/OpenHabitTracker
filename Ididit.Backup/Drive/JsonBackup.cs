using Ididit.Data;
using System.Text.Json;

namespace Ididit.Backup.Drive;

public class JsonBackup(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public async Task<string> GetDataExportFileString()
    {
        UserData userData = await _appData.GetUserData();

        return JsonSerializer.Serialize(userData, _options);
    }
}
