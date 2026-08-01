using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests.Screenshot;

// Store screenshot capture harness - it records, it does not assert, like the Video fixtures.
// Prerequisite: the app is served at http://localhost (OpenHabitTracker.md, Deployment).

[TestFixture]
public class StoreScreenshotTests : PlaywrightTest
{
    private const string BaseUrl = "http://localhost";

    // CSS viewport x device scale factor = the pixel size the store requires.
    // Playwright renders offscreen, so none of these are bound by the physical display.
    private sealed record Target(string Name, int Width, int Height, float Scale);

    private static readonly Target[] Targets =
    [
        new("play-phone", 360, 640, 3),      // 1080x1920 - Play phone, 9:16
        new("iphone-69", 440, 956, 3),       // 1320x2868 - App Store iPhone 6.9", the size Apple scales down from
        new("play-tablet", 600, 960, 2),     // 1200x1920 - Play seven-inch and ten-inch, 9:16
        new("ipad-13", 1032, 1376, 2),       // 2064x2752 - App Store iPad 13"
        // 2880x1800 - Mac App Store 16:10, clears the Microsoft 1366x768 minimum.
        // 1920x1200 at scale 1.5 produces the same pixel size but a worse shot:
        // the content column is capped in CSS px, so a wider viewport only adds empty background.
        new("desktop", 1440, 900, 2)
    ];

    private IBrowser _browser = null!;

    [SetUp]
    public async Task BrowserSetUp()
    {
        _browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    [TearDown]
    public async Task BrowserTearDown()
    {
        await _browser.CloseAsync();
    }

    private static async Task GotoBaseUrlAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle); // initial WASM bundle download from IIS
        await Assertions.Expect(page.Locator("nav[aria-label]")).ToBeVisibleAsync();
    }

    private static async Task OpenDataSidebarAsync(IPage page)
    {
        if (!await page.Locator("[data-data-step-1]").IsVisibleAsync())
        {
            await page.Locator("[data-main-step-1]").ClickAsync(); // menu toggle
            await Assertions.Expect(page.Locator("div[role='menu'] button:has(i.bi-database)")).ToBeVisibleAsync();
            await page.Locator("div[role='menu'] button:has(i.bi-database)").ClickAsync(); // Data sidebar
        }

        await Assertions.Expect(page.Locator("[data-data-step-1]")).ToBeVisibleAsync();
    }

    private static async Task ImportSeedAsync(IPage page, string seedFile)
    {
        await OpenDataSidebarAsync(page);
        await page.Locator("[data-data-step-2]").ClickAsync(); // delete all, so a rerun does not stack data
        await page.Locator("input[type='file'].d-none").SetInputFilesAsync(seedFile);
        await page.WaitForTimeoutAsync(3000); // IndexedDB writes have no observable completion signal here
        await page.Locator("#closeSidebar").ClickAsync();
        await Assertions.Expect(page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    // One context per target: IndexedDB lives in the context, so the seed is imported once and
    // every scene in that session sees the same data.
    private async Task CaptureSessionAsync(Target target, string seedFile, (string File, Func<IPage, Task> Scene)[] scenes)
    {
        await using IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = target.Width, Height = target.Height },
            DeviceScaleFactor = target.Scale,
            IgnoreHTTPSErrors = true
        });

        IPage page = await context.NewPageAsync();

        await GotoBaseUrlAsync(page);
        await ImportSeedAsync(page, seedFile);

        string directory = Path.Combine("screenshots", target.Name);
        Directory.CreateDirectory(directory);

        foreach ((string file, Func<IPage, Task> scene) in scenes)
        {
            await scene(page);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, file) });
        }
    }

    // Three list-and-detail pairs, then the two panels no competitor matches.
    // Details open a named item, never .First - the lists sort the most urgent first, which would
    // open a red overdue habit and make the marquee shot look like a telling-off.
    private static (string File, Func<IPage, Task> Scene)[] Scenes =>
    [
        ("01-habits.png", async page =>
        {
            await page.Locator("[data-main-step-5]").ClickAsync();
            await Assertions.Expect(page.Locator("[data-habits-step-2]").First).ToBeVisibleAsync();
        }),
        ("02-habit-detail.png", async page =>
        {
            await page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasTextString = "Morning run" }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-habits-step-11]")).ToBeVisibleAsync();
        }),
        ("03-tasks.png", async page =>
        {
            await page.Locator("[data-habits-step-11]").ClickAsync(); // close the habit
            await page.Locator("[data-main-step-4]").ClickAsync();
            await Assertions.Expect(page.Locator("[data-tasks-step-2]").First).ToBeVisibleAsync();
        }),
        ("04-task-detail.png", async page =>
        {
            await page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasTextString = "quarterly report" }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-tasks-step-10]")).ToBeVisibleAsync();
        }),
        ("05-notes.png", async page =>
        {
            await page.Locator("[data-tasks-step-10]").ClickAsync(); // close the task
            await page.Locator("[data-main-step-3]").ClickAsync();
            await Assertions.Expect(page.Locator("[data-notes-step-2]").First).ToBeVisibleAsync();
        }),
        ("06-note-detail.png", async page =>
        {
            await page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasTextString = "Atomic Habits" }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-notes-step-7]")).ToBeVisibleAsync();
        }),
        ("07-search.png", async page =>
        {
            await page.Locator("[data-notes-step-7]").ClickAsync(); // close the note
            await page.Locator("[data-main-step-6]").ClickAsync(); // search toggle
            await Assertions.Expect(page.Locator("[data-search-step-1]")).ToBeVisibleAsync();
        }),
        ("08-settings.png", async page =>
        {
            await page.Locator("#closeSidebar").ClickAsync();
            await page.Locator("[data-main-step-1]").ClickAsync(); // menu
            await page.Locator("div[role='menu'] button:has(i.bi-gear)").ClickAsync();
            await Assertions.Expect(page.Locator("label[for='ShowItemList']")).ToBeVisibleAsync();
        })
    ];

    // Desktop and Mac only - Home needs the width to show all three columns at once.
    private static (string File, Func<IPage, Task> Scene) HomeScene =>
        ("09-home.png", async page =>
        {
            await page.Locator("#closeSidebar").ClickAsync();
            await page.Locator("[data-main-step-2]").ClickAsync();
            await Assertions.Expect(page.Locator("main#main-content")).ToBeVisibleAsync();
        });

    [Test]
    public async Task Capture_Desktop()
    {
        string seedFile = SeedData.Write(Path.GetFullPath("seed.json"));
        Target desktop = Targets.Single(target => target.Name == "desktop");

        await CaptureSessionAsync(desktop, seedFile, [.. Scenes, HomeScene]);

        TestContext.Out.WriteLine(Path.GetFullPath("screenshots"));
    }

    [Test]
    public async Task Capture_All()
    {
        string seedFile = SeedData.Write(Path.GetFullPath("seed.json"));

        foreach (Target target in Targets)
        {
            await CaptureSessionAsync(target, seedFile,
                target.Name == "desktop" ? [.. Scenes, HomeScene] : Scenes);
        }

        TestContext.Out.WriteLine(Path.GetFullPath("screenshots"));
    }
}
