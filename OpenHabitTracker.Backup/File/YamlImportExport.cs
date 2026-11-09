using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Converters;

namespace OpenHabitTracker.Backup.File;

public class YamlImportExport(ClientState clientState)
{
    private readonly ClientState _clientState = clientState;

    private readonly ISerializer _serializer = new SerializerBuilder().WithTypeConverter(new TimeOnlyConverter(formats: ["HH:mm:ss"])).Build();

    private readonly IDeserializer _deserializer = new DeserializerBuilder().WithTypeConverter(new TimeOnlyConverter(formats: ["HH:mm:ss"])).Build();

    public async Task<string> GetDataExportFileString()
    {
        UserImportExportData userData = await _clientState.GetUserData();

        using StringWriter stringWriter = new();

        _serializer.Serialize(stringWriter, userData);

        return stringWriter.ToString();
    }

    public async Task<UserImportExportData> ReadDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string content = await streamReader.ReadToEndAsync();

        return _deserializer.Deserialize<UserImportExportData>(content);
    }
}
