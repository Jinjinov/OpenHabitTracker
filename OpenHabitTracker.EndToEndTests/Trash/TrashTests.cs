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
        await Page.Locator("[data-notes-step-6]").ClickAsync();
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

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Restore Me" })).ToHaveCountAsync(0);

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

        await Page.Locator("[data-habits-step-10]").ClickAsync();

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("div.input-group:has(span:text('Restore Habit')) button:has(i.bi-recycle)").ClickAsync();

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

        await Page.Locator("[data-tasks-step-9]").ClickAsync();

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("div.input-group:has(span:text('Restore Task')) button:has(i.bi-recycle)").ClickAsync();

        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-4]");

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Restore Task" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RestoreNote_FromTrash_ReturnsToOriginalCategoryGroup()
    {
        await CreateCategoryAsync("RestoreCategory");
        await EnableGroupedByCategoryAsync();

        // Add a note assigned to RestoreCategory
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync("Category Restore Note");
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = "RestoreCategory" });
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToHaveCountAsync(0);

        // Delete the note
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Category Restore Note" }).ClickAsync();
        await Page.Locator("[data-notes-step-6]").ClickAsync();

        // Restore from trash
        await OpenSidebarAsync("bi-trash");
        await Page.Locator("div.input-group:has(span:text('Category Restore Note')) button:has(i.bi-recycle)").ClickAsync();
        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Category Restore Note" })).ToHaveCountAsync(0);
        await CloseSidebarAsync();

        // Note must be visible and in the RestoreCategory group (not uncategorized)
        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "RestoreCategory" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Category Restore Note" })).ToBeVisibleAsync();

        // Collapse the category — note must disappear (confirms it is in that group, not uncategorized)
        await Page.Locator("button.btn-plain.border-0:has(i.bi-tag)")
            .Filter(new LocatorFilterOptions { HasText = "RestoreCategory" })
            .ClickAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Category Restore Note" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task PermanentDelete_Note_RemovedFromTrash()
    {
        await AddAndDeleteNoteAsync("Permanent Delete");

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("button[aria-label='Delete: Permanent Delete']").ClickAsync();

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Permanent Delete" })).ToHaveCountAsync(0);

        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Permanent Delete" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EmptyTrash_ClearsAllDeletedItems()
    {
        await AddItemAsync("Live Note"); // not deleted — must survive EmptyTrash
        await AddAndDeleteNoteAsync("First Note");
        await AddAndDeleteNoteAsync("Second Note");

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("[data-trash-step-2]").ClickAsync(); // Empty trash

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "First Note" })).ToHaveCountAsync(0);
        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Second Note" })).ToHaveCountAsync(0);

        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Live Note" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RestoreAll_AllItemsReappearsInLists()
    {
        await AddAndDeleteNoteAsync("RestoreAll Note");

        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("RestoreAll Task");
        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "RestoreAll Task" }).ClickAsync();
        await Page.Locator("[data-tasks-step-9]").ClickAsync();

        await OpenSidebarAsync("bi-trash");

        await Page.Locator("[data-trash-step-1]").ClickAsync(); // Restore all

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "RestoreAll Note" })).ToHaveCountAsync(0);
        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "RestoreAll Task" })).ToHaveCountAsync(0);

        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-3]");
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "RestoreAll Note" })).ToBeVisibleAsync();

        await NavigateToAsync("[data-main-step-4]");
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "RestoreAll Task" })).ToBeVisibleAsync();
    }
}
