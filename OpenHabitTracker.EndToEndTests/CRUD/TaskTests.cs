namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class TaskTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync(); // Load app and allow StartPage redirect to complete
        await Page.Locator("[data-main-step-4]").ClickAsync(); // SPA navigate to /tasks (avoids StartPage redirect)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);
    }

    [Test]
    public async Task AddTask_TypeTitle_TaskAppearsInList()
    {
        await AddItemAsync("My Task");

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "My Task" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddTask_EmptyTitle_TaskIsNotAdded()
    {
        int initialCount = await Page.Locator("[data-tasks-step-2]").CountAsync();

        await Page.Locator("button.btn-plain.input-group").ClickAsync();

        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeDisabledAsync();

        await Page.Locator("button:has(i.bi-x-square)").ClickAsync();

        await Expect(Page.Locator("[data-tasks-step-2]")).ToHaveCountAsync(initialCount);
    }

    [Test]
    public async Task MarkTaskAsDone_CheckboxClick_TaskMarkedComplete()
    {
        await AddItemAsync("Finish Report");

        // HideCompletedTasks=true by default — task disappears from list after marking done
        await Page.Locator("[data-tasks-step-4]").Filter(new LocatorFilterOptions { HasText = "Finish Report" }).Or(
            Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Finish Report" })
                .Locator("..").Locator("[data-tasks-step-4]")).First.ClickAsync();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Finish Report" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task MarkTaskAsDone_ThenUnmark_TaskShowsIncomplete()
    {
        // Disable HideCompletedTasks (in Search panel, not Settings) so task stays visible after marking done
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CloseSidebarAsync();

        await AddItemAsync("Toggle Task");

        ILocator markDoneButton = Page.Locator("[data-tasks-step-4]").First;

        await markDoneButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(markDoneButton).ToHaveClassAsync(new Regex("btn-primary"));

        await markDoneButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(markDoneButton).ToHaveClassAsync(new Regex("btn-outline-primary"));
    }

    [Test]
    public async Task DeleteTask_AfterAdd_TaskDisappearsFromList()
    {
        await AddItemAsync("Remove This");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove This" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-tasks-step-9]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove This" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteTask_MovesToTrash()
    {
        await AddItemAsync("Trashed Task");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Trashed Task" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-tasks-step-9]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Trashed Task" })).ToBeVisibleAsync();
    }
}
