using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class SearchFilterServiceTests
{
    private SearchFilterService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new();
    }

    [TearDown]
    public void TearDown()
    {
        _sut.SearchTerm = null;
        _sut.MatchCase = false;
    }

    // --- GetQueryParameters tests ---

    [Test]
    public void GetQueryParameters_MapsSearchTermAndMatchCase()
    {
        _sut.SearchTerm = "hello";
        _sut.MatchCase = true;

        QueryParameters qp = _sut.GetQueryParameters(new SettingsModel());

        Assert.That(qp.SearchTerm, Is.EqualTo("hello"));
        Assert.That(qp.MatchCase, Is.True);
    }

    [Test]
    public void GetQueryParameters_MapsSettingsFields()
    {
        SettingsModel settings = new()
        {
            HideCompletedTasks = true,
            ShowOnlyOverSelectedRatioMin = true,
            SelectedRatioMin = 75,
            ShowOnlyUnderSelectedRatioMax = true,
            SelectedRatioMax = 200,
            SelectedRatio = Ratio.ElapsedToDesired,
            CategoryFilterDisplay = FilterDisplay.CheckBoxes,
            PriorityFilterDisplay = FilterDisplay.SelectOptions,
        };

        QueryParameters qp = _sut.GetQueryParameters(settings);

        Assert.That(qp.HideCompletedTasks, Is.True);
        Assert.That(qp.ShowOnlyOverSelectedRatioMin, Is.True);
        Assert.That(qp.SelectedRatioMin, Is.EqualTo(75));
        Assert.That(qp.ShowOnlyUnderSelectedRatioMax, Is.True);
        Assert.That(qp.SelectedRatioMax, Is.EqualTo(200));
        Assert.That(qp.SelectedRatio, Is.EqualTo(Ratio.ElapsedToDesired));
        Assert.That(qp.CategoryFilterDisplay, Is.EqualTo(FilterDisplay.CheckBoxes));
        Assert.That(qp.PriorityFilterDisplay, Is.EqualTo(FilterDisplay.SelectOptions));
    }

    // --- MarkSearchResults tests ---

    [Test]
    public void MarkSearchResults_CaseInsensitive_WrapsMatchInMarkTag()
    {
        _sut.SearchTerm = "hello";
        _sut.MatchCase = false;

        string result = _sut.MarkSearchResults("Say Hello World");

        Assert.That(result, Does.Contain("<mark>Hello</mark>"));
    }

    [Test]
    public void MarkSearchResults_CaseSensitive_MatchesExactCase()
    {
        _sut.SearchTerm = "hello";
        _sut.MatchCase = true;

        string result = _sut.MarkSearchResults("Say hello World");

        Assert.That(result, Does.Contain("<mark>hello</mark>"));
    }

    [Test]
    public void MarkSearchResults_CaseSensitive_NoMatchOnDifferentCase()
    {
        _sut.SearchTerm = "hello";
        _sut.MatchCase = true;

        string result = _sut.MarkSearchResults("Say Hello World");

        Assert.That(result, Does.Not.Contain("<mark>"));
    }

    [Test]
    public void MarkSearchResults_WhenSearchTermIsNull_ReturnsInputUnchanged()
    {
        _sut.SearchTerm = null;

        string result = _sut.MarkSearchResults("Some text");

        Assert.That(result, Is.EqualTo("Some text"));
    }

    // --- MarkSearchResultsInHtml tests ---

    [Test]
    public void MarkSearchResultsInHtml_WrapsTextNodesOnly_DoesNotCorruptHtmlTags()
    {
        _sut.SearchTerm = "bold";
        _sut.MatchCase = false;

        string html = "<p>This is <strong>bold text</strong> here.</p>";
        string result = _sut.MarkSearchResultsInHtml(html);

        Assert.That(result, Does.Not.Contain("<mark><strong>"));
        Assert.That(result, Does.Contain("<mark>bold</mark>"));
    }

    [Test]
    public void MarkSearchResultsInHtml_WhenSearchTermIsNull_ReturnsInputUnchanged()
    {
        _sut.SearchTerm = null;
        string html = "<p>Some text</p>";

        Assert.That(_sut.MarkSearchResultsInHtml(html), Is.EqualTo(html));
    }

    // --- MarkSearchResults multiple occurrences ---

    [Test]
    public void MarkSearchResults_CaseInsensitive_MultipleOccurrences_WrapsAll()
    {
        _sut.SearchTerm = "cat";
        _sut.MatchCase = false;

        string result = _sut.MarkSearchResults("The cat sat on the Cat mat");

        Assert.That(result, Does.Contain("<mark>cat</mark>"));
        Assert.That(result, Does.Contain("<mark>Cat</mark>"));
        Assert.That(result.Split("<mark>").Length - 1, Is.EqualTo(2));
    }

    [Test]
    public void MarkSearchResults_CaseSensitive_MultipleOccurrences_WrapsAll()
    {
        _sut.SearchTerm = "cat";
        _sut.MatchCase = true;

        string result = _sut.MarkSearchResults("the cat sat on the cat mat");

        Assert.That(result, Does.Contain("<mark>cat</mark>"));
        Assert.That(result.Split("<mark>").Length - 1, Is.EqualTo(2));
    }

    // --- GetQueryParameters DoneAt/PlannedAt mapping ---

    [Test]
    public void GetQueryParameters_MapsDoneAtFilter()
    {
        DateTime filterDate = new(2025, 6, 10);
        _sut.DoneAtFilter = filterDate;
        _sut.DoneAtCompare = DateCompare.After;

        QueryParameters qp = _sut.GetQueryParameters(new SettingsModel());

        Assert.That(qp.DoneAtFilter, Is.EqualTo(filterDate));
        Assert.That(qp.DoneAtCompare, Is.EqualTo(DateCompare.After));
    }

    [Test]
    public void GetQueryParameters_MapsPlannedAtFilter()
    {
        DateTime filterDate = new(2025, 3, 15);
        _sut.PlannedAtFilter = filterDate;
        _sut.PlannedAtCompare = DateCompare.Before;

        QueryParameters qp = _sut.GetQueryParameters(new SettingsModel());

        Assert.That(qp.PlannedAtFilter, Is.EqualTo(filterDate));
        Assert.That(qp.PlannedAtCompare, Is.EqualTo(DateCompare.Before));
    }

    // --- MarkSearchResultsInHtml case-sensitive multiple occurrences ---

    [Test]
    public void MarkSearchResultsInHtml_CaseSensitive_MultipleOccurrences_WrapsAll()
    {
        _sut.SearchTerm = "cat";
        _sut.MatchCase = true;

        string html = "<p>the cat sat on the cat mat</p>";
        string result = _sut.MarkSearchResultsInHtml(html);

        Assert.That(result.Split("<mark>").Length - 1, Is.EqualTo(2));
        Assert.That(result, Does.Contain("<mark>cat</mark>"));
    }
}
