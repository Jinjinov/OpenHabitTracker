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
        await Expect(Page.Locator("button.btn-plain.input-group")).ToBeVisibleAsync();
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

        // After marking done, elapsed time changes from ⊘ (LastTimeDoneAt was null) to "0 m"
        await Expect(habitRow.Locator("[data-habits-step-3]")).ToContainTextAsync("0 m");
    }

    [Test]
    public async Task DeleteHabit_AfterAdd_HabitDisappearsFromList()
    {
        await AddItemAsync("Remove Habit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove Habit" }).ClickAsync();

        await Page.Locator("[data-habits-step-10]").ClickAsync();

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Remove Habit" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task HabitTimer_StartThenStop_ElapsedTimeUpdates()
    {
        await AddItemAsync("Timed Habit");
        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Timed Habit" }).ClickAsync();
        await Page.Locator("button[aria-label='Start']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);
        await Page.Locator("button[aria-label='Stop']").ClickAsync();

        // After Stop, the Start button should be visible again (timer stopped successfully)
        await Expect(Page.Locator("button[aria-label='Start']")).ToBeVisibleAsync();

        // Close the edit component (navigates to /habits and re-renders the list)
        await Page.Locator("[data-habits-step-11]").ClickAsync();

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

        await Page.Locator("[data-habits-step-10]").ClickAsync();

        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Trashed Habit" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditHabit_ChangesTitle_NewTitleVisible()
    {
        await AddItemAsync("Edit Me");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Edit Me" }).ClickAsync();

        await Page.Locator("input[aria-label='Habit title']").FillAsync("Edited Habit");
        await Page.Locator("input[aria-label='Habit title']").BlurAsync();

        await Page.Locator("[data-habits-step-11]").ClickAsync();

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Edited Habit" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Edit Me" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EditHabit_ChangeAllFields_CloseWorksOnFirstClick()
    {
        await AddItemAsync("Habit To Edit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit To Edit" }).ClickAsync();

        // Change title
        await Page.Locator("input[aria-label='Habit title']").FillAsync("Habit Edited Title");
        await Page.Locator("input[aria-label='Habit title']").BlurAsync();

        // Change repeat count (first number input inside data-habits-step-14)
        await Page.Locator("[data-habits-step-14] input[type='number']").First.FillAsync("3");
        await Page.Locator("[data-habits-step-14] input[type='number']").First.BlurAsync();

        // Change repeat interval (second number input inside data-habits-step-14)
        await Page.Locator("[data-habits-step-14] input[type='number']").Last.FillAsync("7");
        await Page.Locator("[data-habits-step-14] input[type='number']").Last.BlurAsync();

        // Change repeat period (default is Day, switch to Week)
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");

        // Change duration hours
        await Page.Locator("select[aria-label='Duration hours']").SelectOptionAsync("1");

        // Change duration minutes
        await Page.Locator("select[aria-label='Duration minutes']").SelectOptionAsync("30");

        // Click Close once — must close the detail panel immediately (no second click required)
        await Page.Locator("[data-habits-step-11]").ClickAsync();

        await Expect(Page.Locator("[data-habits-step-11]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit Edited Title" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddHabit_PersistedAfterReload()
    {
        await AddItemAsync("Persistent Habit");

        await Page.ReloadAsync();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();

        await NavigateToAsync("[data-main-step-5]");

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Persistent Habit" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditHabit_ChangeCategory_HabitMovesToNewGroup()
    {
        await CreateCategoryAsync("HabitMoveToCategory");
        await EnableGroupedByCategoryAsync();

        // Add uncategorized habit — SetUp already navigated to habits
        await AddItemAsync("Movable Habit");

        // Habit should appear under Uncategorized group
        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "Uncategorized" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Habit" })).ToBeVisibleAsync();

        // Open habit and change category
        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Habit" }).ClickAsync();
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = "HabitMoveToCategory" });
        await Page.Locator("[data-habits-step-11]").ClickAsync(); // Close

        // Habit must appear under HabitMoveToCategory group
        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "HabitMoveToCategory" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Habit" })).ToBeVisibleAsync();

        // Collapse HabitMoveToCategory — habit must disappear (confirms it is in that group)
        await Page.Locator("button.btn-plain.border-0:has(i.bi-tag)")
            .Filter(new LocatorFilterOptions { HasText = "HabitMoveToCategory" })
            .ClickAsync();
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Movable Habit" })).ToHaveCountAsync(0);
    }

    // Regression guard for: bug where ChangeCategory + AddHabit both called habitCategory.Habits.Add,
    // causing the habit to appear twice in grouped-by-category view.
    [Test]
    public async Task AddHabit_WithCategory_AppearsExactlyOnce()
    {
        await CreateCategoryAsync("HabitOnceCategory");
        await EnableGroupedByCategoryAsync();

        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync("Once Habit");
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = "HabitOnceCategory" });
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Once Habit" })).ToHaveCountAsync(1);
    }

    [Test]
    public async Task HabitRepeatSettings_PersistedAfterReload()
    {
        await AddItemAsync("Repeat Settings Habit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Repeat Settings Habit" }).ClickAsync();

        // Change repeat count, interval, and period
        await Page.Locator("[data-habits-step-14] input[type='number']").First.FillAsync("3");
        await Page.Locator("[data-habits-step-14] input[type='number']").First.BlurAsync();

        await Page.Locator("[data-habits-step-14] input[type='number']").Last.FillAsync("7");
        await Page.Locator("[data-habits-step-14] input[type='number']").Last.BlurAsync();

        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");

        await Page.Locator("[data-habits-step-11]").ClickAsync(); // Close

        await Page.ReloadAsync();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();

        await NavigateToAsync("[data-main-step-5]");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Repeat Settings Habit" }).ClickAsync();

        await Expect(Page.Locator("[data-habits-step-14] input[type='number']").First).ToHaveValueAsync("3");
        await Expect(Page.Locator("[data-habits-step-14] input[type='number']").Last).ToHaveValueAsync("7");
        await Expect(Page.Locator("select[aria-label='Repeat period']")).ToHaveValueAsync("Week");
    }

    // Regression guard for: StartAt DB migration (adds DateTime? StartAt to HabitEntity).
    // ElapsedTime formula changes to: LastTimeDoneAt ?? TimeSpan.Zero.Max(DateTime.Now - (StartAt ?? CreatedAt)).
    // For a new habit with LastTimeDoneAt=null and StartAt=null, the display must still show ⊘ —
    // the Habits.razor display checks LastTimeDoneAt (not ElapsedTime) for the ⊘ vs value decision.
    [Test]
    public async Task NewHabit_BeforeMarkingDone_ElapsedTimeShowsNoData()
    {
        await AddItemAsync("ElapsedTime Test Habit");

        // data-habits-step-3 shows ⊘ when LastTimeDoneAt is null, or elapsed time when not null
        // It is always rendered in the list row regardless of ShowSmallCalendar setting
        ILocator habitRow = Page.Locator("div.input-group.flex-nowrap").Filter(
            new LocatorFilterOptions { Has = Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "ElapsedTime Test Habit" }) });

        await Expect(habitRow.Locator("[data-habits-step-3]")).ToContainTextAsync("⊘");
    }
}
