using System.Net;
using OpenHabitTracker.Blazor.Web.ApiClient;

namespace OpenHabitTracker.UnitTests.ApiClient;

// The handler writes each request and response to the debug output, so it must hand both on whole.
[TestFixture]
public class DebugResponseHandlerTests
{
    private sealed class EchoHandler : HttpMessageHandler
    {
        public string? ReceivedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ReceivedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("[1,2]") };
        }
    }

    [Test]
    public async Task Send_PassesTheRequestBodyOnAndLeavesTheResponseReadable()
    {
        EchoHandler echo = new();
        using HttpClient client = new(new DebugResponseHandler { InnerHandler = echo });

        using HttpResponseMessage response = await client.PostAsync("http://localhost/api/DataAccess/notes", new StringContent("{\"Title\":\"a\"}"));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(echo.ReceivedBody, Is.EqualTo("{\"Title\":\"a\"}"));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("[1,2]"));
        });
    }

    [Test]
    public async Task Send_WithoutABody_PassesTheRequestOn()
    {
        EchoHandler echo = new();
        using HttpClient client = new(new DebugResponseHandler { InnerHandler = echo });

        using HttpResponseMessage response = await client.GetAsync("http://localhost/api/DataAccess/notes");

        Assert.Multiple(() =>
        {
            Assert.That(echo.ReceivedBody, Is.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        });
    }
}
