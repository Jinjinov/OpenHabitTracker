using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Data.Models;

[TestFixture]
public class SettingsModelTests
{
    [TestCase(null, 0, 0)]
    [TestCase(0, 0, 0)]
    [TestCase(45, 0, 45)]
    [TestCase(60, 1, 0)]
    [TestCase(135, 2, 15)]
    public void NotificationLeadHourAndMinute_SplitTheLeadMinutes(int? leadMinutes, int hour, int minute)
    {
        SettingsModel settings = new() { NotificationLeadMinutes = leadMinutes };

        Assert.Multiple(() =>
        {
            Assert.That(settings.NotificationLeadHour, Is.EqualTo(hour));
            Assert.That(settings.NotificationLeadMinute, Is.EqualTo(minute));
        });
    }

    // A new database starts from these values, so each one is what a first-time user sees.
    [Test]
    public void GetDefaultSettings_HoldsTheFirstRunDefaults()
    {
        SettingsModel settings = SettingsModel.GetDefaultSettings(7);

        Assert.Multiple(() =>
        {
            Assert.That(settings.UserId, Is.EqualTo(7));
            Assert.That(settings.IsDarkMode, Is.True);
            Assert.That(settings.Theme, Is.EqualTo("default"));
            Assert.That(settings.StartPage, Is.EqualTo("/notes"));
            Assert.That(settings.StartSidebar, Is.Empty);
            Assert.That(settings.Culture, Is.EqualTo("en"));
            Assert.That(settings.FirstDayOfWeek, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(settings.DateFormat, Is.Null);
            Assert.That(settings.TimeFormat, Is.Null);
            Assert.That(settings.SelectedRatio, Is.EqualTo(Ratio.ElapsedToDesired));
            Assert.That(settings.RememberMe, Is.True);
            Assert.That(settings.ShowHelp, Is.True);
            Assert.That(settings.UncheckAllItemsOnHabitDone, Is.False);
            Assert.That(settings.ShowPriorityDropdown, Is.True);
            Assert.That(settings.ShowItemList, Is.True);
            Assert.That(settings.ShowSmallCalendar, Is.True);
            Assert.That(settings.MaxSmallCalendarDays, Is.EqualTo(0));
            Assert.That(settings.ShowLargeCalendar, Is.True);
            Assert.That(settings.ShowHabitStatistics, Is.False);
            Assert.That(settings.ShowHabitCharts, Is.False);
            Assert.That(settings.ShowCategory, Is.True);
            Assert.That(settings.ShowColor, Is.True);
            Assert.That(settings.ShowCreatedUpdated, Is.False);
            Assert.That(settings.InsertTabsInNoteContent, Is.True);
            Assert.That(settings.DisplayNoteContentAsMarkdown, Is.True);
            Assert.That(settings.ShowGroupedByCategory, Is.False);
            Assert.That(settings.ShowLastTimeDone, Is.True);
            Assert.That(settings.HideCompletedTasks, Is.True);
            Assert.That(settings.ShowOnlyOverSelectedRatioMin, Is.False);
            Assert.That(settings.ShowOnlyUnderSelectedRatioMax, Is.False);
            Assert.That(settings.SelectedRatioMin, Is.EqualTo(50));
            Assert.That(settings.SelectedRatioMax, Is.EqualTo(150));
            Assert.That(settings.ShowDoneInRange, Is.True);
            Assert.That(settings.HorizontalMargin, Is.EqualTo(1));
            Assert.That(settings.NotificationTime, Is.Null);
            Assert.That(settings.NotificationLeadMinutes, Is.Null);
            Assert.That(settings.NotificationContent, Is.EqualTo(DigestContent.Both));
            Assert.That(settings.NotificationHabitThreshold, Is.EqualTo(100));
            Assert.That(settings.CategoryFilterDisplay, Is.EqualTo(FilterDisplay.CheckBoxes));
            Assert.That(settings.PriorityFilterDisplay, Is.EqualTo(FilterDisplay.CheckBoxes));
            Assert.That(settings.HiddenCategoryIds, Is.Empty);
            Assert.That(settings.ShowPriority.Values, Is.All.True);
            Assert.That(settings.ShowPriority.Keys, Is.EquivalentTo(Enum.GetValues<Priority>()));
            Assert.That(settings.FoldSection.Values, Is.All.False);
            Assert.That(settings.FoldSection.Keys, Is.EquivalentTo(Enum.GetValues<QuerySection>()));
            Assert.That(settings.SortBy[ContentType.Note], Is.EqualTo(Sort.Priority));
            Assert.That(settings.SortBy[ContentType.Task], Is.EqualTo(Sort.PlannedAt));
            Assert.That(settings.SortBy[ContentType.Habit], Is.EqualTo(Sort.SelectedRatio));
        });
    }

    [Test]
    public void GetDefaultSettings_EachCallReturnsItsOwnCollections()
    {
        SettingsModel first = SettingsModel.GetDefaultSettings(1);
        SettingsModel second = SettingsModel.GetDefaultSettings(1);

        first.HiddenCategoryIds.Add(5);
        first.ShowPriority[Priority.Low] = false;

        Assert.Multiple(() =>
        {
            Assert.That(second.HiddenCategoryIds, Is.Empty);
            Assert.That(second.ShowPriority[Priority.Low], Is.True);
        });
    }
}
