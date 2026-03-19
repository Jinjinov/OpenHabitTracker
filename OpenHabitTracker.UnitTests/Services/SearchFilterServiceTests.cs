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
            SelectedRatio = Ratio.ElapsedToDesired,
            CategoryFilterDisplay = FilterDisplay.CheckBoxes,
            PriorityFilterDisplay = FilterDisplay.SelectOptions,
        };

        QueryParameters qp = _sut.GetQueryParameters(settings);

        Assert.That(qp.HideCompletedTasks, Is.True);
        Assert.That(qp.ShowOnlyOverSelectedRatioMin, Is.True);
        Assert.That(qp.SelectedRatioMin, Is.EqualTo(75));
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
}
