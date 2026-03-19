namespace OpenHabitTracker.EndToEndTests.Trash;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class TrashTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-3]");
    }

    private async Task AddAndDeleteNoteAsync(string title)
    {
        await AddItemAsync(title);
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = title }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("[data-notes-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task DeletedNote_AppearsInTrash()
    {
        await AddAndDeleteNoteAsync("Trashed");

        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Trashed" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RestoreNote_FromTrash_ReappearsInNotesList()
    {
        await AddAndDeleteNoteAsync("Restore Me");

        await OpenSidebarAsync("bi-trash");

        // Individual restore button: button with bi-recycle icon next to "Restore Me" span
        await Page.Locator("div.input-group:has(span:text('Restore Me')) button:has(i.bi-recycle)").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Restore Me" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RestoreHabit_FromTrash_ReappearsInHabitsList()
    {
        await NavigateToAsync("[data-main-step-5]");

        await AddItemAsync("Restore Habit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Restore Habit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-habits-step-10]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("div.input-group:has(span:text('Restore Habit')) button:has(i.bi-recycle)").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-5]");

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Restore Habit" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RestoreTask_FromTrash_ReappearsInTasksList()
    {
        await NavigateToAsync("[data-main-step-4]");

        await AddItemAsync("Restore Task");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Restore Task" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-tasks-step-9]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("div.input-group:has(span:text('Restore Task')) button:has(i.bi-recycle)").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-4]");

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Restore Task" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task PermanentDelete_Note_RemovedFromTrash()
    {
        await AddAndDeleteNoteAsync("Permanent Delete");

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("button[aria-label='Delete: Permanent Delete']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Permanent Delete" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EmptyTrash_ClearsAllDeletedItems()
    {
        await AddAndDeleteNoteAsync("First Note");
        await AddAndDeleteNoteAsync("Second Note");

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("[data-trash-step-2]").ClickAsync(); // Empty trash
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "First Note" })).ToHaveCountAsync(0);
        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Second Note" })).ToHaveCountAsync(0);
    }
}
