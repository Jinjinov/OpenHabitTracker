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
    }

    [Test]
    public async Task SearchTerm_MatchingTitle_ShowsOnlyMatchingNotes()
    {
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Diet Plan" })).ToBeVisibleAsync();
        int totalCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        await OpenSearchAsync();
        await Page.Locator("[data-search-step-1]").FillAsync("Markdown");
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Diet Plan" })).ToHaveCountAsync(0);

        int matchingCount = await Page.Locator("[data-notes-step-2]").CountAsync();
        Assert.That(matchingCount, Is.LessThan(totalCount));
        Assert.That(matchingCount, Is.GreaterThan(0));
    }

    [Test]
    public async Task SearchTerm_NoMatch_ShowsEmptyList()
    {
        await OpenSearchAsync();
        await Page.Locator("[data-search-step-1]").FillAsync("xyzzy_no_match_12345");

        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task SearchTerm_Clear_ShowsAllNotes()
    {
        await OpenSearchAsync();
        await Page.Locator("[data-search-step-1]").FillAsync("Markdown");
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Diet Plan" })).ToHaveCountAsync(0);

        int filteredCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        await Page.Locator("[data-search-step-3]").ClickAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Diet Plan" })).ToBeVisibleAsync();

        int restoredCount = await Page.Locator("[data-notes-step-2]").CountAsync();
        Assert.That(restoredCount, Is.GreaterThan(filteredCount));
    }

    [Test]
    public async Task SearchTerm_MatchingTitle_FiltersTasks()
    {
        await NavigateToAsync("[data-main-step-4]");

        // Add a task with a unique searchable prefix so the filter is guaranteed to match exactly this one
        await AddItemAsync("SearchTarget Task");

        int totalCount = await Page.Locator("[data-tasks-step-2]").CountAsync();
        Assert.That(totalCount, Is.GreaterThan(1));

        await OpenSearchAsync();
        await Page.Locator("[data-search-step-1]").FillAsync("SearchTarget");

        await Expect(Page.Locator("[data-tasks-step-2]").Filter(new LocatorFilterOptions { HasText = "SearchTarget Task" })).ToBeVisibleAsync();

        int filteredCount = await Page.Locator("[data-tasks-step-2]").CountAsync();
        Assert.That(filteredCount, Is.LessThan(totalCount));

        // Clear the search — all tasks reappear
        await Page.Locator("[data-search-step-3]").ClickAsync();
        await Expect(Page.Locator("[data-tasks-step-2]")).ToHaveCountAsync(totalCount);
    }

    [Test]
    public async Task CategoryFilter_HideCategory_ExcludesItsItems()
    {
        await OpenSearchAsync();

        int totalCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        // Uncheck the first visible category checkbox to hide its items
        await Page.Locator("input[type='checkbox']").First.ClickAsync();
        await Expect(Page.Locator("[data-notes-step-2]")).Not.ToHaveCountAsync(totalCount);

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

        int filteredCount = await Page.Locator("[data-habits-step-2]").CountAsync();
        Assert.That(filteredCount, Is.LessThanOrEqualTo(totalCount));
    }

    [Test]
    public async Task DoneAtFilter_ClickDone_FiltersToCompletedTasks()
    {
        await NavigateToAsync("[data-main-step-4]");
        int totalCount = await Page.Locator("[data-tasks-step-2]").CountAsync();
        Assert.That(totalCount, Is.GreaterThan(0));

        await OpenSearchAsync();
        await Page.Locator("[data-search-step-8]").ClickAsync(); // Done = today
        await Expect(Page.Locator("[data-tasks-step-2]")).Not.ToHaveCountAsync(totalCount);

        // Example tasks have no CompletedAt, so filter by today shows fewer than total
        int doneCount = await Page.Locator("[data-tasks-step-2]").CountAsync();
        Assert.That(doneCount, Is.LessThan(totalCount));

        // Clear the filter
        await Page.Locator("[data-search-step-11]").ClickAsync();
    }

    [Test]
    public async Task Sort_ByTitle_HabitsListIsInAlphabeticalOrder()
    {
        await NavigateToAsync("[data-main-step-5]");
        await OpenSearchAsync();

        // data-search-step-20 is the Habits sort select; index 2 = Title
        await Page.Locator("[data-search-step-20] select").SelectOptionAsync(new SelectOptionValue { Index = 2 });

        ILocator habits = Page.Locator("[data-habits-step-2]");
        int count = await habits.CountAsync();
        Assert.That(count, Is.GreaterThan(1));

        string firstTitle = (await habits.Nth(0).TextContentAsync() ?? "").Trim();
        string secondTitle = (await habits.Nth(1).TextContentAsync() ?? "").Trim();
        Assert.That(string.Compare(firstTitle, secondTitle, StringComparison.OrdinalIgnoreCase), Is.LessThanOrEqualTo(0));
    }

    // Regression guard for: urgency range filter DB migration (adds SelectedRatioMax + ShowOnlyOverSelectedRatioMax).
    // The existing ShowOnlyOverSelectedRatioMin filter must still work after the new max-side companion is added.
    // A freshly created habit has ratio ≈ 0% (ElapsedTime ≈ 0, RepeatInterval > 0),
    // so enabling the filter at the default 50% threshold must exclude it.
    [Test]
    public async Task RatioMinFilter_Enable_ExcludesHabitsUnderThreshold()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-5]");
        await AddItemAsync("LowRatio Habit"); // ratio ≈ 0% right after creation

        int countBefore = await Page.Locator("[data-habits-step-2]").CountAsync();
        Assert.That(countBefore, Is.GreaterThan(0));

        await OpenSearchAsync();

        // data-search-step-16 wraps ShowOnlyOverSelectedRatioMin; default SelectedRatioMin = 50%
        await Page.Locator("[data-search-step-16] label").ClickAsync();
        await Expect(Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = "LowRatio Habit" })).ToHaveCountAsync(0);

        int countAfter = await Page.Locator("[data-habits-step-2]").CountAsync();
        Assert.That(countAfter, Is.LessThan(countBefore));
    }
}
