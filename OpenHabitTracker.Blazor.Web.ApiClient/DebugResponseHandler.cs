using System.Diagnostics;

namespace OpenHabitTracker.Blazor.Web.ApiClient;

public class DebugResponseHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Debug.WriteLine($"Request: {request.Method} {request.RequestUri}");

        if (request.Content != null)
        {
            string rawRequest = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            Debug.WriteLine("Request Content: " + rawRequest);
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        Debug.WriteLine("Response StatusCode: " + response.StatusCode);

        if (response.Content != null)
        {
            string rawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Debug.WriteLine("Response Content: " + rawResponse);
        }

        return response;
    }
}
