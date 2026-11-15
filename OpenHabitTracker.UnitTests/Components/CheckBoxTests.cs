using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class CheckBoxTests
{
    private BunitContext _ctx = null!;
    private IJsInterop _jsInterop = null!;
    private List<bool?> _changes = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();
        _jsInterop = Substitute.For<IJsInterop>();
        _ctx.Services.AddSingleton(_jsInterop);
        _changes = [];
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<CheckBox> Render(bool? isChecked, bool isTriState) =>
        _ctx.Render<CheckBox>(parameters => parameters
            .Add(p => p.Checked, isChecked)
            .Add(p => p.IsTriState, isTriState)
            .Add(p => p.CheckedChanged, (bool? value) => _changes.Add(value))
            .AddChildContent("Work"));

    private ValueTask ReceivedProperty(string property, bool value) =>
        _jsInterop.Received(1).SetElementProperty(Arg.Any<ElementReference>(), property, value);

    [TestCase(false, false)]
    [TestCase(true, true)]
    [TestCase(null, true)]
    public void Render_CheckedValue_SetsTheCheckedAttribute(bool? isChecked, bool rendered)
    {
        IRenderedComponent<CheckBox> cut = Render(isChecked, isTriState: true);

        Assert.That(cut.Find("input").HasAttribute("checked"), Is.EqualTo(rendered));
    }

    [Test]
    public void Render_LabelIsBoundToTheInput()
    {
        IRenderedComponent<CheckBox> cut = Render(false, isTriState: false);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("label").GetAttribute("for"), Is.EqualTo(cut.Find("input").Id));
            Assert.That(cut.Find("label").TextContent, Is.EqualTo("Work"));
        });
    }

    [Test]
    public void Render_TwoCheckBoxes_HaveDifferentIds()
    {
        IRenderedComponent<CheckBox> first = Render(false, isTriState: false);
        IRenderedComponent<CheckBox> second = Render(false, isTriState: false);

        Assert.That(first.Find("input").Id, Is.Not.EqualTo(second.Find("input").Id));
    }

    [Test]
    public async Task Render_Null_SetsIndeterminateOnce()
    {
        IRenderedComponent<CheckBox> cut = Render(null, isTriState: true);

        cut.Render(parameters => parameters.Add(p => p.Checked, null));

        await ReceivedProperty("indeterminate", true);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Render_NotNull_NeverTouchesIndeterminate(bool isChecked)
    {
        Render(isChecked, isTriState: true);

        await _jsInterop.DidNotReceive().SetElementProperty(Arg.Any<ElementReference>(), "indeterminate", Arg.Any<object>());
    }

    [Test]
    public async Task Render_NullThenChecked_ClearsIndeterminate()
    {
        IRenderedComponent<CheckBox> cut = Render(null, isTriState: true);

        cut.Render(parameters => parameters.Add(p => p.Checked, true));

        await Assert.MultipleAsync(async () =>
        {
            await ReceivedProperty("indeterminate", true);
            await ReceivedProperty("indeterminate", false);
        });
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void Change_TwoState_Toggles(bool isChecked, bool expected)
    {
        IRenderedComponent<CheckBox> cut = Render(isChecked, isTriState: false);

        cut.Find("input").Change(!isChecked);

        Assert.Multiple(() =>
        {
            Assert.That(_changes, Is.EqualTo(new bool?[] { expected }));
            Assert.That(cut.Find("input").HasAttribute("checked"), Is.EqualTo(expected));
        });
    }

    [Test]
    public void Change_TriState_CyclesUncheckedCheckedIndeterminate()
    {
        IRenderedComponent<CheckBox> cut = Render(false, isTriState: true);

        cut.Find("input").Change(true);
        cut.Find("input").Change(false);
        cut.Find("input").Change(true);

        Assert.That(_changes, Is.EqualTo(new bool?[] { true, null, false }));
    }

    [Test]
    public async Task Change_TriStateToIndeterminate_SetsIndeterminateAndKeepsTheBoxChecked()
    {
        IRenderedComponent<CheckBox> cut = Render(true, isTriState: true);

        // the browser unchecks the box on the click, but the indeterminate state shows as checked
        _jsInterop.GetElementProperty<bool>(Arg.Any<ElementReference>(), "checked").Returns(false);

        cut.Find("input").Change(false);

        await Assert.MultipleAsync(async () =>
        {
            await ReceivedProperty("indeterminate", true);
            await ReceivedProperty("checked", true);
        });
    }

    [Test]
    public async Task Change_BrowserAlreadyShowsTheNewState_DoesNotSetChecked()
    {
        IRenderedComponent<CheckBox> cut = Render(false, isTriState: false);
        _jsInterop.GetElementProperty<bool>(Arg.Any<ElementReference>(), "checked").Returns(true);

        cut.Find("input").Change(true);

        await _jsInterop.DidNotReceive().SetElementProperty(Arg.Any<ElementReference>(), "checked", Arg.Any<object>());
    }
}
