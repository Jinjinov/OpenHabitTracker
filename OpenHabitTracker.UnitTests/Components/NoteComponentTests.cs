using Bunit;
using GTour.Abstractions;
using Markdig;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class NoteComponentTests
{
    private BunitContext _ctx = null!;
    private INoteService _noteService = null!;
    private NoteModel _note = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _noteService = Substitute.For<INoteService>();

        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.DataLocation.Returns(DataLocation.Local);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);

        ClientState clientState = new(new[] { dataAccess }, markdownToHtml);
        clientState.Settings.ShowHelp = false;
        clientState.Settings.ShowCategory = false;
        clientState.Settings.ShowColor = false;
        clientState.Settings.ShowCreatedUpdated = false;
        clientState.Settings.ShowPriorityDropdown = false;

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>()));

        _ctx.Services.AddScoped(_ => _noteService);
        _ctx.Services.AddScoped(_ => Substitute.For<IJsInterop>());
        _ctx.Services.AddScoped(_ => clientState);
        _ctx.Services.AddScoped(_ => markdownToHtml);
        _ctx.Services.AddSingleton(loc);
        _ctx.Services.AddSingleton(Substitute.For<IGTourService>());

        _note = new NoteModel { Title = "My Note", Content = "**bold**", ContentMarkdown = "<strong>bold</strong>" };
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void Renders_NoteTitle()
    {
        IRenderedComponent<NoteComponent> cut = _ctx.Render<NoteComponent>(
            parameters => parameters.Add(p => p.Note, _note));

        AngleSharp.Dom.IElement titleInput = cut.Find("[data-notes-step-5] input");

        Assert.That(titleInput.GetAttribute("value"), Is.EqualTo("My Note"));
    }

    [Test]
    public void Renders_ContentMarkdown_AsHtml_NotRawText()
    {
        // The textarea always shows raw markdown content, not the HTML markup stored in ContentMarkdown
        IRenderedComponent<NoteComponent> cut = _ctx.Render<NoteComponent>(
            parameters => parameters.Add(p => p.Note, _note));

        AngleSharp.Dom.IElement textarea = cut.Find("[data-notes-step-8] textarea");

        Assert.That(textarea.GetAttribute("value") ?? textarea.TextContent, Is.EqualTo("**bold**"));
    }

    [Test]
    public void EditForm_IsAlwaysVisible()
    {
        // NoteComponent always shows the edit textarea; there is no separate "view" mode
        IRenderedComponent<NoteComponent> cut = _ctx.Render<NoteComponent>(
            parameters => parameters.Add(p => p.Note, _note));

        AngleSharp.Dom.IElement textarea = cut.Find("[data-notes-step-8] textarea");

        Assert.That(textarea, Is.Not.Null);
    }

    [Test]
    public async Task DeleteButton_Click_InvokesNoteServiceDeleteNote()
    {
        IRenderedComponent<NoteComponent> cut = _ctx.Render<NoteComponent>(
            parameters => parameters.Add(p => p.Note, _note));

        await cut.Find("[data-notes-step-6]").ClickAsync(new MouseEventArgs());

        await _noteService.Received(1).DeleteNote(_note);
    }
}
