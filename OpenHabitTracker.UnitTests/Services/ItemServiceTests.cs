using Markdig;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class ItemServiceTests
{
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;
    private ItemService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        MarkdownToHtml markdownToHtml = new(pipeline);
        _clientState = new(new[] { _dataAccess }, markdownToHtml);

        _sut = new(_clientState);
    }

    // --- Initialize tests ---

    [Test]
    public async Task Initialize_WhenItemsIsNull_LoadsFromDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Items = null;
        _dataAccess.GetItems(habit.Id).Returns(Task.FromResult<IReadOnlyList<ItemEntity>>(
            [new ItemEntity { Id = 5, ParentId = habit.Id, Title = "Step 1" }]));

        await _sut.Initialize(habit);

        Assert.That(habit.Items, Is.Not.Null);
        Assert.That(habit.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Initialize_WhenItemsIsNotNull_DoesNotCallDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Items = [];

        await _sut.Initialize(habit);

        await _dataAccess.DidNotReceive().GetItems(habit.Id);
    }

    [Test]
    public async Task Initialize_WhenNullItemsModel_SetsNewItemIfNull()
    {
        _sut.NewItem = null;

        await _sut.Initialize(null);

        Assert.That(_sut.NewItem, Is.Not.Null);
    }

    // --- AddItem tests ---

    [Test]
    public async Task AddItem_AddsToItemsListAndWritesToDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Items = [];
        _sut.NewItem = new ItemModel { Title = "Push-ups" };

        await _sut.AddItem(habit);

        Assert.That(habit.Items, Has.Count.EqualTo(1));
        Assert.That(habit.Items![0].Title, Is.EqualTo("Push-ups"));
        await _dataAccess.Received(1).AddItem(Arg.Any<ItemEntity>());
    }

    [Test]
    public async Task AddItem_WhenItemsListIsNull_InitializesListFirst()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Items = null;
        _sut.NewItem = new ItemModel { Title = "New" };

        await _sut.AddItem(habit);

        Assert.That(habit.Items, Is.Not.Null);
        Assert.That(habit.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AddItem_SetsParentIdFromItemsModel()
    {
        HabitModel habit = TestData.Habit(id: 42);
        habit.Items = [];
        _sut.NewItem = new ItemModel { Title = "Step" };

        await _sut.AddItem(habit);

        Assert.That(habit.Items![0].ParentId, Is.EqualTo(42));
    }

    [Test]
    public async Task AddItem_ResetsNewItemAfterAdd()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Items = [];
        _sut.NewItem = new ItemModel { Title = "Old" };

        await _sut.AddItem(habit);

        Assert.That(_sut.NewItem, Is.Not.Null);
        Assert.That(_sut.NewItem!.Title, Is.Null.Or.Empty);
    }

    [Test]
    public async Task AddItem_WhenItemsModelIsNull_DoesNothing()
    {
        _sut.NewItem = new ItemModel { Title = "New" };

        await _sut.AddItem(null);

        await _dataAccess.DidNotReceive().AddItem(Arg.Any<ItemEntity>());
    }

    // --- UpdateItem tests ---

    [Test]
    public async Task UpdateItem_UpdatesTitleOnModelAndEntity()
    {
        ItemModel item = new() { Id = 5, Title = "Old Title" };
        _sut.SelectedItem = item;
        _dataAccess.GetItem(item.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item.Id }));

        await _sut.UpdateItem("New Title");

        Assert.That(item.Title, Is.EqualTo("New Title"));
        await _dataAccess.Received(1).UpdateItem(Arg.Is<ItemEntity>(e => e.Title == "New Title"));
    }

    // --- DeleteItem tests ---

    [Test]
    public async Task DeleteItem_RemovesFromItemsListAndCallsDataAccess()
    {
        HabitModel habit = TestData.Habit(id: 1);
        ItemModel item = new() { Id = 5, Title = "Step 1" };
        habit.Items = [item];

        await _sut.DeleteItem(habit, item);

        Assert.That(habit.Items, Is.Empty);
        await _dataAccess.Received(1).RemoveItem(item.Id);
    }

    // --- ClientState.Items dict sync tests ---

    [Test]
    public async Task AddItem_AddsNewItemModel_ToClientStateItems()
    {
        HabitModel habit = TestData.Habit(id: 1);
        habit.Items = [];
        _clientState.Items = new();
        _sut.NewItem = new ItemModel { Title = "Push-ups" };

        await _sut.AddItem(habit);

        Assert.That(_clientState.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DeleteItem_RemovesItemModel_FromClientStateItems()
    {
        HabitModel habit = TestData.Habit(id: 1);
        ItemModel item = new() { Id = 5, Title = "Step 1" };
        habit.Items = [item];
        _clientState.Items = new() { { item.Id, item } };

        await _sut.DeleteItem(habit, item);

        Assert.That(_clientState.Items, Is.Empty);
    }

    // --- SetIsDone tests ---

    [Test]
    public async Task SetIsDone_WhenTrue_SetsDoneAtToNow()
    {
        ItemModel item = new() { Id = 5, Title = "Step" };
        _dataAccess.GetItem(item.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item.Id }));

        DateTime before = DateTime.Now;
        await _sut.SetIsDone(item, true);
        DateTime after = DateTime.Now;

        Assert.That(item.DoneAt, Is.InRange(before, after));
    }

    [Test]
    public async Task SetIsDone_WhenFalse_ClearsDoneAt()
    {
        ItemModel item = new() { Id = 5, Title = "Step", DoneAt = DateTime.Now.AddHours(-1) };
        _dataAccess.GetItem(item.Id).Returns(Task.FromResult<ItemEntity?>(new ItemEntity { Id = item.Id }));

        await _sut.SetIsDone(item, false);

        Assert.That(item.DoneAt, Is.Null);
    }
}
