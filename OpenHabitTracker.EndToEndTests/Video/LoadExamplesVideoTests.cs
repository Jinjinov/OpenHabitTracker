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

    private static async Task LoadExamples(IPage page)
    {
        if (!await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" }).IsVisibleAsync())
        {
            await ClickAsync(page.Locator("[data-main-step-1]")); // menu toggle button (three dots) — open only if Data button not already visible
            await page.WaitForTimeoutAsync(1000);
        }

        await ClickAsync(page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" })); // Data button in menu sidebar
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-data-step-1]")); // Load examples button in Data sidebar
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("#closeSidebar")); // close sidebar button
        await page.WaitForTimeoutAsync(1000);
    }

    private static async Task ShowNotes(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-3]")); // Notes nav link in top bar
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-notes-step-2]").First); // note title button — opens note detail
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-notes-step-7]")); // Close button in note detail
        await page.WaitForTimeoutAsync(1000);
    }

    private static async Task ShowTasks(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-4]")); // Tasks nav link in top bar
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-tasks-step-2]").First); // task title button — opens task detail
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-tasks-step-10]")); // Close button in task detail
        await page.WaitForTimeoutAsync(1000);
    }

    private static async Task ShowHabits(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-5]")); // Habits nav link in top bar
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-habits-step-2]").First); // habit title button — opens habit detail
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-habits-step-11]")); // Close button in habit detail
        await page.WaitForTimeoutAsync(1000);
    }

    private static async Task ShowHome(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-2]")); // Home nav link in top bar
        await page.WaitForTimeoutAsync(1000);
    }

    private static async Task ShowSearch(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-6]")); // Search toggle button in top bar
        await page.WaitForTimeoutAsync(1000);
        await MoveToAsync(page.Locator("[data-search-step-1]")); // move cursor to search input
        await page.Locator("[data-search-step-1]").PressSequentiallyAsync("daily", new LocatorPressSequentiallyOptions { Delay = 200 }); // search input field — typed char by char
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-search-step-3]")); // clear search term button (x)
        await page.WaitForTimeoutAsync(1000);
    }

    private static async Task ShowSettings(IPage page)
    {
        await ClickAsync(page.Locator("[data-main-step-1]")); // menu toggle button (three dots)
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Settings" })); // Settings button in menu sidebar
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("label[for='ShowHelp']")); // Show help checkbox — toggle
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("label[for='ShowPriorityDropdown']")); // Show priority checkbox — toggle
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("label[for='ShowItemList']")); // Show item list checkbox — toggle
        await page.WaitForTimeoutAsync(1000);
    }

    private static async Task ShowMainContent(IPage page)
    {
        await LoadExamples(page);

        await ShowNotes(page);
        await ShowTasks(page);
        await ShowHabits(page);
    }

    private static async Task ShowSidebar(IPage page)
    {
        await LoadExamples(page);

        await ShowHome(page);
        await ShowSearch(page);
        await ShowSettings(page);
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
            // -c:a aac -b:a 256k: encode the silent audio as AAC at 256 kbps — Apple explicitly requires 256 kbps AAC; without -b:a the encoder uses ~2 kbps on silence which fails validation
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
    public async Task Desktop_ShowMainContent_C_inetpub_wwwroot() =>
        await RecordVideo("videos/desktop-main.mp4", "1920x1080", 1920, 1086, false, ShowMainContent); // 1086: +6 for Chromium height rendering discrepancy on Windows — see VideoTests.cs comment block

    //[Test]
    public async Task Desktop_ShowSidebar_C_inetpub_wwwroot() =>
        await RecordVideo("videos/desktop-sidebar.mp4", "1920x1080", 1920, 1086, false, ShowSidebar); // 1086: +6 for Chromium height rendering discrepancy on Windows — see VideoTests.cs comment block

    // NOTE: mobile videos are recorded at 500×1084 (scaled from Apple's required 886×1920) because a 3440×1440 monitor is only 1440px tall — ddagrab cannot capture taller than the physical display.
    // Two approaches that do NOT work:
    //   1. Playwright built-in RecordVideo: captures browser content at any size without ddagrab, but output quality is too poor for store submission.
    //   2. ffmpeg scale=886:1920 upscale: 500/1084 ≈ 886/1920 aspect ratios look identical on paper, but the re-encode produces sample_aspect_ratio: 120000:120053 (non-square pixels) which causes Apple's validator to compute display dimensions different from the encoded dimensions and reject the file.
    // Two real fixes: (1) rotate the Windows display to portrait via Display Settings → Display orientation → Portrait (3440×1440 becomes 1440×3440, so 886×1920 fits); restore to Landscape after recording.
    //               (2) a virtual monitor driver (e.g. parsec-vdd) that creates a display tall enough to fit 886×1920 plus Chromium window chrome (~88px) plus invisible DWM borders (~8px);
    //                   the offsetX/offsetY calculation is already dynamic so it adapts, but the hardcoded +6 viewport height correction and -2 offsetY correction (see VideoTests.cs quirks comment)
    //                   were measured empirically on a physical display and may need to be re-measured if the virtual driver reports window chrome or DWM borders differently.

    //[Test]
    public async Task Mobile_ShowMainContent_C_inetpub_wwwroot() =>
        await RecordVideo("videos/mobile-main.mp4", "500x1084", 500, 1090, true, ShowMainContent); // 1090: original 886×1920 aspect ratio scaled to 500×1084, +6 for Chromium height rendering discrepancy on Windows

    //[Test]
    public async Task Mobile_ShowSidebar_C_inetpub_wwwroot() =>
        await RecordVideo("videos/mobile-sidebar.mp4", "500x1084", 500, 1090, true, ShowSidebar); // 1090: original 886×1920 aspect ratio scaled to 500×1084, +6 for Chromium height rendering discrepancy on Windows
}
