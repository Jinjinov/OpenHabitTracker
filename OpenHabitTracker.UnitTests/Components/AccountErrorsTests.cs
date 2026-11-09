using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Blazor;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class AccountErrorsTests
{
    private IStringLocalizer _loc = null!;

    [SetUp]
    public void SetUp()
    {
        _loc = Substitute.For<IStringLocalizer>();
        _loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));
    }

    [Test]
    public void From_ListOfMessages_ReturnsThem()
    {
        Assert.That(AccountErrors.From(400, "[\"Incorrect password.\",\"Too short.\"]", _loc), Is.EqualTo(new[] { "Incorrect password.", "Too short." }));
    }

    [Test]
    public void From_ValidationProblem_ReturnsItsMessages()
    {
        string body = "{\"title\":\"One or more validation errors occurred.\",\"status\":400,\"errors\":{\"Email\":[\"The Email field is not a valid e-mail address.\"]}}";

        Assert.That(AccountErrors.From(400, body, _loc), Is.EqualTo(new[] { "The Email field is not a valid e-mail address." }));
    }

    [Test]
    public void From_RefusalAsPlainText_ReturnsTheText()
    {
        Assert.That(AccountErrors.From(403, "The administrator's password is set in the container's settings.", _loc),
            Is.EqualTo(new[] { "The administrator's password is set in the container's settings." }));
    }

    [Test]
    public void From_AnyOtherStatus_IsAConnectionError()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AccountErrors.From(500, "{\"error\":\"An error occurred in the API\"}", _loc), Is.EqualTo(new[] { "Connection error" }));
            Assert.That(AccountErrors.From(404, "", _loc), Is.EqualTo(new[] { "Connection error" }));
        });
    }
}
