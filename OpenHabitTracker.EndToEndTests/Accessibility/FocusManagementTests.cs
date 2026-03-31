namespace OpenHabitTracker.EndToEndTests.Accessibility;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class FocusManagementTests : BaseTest
{
    [SetUp]
    public async Task SetUp() => await GotoAsync();

    // --- Step A: sidebar focus on open ---

    [Test]
    public async Task MenuToggle_Click_FocusMovesIntoSidebar()
    {
        await Page.Locator("[data-main-step-1]").ClickAsync();
        await Page.WaitForTimeoutAsync(200);

        bool focusedInside = await Page.EvaluateAsync<bool>("document.activeElement?.closest('#sidebar-container') !== null");
        Assert.That(focusedInside, Is.True);
    }

    [Test]
    public async Task SearchToggle_Click_FocusMovesIntoSidebar()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForTimeoutAsync(200);

        bool focusedInside = await Page.EvaluateAsync<bool>("document.activeElement?.closest('#sidebar-container') !== null");
        Assert.That(focusedInside, Is.True);
    }

    // --- Step B: restore focus to opener on sidebar close ---

    [Test]
    public async Task MenuToggle_ClickTwice_FocusReturnsToMenuButton()
    {
        await Page.Locator("[data-main-step-1]").ClickAsync();

        await Page.Locator("[data-main-step-1]").ClickAsync();

        await Expect(Page.Locator("[data-main-step-1]")).ToBeFocusedAsync();
    }

    [Test]
    public async Task SearchToggle_ClickTwice_FocusReturnsToSearchButton()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();

        await Page.Locator("[data-main-step-6]").ClickAsync();

        await Expect(Page.Locator("[data-main-step-6]")).ToBeFocusedAsync();
    }

    [Test]
    public async Task Sidebar_EscapeKey_FocusReturnsToSearchButton()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("[data-main-step-6]")).ToBeFocusedAsync();
    }

    [Test]
    public async Task Sidebar_CloseButton_FocusReturnsToMenuButton()
    {
        await Page.Locator("[data-main-step-1]").ClickAsync();

        await Page.Locator("button").Filter(new LocatorFilterOptions { HasText = "Settings" }).ClickAsync();

        await Page.Locator("#closeSidebar").ClickAsync();

        await Expect(Page.Locator("[data-main-step-1]")).ToBeFocusedAsync();
    }

    // --- Step C: focus first element when opening edit ---

    [Test]
    public async Task NoteItem_Click_FocusMovesIntoNoteComponent()
    {
        await NavigateToAsync("[data-main-step-3]");
        await AddItemAsync("Focus test note");

        await Page.Locator("[data-notes-step-2]").First.ClickAsync();
        await Page.WaitForTimeoutAsync(200);

        bool focusedInside = await Page.EvaluateAsync<bool>("document.activeElement?.closest('#note-component') !== null");
        Assert.That(focusedInside, Is.True);
    }

    [Test]
    public async Task TaskItem_Click_FocusMovesIntoTaskComponent()
    {
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Focus test task");

        await Page.Locator("[data-tasks-step-2]").First.ClickAsync();
        await Page.WaitForTimeoutAsync(200);

        bool focusedInside = await Page.EvaluateAsync<bool>("document.activeElement?.closest('#task-component') !== null");
        Assert.That(focusedInside, Is.True);
    }

    [Test]
    public async Task HabitItem_Click_FocusMovesIntoHabitComponent()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Focus test habit");

        await Page.Locator("[data-habits-step-2]").First.ClickAsync();
        await Page.WaitForTimeoutAsync(200);

        bool focusedInside = await Page.EvaluateAsync<bool>("document.activeElement?.closest('#habit-component') !== null");
        Assert.That(focusedInside, Is.True);
    }

    // --- Step D: restore focus to list item on edit close ---

    [Test]
    public async Task NoteComponent_Close_FocusReturnsToNoteItem()
    {
        await NavigateToAsync("[data-main-step-3]");
        await AddItemAsync("Focus return note");

        await Page.Locator("[data-notes-step-2]").First.ClickAsync();

        string url = Page.Url;
        long itemId = long.Parse(url.Split('/').Last());

        await Page.Locator("[data-notes-step-7]").ClickAsync();

        await Expect(Page.Locator($"#note-item-{itemId}")).ToBeFocusedAsync();
    }

    [Test]
    public async Task TaskComponent_Close_FocusReturnsToTaskItem()
    {
        await NavigateToAsync("[data-main-step-4]");
        await AddItemAsync("Focus return task");

        await Page.Locator("[data-tasks-step-2]").First.ClickAsync();

        string url = Page.Url;
        long itemId = long.Parse(url.Split('/').Last());

        await Page.Locator("[data-tasks-step-10]").ClickAsync();

        await Expect(Page.Locator($"#task-item-{itemId}")).ToBeFocusedAsync();
    }

    [Test]
    public async Task HabitComponent_Close_FocusReturnsToHabitItem()
    {
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("Focus return habit");

        await Page.Locator("[data-habits-step-2]").First.ClickAsync();

        string url = Page.Url;
        long itemId = long.Parse(url.Split('/').Last());

        await Page.Locator("[data-habits-step-11]").ClickAsync();

        await Expect(Page.Locator($"#habit-item-{itemId}")).ToBeFocusedAsync();
    }
}
