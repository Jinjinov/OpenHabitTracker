using OpenHabitTracker.Data;
using YamlDotNet.Serialization;

namespace OpenHabitTracker.Backup.File;

public class YamlImportExport(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly Serializer _serializer = new();

    private readonly Deserializer _deserializer = new();

    public async Task<string> GetDataExportFileString()
    {
        UserData userData = await _appData.GetUserData();

        using StringWriter stringWriter = new();

        _serializer.Serialize(stringWriter, userData);

        return stringWriter.ToString();
    }

    public async Task ImportDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string content = await streamReader.ReadToEndAsync();

        UserData userData = _deserializer.Deserialize<UserData>(content);

        await _appData.SetUserData(userData);
    }
}