using Bunit;
using Microsoft.AspNetCore.Components.Web;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class NotesPageTests
{
    private AppTestContext _app = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private async Task<ClientState> Seed(Action<SettingsModel>? configure = null)
    {
        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData(
        [
            new CategoryModel
            {
                Title = "Work",
                Notes =
                [
                    new NoteModel { Title = "Agenda", Content = "**Budget** first", Priority = Priority.Low },
                    new NoteModel { Title = "Urgent", Content = "call\nback", Priority = Priority.VeryHigh },
                    new NoteModel { Title = "Gone", IsDeleted = true }
                ]
            },
            new CategoryModel { Title = "", Notes = [new NoteModel { Title = "Loose", Content = "plain" }] }
        ]);

        return clientState;
    }

    private long IdOf(string title) => _app.ClientState.Notes!.Values.Single(x => x.Title == title).Id;

    private static List<string> Titles(IRenderedComponent<Notes> cut) =>
        cut.FindAll("button[id^='note-item-']").Select(x => x.TextContent.Trim()).ToList();

    private static string Content(IRenderedComponent<Notes> cut, long id) =>
        cut.Find($"#note-item-{id}").ParentElement!.ParentElement!.QuerySelector("[data-notes-step-3]")!.InnerHtml;

    [Test]
    public async Task List_ShowsTheNotesInTrashLessOrderByPriorityWithTheirMarkdownRendered()
    {
        await Seed();

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>();

        Assert.Multiple(() =>
        {
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Urgent", "Agenda", "Loose" }));
            Assert.That(Content(cut, IdOf("Agenda")), Is.EqualTo("<p><strong>Budget</strong> first</p>\n"));
            Assert.That(Content(cut, IdOf("Urgent")), Is.EqualTo("<p>call<br>\nback</p>\n"));
        });
    }

    [Test]
    public async Task DisplayAsMarkdownOff_ShowsTheRawText()
    {
        await Seed(settings => settings.DisplayNoteContentAsMarkdown = false);

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>();

        Assert.That(Content(cut, IdOf("Agenda")), Is.EqualTo("**Budget** first"));
    }

    [Test]
    public async Task SearchTerm_MatchesTheContentAndMarksItInTheRenderedMarkdown()
    {
        await Seed();
        _app.Get<ISearchFilterService>().SearchTerm = "budget";

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>();

        Assert.Multiple(() =>
        {
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Agenda" }));
            Assert.That(Content(cut, IdOf("Agenda")), Is.EqualTo("<p><strong><mark>Budget</mark></strong> first</p>\n"));
        });
    }

    [Test]
    public async Task AddNew_TitleAndContent_AreStoredAndTheContentRendersInTheList()
    {
        await Seed();

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>();

        await cut.Find("button.btn-plain.input-group").ClickAsync(new MouseEventArgs());
        cut.Find("input[placeholder='Add new note']").Input("Shopping");
        cut.Find("textarea[placeholder='Add note text']").Change("- milk");
        await cut.Find("button[aria-label='Save']").ClickAsync(new MouseEventArgs());

        NoteEntity stored = (await _app.Store.GetNotes()).Single(x => x.Title == "Shopping");

        Assert.Multiple(() =>
        {
            Assert.That(stored.Content, Is.EqualTo("- milk"));
            Assert.That(Content(cut, stored.Id), Is.EqualTo("<ul>\n<li>milk</li>\n</ul>\n"));
        });
    }

    [Test]
    public async Task GroupedByCategory_ListsEachCategoryAndTheUncategorizedLast()
    {
        await Seed(settings => settings.ShowGroupedByCategory = true);

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button.btn-plain.border-0 small").Select(x => x.TextContent.Trim()), Is.EqualTo(new[] { "Work", "Uncategorized" }));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Urgent", "Agenda", "Loose" }));
        });
    }

    [Test]
    public async Task GroupedByCategory_AHiddenCategoryIsLeftOut()
    {
        ClientState clientState = await Seed(settings => settings.ShowGroupedByCategory = true);
        clientState.Settings.HiddenCategoryIds.Add(clientState.Categories!.Values.Single().Id);

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button.btn-plain.border-0 small").Select(x => x.TextContent.Trim()), Is.EqualTo(new[] { "Uncategorized" }));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Loose" }));
        });
    }

    // Regression for b1394251: the radio-button category filter shows only the selected category.
    [TestCase(FilterDisplay.RadioButtons)]
    [TestCase(FilterDisplay.SelectOptions)]
    public async Task GroupedByCategory_ASingleSelectedCategoryIsTheOnlyGroupShown(FilterDisplay display)
    {
        ClientState clientState = await _app.Start(settings => settings.ShowGroupedByCategory = true);
        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "a" }] },
            new CategoryModel { Title = "Home", Notes = [new NoteModel { Title = "b" }] }
        ]);
        clientState.Settings.CategoryFilterDisplay = display;
        clientState.Settings.SelectedCategoryId = clientState.Categories!.Values.Single(x => x.Title == "Home").Id;
        clientState.Settings.HiddenCategoryIds.Add(clientState.Settings.SelectedCategoryId.Value);

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button.btn-plain.border-0 small").Select(x => x.TextContent.Trim()), Is.EqualTo(new[] { "Home" }));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "b" }));
        });
    }

    [Test]
    public async Task Editor_ContentChanged_IsStoredAndTheListShowsTheNewMarkdown()
    {
        await Seed();
        long agendaId = IdOf("Agenda");

        IRenderedComponent<Notes> cut = _app.Ctx.Render<Notes>(parameters => parameters.Add(p => p.Id, agendaId));

        cut.Find("#note-component textarea").Change("# Heading");

        Assert.That((await _app.Store.GetNote(agendaId))!.Content, Is.EqualTo("# Heading"));

        // the close re-renders the row from the same model
        await cut.Find("[data-notes-step-7]").ClickAsync(new MouseEventArgs());
        cut.Render(parameters => parameters.Add(p => p.Id, (long?)null));

        Assert.That(Content(cut, agendaId), Is.EqualTo("<h1 id=\"heading\">Heading</h1>\n"));
    }

    [Test]
    public async Task Editor_TitleChangedAndDeleted_AreStored()
    {
        await Seed();
        long agendaId = IdOf("Agenda");
        int closed = 0;

        NoteModel note = _app.ClientState.Notes![agendaId];
        _app.Get<INoteService>().SetSelectedNote(agendaId);

        IRenderedComponent<NoteComponent> cut = _app.Ctx.Render<NoteComponent>(parameters => parameters
            .Add(p => p.Note, note)
            .Add(p => p.CloseSelected, () => closed++));

        cut.Find("input[aria-label='Note title']").Change("Agenda v2");

        Assert.That((await _app.Store.GetNote(agendaId))!.Title, Is.EqualTo("Agenda v2"));

        await cut.Find("[data-notes-step-6]").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetNote(agendaId))!.IsDeleted, Is.True);
            Assert.That(closed, Is.EqualTo(1));
        });
    }

    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task Editor_InsertTabsSetting_DecidesWhetherTheTextareaTakesTheTabKey(bool insertTabs, int calls)
    {
        await Seed(settings => settings.InsertTabsInNoteContent = insertTabs);
        long agendaId = IdOf("Agenda");
        _app.Get<INoteService>().SetSelectedNote(agendaId);

        _app.Ctx.Render<NoteComponent>(parameters => parameters.Add(p => p.Note, _app.ClientState.Notes![agendaId]));

        await _app.JsInterop.Received(calls).HandleTabKey(Arg.Any<Microsoft.AspNetCore.Components.ElementReference>());
    }

    [Test]
    public async Task Editor_Escape_BlursAndCloses()
    {
        await Seed();
        long agendaId = IdOf("Agenda");
        int closed = 0;

        IRenderedComponent<NoteComponent> cut = _app.Ctx.Render<NoteComponent>(parameters => parameters
            .Add(p => p.Note, _app.ClientState.Notes![agendaId])
            .Add(p => p.CloseSelected, () => closed++));

        await cut.Find("#note-component").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(closed, Is.EqualTo(1));
        await _app.JsInterop.Received(1).BlurActiveElement();
    }
}
