namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

// The browser store is IndexedDB, which only a browser has, so what a change leaves after a reload is checked here.
[TestFixture]
public class PersistenceTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
    }

    private async Task ReloadAsync()
    {
        await Page.ReloadAsync();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();
    }

    private async Task AddAndDeleteNoteAsync(string title)
    {
        await AddItemAsync(title);
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = title }).ClickAsync();
        await Page.Locator("[data-notes-step-6]").ClickAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = title })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task HabitMarkedDone_StaysDoneAfterReload()
    {
        await OpenSidebarAsync("bi-gear");
        await Page.Locator("label[for='ShowSmallCalendar']").ClickAsync();
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Persisted Stretch");

        ILocator habitRow = Page.Locator("div.input-group.flex-nowrap").Filter(new LocatorFilterOptions { Has = Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Persisted Stretch" }) });

        await habitRow.Locator("[data-habits-step-4]").ClickAsync();
        await Expect(habitRow.Locator("[data-habits-step-3]")).ToContainTextAsync("0 m");

        await WaitForIndexedDbAsync("TimeEntity", "times => times.length == 1 && (times[0].completedAt ?? times[0].CompletedAt) != null");
        await WaitForIndexedDbAsync("HabitEntity", "habits => habits.some(habit => (habit.title ?? habit.Title) == 'Persisted Stretch' && (habit.lastTimeDoneAt ?? habit.LastTimeDoneAt) != null)");

        await ReloadAsync();
        await NavigateToAsync("[data-main-step-5]");

        await Expect(habitRow.Locator("[data-habits-step-3]")).ToContainTextAsync("0 m");
    }

    [Test]
    public async Task TaskMarkedDone_StaysDoneAfterReload()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Persisted Report");

        ILocator markDone = Page.Locator("[data-tasks-step-4]").First;

        await markDone.ClickAsync();
        await Expect(markDone).ToHaveClassAsync(new Regex("btn-primary"));

        await WaitForIndexedDbAsync("TaskEntity", "tasks => tasks.some(task => (task.title ?? task.Title) == 'Persisted Report' && (task.completedAt ?? task.CompletedAt) != null)");
        await WaitForIndexedDbAsync("SettingsEntity", "settings => settings.some(setting => (setting.hideCompletedTasks ?? setting.HideCompletedTasks) === false)");

        await ReloadAsync();
        await NavigateToAsync("[data-main-step-4]");

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Persisted Report" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-tasks-step-4]").First).ToHaveClassAsync(new Regex("btn-primary"));
    }

    // The timer counts once a second, and Stop after a pause completes the task.
    [Test]
    public async Task TaskTimer_CountsAndStopCompletesTheTaskAfterReload()
    {
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Timed Report");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Timed Report" }).ClickAsync();
        await Page.Locator("[data-tasks-step-14] button[aria-label='Start']").ClickAsync();

        ILocator elapsed = Page.Locator("[data-tasks-step-14] span");

        await Expect(elapsed).ToHaveTextAsync(new Regex("^00:00:0[1-9]$"));

        await Page.Locator("[data-tasks-step-14] button[aria-label='Pause']").ClickAsync();
        await Expect(Page.Locator("[data-tasks-step-14] button[aria-label='Resume']")).ToBeVisibleAsync();

        await Page.Locator("[data-tasks-step-14] button[aria-label='Stop']").ClickAsync();

        await Expect(Page.Locator("[data-tasks-step-16]")).ToBeVisibleAsync();

        await WaitForIndexedDbAsync("TaskEntity", "tasks => tasks.some(task => (task.title ?? task.Title) == 'Timed Report' && (task.startedAt ?? task.StartedAt) != null && (task.completedAt ?? task.CompletedAt) != null)");

        await ReloadAsync();

        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-4]");

        await Expect(Page.Locator("[data-tasks-step-4]").First).ToHaveClassAsync(new Regex("btn-primary"));
    }

    [Test]
    public async Task DeletedNote_StaysInTheTrashAfterReload()
    {
        await NavigateToAsync("[data-main-step-3]");
        await AddAndDeleteNoteAsync("Persisted Trash");

        await WaitForIndexedDbAsync("NoteEntity", "notes => notes.some(note => (note.title ?? note.Title) == 'Persisted Trash' && (note.isDeleted ?? note.IsDeleted) === true)");

        await ReloadAsync();
        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Persisted Trash" })).ToHaveCountAsync(0);

        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Persisted Trash" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RestoredNote_StaysRestoredAfterReload()
    {
        await NavigateToAsync("[data-main-step-3]");
        await AddAndDeleteNoteAsync("Persisted Restore");

        await OpenSidebarAsync("bi-trash");
        await Page.Locator("div.input-group:has(span:text('Persisted Restore')) button:has(i.bi-recycle)").ClickAsync();
        await CloseSidebarAsync();

        await WaitForIndexedDbAsync("NoteEntity", "notes => notes.some(note => (note.title ?? note.Title) == 'Persisted Restore' && (note.isDeleted ?? note.IsDeleted) === false)");

        await ReloadAsync();
        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Persisted Restore" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EmptiedTrash_StaysEmptyAfterReloadAndKeepsLiveNotes()
    {
        await NavigateToAsync("[data-main-step-3]");
        await AddItemAsync("Persisted Live");
        await AddAndDeleteNoteAsync("Persisted Gone");

        await OpenSidebarAsync("bi-trash");
        await Page.Locator("[data-trash-step-2]").ClickAsync();
        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Persisted Gone" })).ToHaveCountAsync(0);
        await CloseSidebarAsync();

        await WaitForIndexedDbAsync("NoteEntity", "notes => !notes.some(note => (note.title ?? note.Title) == 'Persisted Gone')");

        await ReloadAsync();

        await OpenSidebarAsync("bi-trash");
        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Persisted Gone" })).ToHaveCountAsync(0);
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-3]");
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Persisted Live" })).ToBeVisibleAsync();
    }

    // The welcome note is added only on the first run, which a store with settings no longer is.
    [Test]
    public async Task DeleteAllData_StaysEmptyAfterReload()
    {
        await NavigateToAsync("[data-main-step-3]");
        await AddItemAsync("Persisted Deleted");

        await OpenSidebarAsync("bi-database");
        await Page.Locator("[data-data-step-2]").ClickAsync();
        await CloseSidebarAsync();

        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(0);

        await WaitForIndexedDbAsync("NoteEntity", "notes => notes.length == 0");
        await WaitForIndexedDbAsync("SettingsEntity", "settings => settings.length == 1");

        await ReloadAsync();
        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(0);
    }
}
