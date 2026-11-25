namespace OpenHabitTracker.EndToEndTests.Accessibility;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

[TestFixture]
public class CalendarKeyboardTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await Page.Locator("[data-main-step-5]").ClickAsync();
        await Expect(Page.Locator("button.btn-plain.input-group")).ToBeVisibleAsync();

        await AddItemAsync("Keyboard Test Habit");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Keyboard Test Habit" }).ClickAsync();
        await Expect(Page.Locator("[data-habits-step-13]")).ToBeVisibleAsync();
    }

    private ILocator LargeCalendarCells() =>
        Page.Locator("[data-habits-step-13] div[role='grid'] div[role='row'] button[role='gridcell']");

    [Test]
    public async Task Calendar_ArrowRight_MovesFocusToNextCell()
    {
        await LargeCalendarCells().First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Expect(LargeCalendarCells().Nth(1)).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_ArrowLeft_MovesFocusToPreviousCell()
    {
        ILocator cells = LargeCalendarCells();

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(cells.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Expect(cells.First).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_ArrowDown_MovesFocusDownOneRow()
    {
        ILocator cells = LargeCalendarCells();

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(cells.Nth(7)).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_ArrowUp_MovesFocusUpOneRow()
    {
        ILocator cells = LargeCalendarCells();

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(cells.Nth(7)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");

        await Expect(cells.First).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_EndKey_MovesFocusToLastCellOfRow()
    {
        ILocator cells = LargeCalendarCells();

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("End");

        await Expect(cells.Nth(6)).ToBeFocusedAsync();
    }

    [Test]
    public async Task Calendar_HomeKey_MovesFocusToFirstCellOfRow()
    {
        ILocator cells = LargeCalendarCells();

        await cells.First.ClickAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(cells.Nth(6)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");

        await Expect(cells.First).ToBeFocusedAsync();
    }

    private ILocator SmallCalendarCells() =>
        Page.Locator("[data-habits-step-6] button[role='gridcell']");

    // The sidebar takes width from the list, so the small calendar gets another length.
    [Test]
    public async Task SmallCalendar_ArrowKeysAfterTheSidebarOpensAndCloses_MoveTheFocus()
    {
        ILocator cells = SmallCalendarCells();

        await Expect(cells.First).ToBeVisibleAsync();
        int wide = await cells.CountAsync();

        await OpenSidebarAsync("bi-gear");
        await Expect(cells).Not.ToHaveCountAsync(wide);

        await cells.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(cells.Nth(1)).ToBeFocusedAsync();

        await CloseSidebarAsync();
        await Expect(cells).ToHaveCountAsync(wide);

        await cells.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(cells.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(cells.Nth(wide - 1)).ToBeFocusedAsync();
    }

    [Test]
    public async Task SmallCalendar_ArrowKeyAfterItsLengthChanges_MovesTheFocus()
    {
        ILocator cells = SmallCalendarCells();

        await OpenSidebarAsync("bi-gear");

        // data-settings-step-11 wraps the max small calendar days select
        await Page.Locator("[data-settings-step-11] select").SelectOptionAsync("3");
        await Expect(cells).ToHaveCountAsync(3);

        await cells.First.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(cells.Nth(1)).ToBeFocusedAsync();
    }
}
