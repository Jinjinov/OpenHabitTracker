using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Blazor.Pages;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class MenuComponentTests
{
    private BunitContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void Renders_SixMenuButtons()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");

        Assert.That(buttons, Has.Count.EqualTo(6));
    }

    [Test]
    public async Task ClickTrashButton_InvokesDynamicComponentTypeChanged_WithTrashType()
    {
        Type? invokedWith = null;
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>(
            parameters => parameters
                .Add(p => p.DynamicComponentTypeChanged, (Type? t) => invokedWith = t));

        await cut.Find("button:has(i.bi-trash)").ClickAsync(new MouseEventArgs());

        Assert.That(invokedWith, Is.EqualTo(typeof(Trash)));
    }

    [Test]
    public async Task ClickSettingsButton_InvokesDynamicComponentTypeChanged_WithSettingsType()
    {
        Type? invokedWith = null;
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>(
            parameters => parameters
                .Add(p => p.DynamicComponentTypeChanged, (Type? t) => invokedWith = t));

        await cut.Find("button:has(i.bi-gear)").ClickAsync(new MouseEventArgs());

        Assert.That(invokedWith, Is.EqualTo(typeof(Settings)));
    }
}
