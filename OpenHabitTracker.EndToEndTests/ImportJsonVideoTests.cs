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

    private async Task RunDemoScript(IPage page)
    {
        // 1. menu → Data → import demo data
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("[data-main-step-1]").ClickAsync();
        await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" }).ClickAsync();
        await page.Locator("input[type=file]").SetInputFilesAsync("TestData/demo-data.json");
        await page.WaitForTimeoutAsync(2000);

        // 2. /notes — open markdown note, pause to show rendered content
        await page.GotoAsync($"{BaseUrl}/notes");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-notes-step-2]").First.ClickAsync();
        await page.WaitForTimeoutAsync(3000);
        await page.Locator("[data-notes-step-7]").ClickAsync();
        await page.WaitForTimeoutAsync(1000);

        // 3. /tasks — mark "Team standup" (due today) as done
        await page.GotoAsync($"{BaseUrl}/tasks");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator(".input-group")
            .Filter(new LocatorFilterOptions { HasText = "Team standup" })
            .Locator("[data-tasks-step-4]")
            .ClickAsync();
        await page.WaitForTimeoutAsync(3000);

        // 4. /habits — open "Morning run" to show calendar dots
        await page.GotoAsync($"{BaseUrl}/habits");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-habits-step-2]")
            .Filter(new LocatorFilterOptions { HasText = "Morning run" })
            .ClickAsync();
        await page.WaitForTimeoutAsync(4000);
        await page.Locator("[data-habits-step-11]").ClickAsync();
        await page.WaitForTimeoutAsync(1000);

        // 5. search icon in top bar — type search term
        await page.Locator("[data-main-step-6]").ClickAsync();
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-search-step-1]").FillAsync("run");
        await page.WaitForTimeoutAsync(2000);

        // 6. menu → Settings — change theme to show visual change
        await page.Locator("[data-main-step-1]").ClickAsync();
        await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Settings" }).ClickAsync();
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-settings-step-2] select").SelectOptionAsync("cerulean");
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
