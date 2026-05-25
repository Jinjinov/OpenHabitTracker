using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class QuantityModalComponentTests
{
    private BunitContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        IJsInterop jsInterop = Substitute.For<IJsInterop>();

        _ctx.Services.AddScoped(_ => jsInterop);
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void Modal_WhenNotVisible_IsNotRendered()
    {
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters.Add(p => p.IsVisible, false));

        Assert.That(cut.FindAll("div.modal"), Is.Empty);
    }

    [Test]
    public void Modal_WhenVisible_IsRendered()
    {
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters.Add(p => p.IsVisible, true));

        Assert.That(cut.Find("div.modal"), Is.Not.Null);
    }

    [Test]
    public async Task OkButton_Click_InvokesOnOKWithInitialQuantity()
    {
        long receivedQuantity = 0;
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.InitialQuantity, 7L)
                .Add(p => p.OnOK, EventCallback.Factory.Create<long>(this, q => receivedQuantity = q)));

        await cut.Find("button.btn-primary").ClickAsync(new MouseEventArgs());

        Assert.That(receivedQuantity, Is.EqualTo(7L));
    }

    [Test]
    public void CancelButton_WhenShowCancelTrue_IsVisible()
    {
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.ShowCancel, true));

        Assert.That(cut.FindAll("button.btn-close"), Is.Not.Empty);
    }

    [Test]
    public void CancelButton_WhenShowCancelFalse_IsHidden()
    {
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.ShowCancel, false));

        Assert.That(cut.FindAll("button.btn-close"), Is.Empty);
    }

    [Test]
    public async Task CancelButton_Click_InvokesOnCancel()
    {
        bool cancelInvoked = false;
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.ShowCancel, true)
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true)));

        await cut.Find("button.btn-close").ClickAsync(new MouseEventArgs());

        Assert.That(cancelInvoked, Is.True);
    }

    [Test]
    public void InitialQuantity_IsUsedAsStartingValue()
    {
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.InitialQuantity, 42L));

        AngleSharp.Dom.IElement input = cut.Find("input[aria-label='Quantity']");
        Assert.That(input.GetAttribute("value"), Is.EqualTo("42"));
    }

    [Test]
    public async Task EscapeKey_WhenShowCancelTrue_InvokesOnCancel()
    {
        bool cancelInvoked = false;
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.ShowCancel, true)
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true)));

        await cut.Find("div.modal").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(cancelInvoked, Is.True);
    }

    [Test]
    public async Task EscapeKey_WhenShowCancelFalse_DoesNotInvokeOnCancel()
    {
        bool cancelInvoked = false;
        IRenderedComponent<QuantityModalComponent> cut = _ctx.Render<QuantityModalComponent>(
            parameters => parameters
                .Add(p => p.IsVisible, true)
                .Add(p => p.ShowCancel, false)
                .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelInvoked = true)));

        await cut.Find("div.modal").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(cancelInvoked, Is.False);
    }
}
