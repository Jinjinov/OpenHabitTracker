using OpenHabitTracker.App;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.UnitTests.Components;

namespace OpenHabitTracker.UnitTests.App;

// An import into data that is already loaded puts the new content into the lists the pages show.
[TestFixture]
public class ClientStateMergeWiringTests
{
    private AppTestContext _app = null!;
    private ClientState _clientState = null!;

    [SetUp]
    public async Task SetUp()
    {
        _app = new AppTestContext();
        _clientState = await _app.Start();

        await _clientState.AddUserData(
        [
            new CategoryModel
            {
                Title = "Work",
                Tasks = [new TaskModel { Title = "Report", Items = [new ItemModel { Title = "Draft" }] }],
                Habits = [new HabitModel { Title = "Run", Items = [new ItemModel { Title = "Shoes" }], TimesDone = [] }]
            }
        ]);

        await _clientState.LoadTasks();
        await _clientState.LoadHabits();
        await _clientState.LoadItems();
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private async Task Import()
    {
        DateTime doneAt = new(2026, 3, 1, 8, 0, 0);

        List<CategoryModel> imported =
        [
            new CategoryModel
            {
                Title = "Work",
                Tasks =
                [
                    new TaskModel { Title = "Report", Items = [new ItemModel { Title = "Draft" }, new ItemModel { Title = "Proofread" }] },
                    new TaskModel { Title = "Invoice" }
                ],
                Habits =
                [
                    new HabitModel { Title = "Run", Items = [new ItemModel { Title = "Shoes" }, new ItemModel { Title = "Socks" }], TimesDone = [new TimeModel { StartedAt = doneAt, CompletedAt = doneAt }] },
                    new HabitModel { Title = "Swim", TimesDone = [] }
                ]
            }
        ];

        await _clientState.AddUserData(await _clientState.MergeUserData(imported));
    }

    [Test]
    public async Task Import_NewTasksAndHabits_JoinTheirLoadedCategory()
    {
        await Import();

        CategoryModel work = _clientState.Categories!.Values.Single();

        Assert.Multiple(() =>
        {
            Assert.That(work.Tasks.Select(x => x.Title), Is.EquivalentTo(new[] { "Report", "Invoice" }));
            Assert.That(work.Habits.Select(x => x.Title), Is.EquivalentTo(new[] { "Run", "Swim" }));
        });
    }

    [Test]
    public async Task Import_NewItems_JoinTheirLoadedTaskAndHabitAndTheItemList()
    {
        await Import();

        TaskModel report = _clientState.Tasks!.Values.Single(x => x.Title == "Report");
        HabitModel run = _clientState.Habits!.Values.Single(x => x.Title == "Run");

        Assert.Multiple(() =>
        {
            Assert.That(report.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Draft", "Proofread" }));
            Assert.That(run.Items!.Select(x => x.Title), Is.EqualTo(new[] { "Shoes", "Socks" }));
            Assert.That(_clientState.Items!.Values.Select(x => x.Title), Is.EquivalentTo(new[] { "Draft", "Proofread", "Shoes", "Socks" }));
        });
    }

    [Test]
    public async Task Import_NewCompletion_JoinsTheLoadedHabit()
    {
        await Import();

        HabitModel run = _clientState.Habits!.Values.Single(x => x.Title == "Run");

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(run.TimesDone, Has.Count.EqualTo(1));
            Assert.That(run.LastTimeDoneAt, Is.EqualTo(new DateTime(2026, 3, 1, 8, 0, 0)));
            Assert.That(await _app.Store.GetTimes(run.Id), Has.Count.EqualTo(1));
        });
    }
}
