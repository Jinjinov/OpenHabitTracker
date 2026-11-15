using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.UnitTests.Query;

[TestFixture]
public class QueryExtensionsTests
{
    private static readonly DateTime _day = new(2026, 6, 15, 10, 0, 0);

    private static QueryParameters Parameters(Sort sort = Sort.Title)
    {
        QueryParameters queryParameters = new();
        queryParameters.SortBy[ContentType.Note] = sort;
        queryParameters.SortBy[ContentType.Task] = sort;
        queryParameters.SortBy[ContentType.Habit] = sort;
        return queryParameters;
    }

    private static HabitModel HabitDoneAt(long id, string title, params DateTime[] startedAts)
    {
        HabitModel habit = TestData.Habit(id, title);
        habit.TimesDone = startedAts.Select((x, i) => new TimeModel { Id = (id * 100) + i, HabitId = id, StartedAt = x, CompletedAt = x }).ToList();
        habit.RefreshTimesDoneByDay();
        return habit;
    }

    // --- FilterNotes ---

    [Test]
    public void FilterNotes_LeavesOutDeletedNotes()
    {
        List<NoteModel> notes = [TestData.Note(1, "a"), TestData.Note(2, "b", isDeleted: true)];

        Assert.That(notes.FilterNotes(Parameters()).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
    }

    [Test]
    public void FilterNotes_PriorityCheckBoxes_LeavesOutTheUncheckedPriorities()
    {
        List<NoteModel> notes = [TestData.Note(1, "a", priority: Priority.Low), TestData.Note(2, "b", priority: Priority.High), TestData.Note(3, "c", priority: Priority.None)];
        QueryParameters queryParameters = Parameters();
        queryParameters.ShowPriority[Priority.Low] = false;
        queryParameters.ShowPriority[Priority.None] = false;

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
    }

    [TestCase(FilterDisplay.RadioButtons)]
    [TestCase(FilterDisplay.SelectOptions)]
    public void FilterNotes_SinglePriorityDisplay_KeepsOnlyTheSelectedPriority(FilterDisplay display)
    {
        List<NoteModel> notes = [TestData.Note(1, "a", priority: Priority.Low), TestData.Note(2, "b", priority: Priority.High)];
        QueryParameters queryParameters = Parameters();
        queryParameters.PriorityFilterDisplay = display;
        queryParameters.SelectedPriority = Priority.High;

        // the check box state is ignored in this display
        queryParameters.ShowPriority[Priority.High] = false;

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
    }

    [Test]
    public void FilterNotes_SinglePriorityDisplayWithNothingSelected_KeepsEveryPriority()
    {
        List<NoteModel> notes = [TestData.Note(1, "a", priority: Priority.Low), TestData.Note(2, "b", priority: Priority.High)];
        QueryParameters queryParameters = Parameters();
        queryParameters.PriorityFilterDisplay = FilterDisplay.SelectOptions;
        queryParameters.ShowPriority[Priority.Low] = false;

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
    }

    [Test]
    public void FilterNotes_SearchTerm_MatchesTitleOrContentIgnoringCase()
    {
        List<NoteModel> notes =
        [
            TestData.Note(1, "Groceries", "milk"),
            TestData.Note(2, "Ideas", "buy MILK later"),
            TestData.Note(3, "Other", "bread")
        ];
        QueryParameters queryParameters = Parameters();
        queryParameters.SearchTerm = "Milk";

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
    }

    [Test]
    public void FilterNotes_SearchTermWithMatchCase_MatchesOnlyTheExactCase()
    {
        List<NoteModel> notes = [TestData.Note(1, "a", "milk"), TestData.Note(2, "b", "MILK")];
        QueryParameters queryParameters = Parameters();
        queryParameters.SearchTerm = "MILK";
        queryParameters.MatchCase = true;

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
    }

    [Test]
    public void FilterNotes_CategoryCheckBoxes_LeavesOutTheHiddenCategoriesIncludingNoCategory()
    {
        List<NoteModel> notes = [TestData.Note(1, "a", categoryId: 0), TestData.Note(2, "b", categoryId: 5), TestData.Note(3, "c", categoryId: 6)];
        QueryParameters queryParameters = Parameters();
        queryParameters.HiddenCategoryIds = [0, 6];

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
    }

    [TestCase(FilterDisplay.RadioButtons)]
    [TestCase(FilterDisplay.SelectOptions)]
    public void FilterNotes_SingleCategoryDisplay_KeepsOnlyTheSelectedCategory(FilterDisplay display)
    {
        List<NoteModel> notes = [TestData.Note(1, "a", categoryId: 5), TestData.Note(2, "b", categoryId: 6)];
        QueryParameters queryParameters = Parameters();
        queryParameters.CategoryFilterDisplay = display;
        queryParameters.SelectedCategoryId = 6;
        queryParameters.HiddenCategoryIds = [6];

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
    }

    [Test]
    public void FilterNotes_SingleCategoryDisplayWithNothingSelected_KeepsEveryCategory()
    {
        List<NoteModel> notes = [TestData.Note(1, "a", categoryId: 5), TestData.Note(2, "b", categoryId: 6)];
        QueryParameters queryParameters = Parameters();
        queryParameters.CategoryFilterDisplay = FilterDisplay.RadioButtons;
        queryParameters.HiddenCategoryIds = [5];

        Assert.That(notes.FilterNotes(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
    }

    [Test]
    public void FilterNotes_SortByCategory_OrdersByCategoryId()
    {
        List<NoteModel> notes = [TestData.Note(1, "a", categoryId: 9), TestData.Note(2, "b", categoryId: 0), TestData.Note(3, "c", categoryId: 4)];

        Assert.That(notes.FilterNotes(Parameters(Sort.Category)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 3L, 1L }));
    }

    [Test]
    public void FilterNotes_SortByPriority_OrdersHighestFirst()
    {
        List<NoteModel> notes = [TestData.Note(1, "a", priority: Priority.Low), TestData.Note(2, "b", priority: Priority.VeryHigh), TestData.Note(3, "c", priority: Priority.None)];

        Assert.That(notes.FilterNotes(Parameters(Sort.Priority)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 1L, 3L }));
    }

    [Test]
    public void FilterNotes_SortByTitle_OrdersAlphabetically()
    {
        List<NoteModel> notes = [TestData.Note(1, "b"), TestData.Note(2, "c"), TestData.Note(3, "a")];

        Assert.That(notes.FilterNotes(Parameters(Sort.Title)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 1L, 2L }));
    }

    [TestCase(Sort.PlannedAt)]
    [TestCase(Sort.Duration)]
    [TestCase(Sort.SelectedRatio)]
    public void FilterNotes_SortKeyNotesDoNotHave_KeepsTheStoredOrder(Sort sort)
    {
        List<NoteModel> notes = [TestData.Note(3, "c"), TestData.Note(1, "a"), TestData.Note(2, "b")];

        Assert.That(notes.FilterNotes(Parameters(sort)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 1L, 2L }));
    }

    // --- FilterTasks ---

    [Test]
    public void FilterTasks_LeavesOutDeletedTasks()
    {
        List<TaskModel> tasks = [TestData.Task(1, "a"), TestData.Task(2, "b", isDeleted: true)];

        Assert.That(tasks.FilterTasks(Parameters()).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
    }

    [Test]
    public void FilterTasks_PriorityFilters_WorkInBothDisplays()
    {
        List<TaskModel> tasks = [TestData.Task(1, "a", priority: Priority.Low), TestData.Task(2, "b", priority: Priority.High)];

        QueryParameters checkBoxes = Parameters();
        checkBoxes.ShowPriority[Priority.High] = false;

        QueryParameters select = Parameters();
        select.PriorityFilterDisplay = FilterDisplay.SelectOptions;
        select.SelectedPriority = Priority.High;

        Assert.Multiple(() =>
        {
            Assert.That(tasks.FilterTasks(checkBoxes).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
            Assert.That(tasks.FilterTasks(select).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
        });
    }

    [Test]
    public void FilterTasks_SearchTerm_MatchesTitleOrAnItemTitle()
    {
        TaskModel byTitle = TestData.Task(1, "Buy bread");
        TaskModel byItem = TestData.Task(2, "Shop");
        byItem.Items = [new ItemModel { Title = "Bread" }];
        TaskModel itemsNotLoaded = TestData.Task(3, "Bake");
        TaskModel noMatch = TestData.Task(4, "Run");
        noMatch.Items = [new ItemModel { Title = "Shoes" }];

        QueryParameters queryParameters = Parameters();
        queryParameters.SearchTerm = "bread";

        Assert.That(new[] { byTitle, byItem, itemsNotLoaded, noMatch }.FilterTasks(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
    }

    [Test]
    public void FilterTasks_SearchTermWithMatchCase_IgnoresOtherCaseInItems()
    {
        TaskModel task = TestData.Task(1, "Shop");
        task.Items = [new ItemModel { Title = "bread" }];

        QueryParameters queryParameters = Parameters();
        queryParameters.SearchTerm = "Bread";
        queryParameters.MatchCase = true;

        Assert.That(new[] { task }.FilterTasks(queryParameters), Is.Empty);
    }

    [TestCase(DateCompare.Before, new long[] { 1 })]
    [TestCase(DateCompare.On, new long[] { 2, 3 })]
    [TestCase(DateCompare.After, new long[] { 4 })]
    [TestCase(DateCompare.NotOn, new long[] { 1, 4, 5 })]
    public void FilterTasks_DoneAtFilter_ComparesTheCompletionDay(DateCompare compare, long[] expected)
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "a", completedAt: _day.AddDays(-1)),
            TestData.Task(2, "b", completedAt: _day.Date),
            TestData.Task(3, "c", completedAt: _day.Date.AddHours(23).AddMinutes(59)),
            TestData.Task(4, "d", completedAt: _day.AddDays(1)),
            TestData.Task(5, "e", completedAt: null)
        ];
        QueryParameters queryParameters = Parameters();
        queryParameters.DoneAtFilter = _day;
        queryParameters.DoneAtCompare = compare;

        Assert.That(tasks.FilterTasks(queryParameters).Select(x => x.Id), Is.EqualTo(expected));
    }

    [TestCase(DateCompare.Before, new long[] { 1 })]
    [TestCase(DateCompare.On, new long[] { 2 })]
    [TestCase(DateCompare.After, new long[] { 3 })]
    [TestCase(DateCompare.NotOn, new long[] { 1, 3, 4 })]
    public void FilterTasks_PlannedAtFilter_ComparesThePlannedDay(DateCompare compare, long[] expected)
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "a", plannedAt: _day.AddDays(-3)),
            TestData.Task(2, "b", plannedAt: _day.Date + TaskModel.DateOnlySentinel),
            TestData.Task(3, "c", plannedAt: _day.AddDays(3)),
            TestData.Task(4, "d", plannedAt: null)
        ];
        QueryParameters queryParameters = Parameters();
        queryParameters.PlannedAtFilter = _day;
        queryParameters.PlannedAtCompare = compare;

        Assert.That(tasks.FilterTasks(queryParameters).Select(x => x.Id), Is.EqualTo(expected));
    }

    [Test]
    public void FilterTasks_InvalidCompare_Throws()
    {
        QueryParameters queryParameters = Parameters();
        queryParameters.DoneAtFilter = _day;
        queryParameters.DoneAtCompare = (DateCompare)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { TestData.Task() }.FilterTasks(queryParameters).ToList());
    }

    [Test]
    public void FilterTasks_PlannedRangeEndOnly_KeepsTasksPlannedUpToTheEndDay()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "a", plannedAt: _day.AddDays(-30)),
            TestData.Task(2, "b", plannedAt: _day.Date.AddHours(23)),
            TestData.Task(3, "c", plannedAt: _day.AddDays(1)),
            TestData.Task(4, "d", plannedAt: null)
        ];
        QueryParameters queryParameters = Parameters();
        queryParameters.PlannedRangeEnd = _day;

        Assert.That(tasks.FilterTasks(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
    }

    [Test]
    public void FilterTasks_DoneRangeStartOnly_KeepsTasksCompletedFromTheStartDay()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "a", completedAt: _day.Date.AddMinutes(1)),
            TestData.Task(2, "b", completedAt: _day.AddDays(-1)),
            TestData.Task(3, "c", completedAt: null)
        ];
        QueryParameters queryParameters = Parameters();
        queryParameters.DoneRangeStart = _day;
        queryParameters.ShowDoneInRange = true;

        Assert.That(tasks.FilterTasks(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
    }

    [Test]
    public void FilterTasks_DoneRangeEndOnlyNotDone_KeepsTasksNotCompletedByTheEndDay()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "a", completedAt: _day.AddDays(-5)),
            TestData.Task(2, "b", completedAt: _day.AddDays(2)),
            TestData.Task(3, "c", completedAt: null)
        ];
        QueryParameters queryParameters = Parameters();
        queryParameters.DoneRangeEnd = _day;
        queryParameters.ShowDoneInRange = false;

        Assert.That(tasks.FilterTasks(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L, 3L }));
    }

    [Test]
    public void FilterTasks_CategoryFilters_WorkInBothDisplays()
    {
        List<TaskModel> tasks = [TestData.Task(1, "a", categoryId: 0), TestData.Task(2, "b", categoryId: 7)];

        QueryParameters checkBoxes = Parameters();
        checkBoxes.HiddenCategoryIds = [7];

        QueryParameters radio = Parameters();
        radio.CategoryFilterDisplay = FilterDisplay.RadioButtons;
        radio.SelectedCategoryId = 0;

        Assert.Multiple(() =>
        {
            Assert.That(tasks.FilterTasks(checkBoxes).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
            Assert.That(tasks.FilterTasks(radio).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
        });
    }

    [Test]
    public void FilterTasks_HideCompletedTasks_LeavesOutTheCompletedTasksOnlyWhenOn()
    {
        List<TaskModel> tasks = [TestData.Task(1, "a"), TestData.Task(2, "b", completedAt: _day)];

        QueryParameters hide = Parameters();
        hide.HideCompletedTasks = true;

        Assert.Multiple(() =>
        {
            Assert.That(tasks.FilterTasks(hide).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
            Assert.That(tasks.FilterTasks(Parameters()).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
        });
    }

    [Test]
    public void FilterTasks_SortByPlannedAt_OrdersUnplannedFirstThenByTime()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "a", plannedAt: _day.AddDays(1)),
            TestData.Task(2, "b", plannedAt: _day.Date + TaskModel.DateOnlySentinel),
            TestData.Task(3, "c", plannedAt: null),
            TestData.Task(4, "d", plannedAt: _day.Date.AddHours(9))
        ];

        Assert.That(tasks.FilterTasks(Parameters(Sort.PlannedAt)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 4L, 2L, 1L }));
    }

    [Test]
    public void FilterTasks_SortByDuration_OrdersNoDurationFirst()
    {
        TaskModel longTask = TestData.Task(1, "a");
        longTask.Duration = new TimeOnly(2, 0);
        TaskModel shortTask = TestData.Task(2, "b");
        shortTask.Duration = new TimeOnly(0, 15);
        TaskModel noDuration = TestData.Task(3, "c");

        Assert.That(new[] { longTask, shortTask, noDuration }.FilterTasks(Parameters(Sort.Duration)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 2L, 1L }));
    }

    [Test]
    public void FilterTasks_SortByElapsedTime_OrdersByCompletionNotCompletedFirst()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "a", completedAt: _day.AddDays(2)),
            TestData.Task(2, "b", completedAt: _day),
            TestData.Task(3, "c", completedAt: null)
        ];

        Assert.That(tasks.FilterTasks(Parameters(Sort.ElapsedTime)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 2L, 1L }));
    }

    [Test]
    public void FilterTasks_SortByTimeSpent_OrdersByCompletedMinusStarted()
    {
        TaskModel hour = TestData.Task(1, "a", completedAt: _day.AddHours(1));
        hour.StartedAt = _day;
        TaskModel minute = TestData.Task(2, "b", completedAt: _day.AddMinutes(1));
        minute.StartedAt = _day;
        TaskModel notStarted = TestData.Task(3, "c");

        Assert.That(new[] { hour, minute, notStarted }.FilterTasks(Parameters(Sort.TimeSpent)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 2L, 1L }));
    }

    [Test]
    public void FilterTasks_SortByCategoryPriorityAndTitle_OrderAsForNotes()
    {
        List<TaskModel> tasks =
        [
            TestData.Task(1, "b", priority: Priority.Low, categoryId: 3),
            TestData.Task(2, "c", priority: Priority.High, categoryId: 1),
            TestData.Task(3, "a", priority: Priority.Medium, categoryId: 2)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(tasks.FilterTasks(Parameters(Sort.Category)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 3L, 1L }));
            Assert.That(tasks.FilterTasks(Parameters(Sort.Priority)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 3L, 1L }));
            Assert.That(tasks.FilterTasks(Parameters(Sort.Title)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 1L, 2L }));
        });
    }

    [TestCase(Sort.RepeatInterval)]
    [TestCase(Sort.SelectedRatio)]
    public void FilterTasks_SortKeyTasksDoNotHave_KeepsTheStoredOrder(Sort sort)
    {
        List<TaskModel> tasks = [TestData.Task(3, "c"), TestData.Task(1, "a")];

        Assert.That(tasks.FilterTasks(Parameters(sort)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 1L }));
    }

    // --- FilterHabits ---

    [Test]
    public void FilterHabits_LeavesOutDeletedHabits()
    {
        List<HabitModel> habits = [TestData.Habit(1, "a"), TestData.Habit(2, "b", isDeleted: true)];

        Assert.That(habits.FilterHabits(Parameters()).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
    }

    [Test]
    public void FilterHabits_PriorityAndCategoryFilters_WorkInBothDisplays()
    {
        List<HabitModel> habits = [TestData.Habit(1, "a", priority: Priority.Low, categoryId: 2), TestData.Habit(2, "b", priority: Priority.High, categoryId: 3)];

        QueryParameters checkBoxes = Parameters();
        checkBoxes.ShowPriority[Priority.Low] = false;

        QueryParameters selectPriority = Parameters();
        selectPriority.PriorityFilterDisplay = FilterDisplay.RadioButtons;
        selectPriority.SelectedPriority = Priority.Low;

        QueryParameters hiddenCategory = Parameters();
        hiddenCategory.HiddenCategoryIds = [3];

        QueryParameters selectCategory = Parameters();
        selectCategory.CategoryFilterDisplay = FilterDisplay.SelectOptions;
        selectCategory.SelectedCategoryId = 3;

        Assert.Multiple(() =>
        {
            Assert.That(habits.FilterHabits(checkBoxes).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
            Assert.That(habits.FilterHabits(selectPriority).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
            Assert.That(habits.FilterHabits(hiddenCategory).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
            Assert.That(habits.FilterHabits(selectCategory).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
        });
    }

    [Test]
    public void FilterHabits_SearchTerm_MatchesTitleOrAnItemTitle()
    {
        HabitModel byTitle = TestData.Habit(1, "Stretch");
        HabitModel byItem = TestData.Habit(2, "Gym");
        byItem.Items = [new ItemModel { Title = "stretching" }];
        HabitModel noMatch = TestData.Habit(3, "Read");

        QueryParameters queryParameters = Parameters();
        queryParameters.SearchTerm = "STRETCH";

        Assert.That(new[] { byTitle, byItem, noMatch }.FilterHabits(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L, 1L }));
    }

    [TestCase(DateCompare.Before, new long[] { 1, 4 })]
    [TestCase(DateCompare.On, new long[] { 2, 4 })]
    [TestCase(DateCompare.After, new long[] { 3, 4 })]
    [TestCase(DateCompare.NotOn, new long[] { 1, 3, 5 })]
    public void FilterHabits_DoneAtFilter_ComparesAnyCompletionDay(DateCompare compare, long[] expected)
    {
        List<HabitModel> habits =
        [
            HabitDoneAt(1, "a", _day.AddDays(-2)),
            HabitDoneAt(2, "b", _day.Date.AddHours(22)),
            HabitDoneAt(3, "c", _day.AddDays(2)),
            HabitDoneAt(4, "d", _day.AddDays(-2), _day, _day.AddDays(2)),
            TestData.Habit(5, "e")
        ];
        QueryParameters queryParameters = Parameters();
        queryParameters.DoneAtFilter = _day;
        queryParameters.DoneAtCompare = compare;

        Assert.That(habits.FilterHabits(queryParameters).Select(x => x.Id), Is.EqualTo(expected));
    }

    [Test]
    public void FilterHabits_DoneAtFilter_IgnoresCompletionsThatAreOnlyStarted()
    {
        HabitModel started = TestData.Habit(1, "a");
        started.TimesDone = [new TimeModel { Id = 1, HabitId = 1, StartedAt = _day, CompletedAt = null }];

        QueryParameters queryParameters = Parameters();
        queryParameters.DoneAtFilter = _day;
        queryParameters.DoneAtCompare = DateCompare.On;

        Assert.That(new[] { started }.FilterHabits(queryParameters), Is.Empty);
    }

    [Test]
    public void FilterHabits_InvalidCompare_Throws()
    {
        QueryParameters queryParameters = Parameters();
        queryParameters.DoneAtFilter = _day;
        queryParameters.DoneAtCompare = (DateCompare)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { TestData.Habit() }.FilterHabits(queryParameters).ToList());
    }

    [Test]
    public void FilterHabits_DoneRangeEndOnly_KeepsHabitsDoneByTheEndDay()
    {
        List<HabitModel> habits = [HabitDoneAt(1, "a", _day.AddDays(-9)), HabitDoneAt(2, "b", _day.AddDays(1)), TestData.Habit(3, "c")];
        QueryParameters queryParameters = Parameters();
        queryParameters.DoneRangeEnd = _day;
        queryParameters.ShowDoneInRange = true;

        Assert.That(habits.FilterHabits(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
    }

    // AverageToDesired depends only on the completions, so the ratio bounds can be tested exactly.
    [Test]
    public void FilterHabits_RatioMinimum_KeepsOnlyRatiosStrictlyAboveIt()
    {
        HabitModel half = HabitDoneAt(1, "half", _day, _day.AddHours(12));
        HabitModel once = HabitDoneAt(2, "once", _day, _day.AddDays(1));
        HabitModel late = HabitDoneAt(3, "late", _day, _day.AddDays(1.5));

        QueryParameters queryParameters = Parameters();
        queryParameters.SelectedRatio = Ratio.AverageToDesired;
        queryParameters.ShowOnlyOverSelectedRatioMin = true;
        queryParameters.SelectedRatioMin = 100;

        Assert.Multiple(() =>
        {
            Assert.That(half.GetRatio(Ratio.AverageToDesired), Is.EqualTo(50));
            Assert.That(once.GetRatio(Ratio.AverageToDesired), Is.EqualTo(100));
            Assert.That(late.GetRatio(Ratio.AverageToDesired), Is.EqualTo(150));
            Assert.That(new[] { half, once, late }.FilterHabits(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 3L }));
        });
    }

    [Test]
    public void FilterHabits_RatioMaximum_KeepsOnlyRatiosStrictlyBelowIt()
    {
        HabitModel half = HabitDoneAt(1, "half", _day, _day.AddHours(12));
        HabitModel once = HabitDoneAt(2, "once", _day, _day.AddDays(1));
        HabitModel late = HabitDoneAt(3, "late", _day, _day.AddDays(1.5));

        QueryParameters queryParameters = Parameters();
        queryParameters.SelectedRatio = Ratio.AverageToDesired;
        queryParameters.ShowOnlyUnderSelectedRatioMax = true;
        queryParameters.SelectedRatioMax = 100;

        Assert.That(new[] { half, once, late }.FilterHabits(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 1L }));
    }

    [Test]
    public void FilterHabits_BothRatioBounds_KeepOnlyTheWindowBetweenThem()
    {
        HabitModel half = HabitDoneAt(1, "half", _day, _day.AddHours(12));
        HabitModel once = HabitDoneAt(2, "once", _day, _day.AddDays(1));
        HabitModel late = HabitDoneAt(3, "late", _day, _day.AddDays(1.5));

        QueryParameters queryParameters = Parameters();
        queryParameters.SelectedRatio = Ratio.AverageToDesired;
        queryParameters.ShowOnlyOverSelectedRatioMin = true;
        queryParameters.SelectedRatioMin = 60;
        queryParameters.ShowOnlyUnderSelectedRatioMax = true;
        queryParameters.SelectedRatioMax = 140;

        Assert.That(new[] { half, once, late }.FilterHabits(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L }));
    }

    [Test]
    public void FilterHabits_SortBySelectedRatio_OrdersHighestFirst()
    {
        HabitModel half = HabitDoneAt(1, "half", _day, _day.AddHours(12));
        HabitModel late = HabitDoneAt(2, "late", _day, _day.AddDays(1.5));
        HabitModel once = HabitDoneAt(3, "once", _day, _day.AddDays(1));

        QueryParameters queryParameters = Parameters(Sort.SelectedRatio);
        queryParameters.SelectedRatio = Ratio.AverageToDesired;

        Assert.That(new[] { half, late, once }.FilterHabits(queryParameters).Select(x => x.Id), Is.EqualTo(new[] { 2L, 3L, 1L }));
    }

    [Test]
    public void FilterHabits_SortByRepeatInterval_OrdersByTheTimeBetweenTwoRepetitions()
    {
        HabitModel threeTimesAWeek = TestData.Habit(1, "a", repeatCount: 3, repeatInterval: 1, repeatPeriod: Period.Week);
        HabitModel everyTwoDays = TestData.Habit(2, "b", repeatCount: 1, repeatInterval: 2, repeatPeriod: Period.Day);
        HabitModel daily = TestData.Habit(3, "c", repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Day);
        HabitModel monthly = TestData.Habit(4, "d", repeatCount: 1, repeatInterval: 1, repeatPeriod: Period.Month);

        Assert.That(new[] { threeTimesAWeek, everyTwoDays, daily, monthly }.FilterHabits(Parameters(Sort.RepeatInterval)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 2L, 1L, 4L }));
    }

    [Test]
    public void FilterHabits_SortByAverageInterval_OrdersByTheAverageTimeBetweenRepetitions()
    {
        HabitModel weekly = HabitDoneAt(1, "a", _day, _day.AddDays(7));
        HabitModel daily = HabitDoneAt(2, "b", _day, _day.AddDays(1), _day.AddDays(2));
        HabitModel twiceADayCounted = HabitDoneAt(3, "c", _day, _day.AddDays(3));
        twiceADayCounted.RepeatCount = 2;

        Assert.That(new[] { weekly, daily, twiceADayCounted }.FilterHabits(Parameters(Sort.AverageInterval)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 3L, 1L }));
    }

    [Test]
    public void FilterHabits_SortByTimeSpent_OrdersByTheTotalOfAllCompletions()
    {
        HabitModel twoHours = TestData.Habit(1, "a");
        twoHours.TimesDone = [new TimeModel { StartedAt = _day, CompletedAt = _day.AddHours(1) }, new TimeModel { StartedAt = _day.AddDays(1), CompletedAt = _day.AddDays(1).AddHours(1) }];
        twoHours.RefreshTimesDoneByDay();
        HabitModel ninetyMinutes = TestData.Habit(2, "b");
        ninetyMinutes.TimesDone = [new TimeModel { StartedAt = _day, CompletedAt = _day.AddMinutes(90) }];
        ninetyMinutes.RefreshTimesDoneByDay();

        Assert.Multiple(() =>
        {
            Assert.That(new[] { twoHours, ninetyMinutes }.FilterHabits(Parameters(Sort.TimeSpent)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 1L }));
            Assert.That(new[] { ninetyMinutes, twoHours }.FilterHabits(Parameters(Sort.AverageTimeSpent)).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
        });
    }

    [Test]
    public void FilterHabits_SortByElapsedTime_OrdersNeverDoneFirstThenOldestCompletion()
    {
        List<HabitModel> habits =
        [
            TestData.Habit(1, "a", lastTimeDoneAt: _day),
            TestData.Habit(2, "b", lastTimeDoneAt: null),
            TestData.Habit(3, "c", lastTimeDoneAt: _day.AddDays(-3))
        ];

        Assert.That(habits.FilterHabits(Parameters(Sort.ElapsedTime)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 3L, 1L }));
    }

    [Test]
    public void FilterHabits_SortByDurationCategoryPriorityAndTitle()
    {
        HabitModel a = TestData.Habit(1, "b", priority: Priority.High, categoryId: 2);
        a.Duration = new TimeOnly(1, 0);
        HabitModel b = TestData.Habit(2, "a", priority: Priority.Low, categoryId: 1);
        b.Duration = new TimeOnly(0, 5);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { a, b }.FilterHabits(Parameters(Sort.Duration)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 1L }));
            Assert.That(new[] { a, b }.FilterHabits(Parameters(Sort.Category)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 1L }));
            Assert.That(new[] { b, a }.FilterHabits(Parameters(Sort.Priority)).Select(x => x.Id), Is.EqualTo(new[] { 1L, 2L }));
            Assert.That(new[] { a, b }.FilterHabits(Parameters(Sort.Title)).Select(x => x.Id), Is.EqualTo(new[] { 2L, 1L }));
        });
    }

    [Test]
    public void FilterHabits_SortKeyHabitsDoNotHave_KeepsTheStoredOrder()
    {
        List<HabitModel> habits = [TestData.Habit(3, "c"), TestData.Habit(1, "a")];

        Assert.That(habits.FilterHabits(Parameters(Sort.PlannedAt)).Select(x => x.Id), Is.EqualTo(new[] { 3L, 1L }));
    }
}
