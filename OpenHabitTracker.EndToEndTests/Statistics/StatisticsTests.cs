namespace OpenHabitTracker.EndToEndTests.Statistics;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

[TestFixture]
public class StatisticsTests : BaseTest
{
    [Test]
    public async Task AddNote_StatsPanel_ShowsTotalOne()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-3]");

        await AddItemAsync("Stats Note");

        await Expect(Page.Locator("p").Filter(new LocatorFilterOptions { HasText = "Total: 2" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddTask_StatsPanel_ShowsTotalOne()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-4]");

        await AddItemAsync("Stats Task");

        await Expect(Page.Locator("span.badge.bg-body-secondary").Filter(new LocatorFilterOptions { HasText = "Total: 1" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task MarkTaskAsDone_StatsPanel_UpdatesDoneCount()
    {
        await GotoAsync();

        // Disable HideCompletedTasks so the task stays in the list and Done count is visible after marking done
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Done Task");

        await Expect(Page.Locator("span.badge.bg-body-secondary").Filter(new LocatorFilterOptions { HasText = "Total: 1" })).ToBeVisibleAsync();
        await Expect(Page.Locator("span.badge.bg-success-subtle").Filter(new LocatorFilterOptions { HasText = "Done: 0" })).ToBeVisibleAsync();

        await Page.Locator("[data-tasks-step-4]").First.ClickAsync();

        await Expect(Page.Locator("span.badge.bg-success-subtle").Filter(new LocatorFilterOptions { HasText = "Done: 1" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddHabit_StatsPanel_ShowsHabitInWeeklyCount()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-5]");

        await AddItemAsync("Stats Habit");

        // "0 out of 1 done" — doneThisWeek=0, total=1
        await Expect(Page.Locator("p").Filter(new LocatorFilterOptions { HasText = "out of 1 done" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task MarkHabitAsDone_StatsPanel_UpdatesDoneThisWeekCount()
    {
        await GotoAsync();

        // Disable ShowSmallCalendar so the Mark as done button (data-habits-step-4) is visible in the list
        await OpenSidebarAsync("bi-gear");
        await Page.Locator("label[for='ShowSmallCalendar']").ClickAsync();
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Done Habit");

        await Expect(Page.Locator("p").Filter(new LocatorFilterOptions { HasText = "out of 1 done" })).ToBeVisibleAsync();

        ILocator habitRow = Page.Locator("div.input-group.flex-nowrap").Filter(
            new LocatorFilterOptions { Has = Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Done Habit" }) });
        await habitRow.Locator("[data-habits-step-4]").ClickAsync();

        // "1 out of 1 done" — doneThisWeek=1, total=1
        await Expect(Page.Locator("p").Filter(new LocatorFilterOptions { HasText = "1 out of 1 done" })).ToBeVisibleAsync();
    }
}
