namespace OpenHabitTracker.EndToEndTests.Accessibility;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class SidebarBehaviorTests : BaseTest
{
    [SetUp]
    public async Task SetUp() => await GotoAsync();

    [Test]
    public async Task MenuToggle_Click_OpensSidebar()
    {
        await Page.Locator("[data-main-step-1]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("div.col-12.col-md-2.child-column")).ToBeVisibleAsync();
    }

    [Test]
    public async Task MenuToggle_ClickTwice_ClosesSidebar()
    {
        await Page.Locator("[data-main-step-1]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-main-step-1]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("div.col-12.col-md-2.child-column")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Sidebar_EscapeKey_ClosesSidebar()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("#closeSidebar").FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("div.col-12.col-md-2.child-column")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task SearchToggle_Click_OpensSidebar()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("#closeSidebar")).ToBeVisibleAsync();
    }

    [Test]
    public async Task SearchToggle_ClickTwice_ClosesSidebar()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("div.col-12.col-md-2.child-column")).Not.ToBeVisibleAsync();
    }
}
