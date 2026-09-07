using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests.Statistics;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

[TestFixture]
public class HabitChartsTests : BaseTest
{
    [SetUp]
    public async Task SetUp() => await GotoAsync();

    private async Task OpenSettingsAsync() => await OpenSidebarAsync("bi-gear");

    // data-settings-step-14 wraps the show habit charts checkbox
    private async Task SetShowHabitChartsAsync(bool show)
    {
        await OpenSettingsAsync();

        ILocator checkbox = Page.Locator("[data-settings-step-14] input");

        if (await checkbox.IsCheckedAsync() != show)
            await Page.Locator("label[for='ShowHabitCharts']").ClickAsync();

        await CloseSidebarAsync();
    }

    [Test]
    public async Task Charts_WhenSettingOff_AreNotInTheHabitDetail()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Charts Off Habit");

        await SetShowHabitChartsAsync(false);

        await Page.Locator("[data-habits-step-2]:has-text('Charts Off Habit')").ClickAsync();

        await Expect(Page.Locator("[data-habits-step-27]")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Charts_WhenSettingOn_ShowAllFivePanelsInTheHabitDetail()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Charts On Habit");

        await SetShowHabitChartsAsync(true);

        await Page.Locator("[data-habits-step-2]:has-text('Charts On Habit')").ClickAsync();

        await Expect(Page.Locator("[data-habits-step-27]")).ToHaveCountAsync(1); // target
        await Expect(Page.Locator("[data-habits-step-28]")).ToHaveCountAsync(1); // history
        await Expect(Page.Locator("[data-habits-step-29]")).ToHaveCountAsync(1); // calendar
        await Expect(Page.Locator("[data-habits-step-30]")).ToHaveCountAsync(1); // best streaks
        await Expect(Page.Locator("[data-habits-step-31]")).ToHaveCountAsync(1); // frequency

        await SetShowHabitChartsAsync(false); // leave the default for other tests
    }

    [Test]
    public async Task Charts_AreIndependentOfTheHabitStatisticsBlock()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Charts Independent Habit");

        await SetShowHabitChartsAsync(true);

        await Page.Locator("[data-habits-step-2]:has-text('Charts Independent Habit')").ClickAsync();

        // The text statistics block ships off; turning the charts on must not turn it on or off.
        await Expect(Page.Locator("[data-habits-step-27]")).ToHaveCountAsync(1);
        await Expect(Page.Locator("[data-habits-step-20]")).ToHaveCountAsync(0);

        await SetShowHabitChartsAsync(false);
    }

    [Test]
    public async Task Charts_TargetPanel_ShowsProgressAfterCompletingTheHabit()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Charts Target Habit");

        await SetShowHabitChartsAsync(true);

        await Page.Locator("[data-habits-step-2]:has-text('Charts Target Habit')").ClickAsync();

        // The month calendar in the detail: clicking today selects the day, then + adds the completion.
        await Page.Locator("[data-habits-step-13] button[role='gridcell'].border-primary-subtle").ClickAsync();
        await Page.Locator("[data-habits-step-13] button[aria-label='Increase']").ClickAsync();

        // Once a day by default, so today reads one of one.
        await Expect(Page.Locator("[data-habits-step-27]")).ToContainTextAsync("1 of 1");

        await SetShowHabitChartsAsync(false);
    }

    [Test]
    public async Task Charts_Setting_PersistedAfterReload()
    {
        await SetShowHabitChartsAsync(true);

        await Page.ReloadAsync();

        await OpenSettingsAsync();
        await Expect(Page.Locator("[data-settings-step-14] input")).ToBeCheckedAsync();
        await Page.Locator("label[for='ShowHabitCharts']").ClickAsync();
        await CloseSidebarAsync();
    }
}
