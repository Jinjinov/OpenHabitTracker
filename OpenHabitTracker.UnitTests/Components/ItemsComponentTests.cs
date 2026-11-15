using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class ItemsComponentTests
{
    private AppTestContext _app = null!;
    private TaskModel _task = null!;

    [SetUp]
    public async Task SetUp()
    {
        _app = new AppTestContext();

        ClientState clientState = await _app.Start();

        await clientState.AddUserData([new CategoryModel { Title = "Work", Tasks = [new TaskModel { Title = "Report", Color = "bg-info-subtle", Items = [new ItemModel { Title = "Draft" }, new ItemModel { Title = "Proofread" }] }] }]);

        _task = clientState.Tasks!.Values.Single();
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<ItemsComponent> Render(bool isReadOnly) =>
        _app.Ctx.Render<ItemsComponent>(parameters => parameters
            .Add(p => p.Items, _task)
            .Add(p => p.IsReadOnly, isReadOnly));

    private async Task<IReadOnlyList<ItemEntity>> StoredItems() => await _app.Store.GetItems(_task.Id);

    [Test]
    public void Render_UsesTheColorOfItsOwner()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: true);

        Assert.That(cut.Find("div").ClassList, Is.EqualTo(new[] { "bg-info-subtle" }));
    }

    [Test]
    public void Render_ItemsNotLoadedYet_ReadsThemFromTheStore()
    {
        _task.Items = null;

        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: true);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("label").Select(x => x.TextContent), Is.EqualTo(new[] { "Draft", "Proofread" }));
            Assert.That(_app.ClientState.Items!.Values.Select(x => x.Title), Is.SupersetOf(new[] { "Draft", "Proofread" }));
        });
    }

    [Test]
    public void ReadOnly_ShowsLabelledCheckBoxesWithoutEditingControls()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: true);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("input[type='checkbox']").Select(x => x.Id), Is.EqualTo(_task.Items!.Select(x => $"item-{x.Id}")));
            Assert.That(cut.FindAll("label").Select(x => x.GetAttribute("for")), Is.EqualTo(_task.Items!.Select(x => $"item-{x.Id}")));
            Assert.That(cut.FindAll("button"), Is.Empty);
            Assert.That(cut.FindAll("form"), Is.Empty);
        });
    }

    [Test]
    public async Task ReadOnly_TickAndUntick_StoresAndClearsTheDoneTime()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: true);
        long id = _task.Items![1].Id;

        cut.Find($"#item-{id}").Change(true);

        Assert.That((await StoredItems()).Single(x => x.Id == id).DoneAt, Is.Not.Null);

        cut.Find($"#item-{id}").Change(false);

        Assert.That((await StoredItems()).Select(x => x.DoneAt), Is.All.Null);
    }

    [Test]
    public void Search_MarksTheMatchInTheItemTitle()
    {
        _app.Get<ISearchFilterService>().SearchTerm = "raf";

        IRenderedComponent<ItemsComponent> read = Render(isReadOnly: true);
        IRenderedComponent<ItemsComponent> edit = Render(isReadOnly: false);

        Assert.Multiple(() =>
        {
            Assert.That(read.FindAll("label")[0].InnerHtml, Is.EqualTo("D<mark>raf</mark>t"));
            Assert.That(edit.FindAll("button.highlightable")[0].InnerHtml, Is.EqualTo("D<mark>raf</mark>t"));
        });
    }

    [Test]
    public void Add_DisabledWhileTheTitleIsBlank()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: false);

        Assert.That(cut.Find("button[aria-label='Add']").HasAttribute("disabled"), Is.True);

        cut.Find("input[placeholder='Add new item']").Input("   ");

        Assert.That(cut.Find("button[aria-label='Add']").HasAttribute("disabled"), Is.True);

        cut.Find("input[placeholder='Add new item']").Input("Send");

        Assert.That(cut.Find("button[aria-label='Add']").HasAttribute("disabled"), Is.False);
    }

    [Test]
    public async Task Add_BlankTitleSubmitted_StoresNothing()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: false);

        cut.Find("input[placeholder='Add new item']").Input("   ");
        await cut.Find("form").SubmitAsync();

        Assert.That((await StoredItems()).Select(x => x.Title), Is.EqualTo(new[] { "Draft", "Proofread" }));
    }

    [Test]
    public async Task Add_StoresTheItemAndClearsTheField()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: false);

        cut.Find("input[placeholder='Add new item']").Input("Send");
        await cut.Find("form").SubmitAsync();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredItems()).Select(x => (x.Title, x.ParentId)), Is.EqualTo(new[] { ("Draft", _task.Id), ("Proofread", _task.Id), ("Send", _task.Id) }));
            Assert.That(cut.Find("input[placeholder='Add new item']").GetAttribute("value"), Is.Null.Or.Empty);
            Assert.That(cut.FindAll("button.highlightable").Select(x => x.TextContent), Is.EqualTo(new[] { "Draft", "Proofread", "Send" }));
        });
    }

    [Test]
    public async Task Select_OpensTheEditFieldAndFocusesIt()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: false);

        await cut.FindAll("button.highlightable")[1].ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("input[aria-label='Edit item']").GetAttribute("value"), Is.EqualTo("Proofread"));
            Assert.That(cut.FindAll("button.highlightable").Select(x => x.TextContent), Is.EqualTo(new[] { "Draft" }));
            _app.Ctx.JSInterop.VerifyFocusAsyncInvoke();
        });
    }

    [Test]
    public async Task EditField_FocusLeft_ShowsTheTitleAgain()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: false);

        await cut.FindAll("button.highlightable")[0].ClickAsync(new MouseEventArgs());
        cut.Find("input[aria-label='Edit item']").FocusOut();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("input[aria-label='Edit item']"), Is.Empty);
            Assert.That(_app.Get<IItemService>().SelectedItem, Is.Null);
            Assert.That(cut.FindAll("button.highlightable").Select(x => x.TextContent), Is.EqualTo(new[] { "Draft", "Proofread" }));
        });
    }

    [Test]
    public async Task Delete_RemovesOnlyThatItem()
    {
        IRenderedComponent<ItemsComponent> cut = Render(isReadOnly: false);

        await cut.FindAll("button[aria-label='Delete']")[0].ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await StoredItems()).Select(x => x.Title), Is.EqualTo(new[] { "Proofread" }));
            Assert.That(cut.FindAll("button.highlightable").Select(x => x.TextContent), Is.EqualTo(new[] { "Proofread" }));
        });
    }
}
