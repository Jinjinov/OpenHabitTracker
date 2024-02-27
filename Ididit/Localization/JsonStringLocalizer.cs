using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Text.Json;

namespace Ididit.Localization;

public class JsonStringLocalizer : IStringLocalizer
{
    private IFileProvider FileProvider { get; }
    private string Name { get; }
    private string ResourcesPath { get; }

    public JsonStringLocalizer(IFileProvider fileProvider, string resourcePath, string name)
    {
        FileProvider = fileProvider;
        Name = name;
        ResourcesPath = resourcePath;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        throw new NotImplementedException();
    }

    public IStringLocalizer WithCulture(CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public LocalizedString this[string name]
    {
        get
        {
            var stringMap = LoadStringMap();

            return new LocalizedString(name, stringMap[name]);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var stringMap = LoadStringMap();

            return new LocalizedString(name, string.Format(stringMap[name], arguments));
        }
    }

    private Dictionary<string, string> LoadStringMap()
    {
        var cultureInfo = CultureInfo.CurrentUICulture;
        var cultureName = cultureInfo.TwoLetterISOLanguageName;

        var fileInfo = FileProvider.GetFileInfo(Path.Combine(ResourcesPath, $"{Name}-{cultureName}.json"));

        if (!fileInfo.Exists)
        {
            fileInfo = FileProvider.GetFileInfo(Path.Combine(ResourcesPath, $"{Name}.json"));
        }

        using var stream = fileInfo.CreateReadStream();

        return JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream).Result;
    }
}
