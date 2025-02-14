namespace OpenHabitTracker.Blazor.Web.ApiClient;

public class DebugResponseHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.Content != null)
        {
            string rawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine(rawResponse);
        }

        return response;
    }
}
