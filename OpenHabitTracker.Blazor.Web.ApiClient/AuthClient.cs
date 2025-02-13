namespace OpenHabitTracker.Blazor.Web.ApiClient;

public partial class AuthClient
{
    private string? _token;

    public void SetBearerToken(string token) =>
        _token = token;

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        if (!string.IsNullOrEmpty(_token))
            request.Headers.Authorization = new("Bearer", _token);
    }
}
