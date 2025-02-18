using Microsoft.Extensions.DependencyInjection;
using OpenHabitTracker.App;
using System.Text;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public partial class AuthClient
{
    private readonly ApiClientOptions _options;

    [ActivatorUtilitiesConstructor] // This ctor will be used by DI
    public AuthClient(HttpClient httpClient, ApiClientOptions options) : this(httpClient) // Call generated ctor
    {
        _options = options;
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder)
    {
        if (!string.IsNullOrEmpty(_options.BearerToken))
            request.Headers.Authorization = new("Bearer", _options.BearerToken);

        if (!string.IsNullOrEmpty(_options.BaseUrl))
            urlBuilder.Insert(0, _options.BaseUrl);
    }
}
