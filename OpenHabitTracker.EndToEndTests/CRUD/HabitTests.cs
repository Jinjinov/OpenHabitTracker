namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class HabitTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync(); // Load app and allow StartPage redirect to complete
        await Page.Locator("[data-main-step-5]").ClickAsync(); // SPA navigate to /habits (avoids StartPage redirect)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);
    }

    [Test]
    public async Task AddHabit_TypeTitle_HabitAppearsInList()
    {
        await AddItemAsync("Morning Run");

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Morning Run" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddHabit_EmptyTitle_HabitIsNotAdded()
    {
        int initialCount = await Page.Locator("[data-habits-step-2]").CountAsync();

        await Page.Locator("button.btn-plain.input-group").ClickAsync();

        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeDisabledAsync();

        await Page.Locator("button:has(i.bi-x-square)").ClickAsync();

        await Expect(Page.Locator("[data-habits-step-2]")).ToHaveCountAsync(initialCount);
    }

    [Test]
    public async Task MarkHabitAsDone_ButtonClick_ElapsedTimeUpdates()
    {
        // Disable ShowSmallCalendar so the Mark as done button (data-habits-step-4) becomes visible in the list
        await OpenSidebarAsync("bi-gear");
        await Page.Locator("label[for='ShowSmallCalendar']").ClickAsync();
        await CloseSidebarAsync();

        await AddItemAsync("Daily Stretch");

        ILocator habitRow = Page.Locator("div.input-group.flex-nowrap").Filter(new LocatorFilterOptions { Has = Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Daily Stretch" }) });

        await habitRow.Locator("[data-habits-step-4]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // After marking done, elapsed time changes from ⊘ (LastTimeDoneAt was null) to "0 m"
        await Expect(habitRow.Locator("[data-habits-step-3]")).ToContainTextAsync("0 m");
    }

    [Test]
    public async Task DeleteHabit_AfterAdd_HabitDisappearsFromList()
    {
        await AddItemAsync("Remove Habit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove Habit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-habits-step-10]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove Habit" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task HabitTimer_StartThenStop_ElapsedTimeUpdates()
    {
        await AddItemAsync("Timed Habit");
        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Timed Habit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("button[aria-label='Start']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);
        await Page.Locator("button[aria-label='Stop']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // After Stop, the Start button should be visible again (timer stopped successfully)
        await Expect(Page.Locator("button[aria-label='Start']")).ToBeVisibleAsync();

        // Close the edit component (navigates to /habits and re-renders the list)
        await Page.Locator("[data-habits-step-11]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1000); // allow WASM re-render after SPA navigation

        await Expect(Page.Locator("[data-habits-step-3]").First).ToContainTextAsync("0 m");
    }

    [Test]
    public async Task Calendar_ClickDay_ElapsedTimeUpdates()
    {
        await AddItemAsync("Calendar Habit");

        // data-habits-step-6 is the small calendar in the list view
        // In non-month mode, clicking a cell directly calls AddTimeDone (no Increase button needed)
        ILocator gridCells = Page.Locator("[data-habits-step-6] button[role='gridcell']");
        await Expect(gridCells.First).ToBeVisibleAsync();

        ILocator todayCell = gridCells
            .Filter(new LocatorFilterOptions { HasText = $"{DateTime.Today.Day}" })
            .First;
        await Expect(todayCell).ToBeVisibleAsync();

        await todayCell.ClickAsync(new LocatorClickOptions { Force = true });

        // After clicking today's cell, elapsed time changes from ⊘ to "0 m"
        await Expect(Page.Locator("[data-habits-step-3]")).ToContainTextAsync("0 m");
    }

    [Test]
    public async Task DeleteHabit_MovesToTrash()
    {
        await AddItemAsync("Trashed Habit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Trashed Habit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-habits-step-10]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Trashed Habit" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditHabit_ChangesTitle_NewTitleVisible()
    {
        await AddItemAsync("Edit Me");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Edit Me" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("input[aria-label='Habit title']").FillAsync("Edited Habit");
        await Page.Locator("input[aria-label='Habit title']").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-habits-step-11]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Edited Habit" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Edit Me" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EditHabit_ChangeAllFields_CloseWorksOnFirstClick()
    {
        await AddItemAsync("Habit To Edit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit To Edit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change title
        await Page.Locator("input[aria-label='Habit title']").FillAsync("Habit Edited Title");
        await Page.Locator("input[aria-label='Habit title']").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change repeat count (first number input inside data-habits-step-14)
        await Page.Locator("[data-habits-step-14] input[type='number']").First.FillAsync("3");
        await Page.Locator("[data-habits-step-14] input[type='number']").First.BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change repeat interval (second number input inside data-habits-step-14)
        await Page.Locator("[data-habits-step-14] input[type='number']").Last.FillAsync("7");
        await Page.Locator("[data-habits-step-14] input[type='number']").Last.BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change repeat period (default is Day, switch to Week)
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change duration hours
        await Page.Locator("select[aria-label='Duration hours']").SelectOptionAsync("1");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change duration minutes
        await Page.Locator("select[aria-label='Duration minutes']").SelectOptionAsync("30");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click Close once — must close the detail panel immediately (no second click required)
        await Page.Locator("[data-habits-step-11]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-habits-step-11]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit Edited Title" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddHabit_PersistedAfterReload()
    {
        await AddItemAsync("Persistent Habit");

        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        await NavigateToAsync("[data-main-step-5]");

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Persistent Habit" })).ToBeVisibleAsync();
    }
}
