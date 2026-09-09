namespace OpenHabitTracker.Blazor.Web.ApiClient;

public class ApiClientOptions
{
    private string _baseUrl = string.Empty;

    public string BaseUrl
    {
        get { return _baseUrl; }
        set
        {
            _baseUrl = value;
            if (!string.IsNullOrEmpty(_baseUrl) && !_baseUrl.EndsWith('/'))
                _baseUrl += '/';
        }
    }

    public string BearerToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset BearerTokenExpiresAt { get; set; }
}
