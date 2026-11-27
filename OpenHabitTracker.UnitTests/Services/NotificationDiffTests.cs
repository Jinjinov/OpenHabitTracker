using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class NotificationDiffTests
{
    private static readonly DateTime Now = new(2026, 11, 27, 10, 0, 0);

    private static NotificationRequest Request(string id, int minutesFromNow, string title = "Title", string body = "Body", string route = "/habits/1")
    {
        return new NotificationRequest { Id = id, Title = title, Body = body, Route = route, NotifyAt = Now.AddMinutes(minutesFromNow) };
    }

    private static Dictionary<string, NotificationRequest> Set(params NotificationRequest[] requests)
    {
        return requests.ToDictionary(request => request.Id);
    }

    [Test]
    public void Compare_TheSameSet_ChangesNothing()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(
            Set(Request("a", 10), Request("b", 20)),
            Set(Request("a", 10), Request("b", 20)),
            Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Fired, Is.Empty);
        });
    }

    private static IEnumerable<TestCaseData> ChangedFields()
    {
        yield return new TestCaseData(Request("a", 11)).SetName("Compare_ChangedMoment_RemovesAndAddsIt");
        yield return new TestCaseData(Request("a", 10, title: "Other")).SetName("Compare_ChangedTitle_RemovesAndAddsIt");
        yield return new TestCaseData(Request("a", 10, body: "Other")).SetName("Compare_ChangedBody_RemovesAndAddsIt");
        yield return new TestCaseData(Request("a", 10, route: "/habits/2")).SetName("Compare_ChangedRoute_RemovesAndAddsIt");
    }

    [TestCaseSource(nameof(ChangedFields))]
    public void Compare_OneFieldChanged(NotificationRequest changed)
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("a", 10), Request("b", 20)), Set(changed, Request("b", 20)), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.EqualTo(new[] { "a" }));
            Assert.That(changes.Added, Is.EqualTo(new[] { "a" }));
            Assert.That(changes.Fired, Is.Empty);
        });
    }

    [Test]
    public void Compare_RequestGone_RemovesIt()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("a", 10), Request("b", 20)), Set(Request("b", 20)), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.EqualTo(new[] { "a" }));
            Assert.That(changes.Added, Is.Empty);
        });
    }

    [Test]
    public void Compare_RequestNew_AddsIt()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("b", 20)), Set(Request("a", 10), Request("b", 20)), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.Empty);
            Assert.That(changes.Added, Is.EqualTo(new[] { "a" }));
        });
    }

    [Test]
    public void Compare_OneGoneAndOneNew_RemovesTheOneAndAddsTheOther()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("a", 10), Request("b", 20)), Set(Request("b", 20), Request("c", 30)), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.EqualTo(new[] { "a" }));
            Assert.That(changes.Added, Is.EqualTo(new[] { "c" }));
        });
    }

    // A notification whose moment has passed has been shown; removing it would take it off the screen.
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-24 * 60)]
    public void Compare_HeldMomentPassed_IsFiredNotRemoved(int minutesFromNow)
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("a", minutesFromNow), Request("b", 20)), Set(Request("b", 20)), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Fired, Is.EqualTo(new[] { "a" }));
        });
    }

    [Test]
    public void Compare_HeldOneMinuteAhead_IsNotFired()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("a", 1)), Set(), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.EqualTo(new[] { "a" }));
            Assert.That(changes.Fired, Is.Empty);
        });
    }

    // A summary keeps its id for its day; once one has fired, the next one under the same id is added, not compared with it.
    [Test]
    public void Compare_FiredAndWantedAgainUnderTheSameId_IsAdded()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("a", -5)), Set(Request("a", 60)), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.Empty);
            Assert.That(changes.Added, Is.EqualTo(new[] { "a" }));
            Assert.That(changes.Fired, Is.EqualTo(new[] { "a" }));
        });
    }

    [Test]
    public void Compare_NothingHeld_AddsEverything()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(), Set(Request("a", 10), Request("b", 20)), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.Empty);
            Assert.That(changes.Added, Is.EquivalentTo(new[] { "a", "b" }));
        });
    }

    [Test]
    public void Compare_NothingWanted_RemovesEverything()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(Request("a", 10), Request("b", 20)), Set(), Now);

        Assert.Multiple(() =>
        {
            Assert.That(changes.Removed, Is.EquivalentTo(new[] { "a", "b" }));
            Assert.That(changes.Added, Is.Empty);
        });
    }

    [Test]
    public void Compare_BothEmpty_ChangesNothing()
    {
        NotificationDiff.Changes<string> changes = NotificationDiff.Compare(Set(), Set(), Now);

        Assert.That(changes.Removed.Count + changes.Added.Count + changes.Fired.Count, Is.Zero);
    }

    // The numbers are what the platform holds across app starts and updates, so the function must never change.
    [TestCase("habit-12-20261127", 424867593)]
    [TestCase("task-368724", 827528109)]
    [TestCase("", 18652613)]
    public void StableNumber_IsTheSameInEveryProcess(string id, int expected)
    {
        Assert.That(NotificationDiff.StableNumber(id), Is.EqualTo(expected));
    }

    [Test]
    public void Numbered_TwoIdsWithTheSameHash_TheFirstInOrderKeepsItAndTheOtherTakesTheNext()
    {
        // "task-368724" and "task-798200" hash to the same number.
        NotificationRequest first = Request("task-368724", 10);
        NotificationRequest second = Request("task-798200", 20);

        Dictionary<int, NotificationRequest> forward = NotificationDiff.Numbered([first, second]);
        Dictionary<int, NotificationRequest> backward = NotificationDiff.Numbered([second, first]);

        Assert.Multiple(() =>
        {
            Assert.That(NotificationDiff.StableNumber("task-798200"), Is.EqualTo(827528109));
            Assert.That(forward[827528109], Is.SameAs(first));
            Assert.That(forward[827528110], Is.SameAs(second));
            Assert.That(backward.Keys, Is.EquivalentTo(forward.Keys));
            Assert.That(backward[827528110], Is.SameAs(second));
        });
    }

    [Test]
    public void Numbered_DistinctIds_EachUnderItsOwnHash()
    {
        Dictionary<int, NotificationRequest> numbered = NotificationDiff.Numbered([Request("habit-12-20261127", 10), Request("task-368724", 20)]);

        Assert.That(numbered.Keys, Is.EquivalentTo(new[] { 424867593, 827528109 }));
    }
}
