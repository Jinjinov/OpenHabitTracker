using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests.Screenshot;

// Microsoft Store "16:9 Super hero art" - the banner that lets a trailer sit at the top of the
// listing (Automate.md section 6). Generated, not designed: hero.html renders a field of habit
// cells in the app's own sampled palette, so the asset is reproducible and versioned.
// Its one hard rule is that it must not carry the product's title, which is why the Play feature
// graphic - a wordmark and nothing else - cannot be reused here.

[TestFixture]
public class HeroArtTests : PlaywrightTest
{
    [Test]
    public async Task Capture_SuperHeroArt()
    {
        string page = new Uri(Path.GetFullPath(Path.Combine("..", "..", "..", "Screenshot", "hero.html"))).AbsoluteUri;

        await using IBrowser browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        Directory.CreateDirectory("hero");

        foreach (string mark in new[] { "", "grey" })
        {
            foreach (string mode in new[] { "dark", "light" })
            foreach (int scale in new[] { 1, 2 }) // 1920x1080 and 3840x2160, both accepted
            {
                
                await using IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    DeviceScaleFactor = scale
                });

                IPage tab = await context.NewPageAsync();
                await tab.GotoAsync($"{page}?mode={mode}" + (mark == "" ? "" : $"&mark={mark}"));

                await tab.Locator("#art").ScreenshotAsync(
                    new LocatorScreenshotOptions { Path = Path.Combine("hero", $"hero-{mode}{(mark == "" ? "" : "-" + mark)}-{1920 * scale}x{1080 * scale}.png") });
            }
        }

        TestContext.Out.WriteLine(Path.GetFullPath("hero"));
    }
}
