using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// dotnet run --project OpenHabitTracker.Blazor.Web --configuration Release

public abstract class BaseTest : PlaywrightTest
{
    protected const string BaseUrl = "http://localhost";

    protected IBrowser Browser = null!;
    protected IBrowserContext Context = null!;
    protected IPage Page = null!;

    [SetUp]
    public async Task BaseSetUp()
    {
        Browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });
        Page = await Context.NewPageAsync();
    }

    [TearDown]
    public async Task BaseTearDown()
    {
        await Context.CloseAsync();
        await Browser.CloseAsync();
    }

    protected async Task GotoAsync(string path = "")
    {
        await Page.GotoAsync(BaseUrl + "/" + path.TrimStart('/'));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500); // OnAfterRenderAsync completes after NetworkIdle
    }

    protected async Task OpenMenuAsync()
    {
        await Page.Locator("[data-main-step-1]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    protected async Task OpenSidebarAsync(string buttonIconClass)
    {
        await OpenMenuAsync();
        await Page.Locator($"button:has(i.{buttonIconClass})").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    protected async Task CloseSidebarAsync()
    {
        await Page.Locator("#closeSidebar").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    protected async Task LoadExamplesViaUiAsync()
    {
        if (!await Page.Locator("[data-data-step-1]").IsVisibleAsync())
        {
            await OpenSidebarAsync("bi-database");
        }
        await Page.Locator("[data-data-step-1]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CloseSidebarAsync();
    }

    protected async Task NavigateToAsync(string dataStepSelector)
    {
        await Page.Locator(dataStepSelector).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500); // OnAfterRenderAsync completes after NetworkIdle
    }

    protected async Task AddItemAsync(string title)
    {
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync(title);
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync(); // wait for Blazor to process oninput before clicking
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500); // allow Blazor re-render to propagate
    }
}
