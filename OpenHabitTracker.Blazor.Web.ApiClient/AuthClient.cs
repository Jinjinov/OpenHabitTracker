using Microsoft.Extensions.DependencyInjection;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public partial class AuthClient
{
    [ActivatorUtilitiesConstructor] // This ctor will be used by DI
    public AuthClient(HttpClient httpClient) : this("", httpClient) // Call generated ctor
    {
    }

    private string? _token;

    public void SetBearerToken(string token) =>
        _token = token;

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        if (!string.IsNullOrEmpty(_token))
            request.Headers.Authorization = new("Bearer", _token);
    }
}
