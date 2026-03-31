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
        await Expect(Page.Locator("input.form-control").Last).ToBeVisibleAsync();

        // FillAsync replaces the content; Tab fires onchange before focusout removes the InputText
        await Page.Locator("input.form-control").Last.FillAsync("NewName");
        await Page.Locator("input.form-control").Last.PressAsync("Tab");

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
    public async Task RenameCategory_UpdatesGroupHeaderInNotesList()
    {
        // SetUp already opened categories sidebar — add the category
        await Page.Locator("[data-categories-step-1] input").FillAsync("BeforeRename");
        await Page.Locator("[data-categories-step-1] button:has(i.bi-plus-square)").ClickAsync();
        await CloseSidebarAsync();

        await EnableGroupedByCategoryAsync();

        // Add a note assigned to the category
        await NavigateToAsync("[data-main-step-3]");
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync("RenameTest Note");
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = "BeforeRename" });
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToHaveCountAsync(0);

        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "BeforeRename" })).ToBeVisibleAsync();

        // Rename the category
        await OpenMenuAsync();
        await Page.Locator("div[role='menu'] button:has(i.bi-tag)").ClickAsync();
        await Expect(Page.Locator("[data-categories-step-1] input")).ToBeVisibleAsync();
        await Page.Locator("button.input-group-text.flex-grow-1").Filter(new LocatorFilterOptions { HasText = "BeforeRename" }).ClickAsync();
        await Expect(Page.Locator("input.form-control").Last).ToBeVisibleAsync();
        await Page.Locator("input.form-control").Last.FillAsync("AfterRename");
        await Page.Locator("input.form-control").Last.PressAsync("Tab");
        await Expect(Page.Locator("button.input-group-text.flex-grow-1").Filter(new LocatorFilterOptions { HasText = "AfterRename" })).ToBeVisibleAsync();
        await CloseSidebarAsync();

        // Group header in notes list must reflect the rename
        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "AfterRename" })).ToBeVisibleAsync();
        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "BeforeRename" })).ToHaveCountAsync(0);
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
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "HabitInTempCat" })).ToBeVisibleAsync();

        // Navigate back to categories and delete TempCategory
        await OpenMenuAsync();
        await Page.Locator("button:has(i.bi-tag)").ClickAsync();

        await Page.Locator("button[aria-label='Delete: TempCategory']").ClickAsync();

        // Navigate back to habits — the habit should be gone (soft-deleted via category cascade)
        await NavigateToAsync("[data-main-step-5]");

        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "HabitInTempCat" })).ToHaveCountAsync(0);
    }
}
