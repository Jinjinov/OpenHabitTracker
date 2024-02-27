using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Ididit.Localization;

public class JsonStringLocalizerFactory(IOptions<LocalizationOptions> options) : IStringLocalizerFactory
{
    private readonly string _resourcesPath = options.Value.ResourcesPath;

    public IStringLocalizer Create(Type resourceSource)
    {
        EmbeddedFileProvider resources = new(resourceSource.Assembly);

        return new JsonStringLocalizer(resources, _resourcesPath, resourceSource.Name);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        Assembly assembly = Assembly.LoadFrom(location);

        EmbeddedFileProvider resources = new(assembly);

        return new JsonStringLocalizer(resources, _resourcesPath, baseName);
    }
}
