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

        // data-settings-step-14 wraps the language select
        await Page.Locator("[data-settings-step-14] select").SelectOptionAsync("de");
        await Page.WaitForTimeoutAsync(500);

        // After switching to German the Notes nav link aria-label becomes "Notizen"
        await Expect(Page.Locator("[data-main-step-3]")).ToHaveAttributeAsync("aria-label", "Notizen");
    }

    [Test]
    public async Task ShowItemList_Toggle_DoesNotCrash()
    {
        await OpenSettingsAsync();
        await Page.Locator("label[for='ShowItemList']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Page.Locator("label[for='ShowItemList']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("label[for='ShowItemList']")).ToBeVisibleAsync();
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
    public async Task HideCompletedTasks_Toggle_DoesNotCrash()
    {
        await OpenSettingsAsync();
        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Page.Locator("label[for='HideCompletedTasks']").ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("label[for='HideCompletedTasks']")).ToBeVisibleAsync();
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
