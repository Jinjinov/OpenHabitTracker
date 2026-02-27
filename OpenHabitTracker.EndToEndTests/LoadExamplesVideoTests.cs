using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace OpenHabitTracker.EndToEndTests;

[TestFixture]
public class LoadExamplesVideoTests : BrowserTest
{
    private const string BaseUrl = "http://localhost";

    private async Task RunDemoScript(IPage page)
    {
        // 1. menu → Data → load built-in examples
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("[data-main-step-1]").ClickAsync();
        await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" }).ClickAsync();
        await page.Locator("[data-data-step-1]").ClickAsync();
        await page.WaitForTimeoutAsync(2000);

        // 2. /notes — open first note, pause to show rendered content
        await page.GotoAsync($"{BaseUrl}/notes");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-notes-step-2]").First.ClickAsync();
        await page.WaitForTimeoutAsync(3000);
        await page.Locator("[data-notes-step-7]").ClickAsync();
        await page.WaitForTimeoutAsync(1000);

        // 3. /tasks — open first task
        await page.GotoAsync($"{BaseUrl}/tasks");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-tasks-step-2]").First.ClickAsync();
        await page.WaitForTimeoutAsync(3000);
        await page.Locator("[data-tasks-step-10]").ClickAsync();
        await page.WaitForTimeoutAsync(1000);

        // 4. /habits — open first habit to show calendar
        await page.GotoAsync($"{BaseUrl}/habits");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-habits-step-2]").First.ClickAsync();
        await page.WaitForTimeoutAsync(4000);
        await page.Locator("[data-habits-step-11]").ClickAsync();
        await page.WaitForTimeoutAsync(1000);

        // 5. search icon in top bar — type search term
        await page.Locator("[data-main-step-6]").ClickAsync();
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-search-step-1]").FillAsync("daily");
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
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await RunDemoScript(page);
        await context.CloseAsync(); // must close context to flush the .webm file
    }

    [Test]
    public async Task RecordMobileVideo()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 886, Height = 1920 },
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 886, Height = 1920 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await RunDemoScript(page);
        await context.CloseAsync();
    }
}
