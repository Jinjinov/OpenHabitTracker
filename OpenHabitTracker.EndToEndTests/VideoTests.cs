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
            ViewportSize = new ViewportSize { Width = 1920, Height = 1086 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsetX = await page.EvaluateAsync<int>("window.screenX + (window.outerWidth - window.innerWidth) / 2");
        var offsetY = await page.EvaluateAsync<int>("window.screenY + window.outerHeight - window.innerHeight - 2");

        var innerWidth = await page.EvaluateAsync<int>("window.innerWidth");
        var innerHeight = await page.EvaluateAsync<int>("window.innerHeight");
        var outerWidth = await page.EvaluateAsync<int>("window.outerWidth");
        var outerHeight = await page.EvaluateAsync<int>("window.outerHeight");
        Console.WriteLine($"offsetX={offsetX} offsetY={offsetY} innerWidth={innerWidth} innerHeight={innerHeight} outerWidth={outerWidth} outerHeight={outerHeight}");
        Console.WriteLine($"outerWidth - innerWidth={outerWidth - innerWidth} outerHeight - innerHeight={outerHeight - innerHeight}");
        // offsetX=108 offsetY=86 innerWidth=1920 innerHeight=1086 outerWidth=1936 outerHeight=1174
        // outerWidth - innerWidth = 16 outerHeight - innerHeight = 88

        Directory.CreateDirectory("videos");

        using var ffmpeg = new Process();
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

        await context.CloseAsync();

        if (!ffmpeg.HasExited)
            await ffmpeg.StandardInput.WriteAsync('q'); // graceful FFmpeg shutdown — finalizes the MP4 container
        await ffmpeg.WaitForExitAsync();
    }
}
