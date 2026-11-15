using System.Net.Http.Json;
using System.Text.Json;
using Markdig;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.UnitTests.Web;

// The query API reads its parameters from the query string or from a JSON body.
[TestFixture]
public class QueryControllerTests
{
    private InProcessServer _server = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public async Task SetUp()
    {
        IDataAccess dataAccess = Substitute.For<IDataAccess>();
        dataAccess.GetNotes().Returns(Task.FromResult<IReadOnlyList<NoteEntity>>(
        [
            new NoteEntity { Id = 1, Title = "Milk", Content = "**buy**", CategoryId = 1 },
            new NoteEntity { Id = 2, Title = "Bread", Content = "", CategoryId = 2 }
        ]));
        dataAccess.GetTasks().Returns(Task.FromResult<IReadOnlyList<TaskEntity>>(
        [
            new TaskEntity { Id = 3, Title = "Open" },
            new TaskEntity { Id = 4, Title = "Done", CompletedAt = new DateTime(2026, 1, 2) }
        ]));
        dataAccess.GetHabits().Returns(Task.FromResult<IReadOnlyList<HabitEntity>>(
        [
            new HabitEntity { Id = 5, Title = "Walk", RepeatCount = 1, RepeatInterval = 1 },
            new HabitEntity { Id = 6, Title = "Read", RepeatCount = 1, RepeatInterval = 1 }
        ]));
        dataAccess.GetItems().Returns(Task.FromResult<IReadOnlyList<ItemEntity>>([]));
        dataAccess.GetTimes().Returns(Task.FromResult<IReadOnlyList<TimeEntity>>([]));

        _server = await InProcessServer.Start(services =>
        {
            services.AddSingleton(dataAccess);
            services.AddSingleton(new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));
        });

        _httpClient = new HttpClient { BaseAddress = new Uri(_server.BaseUrl) };
    }

    [TearDown]
    public async Task TearDown()
    {
        _httpClient.Dispose();
        await _server.DisposeAsync();
    }

    private static async Task<List<JsonElement>> Read(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<List<JsonElement>>())!;
    }

    private static IEnumerable<string> Titles(List<JsonElement> rows) => rows.Select(x => x.GetProperty("Title").GetString()!);

    [Test]
    public async Task NotesGet_SearchTermInTheQuery_ReturnsTheMatchingNotesWithTheirContent()
    {
        List<JsonElement> notes = await Read(await _httpClient.GetAsync("api/Query/notes?SearchTerm=milk"));

        Assert.Multiple(() =>
        {
            Assert.That(Titles(notes), Is.EqualTo(new[] { "Milk" }));
            Assert.That(notes[0].GetProperty("Content").GetString(), Is.EqualTo("**buy**"));
        });
    }

    [Test]
    public async Task NotesGet_HiddenCategoryInTheQuery_LeavesItsNotesOut()
    {
        List<JsonElement> notes = await Read(await _httpClient.GetAsync("api/Query/notes?HiddenCategoryIds=1"));

        Assert.That(Titles(notes), Is.EqualTo(new[] { "Bread" }));
    }

    [Test]
    public async Task HabitsGet_SortInTheQuery_SortsByIt()
    {
        List<JsonElement> habits = await Read(await _httpClient.GetAsync("api/Query/habits?SortBy[Habit]=Title"));

        Assert.That(Titles(habits), Is.EqualTo(new[] { "Read", "Walk" }));
    }

    [Test]
    public async Task Tasks_JsonBody_FiltersByIt()
    {
        List<JsonElement> open = await Read(await _httpClient.PostAsJsonAsync("api/Query/tasks", new { HideCompletedTasks = true }));
        List<JsonElement> all = await Read(await _httpClient.GetAsync("api/Query/tasks"));

        Assert.Multiple(() =>
        {
            Assert.That(Titles(open), Is.EqualTo(new[] { "Open" }));
            Assert.That(Titles(all), Is.EquivalentTo(new[] { "Open", "Done" }));
        });
    }

    [Test]
    public async Task NotesAndHabits_JsonBody_MatchTheQueryString()
    {
        List<JsonElement> notes = await Read(await _httpClient.PostAsJsonAsync("api/Query/notes", new { SearchTerm = "bread" }));
        List<JsonElement> habits = await Read(await _httpClient.PostAsJsonAsync("api/Query/habits", new { SearchTerm = "walk" }));

        Assert.Multiple(() =>
        {
            Assert.That(Titles(notes), Is.EqualTo(new[] { "Bread" }));
            Assert.That(Titles(habits), Is.EqualTo(new[] { "Walk" }));
        });
    }
}
