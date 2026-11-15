using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
[SetCulture("en-US")]
public class SearchPageTests
{
    private AppTestContext _app = null!;
    private List<bool> _stateChanges = null!;
    private long _workId;
    private long _homeId;

    [SetUp]
    public async Task SetUp()
    {
        _app = new AppTestContext();
        _stateChanges = [];

        ClientState clientState = await _app.Start();
        await clientState.AddUserData([new CategoryModel { Title = "Work" }, new CategoryModel { Title = "Home" }]);

        _workId = clientState.Categories!.Values.Single(x => x.Title == "Work").Id;
        _homeId = clientState.Categories!.Values.Single(x => x.Title == "Home").Id;
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private IRenderedComponent<Search> Render() =>
        _app.Ctx.Render<Search>(parameters => parameters
            .Add(p => p.StateChanged, new Ref<bool>())
            .Add(p => p.StateChangedChanged, (bool value) => _stateChanges.Add(value)));

    private SettingsModel Settings => _app.ClientState.Settings;

    private ISearchFilterService SearchFilter => _app.Get<ISearchFilterService>();

    private async Task<SettingsEntity> Stored() => (await _app.Store.GetSettings())[0];

    [Test]
    public void SearchTerm_TypedAndCleared_UpdatesTheFilterAndTheLists()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("[data-search-step-1]").Input("milk");

        Assert.Multiple(() =>
        {
            Assert.That(SearchFilter.SearchTerm, Is.EqualTo("milk"));
            Assert.That(_stateChanges, Is.EqualTo(new[] { true }));
        });

        cut.Find("[data-search-step-3]").Click();

        Assert.Multiple(() =>
        {
            Assert.That(SearchFilter.SearchTerm, Is.Null);
            Assert.That(cut.Find("[data-search-step-1]").GetAttribute("value"), Is.Null.Or.Empty);
            Assert.That(_stateChanges, Is.EqualTo(new[] { true, false }));
        });
    }

    [Test]
    public void MatchCase_TogglesAndShowsItsState()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("[data-search-step-2]").Click();

        Assert.Multiple(() =>
        {
            Assert.That(SearchFilter.MatchCase, Is.True);
            Assert.That(cut.Find("[data-search-step-2]").ClassList, Does.Contain("btn-primary"));
        });

        cut.Find("[data-search-step-2]").Click();

        Assert.Multiple(() =>
        {
            Assert.That(SearchFilter.MatchCase, Is.False);
            Assert.That(cut.Find("[data-search-step-2]").ClassList, Does.Contain("btn-outline-primary"));
        });
    }

    [Test]
    public void DateFilters_TodayCompareAndClear_SetTheFilterAndNotTheSettings()
    {
        IRenderedComponent<Search> cut = Render();

        DateTime before = DateTime.Today;
        cut.Find("[data-search-step-4]").Click();
        cut.Find("select[aria-label='Planned date comparison']").Change(nameof(DateCompare.Before));
        cut.Find("[data-search-step-10]").Change("2030-05-06");
        cut.Find("select[aria-label='Done date comparison']").Change(nameof(DateCompare.NotOn));

        Assert.Multiple(() =>
        {
            Assert.That(SearchFilter.PlannedAtFilter, Is.EqualTo(before).Or.EqualTo(DateTime.Today));
            Assert.That(SearchFilter.PlannedAtCompare, Is.EqualTo(DateCompare.Before));
            Assert.That(SearchFilter.DoneAtFilter, Is.EqualTo(new DateTime(2030, 5, 6)));
            Assert.That(SearchFilter.DoneAtCompare, Is.EqualTo(DateCompare.NotOn));
        });

        cut.Find("[data-search-step-7]").Click();
        cut.Find("[data-search-step-11]").Click();

        Assert.Multiple(() =>
        {
            Assert.That(SearchFilter.PlannedAtFilter, Is.Null);
            Assert.That(SearchFilter.DoneAtFilter, Is.Null);
            Assert.That(_stateChanges, Has.Count.EqualTo(6));
        });
    }

    [Test]
    public async Task PlannedRange_ALaterFromPushesTheToAlongAndIsStored()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("input[aria-label='Latest planned day offset from today']").Change("-3");
        cut.Find("input[aria-label='Earliest planned day offset from today']").Change("5");

        SettingsEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That((Settings.PlannedFromDayOffset, Settings.PlannedToDayOffset), Is.EqualTo(((int?)5, (int?)5)));
            Assert.That((stored.PlannedFromDayOffset, stored.PlannedToDayOffset), Is.EqualTo(((int?)5, (int?)5)));
        });
    }

    [Test]
    public void PlannedRange_AnEarlierToPullsTheFromAlong()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("input[aria-label='Earliest planned day offset from today']").Change("2");
        cut.Find("input[aria-label='Latest planned day offset from today']").Change("-4");

        Assert.That((Settings.PlannedFromDayOffset, Settings.PlannedToDayOffset), Is.EqualTo(((int?)-4, (int?)-4)));
    }

    [Test]
    public void DoneRange_OffsetsBeyondTheCapAreClampedAndKeptInOrder()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("input[aria-label='Earliest done day offset from today']").Change("-20000");
        cut.Find("input[aria-label='Latest done day offset from today']").Change("20000");

        Assert.That((Settings.DoneFromDayOffset, Settings.DoneToDayOffset), Is.EqualTo(((int?)-SettingsModel.MaxDayOffset, (int?)SettingsModel.MaxDayOffset)));

        cut.Find("input[aria-label='Latest done day offset from today']").Change("-9500");

        Assert.That((Settings.DoneFromDayOffset, Settings.DoneToDayOffset), Is.EqualTo(((int?)-SettingsModel.MaxDayOffset, (int?)-SettingsModel.MaxDayOffset)));
    }

    [Test]
    public async Task RangePreview_ShowsTheResolvedDaysAndClearRemovesBothBounds()
    {
        IRenderedComponent<Search> cut = Render();

        Assert.That(cut.FindAll("button[aria-label='Clear planned range']"), Is.Empty);

        cut.Find("input[aria-label='Earliest planned day offset from today']").Change("-1");
        cut.Find("input[aria-label='Latest planned day offset from today']").Change("2");

        DateTime today = DateTime.Today;
        string preview = cut.Find("[data-search-step-12] span.flex-grow-1").TextContent;

        Assert.That(preview, Is.EqualTo($"{today.AddDays(-1).ToShortDateString()} to {today.AddDays(2).ToShortDateString()}"));

        cut.Find("button[aria-label='Clear planned range']").Click();

        SettingsEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That((stored.PlannedFromDayOffset, stored.PlannedToDayOffset), Is.EqualTo(((int?)null, (int?)null)));
            Assert.That(cut.FindAll("[data-search-step-12] span.flex-grow-1"), Is.Empty);
        });
    }

    [Test]
    public void RangePreview_OneBoundOnly_UsesTheFromOrUntilText()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("input[aria-label='Earliest done day offset from today']").Change("-7");

        Assert.That(cut.Find("[data-search-step-13] span.flex-grow-1").TextContent, Is.EqualTo("Relative range preview from"));

        cut.Find("button[aria-label='Clear done range']").Click();
        cut.Find("input[aria-label='Latest done day offset from today']").Change("7");

        Assert.That(cut.Find("[data-search-step-13] span.flex-grow-1").TextContent, Is.EqualTo("Relative range preview until"));
    }

    [Test]
    public async Task ShowDoneInRange_Switched_ChangesTheLabelAndIsStored()
    {
        IRenderedComponent<Search> cut = Render();

        Assert.That(cut.Find("[data-search-step-13] small").TextContent, Is.EqualTo("Done within days from today:"));

        cut.Find("[data-search-step-13] input[role='switch']").Change(false);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.Find("[data-search-step-13] small").TextContent, Is.EqualTo("Not done within days from today:"));
            Assert.That((await Stored()).ShowDoneInRange, Is.False);
        });
    }

    [Test]
    public async Task CategoryCheckBoxes_HideOneThenAllThenShowAll()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find($"#category-{_workId}").Change(false);

        Assert.That((await Stored()).HiddenCategoryIds, Is.EqualTo(new[] { _workId }));

        // indeterminate: a click shows every category
        cut.Find("[data-search-step-14] .form-check input[type='checkbox']").Change(true);

        Assert.That((await Stored()).HiddenCategoryIds, Is.Empty);

        // all shown: a click hides every category
        cut.Find("[data-search-step-14] .form-check input[type='checkbox']").Change(false);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await Stored()).HiddenCategoryIds, Is.EquivalentTo(new[] { _workId, _homeId }));
            Assert.That(cut.Find($"#category-{_homeId}").HasAttribute("checked"), Is.False);
        });
    }

    [Test]
    public async Task CategoryRadioButtons_SelectOneThenAll()
    {
        Settings.CategoryFilterDisplay = FilterDisplay.RadioButtons;

        IRenderedComponent<Search> cut = Render();

        cut.Find($"#category-{_homeId}").Change(true);

        Assert.That((await Stored()).SelectedCategoryId, Is.EqualTo(_homeId));

        cut.Find("#category-all").Change(true);

        Assert.That((await Stored()).SelectedCategoryId, Is.Null);
    }

    [Test]
    public async Task CategorySelect_SelectOneThenAll()
    {
        Settings.CategoryFilterDisplay = FilterDisplay.SelectOptions;

        IRenderedComponent<Search> cut = Render();

        cut.Find("select[aria-label='Categories']").Change(_workId.ToString());

        Assert.That((await Stored()).SelectedCategoryId, Is.EqualTo(_workId));

        cut.Find("select[aria-label='Categories']").Change("");

        Assert.That((await Stored()).SelectedCategoryId, Is.Null);
    }

    [Test]
    public async Task PriorityCheckBoxes_HideOneThenShowAllThenHideAll()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("#Priority\\.Low").Change(false);

        Assert.That((await Stored()).ShowPriority[Priority.Low], Is.False);

        cut.Find("[data-search-step-15] .form-check input[type='checkbox']").Change(true);

        Assert.That((await Stored()).ShowPriority.Values, Is.All.True);

        cut.Find("[data-search-step-15] .form-check input[type='checkbox']").Change(false);

        Assert.That((await Stored()).ShowPriority.Values, Is.All.False);
    }

    [Test]
    public async Task PriorityRadioButtonsAndSelect_StoreTheSelectedPriority()
    {
        Settings.PriorityFilterDisplay = FilterDisplay.RadioButtons;

        IRenderedComponent<Search> cut = Render();

        cut.Find("#Priority\\.High").Change(true);

        Assert.That((await Stored()).SelectedPriority, Is.EqualTo(Priority.High));

        cut.Find("#Priority\\.All").Change(true);

        Assert.That((await Stored()).SelectedPriority, Is.Null);

        Settings.PriorityFilterDisplay = FilterDisplay.SelectOptions;
        cut.Render();

        cut.Find("select[aria-label='Priorities']").Change(nameof(Priority.VeryLow));

        Assert.That((await Stored()).SelectedPriority, Is.EqualTo(Priority.VeryLow));
    }

    [Test]
    public async Task Status_HideCompletedRatioAndRatioBoundsAreStored()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("#HideCompletedTasks").Change(false);
        cut.Find("select[aria-label='Habit interval ratio']").Change(nameof(Ratio.AverageToDesired));
        cut.Find("[data-search-step-18] input").Change(true);
        cut.Find("#ShowOnlyUnderSelectedRatioMax").Change(true);
        cut.Find("[data-search-step-21]").Change("200");

        SettingsEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.HideCompletedTasks, Is.False);
            Assert.That(stored.SelectedRatio, Is.EqualTo(Ratio.AverageToDesired));
            Assert.That(stored.ShowOnlyOverSelectedRatioMin, Is.True);
            Assert.That(stored.ShowOnlyUnderSelectedRatioMax, Is.True);
            Assert.That(stored.SelectedRatioMax, Is.EqualTo(200));
            Assert.That(cut.Find("label[for='ShowOnlyUnderSelectedRatioMax']").TextContent, Is.EqualTo("Show only habits with interval ratio under 200%"));
        });
    }

    [Test]
    public async Task Sort_EachContentTypeIsStored()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("select[aria-label='Sort notes']").Change(nameof(Sort.Title));
        cut.Find("select[aria-label='Sort tasks']").Change(nameof(Sort.TimeSpent));
        cut.Find("select[aria-label='Sort habits']").Change(nameof(Sort.AverageInterval));

        SettingsEntity stored = await Stored();

        Assert.That(stored.SortBy, Is.EqualTo(new Dictionary<ContentType, Sort>
        {
            [ContentType.Note] = Sort.Title,
            [ContentType.Task] = Sort.TimeSpent,
            [ContentType.Habit] = Sort.AverageInterval
        }));
    }

    [Test]
    public void DoneRange_ALaterFromPushesTheToAndAnEarlierToPullsTheFrom()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find("input[aria-label='Latest done day offset from today']").Change("-3");
        cut.Find("input[aria-label='Earliest done day offset from today']").Change("2");

        Assert.That((Settings.DoneFromDayOffset, Settings.DoneToDayOffset), Is.EqualTo(((int?)2, (int?)2)));

        cut.Find("input[aria-label='Latest done day offset from today']").Change("-5");

        Assert.That((Settings.DoneFromDayOffset, Settings.DoneToDayOffset), Is.EqualTo(((int?)-5, (int?)-5)));
    }

    [Test]
    public async Task CategoryCheckBox_CheckedAgain_ShowsTheCategoryAgain()
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find($"#category-{_homeId}").Change(false);
        cut.Find($"#category-{_homeId}").Change(true);

        Assert.That((await Stored()).HiddenCategoryIds, Is.Empty);
    }

    [TestCase(Priority.None)]
    [TestCase(Priority.VeryLow)]
    [TestCase(Priority.Low)]
    [TestCase(Priority.Medium)]
    [TestCase(Priority.High)]
    [TestCase(Priority.VeryHigh)]
    public async Task PriorityCheckBox_EachPriority_IsHiddenAndShownByItsOwnBox(Priority priority)
    {
        IRenderedComponent<Search> cut = Render();

        cut.Find($"#Priority\\.{priority}").Change(false);

        SettingsEntity hidden = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(hidden.ShowPriority[priority], Is.False);
            Assert.That(hidden.ShowPriority.Where(x => x.Key != priority).Select(x => x.Value), Is.All.True);
        });

        cut.Find($"#Priority\\.{priority}").Change(true);

        Assert.That((await Stored()).ShowPriority.Values, Is.All.True);
    }

    [TestCase(Priority.None)]
    [TestCase(Priority.VeryLow)]
    [TestCase(Priority.Low)]
    [TestCase(Priority.Medium)]
    [TestCase(Priority.High)]
    [TestCase(Priority.VeryHigh)]
    public async Task PriorityRadioButton_EachPriority_IsSelectedByItsOwnButton(Priority priority)
    {
        Settings.PriorityFilterDisplay = FilterDisplay.RadioButtons;

        IRenderedComponent<Search> cut = Render();

        cut.Find($"#Priority\\.{priority}").Change(true);

        Assert.That((await Stored()).SelectedPriority, Is.EqualTo(priority));
    }

    [TestCase(0, QuerySection.Search)]
    [TestCase(1, QuerySection.FilterByDate)]
    [TestCase(2, QuerySection.FilterByCategory)]
    [TestCase(3, QuerySection.FilterByPriority)]
    [TestCase(4, QuerySection.FilterByStatus)]
    [TestCase(5, QuerySection.Sort)]
    public async Task FoldSection_EachHeader_FoldsItsOwnSection(int header, QuerySection section)
    {
        IRenderedComponent<Search> cut = Render();

        cut.FindAll("button.btn-plain.border-0")[header].Click();

        SettingsEntity stored = await Stored();

        Assert.Multiple(() =>
        {
            Assert.That(stored.FoldSection[section], Is.True);
            Assert.That(stored.FoldSection.Where(x => x.Key != section).Select(x => x.Value), Is.All.False);
        });
    }

    [Test]
    public async Task FoldSection_HidesTheSectionAndIsStored()
    {
        IRenderedComponent<Search> cut = Render();

        cut.FindAll("button.btn-plain.border-0")[4].Click();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.FindAll("[data-search-step-16]"), Is.Empty);
            Assert.That(cut.FindAll("button.btn-plain.border-0")[4].InnerHtml, Does.Contain("bi-chevron-right"));
            Assert.That((await Stored()).FoldSection[QuerySection.FilterByStatus], Is.True);
        });

        cut.FindAll("button.btn-plain.border-0")[4].Click();

        Assert.That(cut.FindAll("[data-search-step-16]"), Has.Count.EqualTo(1));
    }

    // Blazor renders a bool attribute value as an empty attribute when true and drops it when false.
    [Test]
    [Explicit("Bug: aria-expanded on the fold-section headers is bound to a bool, so it renders empty or not at all")]
    public void FoldSection_HeaderReportsItsExpandedState()
    {
        IRenderedComponent<Search> cut = Render();

        Assert.That(cut.FindAll("button.btn-plain.border-0")[0].GetAttribute("aria-expanded"), Is.EqualTo("true"));

        cut.FindAll("button.btn-plain.border-0")[0].Click();

        Assert.That(cut.FindAll("button.btn-plain.border-0")[0].GetAttribute("aria-expanded"), Is.EqualTo("false"));
    }
}
