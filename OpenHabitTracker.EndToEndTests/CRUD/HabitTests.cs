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
    public async Task Calendar_ClickDayThenIncrease_CountUpdatesInCell()
    {
        await AddItemAsync("Calendar Habit");
        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Calendar Habit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // ShowLargeCalendar defaults to true — month calendar is visible in detail view at data-habits-step-13
        // Clicking a cell in month mode selects it and shows Increase/Decrease buttons (does not directly add)
        ILocator todayCell = Page.Locator("[data-habits-step-13] button[role='gridcell']")
            .Filter(new LocatorFilterOptions { HasText = $"{DateTime.Today.Day}" })
            .First;
        await todayCell.ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        // Click Increase twice — (N) only renders when N > 1, so two clicks gives (2)
        await Page.Locator("button[aria-label='Increase']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        await Page.Locator("button[aria-label='Increase']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Expect(todayCell).ToContainTextAsync("(2)");
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
}
