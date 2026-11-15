using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class PriorityAndColorComponentTests
{
    private BunitContext _ctx = null!;
    private int _changes;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();
        _changes = 0;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddSingleton<IPriorityService, PriorityService>();
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private IRenderedComponent<PriorityComponent> RenderPriority(ContentModel? model) =>
        _ctx.Render<PriorityComponent>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnChange, () => _changes++));

    private IRenderedComponent<ColorComponent> RenderColor(ContentModel? model) =>
        _ctx.Render<ColorComponent>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.OnChange, () => _changes++));

    [Test]
    public void Priority_NoModel_RendersNothing()
    {
        IRenderedComponent<PriorityComponent> cut = RenderPriority(null);

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void Priority_ListsThePrioritiesFromHighestToNoneWithTheirSymbols()
    {
        IRenderedComponent<PriorityComponent> cut = RenderPriority(new NoteModel { Priority = Priority.Low });

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("option").Select(x => x.GetAttribute("value")), Is.EqualTo(new[] { "VeryHigh", "High", "Medium", "Low", "VeryLow", "None" }));
            Assert.That(cut.FindAll("option").Select(x => x.TextContent), Is.EqualTo(new[] { "︽", "︿", "—", "﹀", "︾", "⊘" }));
            Assert.That(cut.Find("select").GetAttribute("value"), Is.EqualTo("Low"));
        });
    }

    [Test]
    public void Priority_SelectsTheModelsColorAsBackground()
    {
        IRenderedComponent<PriorityComponent> cut = RenderPriority(new NoteModel { Color = "bg-info-subtle" });

        Assert.That(cut.Find("select").ClassList, Is.SupersetOf(new[] { "form-select", "bg-info-subtle" }));
    }

    [Test]
    public void Priority_Change_SetsThePriorityAndReportsIt()
    {
        NoteModel note = new() { Priority = Priority.Low };
        IRenderedComponent<PriorityComponent> cut = RenderPriority(note);

        cut.Find("select").Change("VeryHigh");

        Assert.Multiple(() =>
        {
            Assert.That(note.Priority, Is.EqualTo(Priority.VeryHigh));
            Assert.That(_changes, Is.EqualTo(1));
        });
    }

    [Test]
    public void Color_NoModel_RendersNothing()
    {
        IRenderedComponent<ColorComponent> cut = RenderColor(null);

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void Color_OffersTwelveBackgroundsEachShownInItsOwnColor()
    {
        IRenderedComponent<ColorComponent> cut = RenderColor(new NoteModel { Color = "bg-body" });

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("option"), Has.Count.EqualTo(12));
            Assert.That(cut.FindAll("option").All(x => x.GetAttribute("class") == x.GetAttribute("value")), Is.True);
            Assert.That(cut.Find("select").GetAttribute("value"), Is.EqualTo("bg-body"));
        });
    }

    [Test]
    public void Color_Change_SetsTheColorShowsItAndReportsIt()
    {
        NoteModel note = new() { Color = "bg-body" };
        IRenderedComponent<ColorComponent> cut = RenderColor(note);

        cut.Find("select").Change("bg-danger-subtle");

        Assert.Multiple(() =>
        {
            Assert.That(note.Color, Is.EqualTo("bg-danger-subtle"));
            Assert.That(cut.Find("select").ClassList, Is.SupersetOf(new[] { "form-select", "bg-danger-subtle" }));
            Assert.That(_changes, Is.EqualTo(1));
        });
    }
}
