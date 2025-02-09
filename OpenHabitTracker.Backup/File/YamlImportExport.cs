using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;

namespace OpenHabitTracker.Backup.File;

public class YamlImportExport(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly ISerializer _serializer = new SerializerBuilder().WithTypeConverter(new TimeOnlyConverter(formats: ["HH:mm:ss"])).Build();

    private readonly IDeserializer _deserializer = new DeserializerBuilder().WithTypeConverter(new TimeOnlyConverter(formats: ["HH:mm:ss"])).Build();

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