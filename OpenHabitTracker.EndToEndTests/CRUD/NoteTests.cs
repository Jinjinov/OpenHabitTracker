namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class NoteTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-3]");
    }

    [Test]
    public async Task AddNote_TypeTitle_NoteAppearsInList()
    {
        await AddItemAsync("My Note");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "My Note" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddNote_EmptyTitle_NoteIsNotAdded()
    {
        int initialCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        await Page.Locator("button.btn-plain.input-group").ClickAsync();

        // Save button is disabled when title is empty
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeDisabledAsync();

        await Page.Locator("button:has(i.bi-x-square)").ClickAsync();

        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(initialCount);
    }

    [Test]
    public async Task DeleteNote_AfterAdd_NoteDisappearsFromList()
    {
        await AddItemAsync("Delete Me");

        // Open note detail (second column at 1920px width)
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Delete Me" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-notes-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Delete Me" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteNote_MovesToTrash()
    {
        await AddItemAsync("Trashed Note");

        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Trashed Note" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-notes-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open Trash sidebar
        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Trashed Note" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditNote_ChangesTitle_NewTitleVisible()
    {
        await AddItemAsync("Original");

        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Original" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // data-notes-step-5 wraps the title input in NoteComponent
        await Page.Locator("[data-notes-step-5] input").FillAsync("Updated");
        await Page.Locator("[data-notes-step-5] input").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-notes-step-7]").ClickAsync(); // Close note detail
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Updated" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Original" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EditNote_ChangeAllFields_CloseWorksOnFirstClick()
    {
        await AddItemAsync("Note To Edit");

        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Note To Edit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change title
        await Page.Locator("[data-notes-step-5] input").FillAsync("Note Edited Title");
        await Page.Locator("[data-notes-step-5] input").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change content
        await Page.Locator("[data-notes-step-8] textarea").FillAsync("Note edited content text");
        await Page.Locator("[data-notes-step-8] textarea").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click Close once — must close the detail panel immediately (no second click required)
        await Page.Locator("[data-notes-step-7]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-notes-step-7]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Note Edited Title" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddNote_PersistedAfterReload()
    {
        await AddItemAsync("Persistent Note");

        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Persistent Note" })).ToBeVisibleAsync();
    }
}
