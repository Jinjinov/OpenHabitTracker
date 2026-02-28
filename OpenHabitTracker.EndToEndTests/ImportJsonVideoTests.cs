using System.Diagnostics;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace OpenHabitTracker.EndToEndTests;

[TestFixture]
public class ImportJsonVideoTests : PlaywrightTest
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
        var box = await locator.BoundingBoxAsync();
        if (box == null) return;
        await locator.Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2, new MouseMoveOptions { Steps = 20 }); // move cursor smoothly to element center
    }

    private static async Task ClickAsync(ILocator locator)
    {
        await MoveToAsync(locator); // move cursor smoothly to element before hovering
        await locator.HoverAsync(); // trigger :hover CSS state
        await locator.Page.WaitForTimeoutAsync(500); // pause on hover so :hover state is visible
        await locator.ClickAsync(new LocatorClickOptions { Delay = 500 }); // hold mousedown to show :active CSS state
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

    private async Task RunDemoScript(IPage page)
    {
        // 1. menu → Data → import demo data
        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(2000); // wait for Blazor OnAfterRenderAsync to finish
        if (!await page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" }).IsVisibleAsync())
            await ClickAsync(page.Locator("[data-main-step-1]")); // menu toggle button (three dots) — open only if Data button not already visible
        await page.WaitForTimeoutAsync(2000);
        await ClickAsync(page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Data" })); // Data button in menu sidebar
        await page.Locator("input[type=file]").SetInputFilesAsync("TestData/demo-data.json"); // hidden file input for JSON import
        await page.WaitForTimeoutAsync(2000);

        // 2. /notes — open markdown note, pause to show rendered content
        await ClickAsync(page.Locator("[data-main-step-3]")); // Notes nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-notes-step-2]").First); // note title button — opens note detail
        await page.WaitForTimeoutAsync(3000);
        await ClickAsync(page.Locator("[data-notes-step-7]")); // Close button in note detail
        await page.WaitForTimeoutAsync(1000);

        // 3. /tasks — mark "Team standup" (due today) as done
        await ClickAsync(page.Locator("[data-main-step-4]")); // Tasks nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator(".input-group")
            .Filter(new LocatorFilterOptions { HasText = "Team standup" })
            .Locator("[data-tasks-step-4]")); // mark-as-done button in task row
        await page.WaitForTimeoutAsync(3000);

        // 4. /habits — open "Morning run" to show calendar dots
        await ClickAsync(page.Locator("[data-main-step-5]")); // Habits nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);
        await ClickAsync(page.Locator("[data-habits-step-2]") // habit title button — opens habit detail
            .Filter(new LocatorFilterOptions { HasText = "Morning run" }));
        await page.WaitForTimeoutAsync(4000);
        await ClickAsync(page.Locator("[data-habits-step-11]")); // Close button in habit detail
        await page.WaitForTimeoutAsync(1000);

        // 5. search icon in top bar — type search term
        await ClickAsync(page.Locator("[data-main-step-6]")); // Search toggle button in top bar
        await page.WaitForTimeoutAsync(1000);
        await MoveToAsync(page.Locator("[data-search-step-1]")); // move cursor to search input
        await page.Locator("[data-search-step-1]").PressSequentiallyAsync("run", new LocatorPressSequentiallyOptions { Delay = 200 }); // search input field — typed char by char
        await page.WaitForTimeoutAsync(2000);

        // 6. navigate to Home — shows all notes, tasks and habits together
        await ClickAsync(page.Locator("[data-main-step-2]")); // Home nav link in top bar
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1000);

        // 7. menu → Settings — toggle off dark mode, then change theme to show visual change
        await ClickAsync(page.Locator("[data-main-step-1]")); // menu toggle button (three dots)
        await page.WaitForTimeoutAsync(2000);
        await ClickAsync(page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Settings" })); // Settings button in menu sidebar
        await page.WaitForTimeoutAsync(1000);
        if (await page.Locator("#IsDarkMode").IsCheckedAsync())
            await ClickAsync(page.Locator("label[for='IsDarkMode']")); // Dark mode label — click to turn off dark mode
        await page.WaitForTimeoutAsync(2000);
        await page.Locator("[data-settings-step-2] select").SelectOptionAsync("united"); // theme selector dropdown
        await page.WaitForTimeoutAsync(3000);
    }

    [Test]
    public async Task RecordDesktopVideo()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1086 }, // +6 for Chromium height rendering discrepancy on Windows — see VideoTests.cs comment block
            IgnoreHTTPSErrors = true
        });

        await SetupFakeCursorAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsetX = await page.EvaluateAsync<int>("window.screenX + (window.outerWidth - window.innerWidth) / 2");
        var offsetY = await page.EvaluateAsync<int>("window.screenY + window.outerHeight - window.innerHeight - 2");

        Directory.CreateDirectory("videos");

        using var ffmpeg = new Process();
        ffmpeg.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -f lavfi -i \"ddagrab=output_idx=0:framerate=60:offset_x={offsetX}:offset_y={offsetY}:video_size=1920x1080:draw_mouse=0\" -vf \"hwdownload,format=bgra\" -crf 18 -preset slow videos/import-json-desktop.mp4",
            UseShellExecute = false,
            RedirectStandardInput = true,
        };
        ffmpeg.Start();

        await RunDemoScript(page);

        if (!ffmpeg.HasExited)
            await ffmpeg.StandardInput.WriteAsync('q'); // graceful FFmpeg shutdown — finalizes the MP4 container
        await ffmpeg.WaitForExitAsync();

        await context.CloseAsync();
    }

    [Test]
    public async Task RecordMobileVideo()
    {
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 500, Height = 1090 }, // original 886×1920 aspect ratio scaled to 500×1084, +6 for Chromium height rendering discrepancy on Windows
            IgnoreHTTPSErrors = true
        });

        await SetupFakeMobileCursorAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var offsetX = await page.EvaluateAsync<int>("window.screenX + (window.outerWidth - window.innerWidth) / 2");
        var offsetY = await page.EvaluateAsync<int>("window.screenY + window.outerHeight - window.innerHeight - 2");

        Directory.CreateDirectory("videos");

        using var ffmpeg = new Process();
        ffmpeg.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -f lavfi -i \"ddagrab=output_idx=0:framerate=60:offset_x={offsetX}:offset_y={offsetY}:video_size=500x1084:draw_mouse=0\" -vf \"hwdownload,format=bgra\" -crf 18 -preset slow videos/import-json-mobile.mp4",
            UseShellExecute = false,
            RedirectStandardInput = true,
        };
        ffmpeg.Start();

        await RunDemoScript(page);

        if (!ffmpeg.HasExited)
            await ffmpeg.StandardInput.WriteAsync('q'); // graceful FFmpeg shutdown — finalizes the MP4 container
        await ffmpeg.WaitForExitAsync();

        await context.CloseAsync();
    }
}
