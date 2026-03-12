namespace OpenHabitTracker.EndToEndTests.Accessibility;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class CalendarKeyboardTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await Page.Locator("[data-main-step-5]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        await AddItemAsync("Keyboard Test Habit");
    }

    [Test]
    public async Task Calendar_ArrowRight_MovesFocusToNextCell()
    {
        await Page.Locator("div[role='grid'] div[role='row'] button[role='gridcell']").First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Expect(Page.Locator("div[role='grid'] div[role='row'] button[role='gridcell']").Nth(1)).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_ArrowLeft_MovesFocusToPreviousCell()
    {
        ILocator cells = Page.Locator("div[role='grid'] div[role='row'] button[role='gridcell']");

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(cells.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Expect(cells.First).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_ArrowDown_MovesFocusDownOneRow()
    {
        ILocator cells = Page.Locator("div[role='grid'] div[role='row'] button[role='gridcell']");

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(cells.Nth(7)).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_ArrowUp_MovesFocusUpOneRow()
    {
        ILocator cells = Page.Locator("div[role='grid'] div[role='row'] button[role='gridcell']");

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(cells.Nth(7)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");

        await Expect(cells.First).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_EndKey_MovesFocusToLastCellOfRow()
    {
        ILocator cells = Page.Locator("div[role='grid'] div[role='row'] button[role='gridcell']");

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("End");

        await Expect(cells.Nth(6)).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_HomeKey_MovesFocusToFirstCellOfRow()
    {
        ILocator cells = Page.Locator("div[role='grid'] div[role='row'] button[role='gridcell']");

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(cells.Nth(6)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");

        await Expect(cells.First).ToBeFocusedAsync();
    }
}
