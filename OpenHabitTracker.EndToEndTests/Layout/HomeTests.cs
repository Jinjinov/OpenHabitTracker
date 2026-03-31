namespace OpenHabitTracker.EndToEndTests.Layout;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class HomeTests : BaseTest
{
    private const string CategoryName = "HomeTestCat";

    private async Task AddNoteWithCategoryAsync(string title)
    {
        await NavigateToAsync("[data-main-step-3]");
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync(title);
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = CategoryName });
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = title })).ToBeVisibleAsync();
    }

    private async Task AddTaskWithCategoryAsync(string title)
    {
        await NavigateToAsync("[data-main-step-4]");
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync(title);
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = CategoryName });
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = title })).ToBeVisibleAsync();
    }

    [Test]
    public async Task GroupedByCategory_CollapseCategory_HidesItems()
    {
        // Step 1: GotoAsync
        await GotoAsync();
        int countStep1 = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep1 = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep1, Is.EqualTo(0), $"[after GotoAsync] Expected 0 'Collapsible Note' but found {matchingStep1}. Total notes: {countStep1}");

        // Step 2a: Open settings sidebar
        await OpenSidebarAsync("bi-gear");
        int countStep2a = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep2a = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep2a, Is.EqualTo(0), $"[after OpenSidebarAsync(gear)] Expected 0 'Collapsible Note' but found {matchingStep2a}. Total notes: {countStep2a}");

        // Step 2b: Toggle ShowGroupedByCategory
        await Page.Locator("label[for='ShowGroupedByCategory']").ClickAsync();
        int countStep2b = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep2b = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep2b, Is.EqualTo(0), $"[after ClickAsync(ShowGroupedByCategory)] Expected 0 'Collapsible Note' but found {matchingStep2b}. Total notes: {countStep2b}");

        // Step 2c: Close settings sidebar
        await CloseSidebarAsync();
        int countStep2c = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep2c = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep2c, Is.EqualTo(0), $"[after CloseSidebarAsync(gear)] Expected 0 'Collapsible Note' but found {matchingStep2c}. Total notes: {countStep2c}");

        // Step 3a: Open menu
        await OpenMenuAsync();
        int countStep3a = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep3a = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep3a, Is.EqualTo(0), $"[after OpenMenuAsync] Expected 0 'Collapsible Note' but found {matchingStep3a}. Total notes: {countStep3a}");

        // Step 3b: Click Categories button in menu
        await Page.Locator("div[role='menu'] button:has(i.bi-tag)").ClickAsync();
        int countStep3b = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep3b = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep3b, Is.EqualTo(0), $"[after clicking Categories in menu] Expected 0 'Collapsible Note' but found {matchingStep3b}. Total notes: {countStep3b}");

        // Step 3c: Fill category name
        await Page.Locator("[data-categories-step-1] input").FillAsync(CategoryName);
        int countStep3c = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep3c = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep3c, Is.EqualTo(0), $"[after FillAsync(CategoryName)] Expected 0 'Collapsible Note' but found {matchingStep3c}. Total notes: {countStep3c}");

        // Step 3d: Click Add category button
        await Page.Locator("[data-categories-step-1] button:has(i.bi-plus-square)").ClickAsync();
        int countStep3d = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep3d = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep3d, Is.EqualTo(0), $"[after clicking Add category] Expected 0 'Collapsible Note' but found {matchingStep3d}. Total notes: {countStep3d}");

        // Step 3e: Close categories sidebar
        await CloseSidebarAsync();
        int countStep3e = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep3e = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep3e, Is.EqualTo(0), $"[after CloseSidebarAsync(categories)] Expected 0 'Collapsible Note' but found {matchingStep3e}. Total notes: {countStep3e}");

        // Step 4a: Navigate to Notes
        await NavigateToAsync("[data-main-step-3]");
        int countStep4a = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep4a = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep4a, Is.EqualTo(0), $"[after NavigateToAsync(notes)] Expected 0 'Collapsible Note' but found {matchingStep4a}. Total notes: {countStep4a}");

        // Step 4b: Click Add note button
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        int countStep4b = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep4b = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep4b, Is.EqualTo(0), $"[after clicking Add note] Expected 0 'Collapsible Note' but found {matchingStep4b}. Total notes: {countStep4b}");

        // Step 4c: Fill note title
        await Page.Locator("input[aria-required='true']").FillAsync("Collapsible Note");
        int countStep4c = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep4c = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep4c, Is.EqualTo(0), $"[after FillAsync(title)] Expected 0 'Collapsible Note' but found {matchingStep4c}. Total notes: {countStep4c}");

        // Step 4d: Select category
        await Page.Locator("select[aria-label='Category']").SelectOptionAsync(new SelectOptionValue { Label = CategoryName });
        int countStep4d = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep4d = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep4d, Is.EqualTo(0), $"[after SelectOptionAsync(Category)] Expected 0 'Collapsible Note' but found {matchingStep4d}. Total notes: {countStep4d}");

        // Step 4e: Wait for save button enabled
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeEnabledAsync();
        int countStep4e = await Page.Locator("[data-notes-step-2]").CountAsync();
        int matchingStep4e = await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" }).CountAsync();
        Assert.That(matchingStep4e, Is.EqualTo(0), $"[after save button enabled] Expected 0 'Collapsible Note' but found {matchingStep4e}. Total notes: {countStep4e}");

        // Step 4f: Click Save — list briefly has 2 "Collapsible Note" elements during Blazor re-render; do not assert count here
        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" })).ToHaveCountAsync(1);

        // Final: collapse the category and verify note is hidden
        await Page.Locator("button.btn-plain.border-0:has(i.bi-tag)")
            .Filter(new LocatorFilterOptions { HasText = CategoryName })
            .ClickAsync();

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Collapsible Note" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GroupedByCategory_ExpandCategory_ShowsItems()
    {
        await GotoAsync();
        await EnableGroupedByCategoryAsync();
        await CreateCategoryAsync(CategoryName);
        await AddNoteWithCategoryAsync("Expandable Note");

        // Collapse
        await Page.Locator("button.btn-plain.border-0:has(i.bi-tag)")
            .Filter(new LocatorFilterOptions { HasText = CategoryName })
            .ClickAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Expandable Note" })).ToHaveCountAsync(0);

        // Expand
        await Page.Locator("button.btn-plain.border-0:has(i.bi-tag)")
            .Filter(new LocatorFilterOptions { HasText = CategoryName })
            .ClickAsync();

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Expandable Note" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task GroupedByCategory_UncategorizedNote_AppearsInUncategorizedGroup()
    {
        await GotoAsync();
        await EnableGroupedByCategoryAsync();
        await CreateCategoryAsync(CategoryName); // at least one category required for groups to render

        await NavigateToAsync("[data-main-step-3]");
        await AddItemAsync("Uncategorized Note"); // no category selected → CategoryId = 0

        await Expect(Page.Locator("button.btn-plain.border-0:has(i.bi-tag)").Filter(new LocatorFilterOptions { HasText = "Uncategorized" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Uncategorized Note" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_CollapseCategory_PropagatesAcrossAllSections()
    {
        await GotoAsync();
        await EnableGroupedByCategoryAsync();
        await CreateCategoryAsync(CategoryName);
        await AddNoteWithCategoryAsync("Cross Note");
        await AddTaskWithCategoryAsync("Cross Task");

        // Navigate to Home — [data-main-step-2] is only visible at width >= 1280px (1920px viewport)
        await Page.Locator("[data-main-step-2]").ClickAsync();

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Cross Note" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Cross Task" })).ToBeVisibleAsync();

        // Collapse via the first matching button — Notes section renders first in Home
        await Page.Locator("button.btn-plain.border-0:has(i.bi-tag)")
            .Filter(new LocatorFilterOptions { HasText = CategoryName })
            .First.ClickAsync();

        // Note hidden (Notes section collapsed)
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Cross Note" })).ToHaveCountAsync(0);
        // Task also hidden — verifies cross-section propagation via OnIsCollapsedChanged
        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "Cross Task" })).ToHaveCountAsync(0);
    }
}
