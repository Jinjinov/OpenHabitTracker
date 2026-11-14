namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class StartupTests : AndroidTest
{
    // Trimming and AOT apply only to the Release build, which no other test runs,
    // and what they break usually breaks before the first page renders.
    [Test]
    public void FirstLaunch_ReleaseBuild_RendersTheNotesListWithoutErrors()
    {
        ClearApp();
        LaunchApp();

        SwitchToWebView();
        WaitForElement("main#main-content", StartupTimeout);
        WaitForElement("[data-notes-step-2]", StartupTimeout);

        AssertNoErrorShown();

        Driver.Context = NativeContext;
        Assert.That(AppCrash(), Is.Null, "The app crashed during the first launch");
    }
}
