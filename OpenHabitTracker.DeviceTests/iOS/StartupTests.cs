namespace OpenHabitTracker.DeviceTests.iOS;

[TestFixture]
public class StartupTests : IosTest
{
    protected override IosBuild Build => IosBuild.Release;

    // The mark of the None priority, which the first launch's welcome note carries; it is not localized.
    private const string NoPriority = "⊘";

    // Trimming and AOT apply only to the Release build, which no other iOS test runs,
    // and what they break usually breaks before the first page renders.
    // The Release page has no web context and its accessibility tree carries only localized text,
    // so the rendered list is found by the priority mark, and a failure by a crash report or the app leaving the foreground.
    [Test]
    public void FirstLaunch_ReleaseBuild_RendersTheNotesListWithoutCrashing()
    {
        ReinstallApp();
        LaunchApp();

        WaitForNativeElement(MobileBy.IosNSPredicate($"type == 'XCUIElementTypeStaticText' AND value == '{NoPriority}'"), StartupTimeout);

        Assert.That(AppCrash(), Is.Null, "The app crashed during the first launch");
        Assert.That(AppState(), Is.EqualTo(4), "The app left the foreground");
    }
}
