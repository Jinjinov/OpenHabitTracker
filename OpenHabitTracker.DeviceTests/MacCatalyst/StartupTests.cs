namespace OpenHabitTracker.DeviceTests.MacCatalyst;

[TestFixture]
public class StartupTests : MacCatalystTest
{
    // The mark of the None priority, which the first launch's welcome note carries; it is not localized.
    private const string NoPriority = "⊘";

    // A first launch runs every migration in the sandbox container before the page renders.
    // The page's accessibility tree carries only localized text, so the rendered list is found by the priority mark,
    // and a failure by a crash report or the app leaving the foreground.
    [Test]
    public void FirstLaunch_RendersTheNotesListWithoutCrashing()
    {
        ResetApp();
        LaunchApp();

        WaitForNativeElement(MobileBy.IosNSPredicate($"elementType == 48 AND value == '{NoPriority}'"), StartupTimeout);

        Assert.That(AppCrash(), Is.Null, "The app crashed during the first launch");
        Assert.That(AppState(), Is.EqualTo(4), "The app left the foreground");
    }
}
