namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class ItemTests : BaseTest
{
    [Test]
    public async Task AddItem_ToHabit_ItemAppearsInList()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Habit With Items");

        // Open the habit to access the edit component (which shows ItemsComponent in editable mode)
        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit With Items" }).ClickAsync();

        // Add a sub-item via the ItemsComponent
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Morning stretch");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Morning stretch" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddItem_ToTask_ItemAppearsInList()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Task With Items");

        // Open the task to access the edit component
        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Task With Items" }).ClickAsync();

        // Add a sub-item
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Step one");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Step one" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CheckAndUncheckItem_TogglesIsDone()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Habit For Checkbox");

        // Open the habit
        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit For Checkbox" }).ClickAsync();

        // Add a sub-item
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Check me");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();

        // Close the edit component to see the read-only list view
        await Page.Locator("[data-habits-step-11]").ClickAsync(); // Close button

        // Ensure ShowItemList is on (items are visible in read-only view under data-habits-step-5)
        ILocator checkbox = Page.Locator("[data-habits-step-5] input[type='checkbox']").First;

        // Check it
        await checkbox.CheckAsync();
        await Expect(checkbox).ToBeCheckedAsync();

        // Uncheck it
        await checkbox.UncheckAsync();
        await Expect(checkbox).Not.ToBeCheckedAsync();
    }
}
