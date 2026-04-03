using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

public abstract class BaseTest : PlaywrightTest
{
    protected const string BaseUrl = "http://localhost";

    protected IBrowser Browser = null!;
    protected IBrowserContext Context = null!;
    protected IPage Page = null!;

    private List<string> _browserErrors = new();

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
        Page.PageError += (_, error) => _browserErrors.Add($"PageError: {error}");
        Page.Console += (_, msg) => { if (msg.Type == "error") _browserErrors.Add($"Console: {msg.Text}"); };
        Page.RequestFailed += (_, request) => _browserErrors.Add($"RequestFailed: {request.Url}");
    }

    [TearDown]
    public async Task BaseTearDown()
    {
        try
        {
            await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
            if (_browserErrors.Count > 0)
                Assert.Fail("Browser errors during test:\n" + string.Join("\n", _browserErrors));
        }
        finally
        {
            await Context.CloseAsync();
            await Browser.CloseAsync();
        }
    }

    protected async Task GotoAsync(string path = "")
    {
        await Page.GotoAsync(BaseUrl + "/" + path.TrimStart('/'));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync(); // OnAfterRenderAsync completes after NetworkIdle
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
        await Expect(Page.Locator("main#main-content")).ToBeVisibleAsync(); // OnAfterRenderAsync completes after NetworkIdle
    }

    protected async Task AddItemAsync(string title)
    {
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync(title);
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync(); // wait for Blazor to process oninput before clicking
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToHaveCountAsync(0); // wait for add form to close
    }

    protected async Task CreateCategoryAsync(string categoryName)
    {
        await OpenMenuAsync();
        await Page.Locator("div[role='menu'] button:has(i.bi-tag)").ClickAsync();
        await Expect(Page.Locator("[data-categories-step-1] input")).ToBeVisibleAsync();
        await Page.Locator("[data-categories-step-1] input").FillAsync(categoryName);
        await Page.Locator("[data-categories-step-1] button:has(i.bi-plus-square)").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CloseSidebarAsync();
    }

    protected async Task EnableGroupedByCategoryAsync()
    {
        await OpenSidebarAsync("bi-gear");
        await Page.Locator("label[for='ShowGroupedByCategory']").ClickAsync();
        await CloseSidebarAsync();
    }
}
