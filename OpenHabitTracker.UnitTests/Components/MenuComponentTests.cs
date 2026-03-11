using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class MenuComponentTests
{
    private BunitContext _ctx = null!;
    private IJsInterop _jsInterop = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _jsInterop = Substitute.For<IJsInterop>();

        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddScoped(_ => _jsInterop);
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

    [Test]
    public void Renders_FirstButtonWithTabindexZero()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");

        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(buttons[1].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task ArrowDown_MovesTabindexToNextButton()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
        Assert.That(buttons[1].GetAttribute("tabindex"), Is.EqualTo("0"));
    }

    [Test]
    public async Task ArrowDown_FromLastButton_WrapsToFirstButton()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        for (int i = 0; i < 6; i++)
            await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("0"));
    }

    [Test]
    public async Task ArrowUp_FromFirstButton_WrapsToLastButton()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[5].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task Home_MovesTabindexToFirstButton()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "Home" });

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("0"));
    }

    [Test]
    public async Task End_MovesTabindexToLastButton()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "End" });

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[5].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task ClickSecondButton_UpdatesTabindexToSecondButton()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("button:has(i.bi-search)").ClickAsync(new MouseEventArgs());

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[1].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task ArrowDown_CallsFocusElementOnJsInterop()
    {
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        await _jsInterop.Received(1).FocusElement(Arg.Any<Microsoft.AspNetCore.Components.ElementReference>());
    }
}
