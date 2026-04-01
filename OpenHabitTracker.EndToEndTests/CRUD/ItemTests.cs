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
    public async Task RenameItem_InHabit_UpdatesTitle()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Habit For Item Rename");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit For Item Rename" }).ClickAsync();

        // Add a sub-item
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Original Habit Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();
        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Original Habit Item" })).ToBeVisibleAsync();

        // Click the item title button to enter rename mode
        await Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Original Habit Item" }).ClickAsync();
        await Expect(Page.Locator("input[aria-label='Edit item']")).ToBeVisibleAsync();

        // Fill new title and Tab to trigger onchange + focusout
        await Page.Locator("input[aria-label='Edit item']").FillAsync("Renamed Habit Item");
        await Page.Locator("input[aria-label='Edit item']").PressAsync("Tab");

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Renamed Habit Item" })).ToBeVisibleAsync();
        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Original Habit Item" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteItem_FromHabit_ItemDisappearsFromList()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Habit For Item Delete");

        await Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "Habit For Item Delete" }).ClickAsync();

        // Add a sub-item
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Delete This Habit Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Delete This Habit Item" })).ToBeVisibleAsync();

        // Delete the sub-item
        await Page.Locator("div.input-group.flex-nowrap")
            .Filter(new LocatorFilterOptions { HasText = "Delete This Habit Item" })
            .Locator("button[aria-label='Delete']")
            .ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Delete This Habit Item" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteItem_FromTask_ItemDisappearsFromList()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Task For Item Delete");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Task For Item Delete" }).ClickAsync();

        // Add a sub-item
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Delete This Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Delete This Item" })).ToBeVisibleAsync();

        // Delete the sub-item
        await Page.Locator("div.input-group.flex-nowrap")
            .Filter(new LocatorFilterOptions { HasText = "Delete This Item" })
            .Locator("button[aria-label='Delete']")
            .ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Delete This Item" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task RenameItem_InTask_UpdatesTitle()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Task For Item Rename");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Task For Item Rename" }).ClickAsync();

        // Add a sub-item
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Original Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();
        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Original Item" })).ToBeVisibleAsync();

        // Click the item title button to enter rename mode
        await Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Original Item" }).ClickAsync();
        await Expect(Page.Locator("input[aria-label='Edit item']")).ToBeVisibleAsync();

        // Fill new title and Tab to trigger onchange + focusout
        await Page.Locator("input[aria-label='Edit item']").FillAsync("Renamed Item");
        await Page.Locator("input[aria-label='Edit item']").PressAsync("Tab");

        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Renamed Item" })).ToBeVisibleAsync();
        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Original Item" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Items_PersistedAfterReload()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Persistent Item Task");

        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Persistent Item Task" }).ClickAsync();

        await Page.Locator("input[aria-label='Add new item']").FillAsync("Persistent Sub-Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();
        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Persistent Sub-Item" })).ToBeVisibleAsync();

        await Page.Locator("[data-tasks-step-10]").ClickAsync(); // Close task edit

        await Page.ReloadAsync();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();

        await NavigateToAsync("[data-main-step-4]");

        // In the read-only list view, items render as labels inside data-tasks-step-5
        await Expect(Page.Locator("[data-tasks-step-5] label").Filter(new LocatorFilterOptions { HasText = "Persistent Sub-Item" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task TaskItem_IsDone_PersistedAfterReload()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Task With Checkable Item");

        // Open task edit and add sub-item
        await Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Task With Checkable Item" }).ClickAsync();
        await Page.Locator("input[aria-label='Add new item']").FillAsync("Check Me Item");
        await Page.Locator("button[aria-label='Add']:has(i.bi-plus-square)").ClickAsync();
        await Expect(Page.Locator("button.input-group-text.flex-grow-1.text-wrap").Filter(new LocatorFilterOptions { HasText = "Check Me Item" })).ToBeVisibleAsync();

        // Close to return to read-only list view
        await Page.Locator("[data-tasks-step-10]").ClickAsync();

        // Check the item in read-only list view (data-tasks-step-5)
        ILocator checkbox = Page.Locator("[data-tasks-step-5] input[type='checkbox']").First;
        await checkbox.CheckAsync();
        await Expect(checkbox).ToBeCheckedAsync();

        await Page.ReloadAsync();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();

        await NavigateToAsync("[data-main-step-4]");

        // Checked state must survive reload
        await Expect(Page.Locator("[data-tasks-step-5] input[type='checkbox']").First).ToBeCheckedAsync();
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
