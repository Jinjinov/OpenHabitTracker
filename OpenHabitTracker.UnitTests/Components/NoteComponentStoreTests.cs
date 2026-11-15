using Bunit;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class NoteComponentStoreTests
{
    private AppTestContext _app = null!;
    private NoteModel _note = null!;

    private async Task StartWith(Action<SettingsModel> configure)
    {
        _app = new AppTestContext();

        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData([new CategoryModel { Title = "Work", Notes = [new NoteModel { Title = "Agenda", Priority = Priority.Low, Color = "bg-body" }] }, new CategoryModel { Title = "Home" }]);

        INoteService noteService = _app.Get<INoteService>();
        await noteService.Initialize();

        _note = clientState.Notes!.Values.Single();
        noteService.SetSelectedNote(_note.Id);
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<NoteComponent> Render() => _app.Ctx.Render<NoteComponent>(parameters => parameters.Add(p => p.Note, _note));

    private async Task<NoteEntity> Stored() => (await _app.Store.GetNote(_note.Id))!;

    [Test]
    public async Task PriorityCategoryAndColor_Changed_AreStored()
    {
        await StartWith(settings => { settings.ShowPriorityDropdown = true; settings.ShowCategory = true; settings.ShowColor = true; });
        long home = _app.ClientState.Categories!.Values.Single(x => x.Title == "Home").Id;

        IRenderedComponent<NoteComponent> cut = Render();

        cut.Find("[data-notes-step-4] select").Change(nameof(Priority.VeryHigh));
        cut.Find("[data-notes-step-9] select").Change(home.ToString());
        cut.Find("[data-notes-step-10] select").Change("bg-warning-subtle");

        NoteEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That((stored.Priority, stored.CategoryId, stored.Color), Is.EqualTo((Priority.VeryHigh, home, "bg-warning-subtle")));
            Assert.That(cut.Find("#note-component").ClassList, Does.Contain("bg-warning-subtle"));
        });
    }

    [Test]
    public async Task Settings_HideOrShowTheOptionalRows()
    {
        await StartWith(settings => { settings.ShowPriorityDropdown = false; settings.ShowCategory = false; settings.ShowColor = false; settings.ShowCreatedUpdated = true; });

        IRenderedComponent<NoteComponent> cut = Render();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("[data-notes-step-4]"), Is.Empty);
            Assert.That(cut.FindAll("[data-notes-step-9]"), Is.Empty);
            Assert.That(cut.FindAll("[data-notes-step-10]"), Is.Empty);
            Assert.That(cut.Find("[data-notes-step-11]").TextContent, Does.Contain($"Created: {_note.CreatedAt}").And.Contain($"Updated: {_note.UpdatedAt}"));
        });
    }

    [Test]
    public async Task CreatedUpdatedOff_HidesTheDates()
    {
        await StartWith(settings => settings.ShowCreatedUpdated = false);

        IRenderedComponent<NoteComponent> cut = Render();

        Assert.That(cut.FindAll("[data-notes-step-11]"), Is.Empty);
    }
}
