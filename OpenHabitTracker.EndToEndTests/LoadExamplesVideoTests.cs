using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace OpenHabitTracker.EndToEndTests;

[TestFixture]
public class LoadExamplesVideoTests : PlaywrightTest
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

    private async Task RunDemoScript(IPage page)
    {
        // 1. menu → Data → load built-in examples
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("[data-main-step-1]").ClickAsync(); // menu toggle button (three dots)
        await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" }).ClickAsync(); // Data button in menu sidebar
        await page.Locator("[data-data-step-1]").ClickAsync(); // Load examples button in Data sidebar
        await page.WaitForTimeoutAsync(2000);

        // 2. /notes — open first note, pause to show rendered content
        await page.Locator("[data-main-step-3]").ClickAsync(); // Notes nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-notes-step-2]").First.ClickAsync(); // note title button — opens note detail
        await page.WaitForTimeoutAsync(3000);
        await page.Locator("[data-notes-step-7]").ClickAsync(); // Close button in note detail
        await page.WaitForTimeoutAsync(1000);

        // 3. /tasks — open first task
        await page.Locator("[data-main-step-4]").ClickAsync(); // Tasks nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-tasks-step-2]").First.ClickAsync(); // task title button — opens task detail
        await page.WaitForTimeoutAsync(3000);
        await page.Locator("[data-tasks-step-10]").ClickAsync(); // Close button in task detail
        await page.WaitForTimeoutAsync(1000);

        // 4. /habits — open first habit to show calendar
        await page.Locator("[data-main-step-5]").ClickAsync(); // Habits nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-habits-step-2]").First.ClickAsync(); // habit title button — opens habit detail
        await page.WaitForTimeoutAsync(4000);
        await page.Locator("[data-habits-step-11]").ClickAsync(); // Close button in habit detail
        await page.WaitForTimeoutAsync(1000);

        // 5. search icon in top bar — type search term
        await page.Locator("[data-main-step-6]").ClickAsync(); // Search toggle button in top bar
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-search-step-1]").FillAsync("daily"); // search input field
        await page.WaitForTimeoutAsync(2000);

        // 6. menu → Settings — change theme to show visual change
        await page.Locator("[data-main-step-1]").ClickAsync(); // menu toggle button (three dots)
        await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Settings" }).ClickAsync(); // Settings button in menu sidebar
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-settings-step-2] select").SelectOptionAsync("cerulean"); // theme selector dropdown
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
