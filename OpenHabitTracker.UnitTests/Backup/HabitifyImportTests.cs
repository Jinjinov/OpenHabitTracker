using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Backup.Habitify;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Backup;

// The free plan allows three habits and logging only today, so the fixture holds one day of history.
[TestFixture]
public class HabitifyImportTests
{
    private ImportedData _data = null!;

    [SetUp]
    public async Task SetUp()
    {
        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        await using Stream stream = File.OpenRead(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Importers", "Habitify", "backup.db"));

        _data = await new HabitifyImport(loc).ReadDataFile(stream);
    }

    private HabitModel Habit(string title) => _data.Categories.SelectMany(x => x.Habits).Single(x => x.Title == title);

    private static DateTime Midday(int year, int month, int day) => new(year, month, day, 12, 0, 0);

    private static (int, int, Period) Rule(HabitModel habit) => (habit.RepeatCount, habit.RepeatInterval, habit.RepeatPeriod);

    [Test]
    public void Habits_AreImportedWithTrimmedTitlesAndTheirSchedule()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_data.Categories.Single().Habits.Select(x => x.Title), Is.EqualTo(new[] { "Drink water", "Read", "Run (km)" }));
            Assert.That(Rule(Habit("Drink water")), Is.EqualTo((1, 1, Period.Day)));
            Assert.That(Rule(Habit("Read")), Is.EqualTo((3, 1, Period.Week)));
            Assert.That(Rule(Habit("Run (km)")), Is.EqualTo((1, 2, Period.Day)));
            Assert.That(Habit("Read").StartAt, Is.EqualTo(new DateTime(2026, 11, 1)));
            Assert.That(Habit("Read").Color, Is.EqualTo("bg-primary-subtle"));
            Assert.That(_data.Options.MatchCompletionsByDay, Is.True);
        });
    }

    [Test]
    public void RepetitionGoal_IsAHabitThatIsDone()
    {
        HabitModel drinkWater = Habit("Drink water");

        Assert.Multiple(() =>
        {
            Assert.That(drinkWater.DisplayMetric, Is.EqualTo(DisplayMetric.Repetitions));
            Assert.That(drinkWater.TimesDone!.Select(x => x.StartedAt), Is.EqualTo(new[] { Midday(2026, 11, 8) }));
            Assert.That(Habit("Read").TimesDone, Is.Empty);
        });
    }

    [Test]
    public void LogInTheBaseUnit_IsConvertedToTheGoalsUnit()
    {
        HabitModel run = Habit("Run (km)");

        Assert.Multiple(() =>
        {
            Assert.That(run.DisplayMetric, Is.EqualTo(DisplayMetric.Quantity));
            Assert.That(run.TargetQuantity, Is.EqualTo(5));
            Assert.That(run.TimesDone!.Select(x => (x.StartedAt, x.Quantity)), Is.EqualTo(new[] { (Midday(2026, 11, 8), 5.0) }));
            Assert.That(run.LastTimeDoneAt, Is.EqualTo(Midday(2026, 11, 8)));
        });
    }
}
