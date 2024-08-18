using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Ididit.Localization;

public class JsonStringLocalizer(IFileProvider fileProvider, string resourcesPath, string resourcesName) : IStringLocalizer
{
    private readonly IFileProvider _fileProvider = fileProvider;
    private readonly string _resourcesPath = resourcesPath;
    private readonly string _resourcesName = resourcesName;

    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _stringMapsCache = new();

    private static readonly Dictionary<string, string> _missing = [];
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public static void Serialize()
    {
        string file = JsonSerializer.Serialize(_missing, _options);
        File.WriteAllText("en.json", file);
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (var stringMap in _stringMapsCache.Values)
        {
            foreach ((string name, string value) in stringMap)
            {
                yield return new LocalizedString(name, value);
            }
        }
    }

    public LocalizedString this[string name]
    {
        get
        {
            Dictionary<string, string> stringMap = LoadStringMap();

            if (!stringMap.TryGetValue(name, out string? translation))
            {
                string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                translation = $"❗ {cultureName} {name} ❗";

                //_missing[name] = name;
            }

            return new LocalizedString(name, translation);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            Dictionary<string, string> stringMap = LoadStringMap();

            if (!stringMap.TryGetValue(name, out string? translation))
            {
                string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                translation = $"❗ {cultureName} {name} ❗";
            }

            return new LocalizedString(name, string.Format(translation, arguments));
        }
    }

    private Dictionary<string, string> LoadStringMap()
    {
        string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (_stringMapsCache.GetValueOrDefault(cultureName) is Dictionary<string, string> stringMap)
            return stringMap;

        IFileInfo fileInfo = _fileProvider.GetFileInfo(Path.Combine(_resourcesPath, $"{_resourcesName}.{cultureName}.json"));
        if (!fileInfo.Exists)
        {
            fileInfo = _fileProvider.GetFileInfo(Path.Combine(_resourcesPath, $"{cultureName}.json"));
        }

        using Stream stream = fileInfo.CreateReadStream();

        stringMap = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? throw new SerializationException(fileInfo.Name);

        _stringMapsCache[cultureName] = stringMap;

        return stringMap;
    }
}
