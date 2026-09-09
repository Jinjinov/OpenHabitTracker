using System.Net;
using System.Net.Http.Headers;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

// The JWT outlives a session of a few hours and nothing else renews it, so a long-running app would
// otherwise get 401 on every call until it is restarted.
// Renewing before the expiry is what avoids almost all of them; the retry covers the rest, such as a
// server whose signing secret changed, where the token is rejected long before it expires.
// This handler must not be built by IHttpClientFactory: the factory gives handlers their own DI
// scope, so an injected ApiClientOptions would not be the instance the client reads.
// https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting
public class RefreshTokenHandler(ApiClientOptions options, ITokenRefresher tokenRefresher) : DelegatingHandler
{
    // Comfortably inside the five minutes of clock skew the server allows by default.
    private static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(10);

    private readonly ApiClientOptions _options = options;

    private readonly ITokenRefresher _tokenRefresher = tokenRefresher;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.BearerToken))
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (_options.BearerTokenExpiresAt - RenewBefore <= DateTimeOffset.UtcNow)
            await _tokenRefresher.TryRefresh().ConfigureAwait(false);

        // PrepareRequest stamped the header before this handler ran, so a renewal above has to replace it.
        SetBearerToken(request);

        // Captured before sending, because HttpClient disposes the request content afterwards.
        byte[]? content = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        if (!await _tokenRefresher.TryRefresh().ConfigureAwait(false))
            return response;

        response.Dispose();

        HttpRequestMessage retry = Clone(request, content);

        SetBearerToken(retry);

        return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
    }

    private void SetBearerToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_options.BearerToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.BearerToken);
    }

    private static HttpRequestMessage Clone(HttpRequestMessage request, byte[]? content)
    {
        HttpRequestMessage clone = new(request.Method, request.RequestUri) { Version = request.Version };

        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (content is not null)
        {
            ByteArrayContent clonedContent = new(content);

            if (request.Content is not null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                {
                    clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            clone.Content = clonedContent;
        }

        return clone;
    }
}
