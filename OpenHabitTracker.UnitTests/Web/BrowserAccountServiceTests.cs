using System.Text.Json;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Dto;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class BrowserAccountServiceTests
{
    // The browser's fetch, answering every call with the status and body a test sets.
    private sealed class FakeModule : IJSObjectReference
    {
        public List<(string Method, string Url, string Body)> Calls { get; } = [];

        public int Status { get; set; } = 200;

        public string Body { get; set; } = "{\"username\":\"a\",\"email\":\"a@x\",\"isAdministrator\":false}";

        public int Disposed { get; private set; }

        public bool Disconnected { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.That(identifier, Is.EqualTo("send"));

            Calls.Add(((string)args![0]!, (string)args[1]!, JsonSerializer.Serialize(args[2])));

            // the response type is private to the service, so it is filled the way the interop fills it
            return ValueTask.FromResult(JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(new { Status, Body }))!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Disposed++;

            return Disconnected ? ValueTask.FromException(new JSDisconnectedException("gone")) : ValueTask.CompletedTask;
        }
    }

    private sealed class FakeRuntime(FakeModule module) : IJSRuntime
    {
        public List<string> Imports { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Assert.That(identifier, Is.EqualTo("import"));

            Imports.Add((string)args![0]!);

            return ValueTask.FromResult((TValue)(object)module);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
    }

    private FakeModule _module = null!;
    private FakeRuntime _runtime = null!;
    private BunitNavigationManager _navigation = null!;
    private BrowserAccountService _sut = null!;
    private Bunit.BunitContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new Bunit.BunitContext();
        _navigation = (BunitNavigationManager)_ctx.Services.GetService(typeof(NavigationManager))!;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _module = new FakeModule();
        _runtime = new FakeRuntime(_module);
        _sut = new BrowserAccountService(_runtime, _navigation, loc);
    }

    [TearDown]
    public async Task TearDown()
    {
        _module.Disconnected = false;
        await _sut.DisposeAsync();
        await _module.DisposeAsync();
        _ctx.Dispose();
    }

    [Test]
    public async Task GetAccount_Ok_ReadsTheAccountFromTheCamelCaseBody()
    {
        _module.Body = "{\"username\":\"urban\",\"email\":\"u@example.com\",\"isAdministrator\":true}";

        AccountResponse? account = await _sut.GetAccount();

        Assert.Multiple(() =>
        {
            Assert.That((account?.Username, account?.Email, account?.IsAdministrator), Is.EqualTo(("urban", "u@example.com", (bool?)true)));
            Assert.That(_module.Calls, Is.EqualTo(new[] { ("GET", "api/account", "null") }));
        });
    }

    [Test]
    public async Task GetAccount_NotOk_IsNull()
    {
        _module.Status = 401;

        Assert.That(await _sut.GetAccount(), Is.Null);
    }

    [Test]
    public async Task GetAccounts_OkNotOkAndNull()
    {
        _module.Body = "[{\"username\":\"a\",\"email\":\"a@x\",\"isAdministrator\":false},{\"username\":\"b\",\"email\":\"b@x\",\"isAdministrator\":true}]";

        IReadOnlyList<AccountResponse> accounts = await _sut.GetAccounts();

        _module.Body = "null";
        IReadOnlyList<AccountResponse> none = await _sut.GetAccounts();

        _module.Status = 403;
        IReadOnlyList<AccountResponse> refused = await _sut.GetAccounts();

        Assert.Multiple(() =>
        {
            Assert.That(accounts.Select(x => (x.Username, x.IsAdministrator)), Is.EqualTo(new[] { ("a", false), ("b", true) }));
            Assert.That(none, Is.Empty);
            Assert.That(refused, Is.Empty);
            Assert.That(_module.Calls.Select(x => (x.Method, x.Url)).Distinct(), Is.EqualTo(new[] { ("GET", "api/account/accounts") }));
        });
    }

    [Test]
    public async Task Changes_PostTheirFieldsAndReturnNoErrorsWhenOk()
    {
        IReadOnlyList<string> changed = await _sut.ChangePassword("old", "new");
        IReadOnlyList<string> created = await _sut.CreateAccount("b", "b@x", "pw");
        IReadOnlyList<string> reset = await _sut.ResetPassword("b", "pw2");

        Assert.Multiple(() =>
        {
            Assert.That(changed.Concat(created).Concat(reset), Is.Empty);
            Assert.That(_module.Calls, Is.EqualTo(new[]
            {
                ("POST", "api/account/password", "{\"CurrentPassword\":\"old\",\"NewPassword\":\"new\"}"),
                ("POST", "api/account/accounts", "{\"Username\":\"b\",\"Email\":\"b@x\",\"Password\":\"pw\"}"),
                ("POST", "api/account/accounts/password", "{\"Username\":\"b\",\"Password\":\"pw2\"}")
            }));
        });
    }

    [Test]
    public async Task Changes_Refused_ReturnTheServersMessages()
    {
        _module.Status = 400;
        _module.Body = "[\"Passwords must have at least one digit.\"]";

        IReadOnlyList<string> errors = await _sut.ChangePassword("old", "new");

        _module.Status = 500;
        _module.Body = "";


        IReadOnlyList<string> failed = await _sut.CreateAccount("b", "b@x", "pw");

        Assert.Multiple(() =>
        {
            Assert.That(errors, Is.EqualTo(new[] { "Passwords must have at least one digit." }));
            Assert.That(failed, Is.EqualTo(new[] { "Connection error" }));
        });
    }

    [Test]
    public async Task LogOut_PostsAndReloadsTheLoginPage()
    {
        await _sut.LogOut();

        Assert.Multiple(() =>
        {
            Assert.That(_module.Calls, Is.EqualTo(new[] { ("POST", "api/account/logout", "{}") }));
            Assert.That(_navigation.History.Single().Uri, Is.EqualTo("/login"));
            Assert.That(_navigation.History.Single().Options.ForceLoad, Is.True);
        });
    }

    [Test]
    public async Task Module_IsImportedOnceAndDisposedOnce()
    {
        await _sut.GetAccount();
        _module.Body = "[]";
        await _sut.GetAccounts();
        await _sut.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_runtime.Imports, Is.EqualTo(new[] { "./account.js" }));
            Assert.That(_module.Disposed, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Dispose_NeverUsed_ImportsNothing()
    {
        await _sut.DisposeAsync();

        Assert.That(_runtime.Imports, Is.Empty);
    }

    [Test]
    public async Task Dispose_CircuitGone_IsQuiet()
    {
        await _sut.GetAccount();
        _module.Disconnected = true;

        Assert.DoesNotThrowAsync(async () => await _sut.DisposeAsync());
    }
}
