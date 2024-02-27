using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Ididit.Localization;

public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private string ResourcesPath { get; }

    public JsonStringLocalizerFactory(IOptions<LocalizationOptions> options)
    {
        ResourcesPath = options.Value.ResourcesPath;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        var resources = new EmbeddedFileProvider(resourceSource.Assembly);
        return new JsonStringLocalizer(resources, ResourcesPath, resourceSource.Name);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        throw new NotImplementedException();
    }
}
