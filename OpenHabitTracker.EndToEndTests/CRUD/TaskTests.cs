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
        await Expect(Page.Locator("button.btn-plain.input-group")).ToBeVisibleAsync();
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

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Finish Report" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task MarkTaskAsDone_ThenUnmark_TaskShowsIncomplete()
    {
        // Disable HideCompletedTasks (in Search panel, not Settings) so task stays visible after marking done
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await CloseSidebarAsync();

        await AddItemAsync("Toggle Task");

        ILocator markDoneButton = Page.Locator("[data-tasks-step-4]").First;

        await markDoneButton.ClickAsync();
        await Expect(markDoneButton).ToHaveClassAsync(new Regex("btn-primary"));

        await markDoneButton.ClickAsync();
        await Expect(markDoneButton).ToHaveClassAsync(new Regex("btn-outline-primary"));
    }

    [Test]
    public async Task DeleteTask_AfterAdd_TaskDisappearsFromList()
    {
        await AddItemAsync("Remove This");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove This" }).ClickAsync();

        await Page.Locator("[data-tasks-step-9]").ClickAsync();

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove This" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteTask_MovesToTrash()
    {
        await AddItemAsync("Trashed Task");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Trashed Task" }).ClickAsync();

        await Page.Locator("[data-tasks-step-9]").ClickAsync();

        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Trashed Task" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditTask_ChangesTitle_NewTitleVisible()
    {
        await AddItemAsync("Edit Me");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Edit Me" }).ClickAsync();

        await Page.Locator("input[aria-label='Task title']").FillAsync("Edited Task");
        await Page.Locator("input[aria-label='Task title']").BlurAsync();

        await Page.Locator("[data-tasks-step-10]").ClickAsync();

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Edited Task" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Edit Me" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EditTask_ChangeAllFields_CloseWorksOnFirstClick()
    {
        await AddItemAsync("Task To Edit");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Task To Edit" }).ClickAsync();

        // Change title
        await Page.Locator("input[aria-label='Task title']").FillAsync("Task Edited Title");
        await Page.Locator("input[aria-label='Task title']").BlurAsync();

        // Change planned at
        await Page.Locator("[data-tasks-step-12] input").FillAsync("2030-01-15T10:00");
        await Page.Locator("[data-tasks-step-12] input").BlurAsync();

        // Change duration hours
        await Page.Locator("select[aria-label='Duration hours']").SelectOptionAsync("1");

        // Change duration minutes
        await Page.Locator("select[aria-label='Duration minutes']").SelectOptionAsync("30");

        // Click Close once — must close the detail panel immediately (no second click required)
        await Page.Locator("[data-tasks-step-10]").ClickAsync();

        await Expect(Page.Locator("[data-tasks-step-10]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Task Edited Title" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddTask_PersistedAfterReload()
    {
        await AddItemAsync("Persistent Task");

        await Page.ReloadAsync();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();

        await NavigateToAsync("[data-main-step-4]");

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Persistent Task" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditTask_ChangeCategory_TaskMovesToNewGroup()
    {
        await CreateCategoryAsync("MoveToCategory");
        await EnableGroupedByCategoryAsync();

        // Add uncategorized task — SetUp already navigated to tasks
        await AddItemAsync("Movable Task");

        // Task should appear under Uncategorized group
        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "Uncategorized" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Task" })).ToBeVisibleAsync();

        // Open task and change category
        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Task" }).ClickAsync();
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = "MoveToCategory" });
        await Page.Locator("[data-tasks-step-10]").ClickAsync(); // Close

        // Task must appear under MoveToCategory group
        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "MoveToCategory" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Task" })).ToBeVisibleAsync();

        // Collapse MoveToCategory — task must disappear (confirms it is in that group)
        await Page.Locator("button.btn-plain.border-0:has(i.bi-tag)")
            .Filter(new LocatorFilterOptions { HasText = "MoveToCategory" })
            .ClickAsync();
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Task" })).ToHaveCountAsync(0);
    }

    // Regression guard for: bug where ChangeCategory + AddTask both called taskCategory.Tasks.Add,
    // causing the task to appear twice in grouped-by-category view.
    [Test]
    public async Task AddTask_WithCategory_AppearsExactlyOnce()
    {
        await CreateCategoryAsync("TaskOnceCategory");
        await EnableGroupedByCategoryAsync();

        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync("Once Task");
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = "TaskOnceCategory" });
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Once Task" })).ToHaveCountAsync(1);
    }
}
