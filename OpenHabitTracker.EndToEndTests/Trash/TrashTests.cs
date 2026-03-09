namespace OpenHabitTracker.EndToEndTests.Trash;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class TrashTests : BaseTest
{
    [SetUp]
    public async Task SetUp() => await GotoAsync("notes");

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
        await GotoAsync("notes");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Restore Me" })).ToBeVisibleAsync();
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
