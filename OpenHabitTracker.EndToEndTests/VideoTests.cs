using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace OpenHabitTracker.EndToEndTests;

[TestFixture]
public class VideoTests : BrowserTest
{
    private const string BaseUrl = "http://localhost";

    [Test]
    public async Task RecordDesktopVideo()
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 },
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        await page.Locator("[data-main-step-3]").ClickAsync(); // Notes nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        await page.Locator("[data-main-step-4]").ClickAsync(); // Tasks nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        await page.Locator("[data-main-step-5]").ClickAsync(); // Habits nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        await context.CloseAsync();
    }
}
