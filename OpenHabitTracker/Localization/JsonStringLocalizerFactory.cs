using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace OpenHabitTracker.Localization;

public class JsonStringLocalizerFactory(IOptions<LocalizationOptions> options) : IStringLocalizerFactory
{
    private readonly string _resourcesPath = options.Value.ResourcesPath;

    private readonly ConcurrentDictionary<string, JsonStringLocalizer> _localizerCache = new();

    public IStringLocalizer Create(Type resourceSource)
    {
        if (_localizerCache.GetValueOrDefault(resourceSource.Name) is JsonStringLocalizer jsonStringLocalizer)
            return jsonStringLocalizer;

        // every Type has localization in their own Assembly:
        //EmbeddedFileProvider resources = new(resourceSource.Assembly);

        // every Type has localization in this Assembly:
        EmbeddedFileProvider resources = new(typeof(JsonStringLocalizer).Assembly);

        jsonStringLocalizer = new JsonStringLocalizer(resources, _resourcesPath, resourceSource.Name);

        _localizerCache[resourceSource.Name] = jsonStringLocalizer;

        return jsonStringLocalizer;
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        string key = location + baseName;

        if (_localizerCache.GetValueOrDefault(key) is JsonStringLocalizer jsonStringLocalizer)
            return jsonStringLocalizer;

        Assembly assembly;

        if (!string.IsNullOrEmpty(location))
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            assembly = assemblies.FirstOrDefault(a => a.GetName().Name == location)
                ?? throw new InvalidOperationException($"Assembly '{location}' could not be found.");
        }
        else
        {
            assembly = Assembly.GetExecutingAssembly();
        }

        EmbeddedFileProvider resources = new(assembly);

        jsonStringLocalizer = new JsonStringLocalizer(resources, _resourcesPath, baseName);

        _localizerCache[key] = jsonStringLocalizer;

        return jsonStringLocalizer;
    }
}
