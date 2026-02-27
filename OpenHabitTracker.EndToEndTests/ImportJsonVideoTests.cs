using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace OpenHabitTracker.EndToEndTests;

[TestFixture]
public class ImportJsonVideoTests : BrowserTest
{
    private const string BaseUrl = "http://localhost";

    private async Task RunDemoScript(IPage page)
    {
        // 1. /data — import demo data
        await page.GotoAsync($"{BaseUrl}/data");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

        // 5. /search — type search term, show filter UI
        await page.GotoAsync($"{BaseUrl}/search");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await page.Locator("[data-search-step-1]").FillAsync("run");
        await page.WaitForTimeoutAsync(2000);

        // 6. /settings — change theme to show visual change
        await page.GotoAsync($"{BaseUrl}/settings");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
