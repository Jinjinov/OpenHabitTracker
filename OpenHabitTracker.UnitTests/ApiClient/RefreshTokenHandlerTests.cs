using NSubstitute;
using OpenHabitTracker.Blazor.Web.ApiClient;
using System.Net;
using System.Text;

namespace OpenHabitTracker.UnitTests.ApiClient;

[TestFixture]
public class RefreshTokenHandlerTests
{
    private ApiClientOptions _options = null!;
    private ITokenRefresher _tokenRefresher = null!;
    private RecordingHandler _inner = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new ApiClientOptions
        {
            BearerToken = "old-token",
            RefreshToken = "refresh",
            BearerTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        _tokenRefresher = Substitute.For<ITokenRefresher>();
        _tokenRefresher.TryRefresh().Returns(false);

        _inner = new RecordingHandler();
    }

    [TearDown]
    public void TearDown()
    {
        _inner.Dispose();
    }

    private HttpClient CreateClient()
    {
        RefreshTokenHandler handler = new(_options, _tokenRefresher) { InnerHandler = _inner };

        return new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
    }

    private void RefreshSucceedsWith(string bearerToken)
    {
        _tokenRefresher.TryRefresh().Returns(_ =>
        {
            _options.BearerToken = bearerToken;
            _options.BearerTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(1);

            return true;
        });
    }

    [Test]
    public async Task Send_TokenNotNearExpiry_DoesNotRefresh()
    {
        await CreateClient().GetAsync("data");

        await _tokenRefresher.DidNotReceive().TryRefresh();
        Assert.That(_inner.AuthorizationHeaders, Is.EqualTo(new[] { "old-token" }));
    }

    [Test]
    public async Task Send_TokenNearExpiry_RefreshesBeforeSending()
    {
        _options.BearerTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1);
        RefreshSucceedsWith("new-token");

        await CreateClient().GetAsync("data");

        await _tokenRefresher.Received(1).TryRefresh();
        Assert.That(_inner.AuthorizationHeaders, Is.EqualTo(new[] { "new-token" }));
    }

    [Test]
    public async Task Send_NoBearerToken_PassesThroughWithoutRefreshing()
    {
        _options.BearerToken = "";
        _options.BearerTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        await CreateClient().GetAsync("data");

        await _tokenRefresher.DidNotReceive().TryRefresh();
        Assert.That(_inner.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Send_Unauthorized_RefreshSucceeds_RetriesOnceWithTheNewToken()
    {
        _inner.Enqueue(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        RefreshSucceedsWith("new-token");

        HttpResponseMessage response = await CreateClient().GetAsync("data");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(_inner.AuthorizationHeaders, Is.EqualTo(new[] { "old-token", "new-token" }));
        });
    }

    [Test]
    public async Task Send_Unauthorized_RefreshFails_ReturnsTheUnauthorizedResponse()
    {
        _inner.Enqueue(HttpStatusCode.Unauthorized);

        HttpResponseMessage response = await CreateClient().GetAsync("data");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(_inner.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Send_RetryAlsoUnauthorized_DoesNotRetryAgain()
    {
        _inner.Enqueue(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized);
        RefreshSucceedsWith("new-token");

        HttpResponseMessage response = await CreateClient().GetAsync("data");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(_inner.Requests, Has.Count.EqualTo(2));
        });

        await _tokenRefresher.Received(1).TryRefresh();
    }

    [Test]
    public async Task Send_Unauthorized_RetryKeepsTheMethodAndTheBody()
    {
        _inner.Enqueue(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        RefreshSucceedsWith("new-token");

        StringContent content = new("""{"Title":"Read"}""", Encoding.UTF8, "application/json");

        await CreateClient().PostAsync("data", content);

        Assert.Multiple(() =>
        {
            Assert.That(_inner.Methods, Is.EqualTo(new[] { HttpMethod.Post, HttpMethod.Post }));
            Assert.That(_inner.Bodies, Is.EqualTo(new[] { """{"Title":"Read"}""", """{"Title":"Read"}""" }));
            Assert.That(_inner.ContentTypes, Is.EqualTo(new[] { "application/json; charset=utf-8", "application/json; charset=utf-8" }));
        });
    }

    private class RecordingHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _statusCodes = new();

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<HttpMethod> Methods { get; } = [];

        public List<string> AuthorizationHeaders { get; } = [];

        public List<string> Bodies { get; } = [];

        public List<string> ContentTypes { get; } = [];

        public void Enqueue(params HttpStatusCode[] statusCodes)
        {
            foreach (HttpStatusCode statusCode in statusCodes)
            {
                _statusCodes.Enqueue(statusCode);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Methods.Add(request.Method);
            AuthorizationHeaders.Add(request.Headers.Authorization?.Parameter ?? "");
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            ContentTypes.Add(request.Content?.Headers.ContentType?.ToString() ?? "");

            return new HttpResponseMessage(_statusCodes.Count > 0 ? _statusCodes.Dequeue() : HttpStatusCode.OK);
        }
    }
}
