namespace OpenHabitTracker.EndToEndTests.Search;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class SearchTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-3]");
        await LoadExamplesViaUiAsync();
    }

    private async Task OpenSearchAsync()
    {
        await Page.Locator("[data-main-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task SearchTerm_MatchingTitle_ShowsOnlyMatchingNotes()
    {
        int totalCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        await OpenSearchAsync();
        await Page.Locator("[data-search-step-1]").FillAsync("Markdown");
        await Page.WaitForTimeoutAsync(500);

        int matchingCount = await Page.Locator("[data-notes-step-2]").CountAsync();
        Assert.That(matchingCount, Is.LessThan(totalCount));
        Assert.That(matchingCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task SearchTerm_NoMatch_ShowsEmptyList()
    {
        await OpenSearchAsync();
        await Page.Locator("[data-search-step-1]").FillAsync("xyzzy_no_match_12345");
        await Page.WaitForTimeoutAsync(500);

        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task SearchTerm_Clear_ShowsAllNotes()
    {
        await OpenSearchAsync();
        await Page.Locator("[data-search-step-1]").FillAsync("Markdown");
        await Page.WaitForTimeoutAsync(500);

        int filteredCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        await Page.Locator("[data-search-step-3]").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        int restoredCount = await Page.Locator("[data-notes-step-2]").CountAsync();
        Assert.That(restoredCount, Is.GreaterThan(filteredCount));
    }

    [Test]
    public async Task CategoryFilter_HideCategory_ExcludesItsItems()
    {
        await OpenSearchAsync();

        int totalCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        // Uncheck the first visible category checkbox to hide its items
        await Page.Locator("input[type='checkbox']").First.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        int filteredCount = await Page.Locator("[data-notes-step-2]").CountAsync();
        Assert.That(filteredCount, Is.LessThan(totalCount));
    }

    [Test]
    public async Task PriorityFilter_UncheckNone_ReducesOrKeepsHabitCount()
    {
        await NavigateToAsync("[data-main-step-5]");
        await OpenSearchAsync();

        int totalCount = await Page.Locator("[data-habits-step-2]").CountAsync();

        await Page.Locator("[data-search-step-13] label[for='Priority.None']").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        int filteredCount = await Page.Locator("[data-habits-step-2]").CountAsync();
        Assert.That(filteredCount, Is.LessThanOrEqualTo(totalCount));
    }

    [Test]
    public async Task DoneAtFilter_ClickDone_DoesNotCrash()
    {
        await NavigateToAsync("[data-main-step-4]");
        await OpenSearchAsync();

        await Page.Locator("[data-search-step-8]").ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        await Expect(Page.Locator("[data-search-step-8]")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Sort_BySecondOption_HabitsListRemainsVisible()
    {
        await NavigateToAsync("[data-main-step-5]");
        await OpenSearchAsync();

        await Page.Locator("[data-search-step-18] select").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Page.WaitForTimeoutAsync(500);

        int count = await Page.Locator("[data-habits-step-2]").CountAsync();
        Assert.That(count, Is.GreaterThan(0));
    }
}
