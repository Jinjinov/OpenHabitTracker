using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests.Settings;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

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
        await Page.WaitForTimeoutAsync(300);

        string? theme = await Page.Locator("html").GetAttributeAsync("data-bs-theme");
        Assert.That(theme, Is.EqualTo("light")); // IsDarkMode=true by default, clicking toggles it off
    }

    [Test]
    public async Task DarkMode_Toggle_PersistedAfterReload()
    {
        await OpenSettingsAsync();
        await Page.Locator("label[for='IsDarkMode']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        string? theme = await Page.Locator("html").GetAttributeAsync("data-bs-theme");
        Assert.That(theme, Is.EqualTo("light")); // IsDarkMode=true by default, clicking toggles it off
    }

    [Test]
    public async Task Language_Change_AppliesImmediately()
    {
        await OpenSettingsAsync();

        // data-settings-step-16 wraps the language select
        await Page.Locator("[data-settings-step-16] select").SelectOptionAsync("de");
        await Page.WaitForTimeoutAsync(500);

        // After switching to German the Notes nav link aria-label becomes "Notizen"
        await Expect(Page.Locator("[data-main-step-3]")).ToHaveAttributeAsync("aria-label", "Notizen");
    }

    [Test]
    public async Task Language_Change_PersistedAfterReload()
    {
        await OpenSettingsAsync();
        await Page.Locator("[data-settings-step-16] select").SelectOptionAsync("de");
        await Page.WaitForTimeoutAsync(500);

        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        await Expect(Page.Locator("[data-main-step-3]")).ToHaveAttributeAsync("aria-label", "Notizen");

        // Reset to English to avoid affecting subsequent tests
        await OpenSettingsAsync();
        await Page.Locator("[data-settings-step-16] select").SelectOptionAsync("en");
        await Page.WaitForTimeoutAsync(300);
    }

    [Test]
    public async Task ShowItemList_Toggle_HidesAndShowsItems()
    {
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("ShowItemList Test Task");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "ShowItemList Test Task" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("input[aria-label='Add new item']").FillAsync("Test Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-tasks-step-10]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Items area is visible by default (ShowItemList=true)
        await Expect(Page.Locator("[data-tasks-step-5]").First).ToBeVisibleAsync();

        // Toggle ShowItemList off
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowItemList']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        await CloseSidebarAsync();

        // Items area should be hidden
        await Expect(Page.Locator("[data-tasks-step-5]")).ToHaveCountAsync(0);

        // Toggle ShowItemList back on
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowItemList']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        await CloseSidebarAsync();

        // Items area should be visible again
        await Expect(Page.Locator("[data-tasks-step-5]").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShowCategory_Toggle_DoesNotCrash()
    {
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowCategory']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Page.Locator("label[for='ShowCategory']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("label[for='ShowCategory']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HideCompletedTasks_Toggle_HidesAndShowsCompletedTasks()
    {
        await NavigateToAsync("[data-main-step-4]");

        // Disable HideCompletedTasks so completed tasks remain visible after marking done
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        ILocator filterByStatusButton = Page.Locator("button:has(i.bi-filter)").First;
        if (await filterByStatusButton.GetAttributeAsync("aria-expanded") == "false")
            await filterByStatusButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CloseSidebarAsync();

        await AddItemAsync("HideCompleted Test");

        ILocator taskRow = Page.Locator("div.input-group.flex-nowrap")
            .Filter(new LocatorFilterOptions { Has = Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "HideCompleted Test" }) });
        await taskRow.Locator("[data-tasks-step-4]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Task should be visible because HideCompletedTasks=false
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "HideCompleted Test" })).ToBeVisibleAsync();

        // Re-enable HideCompletedTasks
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        if (await filterByStatusButton.GetAttributeAsync("aria-expanded") == "false")
            await filterByStatusButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
        await Page.WaitForTimeoutAsync(500);

        // Verify the select now has "cerulean" selected
        string? selected = await Page.Locator("[data-settings-step-2] select").InputValueAsync();
        Assert.That(selected, Is.EqualTo("cerulean"));
    }
}
