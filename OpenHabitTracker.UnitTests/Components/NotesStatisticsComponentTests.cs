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
public class NotesStatisticsComponentTests
{
    private BunitContext _ctx = null!;
    private INoteService _noteService = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _noteService = Substitute.For<INoteService>();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _ctx.Services.AddScoped(_ => _noteService);
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void NotesIsNull_RendersNothing()
    {
        _noteService.Notes.Returns((IReadOnlyCollection<NoteModel>?)null);

        IRenderedComponent<NotesStatisticsComponent> cut = _ctx.Render<NotesStatisticsComponent>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void EmptyNotesList_RendersNothing()
    {
        _noteService.Notes.Returns(new List<NoteModel>());
        _noteService.GetNotes().Returns(Enumerable.Empty<NoteModel>());

        IRenderedComponent<NotesStatisticsComponent> cut = _ctx.Render<NotesStatisticsComponent>();

        Assert.That(cut.Markup.Trim(), Is.Empty);
    }

    [Test]
    public void WithTwoNotes_ShowsTotalTwo()
    {
        List<NoteModel> notes = new() { TestData.Note(id: 1), TestData.Note(id: 2) };
        _noteService.Notes.Returns(notes);
        _noteService.GetNotes().Returns(notes);

        IRenderedComponent<NotesStatisticsComponent> cut = _ctx.Render<NotesStatisticsComponent>();

        Assert.That(cut.Find("span.badge.bg-body-secondary").TextContent, Is.EqualTo("Total: 2"));
    }

    [Test]
    public void WithHighPriorityNote_ShowsPriorityBadge()
    {
        NoteModel note = TestData.Note(id: 1, priority: Priority.High);
        List<NoteModel> notes = new() { note };
        _noteService.Notes.Returns(notes);
        _noteService.GetNotes().Returns(notes);

        IRenderedComponent<NotesStatisticsComponent> cut = _ctx.Render<NotesStatisticsComponent>();

        Assert.That(cut.Markup, Does.Contain("High"));
    }

    [Test]
    public void PageStateChanged_ReadsServiceOnEachRender_ShowsFreshCount()
    {
        // First render: 1 note
        NoteModel note = TestData.Note(id: 1);
        List<NoteModel> oneNote = new() { note };
        _noteService.Notes.Returns(oneNote);
        _noteService.GetNotes().Returns(oneNote);

        IRenderedComponent<NotesStatisticsComponent> firstCut = _ctx.Render<NotesStatisticsComponent>(
            p => p.Add(c => c.PageStateChanged, false));

        Assert.That(firstCut.Find("span.badge.bg-body-secondary").TextContent, Is.EqualTo("Total: 1"));

        // Second render: 2 notes — component must not cache from first render
        NoteModel secondNote = TestData.Note(id: 2);
        List<NoteModel> twoNotes = new() { note, secondNote };
        _noteService.Notes.Returns(twoNotes);
        _noteService.GetNotes().Returns(twoNotes);

        IRenderedComponent<NotesStatisticsComponent> secondCut = _ctx.Render<NotesStatisticsComponent>(
            p => p.Add(c => c.PageStateChanged, true));

        Assert.That(secondCut.Find("span.badge.bg-body-secondary").TextContent, Is.EqualTo("Total: 2"));
    }
}
