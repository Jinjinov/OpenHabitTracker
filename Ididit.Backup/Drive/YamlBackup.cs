using Ididit.Data;
using YamlDotNet.Serialization;

namespace Ididit.Backup.Drive;

public class YamlBackup(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly ISerializer _serializer = new Serializer();

    private readonly IDeserializer _deserializer = new Deserializer();

    public async Task<string> GetDataExportFileString()
    {
        UserData userData = await _appData.GetUserData();

        using StringWriter stringWriter = new();

        _serializer.Serialize(stringWriter, userData);

        return stringWriter.ToString();
    }
}