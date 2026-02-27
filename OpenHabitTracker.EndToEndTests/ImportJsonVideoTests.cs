using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace OpenHabitTracker.EndToEndTests;

[TestFixture]
public class ImportJsonVideoTests : PlaywrightTest
{
    private const string BaseUrl = "http://localhost";

    private IBrowser _browser = null!;

    [SetUp]
    public async Task BrowserSetUp()
    {
        _browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
    }

    [TearDown]
    public async Task BrowserTearDown()
    {
        await _browser.CloseAsync();
    }

    private static async Task ClickAsync(ILocator locator)
    {
        await locator.HoverAsync(); // trigger :hover CSS state
        await locator.Page.WaitForTimeoutAsync(500); // pause on hover so :hover state is visible
        await locator.ClickAsync(new LocatorClickOptions { Delay = 500 }); // hold mousedown to show :active CSS state
    }

    private async Task RunDemoScript(IPage page)
    {
        // 1. menu → Data → import demo data
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(2000); // wait for Blazor OnAfterRenderAsync to finish
        if (!await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" }).IsVisibleAsync())
            await ClickAsync(page.Locator("[data-main-step-1]")); // menu toggle button (three dots) — open only if Data button not already visible
        await page.WaitForTimeoutAsync(2000);
        await ClickAsync(page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" })); // Data button in menu sidebar
        await page.Locator("input[type=file]").SetInputFilesAsync("TestData/demo-data.json"); // hidden file input for JSON import
        await page.WaitForTimeoutAsync(2000);

        // 2. /notes — open markdown note, pause to show rendered content
        await ClickAsync(page.Locator("[data-main-step-3]")); // Notes nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-notes-step-2]").First); // note title button — opens note detail
        await page.WaitForTimeoutAsync(3000);
        await ClickAsync(page.Locator("[data-notes-step-7]")); // Close button in note detail
        await page.WaitForTimeoutAsync(1000);

        // 3. /tasks — mark "Team standup" (due today) as done
        await ClickAsync(page.Locator("[data-main-step-4]")); // Tasks nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator(".input-group")
            .Filter(new LocatorFilterOptions { HasText = "Team standup" })
            .Locator("[data-tasks-step-4]")); // mark-as-done button in task row
        await page.WaitForTimeoutAsync(3000);

        // 4. /habits — open "Morning run" to show calendar dots
        await ClickAsync(page.Locator("[data-main-step-5]")); // Habits nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-habits-step-2]") // habit title button — opens habit detail
            .Filter(new LocatorFilterOptions { HasText = "Morning run" }));
        await page.WaitForTimeoutAsync(4000);
        await ClickAsync(page.Locator("[data-habits-step-11]")); // Close button in habit detail
        await page.WaitForTimeoutAsync(1000);

        // 5. search icon in top bar — type search term
        await ClickAsync(page.Locator("[data-main-step-6]")); // Search toggle button in top bar
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-search-step-1]").PressSequentiallyAsync("run", new LocatorPressSequentiallyOptions { Delay = 200 }); // search input field — typed char by char
        await page.WaitForTimeoutAsync(2000);

        // 6. navigate to Home — shows all notes, tasks and habits together
        await ClickAsync(page.Locator("[data-main-step-2]")); // Home nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        // 7. menu → Settings — toggle off dark mode, then change theme to show visual change
        await ClickAsync(page.Locator("[data-main-step-1]")); // menu toggle button (three dots)
        await page.WaitForTimeoutAsync(2000);
        await ClickAsync(page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Settings" })); // Settings button in menu sidebar
        await page.WaitForTimeoutAsync(1000);
        if (await page.Locator("#IsDarkMode").IsCheckedAsync())
            await ClickAsync(page.Locator("label[for='IsDarkMode']")); // Dark mode label — click to turn off dark mode
        await page.WaitForTimeoutAsync(2000);
        await page.Locator("[data-settings-step-2] select").SelectOptionAsync("united"); // theme selector dropdown
        await page.WaitForTimeoutAsync(3000);
    }

    [Test]
    public async Task RecordDesktopVideo()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await RunDemoScript(page);
        await context.CloseAsync();
    }

    [Test]
    public async Task RecordMobileVideo()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 886, Height = 1920 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await RunDemoScript(page);
        await context.CloseAsync();
    }
}
