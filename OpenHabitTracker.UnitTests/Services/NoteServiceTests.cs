using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class NoteServiceTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;
    private SearchFilterService _searchFilterService = null!;
    private MarkdownToHtml _markdownToHtml = null!;
    private NoteService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        _markdownToHtml = new(pipeline);
        _clientState = new(new[] { _dataAccess }, _markdownToHtml);

        _searchFilterService = new();
        _sut = new(_clientState, _searchFilterService, _markdownToHtml);
    }

    [TearDown]
    public void TearDown()
    {
        _searchFilterService.SearchTerm = null;
    }

    // --- GetNotes filter tests ---

    [Test]
    public void GetNotes_NoFilter_ReturnsAllNonDeleted()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, title: "A"),
            TestData.Note(id: 2, title: "B", isDeleted: true),
            TestData.Note(id: 3, title: "C"));

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result.Select(n => n.Id), Is.EquivalentTo(new[] { 1L, 3L }));
    }

    [Test]
    public void GetNotes_DeletedNote_IsExcluded()
    {
        _clientState.Notes = TestData.NoteDict(TestData.Note(id: 1, isDeleted: true));

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetNotes_PriorityFilter_CheckBoxes_ExcludesHiddenPriority()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, priority: Priority.High),
            TestData.Note(id: 2, priority: Priority.Low));
        _clientState.Settings.PriorityFilterDisplay = FilterDisplay.CheckBoxes;
        _clientState.Settings.ShowPriority[Priority.Low] = false;

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result.Select(n => n.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetNotes_PriorityFilter_Dropdown_ReturnsOnlyMatchingPriority()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, priority: Priority.High),
            TestData.Note(id: 2, priority: Priority.Low));
        _clientState.Settings.PriorityFilterDisplay = FilterDisplay.SelectOptions;
        _clientState.Settings.SelectedPriority = Priority.High;

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result.Select(n => n.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetNotes_SearchTerm_MatchesTitle()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, title: "Shopping List"),
            TestData.Note(id: 2, title: "Meeting Notes"));
        _searchFilterService.SearchTerm = "shopping";
        _searchFilterService.MatchCase = false;

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result.Select(n => n.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetNotes_SearchTerm_MatchesContent()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, title: "Note A", content: "Buy milk and eggs"),
            TestData.Note(id: 2, title: "Note B", content: "Call dentist"));
        _searchFilterService.SearchTerm = "milk";
        _searchFilterService.MatchCase = false;

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result.Select(n => n.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetNotes_CategoryFilter_CheckBoxes_ExcludesHiddenCategory()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, categoryId: 10),
            TestData.Note(id: 2, categoryId: 20));
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.CheckBoxes;
        _clientState.Settings.HiddenCategoryIds.Add(20);

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result.Select(n => n.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetNotes_CategoryFilter_Dropdown_ReturnsOnlyMatchingCategory()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, categoryId: 10),
            TestData.Note(id: 2, categoryId: 20));
        _clientState.Settings.CategoryFilterDisplay = FilterDisplay.SelectOptions;
        _clientState.Settings.SelectedCategoryId = 10;

        IEnumerable<NoteModel> result = _sut.GetNotes();

        Assert.That(result.Select(n => n.Id), Is.EquivalentTo(new[] { 1L }));
    }

    [Test]
    public void GetNotes_SortByTitle_ReturnsAlphabetically()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, title: "Zzz"),
            TestData.Note(id: 2, title: "Aaa"),
            TestData.Note(id: 3, title: "Mmm"));
        _clientState.Settings.SortBy[ContentType.Note] = Sort.Title;

        List<NoteModel> result = _sut.GetNotes().ToList();

        Assert.That(result.Select(n => n.Title), Is.EqualTo(new[] { "Aaa", "Mmm", "Zzz" }));
    }

    [Test]
    public void GetNotes_SortByPriority_ReturnsHighestFirst()
    {
        _clientState.Notes = TestData.NoteDict(
            TestData.Note(id: 1, priority: Priority.Low),
            TestData.Note(id: 2, priority: Priority.VeryHigh),
            TestData.Note(id: 3, priority: Priority.Medium));
        _clientState.Settings.SortBy[ContentType.Note] = Sort.Priority;

        List<NoteModel> result = _sut.GetNotes().ToList();

        Assert.That(result[0].Priority, Is.EqualTo(Priority.VeryHigh));
        Assert.That(result[1].Priority, Is.EqualTo(Priority.Medium));
        Assert.That(result[2].Priority, Is.EqualTo(Priority.Low));
    }

    // --- AddNote tests ---

    [Test]
    public async Task AddNote_SetsTimestamps_AndAddsToClientState()
    {
        _clientState.Notes = new();
        _sut.NewNote = new NoteModel { Title = "New Note", Content = "content" };

        DateTime before = DateTime.Now;
        await _sut.AddNote();
        DateTime after = DateTime.Now;

        Assert.That(_clientState.Notes, Has.Count.EqualTo(1));
        NoteModel added = _clientState.Notes.Values.Single();
        Assert.That(added.CreatedAt, Is.InRange(before, after));
        Assert.That(added.UpdatedAt, Is.InRange(before, after));
    }

    [Test]
    public async Task AddNote_SetsContentMarkdown_ViaMarkdownToHtml()
    {
        _clientState.Notes = new();
        _sut.NewNote = new NoteModel { Title = "Note", Content = "**bold**" };

        await _sut.AddNote();

        NoteModel added = _clientState.Notes.Values.Single();
        Assert.That(added.ContentMarkdown, Does.Contain("<strong>bold</strong>"));
    }

    [Test]
    public async Task AddNote_ClearsNewNoteAfterAdd()
    {
        _clientState.Notes = new();
        _sut.NewNote = new NoteModel { Title = "Note", Content = "" };

        await _sut.AddNote();

        Assert.That(_sut.NewNote, Is.Null);
    }

    // --- DeleteNote tests ---

    [Test]
    public async Task DeleteNote_SetsIsDeletedTrue()
    {
        NoteModel note = TestData.Note(id: 1);
        _clientState.Notes = TestData.NoteDict(note);
        _dataAccess.GetNote(note.Id).Returns(Task.FromResult<NoteEntity?>(new NoteEntity { Id = note.Id }));

        await _sut.DeleteNote(note);

        Assert.That(note.IsDeleted, Is.True);
    }

    [Test]
    public async Task DeleteNote_AddsToTrash_WhenTrashIsAlreadyLoaded()
    {
        NoteModel note = TestData.Note(id: 1);
        _clientState.Notes = TestData.NoteDict(note);
        _clientState.TrashedNotes = [];
        _dataAccess.GetNote(note.Id).Returns(Task.FromResult<NoteEntity?>(new NoteEntity { Id = note.Id }));

        await _sut.DeleteNote(note);

        Assert.That(_clientState.TrashedNotes, Has.Count.EqualTo(1));
        Assert.That(_clientState.TrashedNotes[0], Is.SameAs(note));
    }
}
