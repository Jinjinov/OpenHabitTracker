using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;

namespace OpenHabitTracker.Backup.File;

public class YamlImportExport(ClientState appData)
{
    private readonly ClientState _appData = appData;

    private readonly ISerializer _serializer = new SerializerBuilder().WithTypeConverter(new TimeOnlyConverter(formats: ["HH:mm:ss"])).Build();

    private readonly IDeserializer _deserializer = new DeserializerBuilder().WithTypeConverter(new TimeOnlyConverter(formats: ["HH:mm:ss"])).Build();

    public async Task<string> GetDataExportFileString()
    {
        UserImportExportData userData = await _appData.GetUserData();

        using StringWriter stringWriter = new();

        _serializer.Serialize(stringWriter, userData);

        return stringWriter.ToString();
    }

    public async Task ImportDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string content = await streamReader.ReadToEndAsync();

        UserImportExportData userData = _deserializer.Deserialize<UserImportExportData>(content);

        await _appData.SetUserData(userData);
    }
}