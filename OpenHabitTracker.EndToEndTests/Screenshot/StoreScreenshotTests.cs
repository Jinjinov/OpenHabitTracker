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
        // 1080x2400 - the Galaxy S and Pixel viewport, the most common phone profile at ~25%.
        // 360 CSS px has been the Android baseline since 2015: a decade of growth went into height
        // (S6 360x640, S25 360x780) and left the width alone, so only the height was ever stale.
        // 20:9 is past the 2.0 long-to-short side ratio Play documents, but the live listing has
        // carried 1080x2400 since 2024, so the uploader takes it. Fall back to 360x720 if it ever
        // stops taking it - that is 1080x2160, exactly 2.0, at the cost of one row.
        new("play-phone", 360, 800, 3),
        new("iphone-69", 440, 956, 3),       // 1320x2868 - App Store iPhone 6.9", the size Apple scales down from
        // The two Play tablet folders get their own capture rather than one shared set: 600 and 800
        // sit either side of the 768 breakpoint, so a ten-inch tablet renders a layout a seven-inch
        // one never shows. 800x1280 at scale 2 is the Galaxy Tab viewport.
        new("play-tablet-7", 600, 960, 2),   // 1200x1920 - Play seven-inch
        new("play-tablet-10", 800, 1280, 2), // 1600x2560 - Play ten-inch
        new("ipad-13", 1032, 1376, 2),       // 2064x2752 - App Store iPad 13"
        // 2880x1800 - Mac App Store 16:10, clears the Microsoft 1366x768 minimum.
        // 1920x1200 at scale 1.5 produces the same pixel size but a worse shot:
        // the content column is capped in CSS px, so a wider viewport only adds empty background.
        new("desktop", 1440, 900, 2)
    ];

    // Store media is committed and read from the repo by the upload tools, so the capture writes
    // there rather than into the bin working directory a clean wipes and git never sees.
    // Three levels up from bin/<config>/<tfm> is the project, four is the app repo root.
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine("..", "..", "..", ".."));

    // en-US only this round - both tools take per-locale folders, so 20 locales is this one string.
    private const string Locale = "en-US";

    private static string AndroidImages(string folder) =>
        Path.Combine(RepoRoot, "fastlane", "metadata", "android", Locale, "images", folder);

    private static string AppleScreenshots => Path.Combine(RepoRoot, "fastlane", "screenshots", Locale);

    private sealed record Destination(string Directory, string Prefix);

    // Play reads one folder per form factor, one capture each.
    // deliver reads one folder per locale and infers the device class from the image resolution,
    // so the three Apple sizes share it and only the filename prefix keeps them apart.
    private static Destination[] DestinationsFor(string target) => target switch
    {
        "play-phone" => [new(AndroidImages("phoneScreenshots"), "")],
        "play-tablet-7" => [new(AndroidImages("sevenInchScreenshots"), "")],
        "play-tablet-10" => [new(AndroidImages("tenInchScreenshots"), "")],
        _ => [new(AppleScreenshots, target + "-")]
    };

    // A rerun must leave exactly the set it captured: supply uploads every file in the folder, so
    // a leftover from an earlier run - or from the 2024 set - would ship alongside the new shots.
    private static void ClearCapturedShots(Destination destination)
    {
        Directory.CreateDirectory(destination.Directory);

        foreach (string file in Directory.EnumerateFiles(destination.Directory, destination.Prefix + "*.png"))
            File.Delete(file);
    }

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

        Destination[] destinations = DestinationsFor(target.Name);

        foreach (Destination destination in destinations)
            ClearCapturedShots(destination);

        foreach ((string file, Func<IPage, Task> scene) in scenes)
        {
            await scene(page);

            string captured = Path.Combine(destinations[0].Directory, destinations[0].Prefix + file);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = captured });

            foreach (Destination duplicate in destinations.Skip(1))
                File.Copy(captured, Path.Combine(duplicate.Directory, duplicate.Prefix + file), overwrite: true);
        }
    }

    // Put the opened detail at the top of the frame. On the narrow viewports the rows above it push
    // the Close button past the fold - measured at 38 to 248 px over, depending on size and whether
    // the item carries a checklist - and a detail starting mid-screen reads as an afterthought.
    // Takes the detail component's own id - #habit-component, #task-component, #note-component -
    // because scrolling any looser ancestor moves nothing: the container that scrolls is the column.
    private static async Task ScrollDetailToTopAsync(IPage page, string componentId)
    {
        await page.Locator(componentId).EvaluateAsync("el => el.scrollIntoView({ block: 'start' })");

        // Scroll settle; there is no observable state change to await on.
        await page.WaitForTimeoutAsync(300);
    }

    // Three list-and-detail pairs, then the two panels no competitor matches.
    // Details open a named item, never .First - the lists sort the most urgent first, which would
    // open a red overdue habit and make the marquee shot look like a telling-off.
    // They also open items WITHOUT a checklist: three checkboxes cost about 114 px, which on a
    // phone is the difference between showing the fields under the calendar and showing none.
    private static (string File, Func<IPage, Task> Scene)[] Scenes =>
    [
        ("01-habits.png", async page =>
        {
            await page.Locator("[data-main-step-5]").ClickAsync();
            await Assertions.Expect(page.Locator("[data-habits-step-2]").First).ToBeVisibleAsync();
        }),
        ("02-habit-detail.png", async page =>
        {
            // Display metric Time rather than the default Repetitions, and no checklist, so the
            // repeat rule, duration, metric and category sit directly under the month calendar.
            await page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasTextString = "Strength training" }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-habits-step-11]")).ToBeVisibleAsync();
            await ScrollDetailToTopAsync(page, "#habit-component");
        }),
        ("03-tasks.png", async page =>
        {
            await page.Locator("[data-habits-step-11]").ClickAsync(); // close the habit
            await page.Locator("[data-main-step-4]").ClickAsync();
            await Assertions.Expect(page.Locator("[data-tasks-step-2]").First).ToBeVisibleAsync();
        }),
        ("04-task-detail.png", async page =>
        {
            await page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasTextString = "Reply to the support mail" }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-tasks-step-10]")).ToBeVisibleAsync();
            await ScrollDetailToTopAsync(page, "#task-component");
        }),
        ("05-notes.png", async page =>
        {
            await page.Locator("[data-tasks-step-10]").ClickAsync(); // close the task
            await page.Locator("[data-main-step-3]").ClickAsync();
            await Assertions.Expect(page.Locator("[data-notes-step-2]").First).ToBeVisibleAsync();
        }),
        ("06-note-detail.png", async page =>
        {
            // The shortest note that still shows rendered markdown - a checkbox list. The longer
            // ones overflow every narrow viewport by more than the scroll range can recover.
            await page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasTextString = "Release checklist" }).ClickAsync();
            await Assertions.Expect(page.Locator("[data-notes-step-7]")).ToBeVisibleAsync();
            await ScrollDetailToTopAsync(page, "#note-component");
        }),
        ("07-search.png", async page =>
        {
            await page.Locator("[data-notes-step-7]").ClickAsync(); // close the note
            await page.Locator("[data-main-step-6]").ClickAsync(); // search toggle
            await Assertions.Expect(page.Locator("[data-search-step-1]")).ToBeVisibleAsync();

            // A term with hits in more than one note, so the list behind shows highlighting
            // rather than an empty filter panel.
            await page.Locator("[data-search-step-1]").FillAsync("release");
            await Assertions.Expect(page.Locator("[data-notes-step-2]").First).ToBeVisibleAsync();
        }),
        ("08-settings.png", async page =>
        {
            await page.Locator("[data-search-step-3]").ClickAsync(); // clear the term, or it leaks onward
            await page.Locator("#closeSidebar").ClickAsync();
            await page.Locator("[data-main-step-5]").ClickAsync(); // Habits behind this sidebar, not Notes again
            await page.Locator("[data-main-step-1]").ClickAsync(); // menu
            await page.Locator("div[role='menu'] button:has(i.bi-gear)").ClickAsync();
            await Assertions.Expect(page.Locator("label[for='ShowItemList']")).ToBeVisibleAsync();

            // Toggled from inside the sidebar so the control and its effect on the list behind it
            // land in one frame. The label is the click target - it carries Bootstrap's
            // stretched-link, which covers the checkbox.
            await page.Locator("label[for='ShowGroupedByCategory']").ClickAsync();
            await Assertions.Expect(page.Locator("[data-habits-step-2]").First).ToBeVisibleAsync();
        })
    ];

    // Needs the temporary build that registers the real auth fragment: IAuthFragment.IsAuthAvailable
    // is false in the PWA and its GetAuthFragment returns an empty RenderFragment, so on a shipped
    // build this scene captures a heading and a blank space.
    private static (string File, Func<IPage, Task> Scene) SyncScene =>
        ("09-sync.png", async page =>
        {
            await page.Locator("label[for='ShowGroupedByCategory']").ClickAsync(); // back off, or it leaks into Home
            await page.Locator("#closeSidebar").ClickAsync(); // close Settings from the previous scene
            await page.Locator("[data-main-step-4]").ClickAsync(); // Tasks behind this one - the third list
            await page.Locator("[data-main-step-1]").ClickAsync(); // menu
            await page.Locator("div[role='menu'] button:has(i.bi-database)").ClickAsync();
            await Assertions.Expect(page.Locator("[data-data-step-12]")).ToBeVisibleAsync();
        });

    // Desktop and Mac only - Home needs the width to show all three columns at once.
    private static (string File, Func<IPage, Task> Scene) HomeScene =>
        ("10-home.png", async page =>
        {
            await page.Locator("#closeSidebar").ClickAsync();
            await page.Locator("[data-main-step-2]").ClickAsync();
            await Assertions.Expect(page.Locator("main#main-content")).ToBeVisibleAsync();
        });

    // Play takes 8 per folder, Apple 10 per device size, Microsoft 10 desktop - so the phone and
    // tablet folders stop at the eight, and scene 10 is desktop-only because Home needs the width.
    // The sync block also falls below the fold at the phone viewport, which the Play cap
    // already excludes.
    private static (string File, Func<IPage, Task> Scene)[] ScenesFor(string target) => target switch
    {
        "play-phone" or "play-tablet-7" or "play-tablet-10" => Scenes,
        "desktop" => [.. Scenes, SyncScene, HomeScene],
        _ => [.. Scenes, SyncScene]
    };

    //[Test]
    public async Task Capture_Desktop()
    {
        string seedFile = SeedData.Write(Path.GetFullPath("seed.json"));
        Target desktop = Targets.Single(target => target.Name == "desktop");

        await CaptureSessionAsync(desktop, seedFile, ScenesFor(desktop.Name));

        TestContext.Out.WriteLine(AppleScreenshots);
    }

    //[Test]
    public async Task Capture_All()
    {
        // The narrow targets get the filter sections folded - see SeedData.
        string wideSeed = SeedData.Write(Path.GetFullPath("seed-wide.json"));
        string narrowSeed = SeedData.Write(Path.GetFullPath("seed-narrow.json"), foldForPhone: true);

        foreach (Target target in Targets)
        {
            bool wide = target.Name is "desktop" or "ipad-13" or "play-tablet-10";

            await CaptureSessionAsync(target, wide ? wideSeed : narrowSeed, ScenesFor(target.Name));
        }

        TestContext.Out.WriteLine(AndroidImages(""));
        TestContext.Out.WriteLine(AppleScreenshots);
    }
}
