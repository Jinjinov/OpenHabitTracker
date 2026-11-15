using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Blazor.Web.Data;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class CurrentUserTests
{
    private IHttpContextAccessor _httpContextAccessor = null!;
    private AuthenticationStateProvider _authenticationStateProvider = null!;
    private CurrentUser _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _authenticationStateProvider = Substitute.For<AuthenticationStateProvider>();
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        _sut = new CurrentUser(_httpContextAccessor, _authenticationStateProvider);
    }

    private static ClaimsPrincipal Principal(string? id, params string[] roles)
    {
        List<Claim> claims = roles.Select(x => new Claim(ClaimTypes.Role, x)).ToList();

        if (id is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, id));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private void SignInByRequest(ClaimsPrincipal principal) => _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = principal });

    private void SignInByCircuit(ClaimsPrincipal principal) =>
        _authenticationStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(new AuthenticationState(principal)));

    [Test]
    public async Task Request_ReadsTheAccountAndRoleFromItsUser()
    {
        SignInByRequest(Principal("7", RoleNames.Administrator));
        SignInByCircuit(Principal("9"));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _sut.GetUserId(), Is.EqualTo(7));
            Assert.That(await _sut.IsAdministrator(), Is.True);
        });
    }

    [Test]
    public async Task Circuit_WithoutARequest_ReadsItsAuthenticationState()
    {
        SignInByCircuit(Principal("9"));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _sut.GetUserId(), Is.EqualTo(9));
            Assert.That(await _sut.IsAdministrator(), Is.False);
        });
    }

    [TestCase(null)]
    [TestCase("not-a-number")]
    public async Task UserWithoutANumericId_IsAccountZero(string? id)
    {
        SignInByRequest(Principal(id));

        Assert.That(await _sut.GetUserId(), Is.Zero);
    }

    [Test]
    public async Task StartupScope_WithoutAnyState_IsAccountZeroAndNoAdministrator()
    {
        _authenticationStateProvider.GetAuthenticationStateAsync().Returns<Task<AuthenticationState>>(_ => throw new InvalidOperationException());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _sut.GetUserId(), Is.Zero);
            Assert.That(await _sut.IsAdministrator(), Is.False);
        });
    }
}
