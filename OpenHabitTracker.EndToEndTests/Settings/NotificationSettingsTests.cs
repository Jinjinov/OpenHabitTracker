using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests.Settings;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

// Headed, because every one of these settings asks for the notification permission before it
// takes a value, and headless Chromium answers "denied" no matter what the context grants.
[TestFixture]
public class NotificationSettingsTests : BaseTest
{
    protected override bool Headless => false;

    [SetUp]
    public async Task SetUp() => await GotoAsync();

    private async Task OpenSettingsAsync()
    {
        await OpenSidebarAsync("bi-gear");
    }

    [Test]
    public async Task NotificationTime_Change_PersistedAfterReload()
    {
        // Moving the setting off its off value asks the browser for permission, and a refusal
        // deliberately puts it straight back to off.
        await Context.GrantPermissionsAsync(["notifications"], new BrowserContextGrantPermissionsOptions { Origin = BaseUrl });

        await OpenSettingsAsync();

        await Page.Locator("[data-settings-step-27] input[type='time']").FillAsync("09:00");

        // TimeOnly is persisted as a string, so match the prefix rather than a serialized shape.
        await WaitForIndexedDbAsync("SettingsEntity", "settings => settings.some(s => String(s.notificationTime ?? s.NotificationTime).startsWith('09:00'))");

        await Page.ReloadAsync();
        await OpenSettingsAsync();

        await Expect(Page.Locator("[data-settings-step-27] input[type='time']")).ToHaveValueAsync("09:00");
    }

    [Test]
    public async Task NotificationLeadTime_Change_PersistedAfterReload()
    {
        await Context.GrantPermissionsAsync(["notifications"], new BrowserContextGrantPermissionsOptions { Origin = BaseUrl });

        await OpenSettingsAsync();

        // The lead time is hours and minutes in two selects, and setting either one turns the reminder on.
        await Page.Locator("[data-settings-step-31] select").Nth(0).SelectOptionAsync("1");
        await Page.Locator("[data-settings-step-31] select").Nth(1).SelectOptionAsync("30");

        await WaitForIndexedDbAsync("SettingsEntity", "settings => settings.some(s => (s.notificationLeadMinutes ?? s.NotificationLeadMinutes) === 90)");

        await Page.ReloadAsync();
        await OpenSettingsAsync();

        await Expect(Page.Locator("[data-settings-step-31] input[type='checkbox']")).ToBeCheckedAsync();
        await Expect(Page.Locator("[data-settings-step-31] select").Nth(0)).ToHaveValueAsync("1");
        await Expect(Page.Locator("[data-settings-step-31] select").Nth(1)).ToHaveValueAsync("30");
    }

    [Test]
    public async Task NotificationTime_WhenPermissionIsRefused_GoesBackToOff()
    {
        await Context.ClearPermissionsAsync();

        await OpenSettingsAsync();

        await Page.Locator("[data-settings-step-27] input[type='time']").FillAsync("09:00");

        // A setting that reads on while nothing can fire is the one outcome to avoid.
        await Expect(Page.Locator("[data-settings-step-27] input[type='time']")).ToHaveValueAsync("");
    }
}
