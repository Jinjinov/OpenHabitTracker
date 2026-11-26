using System.Net;
using System.Text;
using OpenHabitTracker.Blazor.Web.ApiClient;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.ApiClient;

// Every client sends its calls to the address of the login and with the token of the login.
[TestFixture]
public class ApiClientRequestTests
{
    private sealed class CapturingHandler(string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private const string TokenJson = "{\"JwtToken\":\"jwt\",\"RefreshToken\":\"refresh\",\"JwtTokenExpiresAt\":\"2030-01-01T00:00:00+00:00\",\"Email\":\"\"}";

    private const string AccountJson = "{\"Username\":\"bea\",\"Email\":\"b@x\",\"IsAdministrator\":false}";

    [Test]
    public async Task AuthClient_SendsToTheLoginAddressWithTheToken()
    {
        CapturingHandler handler = new(TokenJson);
        AuthClient client = new(new HttpClient(handler), new ApiClientOptions { BaseUrl = "https://sync.example", BearerToken = "jwt" });

        await client.GetJwtTokenAsync(new LoginCredentials { Username = "bea", Password = "pw" });

        Assert.Multiple(() =>
        {
            Assert.That(handler.Request!.RequestUri, Is.EqualTo(new Uri("https://sync.example/api/Auth/jwt-token")));
            Assert.That(handler.Request.Headers.Authorization?.ToString(), Is.EqualTo("Bearer jwt"));
        });
    }

    [Test]
    public async Task AccountClient_SendsToTheLoginAddressWithTheToken()
    {
        CapturingHandler handler = new(AccountJson);
        AccountClient client = new(new HttpClient(handler), new ApiClientOptions { BaseUrl = "https://sync.example/", BearerToken = "jwt" });

        AccountResponse account = await client.GetAccountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Request!.RequestUri, Is.EqualTo(new Uri("https://sync.example/api/Account")));
            Assert.That(handler.Request.Headers.Authorization?.ToString(), Is.EqualTo("Bearer jwt"));
            Assert.That(account.Username, Is.EqualTo("bea"));
        });
    }

    [Test]
    public async Task DataAccessClient_SendsToTheLoginAddressWithTheToken()
    {
        CapturingHandler handler = new("[]");
        DataAccessClient client = new(new HttpClient(handler), new ApiClientOptions { BaseUrl = "https://sync.example", BearerToken = "jwt" });

        await client.GetNotesAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handler.Request!.RequestUri, Is.EqualTo(new Uri("https://sync.example/api/DataAccess/notes")));
            Assert.That(handler.Request.Headers.Authorization?.ToString(), Is.EqualTo("Bearer jwt"));
        });
    }

    // A server of an older version sends no repeat days, and one of a newer version can send fields this client does not know.
    [Test]
    public async Task DataAccessClient_HabitsFromAnOlderAndANewerServer_AreRead()
    {
        CapturingHandler handler = new("""
            [ { "Id": 1, "Title": "Older", "RepeatCount": 3, "RepeatPeriod": 1 },
              { "Id": 2, "Title": "Newer", "RepeatWeekdays": 21, "RepeatMonthDays": 16385, "RepeatMonthWeeks": 18, "RepeatMonths": 2052, "RepeatYearDays": 7 } ]
            """);
        DataAccessClient client = new(new HttpClient(handler), new ApiClientOptions { BaseUrl = "https://sync.example", BearerToken = "jwt" });

        IReadOnlyList<HabitEntity> habits = await client.GetHabitsAsync();

        Assert.Multiple(() =>
        {
            Assert.That((habits[0].Title, habits[0].RepeatCount, habits[0].RepeatWeekdays, habits[0].RepeatMonthDays, habits[0].RepeatMonthWeeks, habits[0].RepeatMonths),
                Is.EqualTo(("Older", 3, Weekdays.None, 0, MonthWeeks.None, Months.None)));
            Assert.That((habits[1].Title, habits[1].RepeatWeekdays, habits[1].RepeatMonthDays, habits[1].RepeatMonthWeeks, habits[1].RepeatMonths),
                Is.EqualTo(("Newer",
                    Weekdays.Monday | Weekdays.Wednesday | Weekdays.Friday,
                    16385,
                    MonthWeeks.Second | MonthWeeks.Last,
                    Months.March | Months.December)));
        });
    }

    // Before a login there is no token, and a login call must not carry an empty one.
    [Test]
    public async Task Clients_WithoutAToken_SendNoAuthorization()
    {
        CapturingHandler auth = new(TokenJson);
        CapturingHandler account = new(AccountJson);
        ApiClientOptions options = new() { BaseUrl = "https://sync.example" };

        await new AuthClient(new HttpClient(auth), options).GetJwtTokenAsync(new LoginCredentials { Username = "bea", Password = "pw" });
        await new AccountClient(new HttpClient(account), options).GetAccountAsync();

        Assert.Multiple(() =>
        {
            Assert.That(auth.Request!.Headers.Authorization, Is.Null);
            Assert.That(account.Request!.Headers.Authorization, Is.Null);
        });
    }
}
