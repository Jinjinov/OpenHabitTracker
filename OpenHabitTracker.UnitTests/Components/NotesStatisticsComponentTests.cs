using Bunit;
using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
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
    private ClientState _clientState = null!;
    private ICategoryService _categoryService = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _noteService = Substitute.For<INoteService>();

        IPriorityService priorityService = Substitute.For<IPriorityService>();
        priorityService.GetPriorityName(Arg.Any<Priority>()).Returns(callInfo => callInfo.Arg<Priority>().ToString());

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        _clientState = new(new[] { dataAccess }, markdownToHtml);

        _categoryService = Substitute.For<ICategoryService>();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _ctx.Services.AddScoped(_ => _noteService);
        _ctx.Services.AddScoped(_ => priorityService);
        _ctx.Services.AddScoped(_ => _clientState);
        _ctx.Services.AddScoped(_ => _categoryService);
        _ctx.Services.AddScoped<ISearchFilterService>(_ => new SearchFilterService());
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

        Assert.That(cut.Find("p.mb-1").TextContent, Is.EqualTo("Total: 2"));
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

        Assert.That(firstCut.Find("p.mb-1").TextContent, Is.EqualTo("Total: 1"));

        // Second render: 2 notes — component must not cache from first render
        NoteModel secondNote = TestData.Note(id: 2);
        List<NoteModel> twoNotes = new() { note, secondNote };
        _noteService.Notes.Returns(twoNotes);
        _noteService.GetNotes().Returns(twoNotes);

        IRenderedComponent<NotesStatisticsComponent> secondCut = _ctx.Render<NotesStatisticsComponent>(
            p => p.Add(c => c.PageStateChanged, true));

        Assert.That(secondCut.Find("p.mb-1").TextContent, Is.EqualTo("Total: 2"));
    }

    // --- grouped by category: a group counts what the list shows ---

    private void SetUpOneCategory(long id, string title, params NoteModel[] notes)
    {
        CategoryModel category = new() { Id = id, Title = title, Notes = notes.ToList() };

        _clientState.Settings.ShowGroupedByCategory = true;
        _clientState.Categories = new() { { id, category } };
        _clientState.Notes = notes.ToDictionary(x => x.Id);
        _categoryService.Categories.Returns(_clientState.Categories.Values);
        _noteService.Notes.Returns(_clientState.Notes.Values);
        _noteService.GetNotes().Returns(_clientState.Notes.Values.Where(x => !x.IsDeleted));
    }

    [Test]
    public void Grouped_TrashedNote_IsNotCounted()
    {
        SetUpOneCategory(1, "Work",
            new NoteModel { Id = 1, Title = "Live", CategoryId = 1 },
            new NoteModel { Id = 2, Title = "Trashed", CategoryId = 1, IsDeleted = true });

        IRenderedComponent<NotesStatisticsComponent> cut = _ctx.Render<NotesStatisticsComponent>();

        Assert.That(cut.Find("p.mb-1").TextContent, Is.EqualTo("Total: 1"));
    }

    // A category unchecked earlier, while the filter showed check boxes, is still selectable as a radio button.
    [Test]
    public void Grouped_RadioCategoryFilter_ShowsTheSelectedCategory()
    {
        SetUpOneCategory(1, "Work", new NoteModel { Id = 1, Title = "Live", CategoryId = 1 });
        _clientState.Settings.HiddenCategoryIds.Add(1);
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.RadioButtons;
        _clientState.Settings.SelectedCategoryId = 1;

        IRenderedComponent<NotesStatisticsComponent> cut = _ctx.Render<NotesStatisticsComponent>();

        Assert.That(cut.Markup, Does.Contain("Work"));
    }
}
