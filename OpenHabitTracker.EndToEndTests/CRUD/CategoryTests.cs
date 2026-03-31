namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class CategoryTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        // Open the menu and navigate to the Categories component
        await OpenMenuAsync();
        await Page.Locator("button:has(i.bi-tag)").ClickAsync();
        await Expect(Page.Locator("[data-categories-step-1] input")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddCategory_TypeTitle_CategoryAppearsInList()
    {
        await Page.Locator("[data-categories-step-1] input").FillAsync("Work");
        await Page.Locator("[data-categories-step-1] button:has(i.bi-plus-square)").ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1").Filter(new LocatorFilterOptions { HasText = "Work" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task RenameCategory_TypeNewTitle_TitleUpdatedInList()
    {
        // Add a category first
        await Page.Locator("[data-categories-step-1] input").FillAsync("OldName");
        await Page.Locator("[data-categories-step-1] button:has(i.bi-plus-square)").ClickAsync();

        // Click the category title to enter edit mode
        await Page.Locator("button.input-group-text.flex-grow-1").Filter(new LocatorFilterOptions { HasText = "OldName" }).ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        // FillAsync replaces the content; Tab fires onchange before focusout removes the InputText
        await Page.Locator("input.form-control").Last.FillAsync("NewName");
        await Page.Locator("input.form-control").Last.PressAsync("Tab");
        await Page.WaitForTimeoutAsync(300);

        await Expect(Page.Locator("button.input-group-text.flex-grow-1").Filter(new LocatorFilterOptions { HasText = "NewName" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task DeleteCategory_CategoryDisappearsFromList()
    {
        // Add a category first
        await Page.Locator("[data-categories-step-1] input").FillAsync("ToDelete");
        await Page.Locator("[data-categories-step-1] button:has(i.bi-plus-square)").ClickAsync();

        // Delete the category using its aria-label
        await Page.Locator("button[aria-label='Delete: ToDelete']").ClickAsync();

        await Expect(Page.Locator("button.input-group-text.flex-grow-1").Filter(new LocatorFilterOptions { HasText = "ToDelete" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteCategory_ChildHabitsDisappearFromHabitsList()
    {
        // Add category
        await Page.Locator("[data-categories-step-1] input").FillAsync("TempCategory");
        await Page.Locator("[data-categories-step-1] button:has(i.bi-plus-square)").ClickAsync();

        // Navigate to habits and add a habit assigned to TempCategory
        await CloseSidebarAsync();
        await NavigateToAsync("[data-main-step-5]");

        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync("HabitInTempCat");
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = "TempCategory" });
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Navigate back to categories and delete TempCategory
        await OpenMenuAsync();
        await Page.Locator("button:has(i.bi-tag)").ClickAsync();

        await Page.Locator("button[aria-label='Delete: TempCategory']").ClickAsync();

        // Navigate back to habits — the habit should be gone (soft-deleted via category cascade)
        await NavigateToAsync("[data-main-step-5]");

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "HabitInTempCat" })).ToHaveCountAsync(0);
    }
}
