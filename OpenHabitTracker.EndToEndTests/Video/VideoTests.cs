using Microsoft.Playwright;
using System.Diagnostics;

namespace OpenHabitTracker.EndToEndTests.Video;

// install browsers: pwsh playwright.ps1 install

[TestFixture]
public class VideoTests : PlaywrightTest
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

    /*
     * WINDOWS + CHROMIUM + FFMPEG QUIRKS — why this code looks the way it does
     *
     * ── 1. --window-position=100,0 instead of 0,0 ──────────────────────────────
     * Windows 10 DWM adds invisible resize borders around every window. These borders
     * exist as mouse hit-test areas for resizing but are not rendered visually.
     * GetWindowRect() (which Chromium uses internally) returns bounds that INCLUDE
     * these invisible borders, so --window-position=0,0 actually places the visible
     * window edge 8px off-screen to the left. Using 100,0 keeps the window fully on
     * screen with comfortable margin.
     * Reference: https://www.w3tutorials.net/blog/getwindowrect-returns-a-size-including-invisible-borders/
     *
     * ── 2. ViewportSize.Height = 1086 instead of 1080 ──────────────────────────
     * Chromium on Windows renders 6px less content than the requested ViewportSize
     * height. Requesting 1080 results in ~1076px of visible content. Requesting 1086
     * results in exactly 1080px of visible content (confirmed by FFmpeg output).
     * window.innerHeight still reports the requested value (1086), not the actual
     * rendered pixels, so it cannot be used to detect this discrepancy.
     * This appears to be caused by DWM compositor frame reservation and Chromium's
     * own non-client area handling on Windows.
     * Reference: https://chromium.googlesource.com/chromium/src/+/bde885b4e8cf11db7ba2af6c70a6580830a52e7a/ui/views/win/hwnd_message_handler.cc
     *
     * ── 3. gdigrab produces black video — must use ddagrab ──────────────────────
     * FFmpeg's gdigrab uses the GDI screen capture API, which cannot capture
     * hardware-accelerated (GPU-rendered) windows. Chromium renders via DirectX/GPU
     * by default, so gdigrab captures a black rectangle.
     * ddagrab uses the Desktop Duplication API (DirectX 11), which correctly captures
     * GPU-composited content. ddagrab is a lavfi filter source, not an input device,
     * so the correct syntax is:
     *   -f lavfi -i "ddagrab=output_idx=0:..." -vf "hwdownload,format=bgra"
     * NOT: -f ddagrab -i 0  (this fails with "Unknown input format: ddagrab")
     * hwdownload is required to copy frames from GPU memory to CPU memory before encoding.
     *
     * ── 4. offsetX = window.screenX + (outerWidth - innerWidth) / 2 ─────────────
     * window.screenX reports the X position of the outer window frame including the
     * invisible left resize border (8px). To find where the actual content starts,
     * we add half of (outerWidth - innerWidth), which equals the left border width:
     *   outerWidth - innerWidth = 16  (8px left + 8px right invisible border)
     *   (outerWidth - innerWidth) / 2 = 8
     * With --window-position=100,0: screenX=100, offsetX = 100 + 8 = 108
     *
     * ── 5. offsetY = window.screenY + outerHeight - innerHeight - 2 ─────────────
     * outerHeight - innerHeight = 88 (visible browser chrome + invisible bottom border).
     * The invisible bottom border is 2px, which overshoots the actual viewport top.
     * Subtracting 2 corrects for this:
     *   outerHeight - innerHeight = visible chrome (86px) + bottom invisible border (2px) = 88
     *   offsetY = screenY + 88 - 2 = 86  (actual Y where viewport content starts)
     * The -2 is a hardcoded empirical correction, NOT derivable from outerWidth/outerHeight.
     * Confirmed values at 100% DPI: offsetX=108, offsetY=86, outerWidth-innerWidth=16,
     * outerHeight-innerHeight=88
     * Reference: https://www.tutorialpedia.org/blog/create-window-without-titlebar-with-resizable-border-and-without-bogus-6px-white-stripe/
     *
     * ── 6. RedirectStandardError deadlock ───────────────────────────────────────
     * FFmpeg writes verbose progress output to stderr continuously throughout recording.
     * If you set RedirectStandardError=true and then call ReadToEndAsync() AFTER
     * WaitForExitAsync(), you get a deadlock:
     *   1. FFmpeg fills the stderr pipe buffer (~4KB)
     *   2. FFmpeg blocks waiting for the buffer to be drained
     *   3. Our code is blocked in WaitForExitAsync() waiting for FFmpeg to exit
     *   4. Neither side unblocks → deadlock
     * Fix: if stderr capture is needed, start ReadToEndAsync() as a concurrent Task
     * BEFORE calling WaitForExitAsync(), so it drains the buffer while FFmpeg runs.
     */

    //[Test]
    public async Task RecordDesktopVideo()
    {
        IBrowserContext context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1086 },
            IgnoreHTTPSErrors = true
        });

        IPage page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        int offsetX = await page.EvaluateAsync<int>("window.screenX + (window.outerWidth - window.innerWidth) / 2");
        int offsetY = await page.EvaluateAsync<int>("window.screenY + window.outerHeight - window.innerHeight - 2");

        int innerWidth = await page.EvaluateAsync<int>("window.innerWidth");
        int innerHeight = await page.EvaluateAsync<int>("window.innerHeight");
        int outerWidth = await page.EvaluateAsync<int>("window.outerWidth");
        int outerHeight = await page.EvaluateAsync<int>("window.outerHeight");
        Console.WriteLine($"offsetX={offsetX} offsetY={offsetY} innerWidth={innerWidth} innerHeight={innerHeight} outerWidth={outerWidth} outerHeight={outerHeight}");
        Console.WriteLine($"outerWidth - innerWidth={outerWidth - innerWidth} outerHeight - innerHeight={outerHeight - innerHeight}");
        // offsetX=108 offsetY=86 innerWidth=1920 innerHeight=1086 outerWidth=1936 outerHeight=1174
        // outerWidth - innerWidth = 16 outerHeight - innerHeight = 88

        Directory.CreateDirectory("videos");

        using Process ffmpeg = new();
        ffmpeg.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -f lavfi -i \"ddagrab=output_idx=0:framerate=60:offset_x={offsetX}:offset_y={offsetY}:video_size=1920x1080:draw_mouse=0\" -vf \"hwdownload,format=bgra\" -crf 18 -preset slow videos/desktop.mp4",
            UseShellExecute = false,
            RedirectStandardInput = true,
        };
        ffmpeg.Start();

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

        if (!ffmpeg.HasExited)
            await ffmpeg.StandardInput.WriteAsync('q'); // graceful FFmpeg shutdown — finalizes the MP4 container
        await ffmpeg.WaitForExitAsync();

        await context.CloseAsync();
    }
}
