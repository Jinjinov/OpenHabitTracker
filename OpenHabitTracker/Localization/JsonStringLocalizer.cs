using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OpenHabitTracker.Localization;

public class JsonStringLocalizer(IFileProvider fileProvider, string resourcesPath, string resourcesName) : IStringLocalizer
{
    private readonly IFileProvider _fileProvider = fileProvider;
    private readonly string _resourcesPath = resourcesPath;
    private readonly string _resourcesName = resourcesName;

    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _stringMapsCache = new();

    private static readonly Dictionary<string, string> _missing = [];
    private static readonly Dictionary<string, string> _unused = [];
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static void SerializeMissingAndUnusedValues()
    {
        string missingFile = JsonSerializer.Serialize(_missing, _options);
        File.WriteAllText("missing.json", missingFile);

        string unusedFile = JsonSerializer.Serialize(_unused, _options);
        File.WriteAllText("unused.json", unusedFile);
    }

    public void SerializeDuplicateValues()
    {
        Dictionary<string, List<string>> duplicateValues = LoadStringMap()
            .GroupBy(kvp => kvp.Value)
            .Where(group => group.Count() > 1)
            .ToDictionary(group => group.Key, group => group.Select(kvp => kvp.Key).ToList());

        string duplicateFile = JsonSerializer.Serialize(duplicateValues, _options);
        File.WriteAllText("duplicate.json", duplicateFile);
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
                translation = name;

                _missing[name] = name;
            }
            else
            {
                _unused.Remove(name);
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
                translation = name;

                _missing[name] = name;
            }
            else
            {
                _unused.Remove(name);
            }

            return new LocalizedString(name, string.Format(translation, arguments));
        }
    }

    private Dictionary<string, string> LoadStringMap()
    {
        string cultureName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        if (!Loc.Cultures.ContainsKey(cultureName))
        {
            cultureName = "en";
        }

        if (_stringMapsCache.GetValueOrDefault(cultureName) is Dictionary<string, string> stringMap)
            return stringMap;

        // use "name-xx.ext" because files with "name.xx.ext" are not embedded as resources
        // https://github.com/dotnet/sdk/issues/13395
        // https://github.com/dotnet/roslyn/issues/43820
        IFileInfo fileInfo = _fileProvider.GetFileInfo(Path.Combine(_resourcesPath, $"{_resourcesName}-{cultureName}.json"));
        if (!fileInfo.Exists)
        {
            fileInfo = _fileProvider.GetFileInfo(Path.Combine(_resourcesPath, $"{cultureName}.json"));
        }

        using Stream stream = fileInfo.CreateReadStream();

        stringMap = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? throw new SerializationException(fileInfo.Name);

        foreach (string key in stringMap.Keys)
        {
            _unused[key] = key;
        }

        _stringMapsCache[cultureName] = stringMap;

        return stringMap;
    }
}
