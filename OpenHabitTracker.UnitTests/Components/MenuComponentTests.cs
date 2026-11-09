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
    private IAuthFragment _authFragment = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _jsInterop = Substitute.For<IJsInterop>();

        _authFragment = Substitute.For<IAuthFragment>();

        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddScoped(_ => _jsInterop);
        _ctx.Services.AddScoped(_ => _authFragment);
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
        Assert.That(cut.FindAll("button:has(i.bi-globe)"), Is.Empty);
    }

    [Test]
    public void Renders_SyncButtonAfterData_WhenAuthIsAvailable()
    {
        _authFragment.IsAuthAvailable.Returns(true);

        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");

        Assert.That(buttons, Has.Count.EqualTo(7));
        Assert.That(buttons[4].QuerySelector("i.bi-database"), Is.Not.Null);
        Assert.That(buttons[5].QuerySelector("i.bi-globe"), Is.Not.Null);
    }

    [Test]
    public async Task ClickSyncButton_InvokesDynamicComponentTypeChanged_WithSyncType()
    {
        _authFragment.IsAuthAvailable.Returns(true);

        Type? invokedWith = null;
        IRenderedComponent<Menu> cut = _ctx.Render<Menu>(
            parameters => parameters
                .Add(p => p.DynamicComponentTypeChanged, (Type? t) => invokedWith = t));

        await cut.Find("button:has(i.bi-globe)").ClickAsync(new MouseEventArgs());

        Assert.That(invokedWith, Is.EqualTo(typeof(Sync)));
    }

    [Test]
    public async Task ClickAboutButton_UpdatesTabindexToLastButton_WhenAuthIsAvailable()
    {
        _authFragment.IsAuthAvailable.Returns(true);

        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("button:has(i.bi-info-circle)").ClickAsync(new MouseEventArgs());

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[6].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(buttons[5].GetAttribute("tabindex"), Is.EqualTo("-1"));
    }

    [Test]
    public async Task End_MovesTabindexToLastButton_WhenAuthIsAvailable()
    {
        _authFragment.IsAuthAvailable.Returns(true);

        IRenderedComponent<Menu> cut = _ctx.Render<Menu>();

        await cut.Find("div[role='menu']").KeyDownAsync(new KeyboardEventArgs { Key = "End" });

        IReadOnlyList<IElement> buttons = cut.FindAll("div.list-group > button");
        Assert.That(buttons[6].GetAttribute("tabindex"), Is.EqualTo("0"));
        Assert.That(buttons[0].GetAttribute("tabindex"), Is.EqualTo("-1"));
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
