using Microsoft.Playwright;
using System.Diagnostics;

namespace OpenHabitTracker.EndToEndTests.Video;

// install browsers: pwsh playwright.ps1 install

[TestFixture]
public class LoadExamplesVideoTests : PlaywrightTest
{
    private const string BaseUrl = "http://localhost";

    private IBrowser _browser = null!;

    [SetUp]
    public async Task BrowserSetUp()
    {
        _browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false, Args = ["--window-position=100,0"] });
    }

    [TearDown]
    public async Task BrowserTearDown()
    {
        await _browser.CloseAsync();
    }

    private static async Task MoveToAsync(ILocator locator)
    {
        LocatorBoundingBoxResult? box = await locator.BoundingBoxAsync();

        if (box == null)
            return;

        await locator.Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2, new MouseMoveOptions { Steps = 20 }); // move cursor smoothly to element center
    }

    private static async Task ClickAsync(ILocator locator)
    {
        await MoveToAsync(locator); // move cursor smoothly to element before hovering
        await locator.HoverAsync(); // trigger :hover CSS state
        await locator.Page.WaitForTimeoutAsync(200); // pause on hover so :hover state is visible
        await locator.ClickAsync(new LocatorClickOptions { Delay = 200 }); // hold mousedown to show :active CSS state
    }

    private static async Task SetupFakeCursorAsync(IBrowserContext context)
    {
        await context.AddInitScriptAsync("""
            (() => {
                function install() {
                    const style = document.createElement('style');
                    style.textContent = '* { cursor: none !important; }';
                    document.head.appendChild(style);

                    const cursor = document.createElement('div');
                    cursor.style.cssText = 'position:fixed;top:0;left:0;width:24px;height:24px;pointer-events:none;z-index:2147483647;transform:translate(-100px,-100px)';
                    cursor.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><path d="M1 1L1 18L5 13L8 20L10 19L7 12L13 12Z" fill="white" stroke="black" stroke-width="1" stroke-linejoin="round"/></svg>';
                    document.body.appendChild(cursor);

                    document.addEventListener('mousemove', e => {
                        cursor.style.transform = `translate(${e.clientX}px, ${e.clientY}px)`;
                    }, true);
                }

                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', install);
                } else {
                    install();
                }
            })();
            """);
    }

    private static async Task SetupFakeMobileCursorAsync(IBrowserContext context)
    {
        await context.AddInitScriptAsync("""
            (() => {
                function install() {
                    const style = document.createElement('style');
                    style.textContent = '* { cursor: none !important; }';
                    document.head.appendChild(style);

                    const cursor = document.createElement('div');
                    cursor.style.cssText = 'position:fixed;top:0;left:0;width:44px;height:44px;border-radius:50%;background:radial-gradient(circle, transparent 52%, rgba(0,0,0,0.65) 62%, rgba(255,255,255,1.0) 76%, rgba(0,0,0,0.65) 90%, transparent 100%);box-shadow:0 0 12px 4px rgba(0,0,0,0.5);filter:blur(1px);pointer-events:none;z-index:2147483647;transform:translate(-100px,-100px)';
                    document.body.appendChild(cursor);

                    document.addEventListener('mousemove', e => {
                        cursor.style.transform = `translate(${e.clientX - 22}px, ${e.clientY - 22}px)`;
                    }, true);
                }

                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', install);
                } else {
                    install();
                }
            })();
            """);
    }

    private static async Task GotoBaseUrl(IPage page)
    {
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000); // wait for Blazor OnAfterRenderAsync to finish
    }

    // Seeding happens before ffmpeg starts. The old scenarios filmed themselves clicking
    // Load examples, which spent four of twenty-five seconds on an empty app and a data menu.
    private static async Task SeedAsync(IPage page, string seedFile)
    {
        if (!await page.Locator("[data-data-step-1]").IsVisibleAsync())
        {
            await page.Locator("[data-main-step-1]").ClickAsync();
            await page.Locator("div[role='menu'] button:has(i.bi-database)").ClickAsync();
        }

        await Assertions.Expect(page.Locator("[data-data-step-1]")).ToBeVisibleAsync();
        await page.Locator("[data-data-step-2]").ClickAsync(); // delete all, so a retake does not stack
        await page.Locator("input[type='file'].d-none").SetInputFilesAsync(seedFile);
        await page.WaitForTimeoutAsync(3000); // IndexedDB writes have no observable completion signal
        await page.Locator("#closeSidebar").ClickAsync();
        await Assertions.Expect(page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    private static Task Beat(IPage page) => page.WaitForTimeoutAsync(1000); // let the eye land

    // ~29.9 s at roughly 1.9 s a beat: the move, hover, hold and click take about 0.9 s and the
    // pause takes 1 s. The timer hold buys its 3 s from the same budget.
    private static async Task Main(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-5]")); // Habits
        await Beat(page);

        // Everything that can be done from the list is done from the list. A demo that opens an
        // item to tick it off teaches that the app is laborious, and none of this needs the detail:
        // in the list strip (DisplayMonth false) a day click calls AddTimeDone directly, and the
        // read-only ItemsComponent still calls SetIsDone - read-only only removes add, rename and
        // delete. First row on purpose: it is the most overdue, so the badge swings red to green.
        await ClickAsync(page.Locator("[data-habits-step-6] button[role='gridcell'].border-primary-subtle").First);
        await Beat(page);

        // The id form of the checkbox is the read-only one, so this is a list row, not a detail.
        await ClickAsync(page.Locator("[data-habits-step-5] input[id^='item-']").First);
        await Beat(page);

        // The detail earns its place with the month calendar and the history, not with ticking.
        await ClickAsync(page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasTextString = "Morning run" }));
        await Beat(page);

        await ClickAsync(page.Locator("[data-habits-step-11]")); // close
        await Beat(page);

        await ClickAsync(page.Locator("[data-main-step-4]")); // Tasks
        await Beat(page);

        await ClickAsync(page.Locator("[data-tasks-step-4]").First); // done from the list, one click
        await Beat(page);

        await ClickAsync(page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasTextString = "quarterly report" }));
        await Beat(page);

        await ClickAsync(page.Locator("[data-tasks-step-14] button")); // the timer exists only here
        await page.WaitForTimeoutAsync(3000); // the only thing a still cannot show

        await ClickAsync(page.Locator("[data-tasks-step-10]")); // close
        await Beat(page);

        // No note is opened: the list renders the markdown itself (data-notes-step-3), so reading
        // a note costs no clicks at all. Opening one would only show the editor.
        await ClickAsync(page.Locator("[data-main-step-3]")); // Notes
        await page.WaitForTimeoutAsync(6500); // hold on the rendered markdown - lands the take at ~29 s
    }

    // The filter half is shared; what differs is that a phone sidebar covers the list, so the
    // payoff of filtering is invisible until it closes.
    private static async Task FilterAsync(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-6]")); // search toggle
        await Beat(page);

        await MoveToAsync(page.Locator("[data-search-step-1]"));
        await page.Locator("[data-search-step-1]").PressSequentiallyAsync("run", new LocatorPressSequentiallyOptions { Delay = 200 });
        await Beat(page);

        await ClickAsync(page.Locator("[data-search-step-3]")); // clear
        await Beat(page);

        // English only, and only in a capture harness - the shoot is en-US by design.
        await ClickAsync(page.GetByText("Priorities:"));
        await page.WaitForTimeoutAsync(200); // a wipe, not a state: with nothing selected the list is empty

        await ClickAsync(page.Locator("label[for='Priority.VeryHigh']"));
        await Beat(page);
    }

    private static async Task SettingsAsync(IPage page, int themeSteps)
    {
        await ClickAsync(page.Locator("[data-main-step-1]")); // menu
        await Beat(page);

        await ClickAsync(page.Locator("div[role='menu'] button:has(i.bi-gear)"));
        await Beat(page);

        await ClickAsync(page.Locator("label[for='IsDarkMode']")); // dark to light, the whole app repaints
        await Beat(page);

        await ClickAsync(page.Locator("label[for='IsDarkMode']")); // and back
        await Beat(page);

        foreach (string toggle in new[] { "ShowHelp", "ShowPriorityDropdown", "ShowItemList" })
        {
            await ClickAsync(page.Locator($"label[for='{toggle}']"));
            await Beat(page);
        }

        ILocator theme = page.Locator("select[aria-label='Theme']");
        for (int step = 1; step <= themeSteps; step++)
        {
            await MoveToAsync(theme);
            await theme.SelectOptionAsync(new SelectOptionValue { Index = step * 4 });
            await Beat(page);
        }
    }

    private static async Task SidebarDesktop(IPage page)
    {
        await FilterAsync(page);
        await SettingsAsync(page, themeSteps: 2); // the menu replaces the sidebar - no close needed
        await page.WaitForTimeoutAsync(4500); // ~29 s, a second under Apple's hard 30
    }

    private static async Task SidebarMobile(IPage page)
    {
        await FilterAsync(page);

        await ClickAsync(page.Locator("#closeSidebar"));
        await page.WaitForTimeoutAsync(1500); // this is the beat where the filtered list is seen

        await SettingsAsync(page, themeSteps: 1); // one theme: they read poorly at 1080 wide
        await page.WaitForTimeoutAsync(3500); // ~29 s
    }

    private async Task RecordVideo(string outputFile, string videoSize, int viewportWidth, int viewportHeight, bool mobile, Func<IPage, Task> scenario)
    {
        IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = viewportWidth, Height = viewportHeight },
            IgnoreHTTPSErrors = true
        });

        if (mobile)
            await SetupFakeMobileCursorAsync(context);
        else
            await SetupFakeCursorAsync(context);

        IPage page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        int offsetX = await page.EvaluateAsync<int>("window.screenX + (window.outerWidth - window.innerWidth) / 2");
        int offsetY = await page.EvaluateAsync<int>("window.screenY + window.outerHeight - window.innerHeight - 2");

        Directory.CreateDirectory("videos");

        await GotoBaseUrl(page);
        await SeedAsync(page, Screenshot.SeedData.Write(Path.GetFullPath("seed-video.json")));

        // Apple App Store — App Preview requirements:
        // https://developer.apple.com/help/app-store-connect/reference/app-information/app-preview-specifications/
        //   Duration:    15–30 seconds
        //   Max size:    500 MB
        //   Max fps:     30
        //   Formats:     .mov, .m4v, .mp4
        //   H.264:       10–12 Mbps, High Profile Level 4.0, progressive
        //   ProRes HQ:   ~220 Mbps VBR, progressive, .mov only
        //   Audio:       stereo, AAC 256 kbps or PCM, 44.1/48 kHz
        //   Resolutions: iPhone 886×1920 (portrait) / 1920×886 (landscape)
        //                iPad 900–1600 px (varies by model)
        //                Mac / Apple TV 1920×1080
        //                Vision Pro 3840×2160

        // Microsoft Store — Trailer requirements:
        // https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images
        //   Duration:    ≤ 60 seconds recommended
        //   Max size:    < 2 GB
        //   Resolution:  1920×1080
        //   Formats:     .mp4 or .mov
        //   Thumbnail:   PNG, 1920×1080
        //   MP4 (H.264/AVC1): High Profile, progressive, 50 Mbps, 4:2:0, AAC-LC 384 kbps stereo / 512 kbps surround, 48 kHz
        //   MOV (ProRes): 1080p ProRes HQ, native fps (29.97 preferred), stereo −16 LKFS/LUFS

        using Process ffmpeg = new();
        ffmpeg.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            // -f lavfi -i anullsrc=r=48000:cl=stereo: silent stereo audio source at 48 kHz — Apple requires stereo (anullsrc defaults to mono) and 44.1/48 kHz sample rate; Microsoft Partner Center silently hangs when uploading videos with no audio track
            // -c:v libx264: explicit H.264 video codec
            // -c:a aac -b:a 256k: encode the silent audio as AAC targeting 256 kbps — Apple specifies 256 kbps AAC; in practice AAC compresses silence to ~2 kbps regardless of the target, but the flag signals intent and Apple's validator appears to check codec/channels/sample-rate rather than actual bitrate
            // -pix_fmt yuv420p: ddagrab outputs bgra which libx264 encodes as yuv444p (High 4:4:4 Predictive profile) — many upload portals reject this; yuv420p uses the standard High profile accepted everywhere
            // -movflags +faststart: moves the moov atom (metadata) to the beginning of the file — without this, web-based uploaders that need to read metadata before the full file is received will silently hang
            // -shortest: stop encoding when the shortest stream ends (the video), so the infinite silent audio source does not extend the output beyond the video duration
            // framerate=30: Apple App Store caps at 30 fps; at 60 fps libx264 produces H.264 Level 4.2 which exceeds Apple's Level 4.0 limit — 30 fps keeps the level at 4.0 and also matches Microsoft's preferred 29.97 fps
            // -level 4.0: explicitly cap H.264 level — without this, libx264 inherits the source level even after dropping to 30 fps
            // -crf 18: high quality constant-rate-factor encode; Apple's 10–12 Mbps figure is a target, not a hard limit — CRF produces lower bitrates on short clips which is acceptable
            Arguments = $"-y -f lavfi -i \"ddagrab=output_idx=0:framerate=30:offset_x={offsetX}:offset_y={offsetY}:video_size={videoSize}:draw_mouse=0\" -vf \"hwdownload,format=bgra\" -f lavfi -i \"anullsrc=r=48000:cl=stereo\" -c:v libx264 -c:a aac -b:a 256k -pix_fmt yuv420p -level 4.0 -movflags +faststart -crf 18 -preset slow -shortest {outputFile}",
            UseShellExecute = false,
            RedirectStandardInput = true,
        };
        ffmpeg.Start();

        await scenario(page);

        if (!ffmpeg.HasExited)
            await ffmpeg.StandardInput.WriteAsync('q'); // graceful FFmpeg shutdown — finalizes the MP4 container
        await ffmpeg.WaitForExitAsync();

        await context.CloseAsync();
    }

    //[Test]
    public async Task Desktop_Main() =>
        await RecordVideo("videos/desktop-main.mp4", "1920x1080", 1920, 1086, false, Main); // 1086: +6 for the Chromium height discrepancy on Windows - see VideoTests.cs

    //[Test]
    public async Task Desktop_Sidebar() =>
        await RecordVideo("videos/desktop-sidebar.mp4", "1920x1080", 1920, 1086, false, SidebarDesktop);

    // Recorded with the Windows display rotated to Portrait: 1440x3440 fits Apple's 886x1920
    // preview natively, which retires the old 500x1084 upscale and the SAR it left behind.
    // Re-measure offsetX/offsetY after rotating - the values in VideoTests.cs were taken in landscape.
    //[Test]
    public async Task Mobile_Main() =>
        await RecordVideo("videos/mobile-main.mp4", "886x1920", 886, 1926, true, Main);

    //[Test]
    public async Task Mobile_Sidebar() =>
        await RecordVideo("videos/mobile-sidebar.mp4", "886x1920", 886, 1926, true, SidebarMobile);
}
