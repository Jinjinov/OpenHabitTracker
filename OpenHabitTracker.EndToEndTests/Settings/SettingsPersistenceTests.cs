using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests.Settings;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

[TestFixture]
public class SettingsPersistenceTests : BaseTest
{
    [SetUp]
    public async Task SetUp() => await GotoAsync();

    private async Task OpenSettingsAsync()
    {
        await OpenSidebarAsync("bi-gear");
    }

    [Test]
    public async Task DarkMode_Toggle_AppliesImmediately()
    {
        await OpenSettingsAsync();

        await Page.Locator("label[for='IsDarkMode']").ClickAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "light"); // IsDarkMode=true by default, clicking toggles it off
    }

    [Test]
    public async Task DarkMode_Toggle_PersistedAfterReload()
    {
        await OpenSettingsAsync();
        await Page.Locator("label[for='IsDarkMode']").ClickAsync();

        await WaitForIndexedDbAsync("SettingsEntity", "settings => settings.some(s => (s.isDarkMode ?? s.IsDarkMode) === false)"); // wait for the IndexedDB write before reload

        await Page.ReloadAsync();

        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "light");
    }

    [Test]
    public async Task Language_Change_AppliesImmediately()
    {
        await OpenSettingsAsync();

        // data-settings-step-17 wraps the language select
        await Page.Locator("[data-settings-step-17] select").SelectOptionAsync("de");

        // After switching to German the Notes nav link aria-label becomes "Notizen"
        await Expect(Page.Locator("[data-main-step-3]")).ToHaveAttributeAsync("aria-label", "Notizen");
    }

    [Test]
    public async Task Language_Change_PersistedAfterReload()
    {
        await OpenSettingsAsync();
        await Page.Locator("[data-settings-step-17] select").SelectOptionAsync("de");
        await Expect(Page.Locator("[data-main-step-3]")).ToHaveAttributeAsync("aria-label", "Notizen"); // wait for IndexedDB write before reload

        await Page.ReloadAsync();

        await Expect(Page.Locator("[data-main-step-3]")).ToHaveAttributeAsync("aria-label", "Notizen");

        // Reset to English to avoid affecting subsequent tests
        await OpenSettingsAsync();
        await Page.Locator("[data-settings-step-17] select").SelectOptionAsync("en");
    }

    [Test]
    public async Task ShowItemList_Toggle_HidesAndShowsItems()
    {
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("ShowItemList Test Task");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "ShowItemList Test Task" }).ClickAsync();

        await Page.Locator("input[aria-label='Add new item']").FillAsync("Test Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();

        await Page.Locator("[data-tasks-step-10]").ClickAsync();

        // Items area is visible by default (ShowItemList=true)
        await Expect(Page.Locator("[data-tasks-step-5]").First).ToBeVisibleAsync();

        // Toggle ShowItemList off
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowItemList']").ClickAsync();
        await CloseSidebarAsync();

        // Items area should be hidden
        await Expect(Page.Locator("[data-tasks-step-5]")).ToHaveCountAsync(0);

        // Toggle ShowItemList back on
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowItemList']").ClickAsync();
        await CloseSidebarAsync();

        // Items area should be visible again
        await Expect(Page.Locator("[data-tasks-step-5]").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShowCategory_Toggle_DoesNotCrash()
    {
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowCategory']").ClickAsync();

        await Page.Locator("label[for='ShowCategory']").ClickAsync();

        await Expect(Page.Locator("label[for='ShowCategory']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HideCompletedTasks_Toggle_HidesAndShowsCompletedTasks()
    {
        await NavigateToAsync("[data-main-step-4]");

        // Disable HideCompletedTasks so completed tasks remain visible after marking done
        await Page.Locator("[data-main-step-6]").ClickAsync();
        ILocator filterByStatusButton = Page.Locator("button:has(i.bi-filter)").First;
        if (await filterByStatusButton.GetAttributeAsync("aria-expanded") == "false")
            await filterByStatusButton.ClickAsync();
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await CloseSidebarAsync();

        await AddItemAsync("HideCompleted Test");

        ILocator taskRow = Page.Locator("div.input-group.flex-nowrap")
            .Filter(new LocatorFilterOptions { Has = Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "HideCompleted Test" }) });
        await taskRow.Locator("[data-tasks-step-4]").ClickAsync();

        // Task should be visible because HideCompletedTasks=false
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "HideCompleted Test" })).ToBeVisibleAsync();

        // Re-enable HideCompletedTasks
        await Page.Locator("[data-main-step-6]").ClickAsync();
        if (await filterByStatusButton.GetAttributeAsync("aria-expanded") == "false")
            await filterByStatusButton.ClickAsync();
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await CloseSidebarAsync();

        // Task should now be hidden
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "HideCompleted Test" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Theme_Change_AppliesColorVariable()
    {
        await OpenSettingsAsync();

        // data-settings-step-2 wraps the theme select
        await Page.Locator("[data-settings-step-2] select").SelectOptionAsync("cerulean");

        await Expect(Page.Locator("[data-settings-step-2] select")).ToHaveValueAsync("cerulean");
    }

    [Test]
    public async Task ShowCategory_WhenOff_HidesCategorySelectInAddForm()
    {
        await NavigateToAsync("[data-main-step-3]");

        // ShowCategory=true by default — category select is visible in the add form
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Expect(Page.Locator("select[aria-label='Category']")).ToBeVisibleAsync();
        await Page.Locator("button:has(i.bi-x-square)").ClickAsync();

        // Toggle ShowCategory off
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowCategory']").ClickAsync();
        await CloseSidebarAsync();

        // Category select must be gone from the add form
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Expect(Page.Locator("select[aria-label='Category']")).ToHaveCountAsync(0);
        await Page.Locator("button:has(i.bi-x-square)").ClickAsync();

        // Restore ShowCategory so other tests are not affected
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowCategory']").ClickAsync();
        await CloseSidebarAsync();
    }

    [Test]
    public async Task MaxSmallCalendarDays_Change_LimitsSmallCalendarCells()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("MaxDays Habit");

        await OpenSettingsAsync();
        // data-settings-step-11 wraps the max small calendar days select
        await Page.Locator("[data-settings-step-11] select").SelectOptionAsync("3");
        await CloseSidebarAsync();

        // Scope to the calendar belonging to our habit — other tests' habits may share the page
        ILocator calendar = Page.Locator("div.w-100:has([data-habits-step-2]:has-text('MaxDays Habit')) + div[data-habits-step-6]");
        await Expect(calendar.Locator("div[role='grid'] button")).ToHaveCountAsync(3);

        // Reset to Auto so other tests see the default dynamic width
        await OpenSettingsAsync();
        await Page.Locator("[data-settings-step-11] select").SelectOptionAsync("0");
        await CloseSidebarAsync();
    }

    [Test]
    public async Task MaxSmallCalendarDays_PersistedAfterReload()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("MaxDays Reload Habit");

        await OpenSettingsAsync();
        await Page.Locator("[data-settings-step-11] select").SelectOptionAsync("3");
        await CloseSidebarAsync();

        // Waiting for the capped calendar guarantees the settings write completed before reload
        ILocator calendar = Page.Locator("div.w-100:has([data-habits-step-2]:has-text('MaxDays Reload Habit')) + div[data-habits-step-6]");
        await Expect(calendar.Locator("div[role='grid'] button")).ToHaveCountAsync(3);

        await Page.ReloadAsync();

        await OpenSettingsAsync();
        await Expect(Page.Locator("[data-settings-step-11] select")).ToHaveValueAsync("3");

        // Reset to Auto so other tests see the default dynamic width
        await Page.Locator("[data-settings-step-11] select").SelectOptionAsync("0");
        await CloseSidebarAsync();
    }

    // Regression guard for: settings presets DB migration (adds Name + SelectedSettingsId columns).
    // LoadSettings() will change to load Settings[0] then Settings[SelectedSettingsId].
    // The ratio filter is stored in the settings row — it must survive a reload after that change.
    [Test]
    public async Task RatioMinFilter_Toggle_PersistedAfterReload()
    {
        await NavigateToAsync("[data-main-step-5]"); // habits page — ratio filter is relevant here

        // Open search panel; FilterByStatus section is expanded by default (FoldSection[FilterByStatus]=false)
        await Page.Locator("[data-main-step-6]").ClickAsync();

        // Enable ShowOnlyOverSelectedRatioMin — data-search-step-16 wraps the checkbox + label
        await Page.Locator("[data-search-step-16] label").ClickAsync();

        await CloseSidebarAsync();

        await WaitForIndexedDbAsync("SettingsEntity", "settings => settings.some(s => (s.showOnlyOverSelectedRatioMin ?? s.ShowOnlyOverSelectedRatioMin) === true)"); // wait for the IndexedDB write before reload

        await Page.ReloadAsync();

        await Page.Locator("[data-main-step-6]").ClickAsync();

        await Expect(Page.Locator("[data-search-step-16] input[type='checkbox']")).ToBeCheckedAsync();
    }
}
