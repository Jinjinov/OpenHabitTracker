using System.Diagnostics;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace OpenHabitTracker.EndToEndTests;

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

    [Test]
    public async Task RecordDesktopVideo()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsetX = await page.EvaluateAsync<int>("window.screenX");
        var offsetY = await page.EvaluateAsync<int>("window.screenY + window.outerHeight - window.innerHeight");

        Directory.CreateDirectory("videos");

        using var ffmpeg = new Process();
        ffmpeg.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -f gdigrab -framerate 60 -offset_x {offsetX} -offset_y {offsetY} -video_size 1920x1080 -i desktop -crf 18 -preset slow videos/desktop.mp4",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardError = true,
        };
        ffmpeg.Start();
        Console.WriteLine($"FFmpeg offset: {offsetX},{offsetY}");

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

        await ffmpeg.StandardInput.WriteAsync('q'); // graceful FFmpeg shutdown — finalizes the MP4 container
        await ffmpeg.WaitForExitAsync();

        var ffmpegError = await ffmpeg.StandardError.ReadToEndAsync();
        if (!string.IsNullOrEmpty(ffmpegError))
            Console.WriteLine($"FFmpeg output:\n{ffmpegError}");
    }
}
