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

        // The error boundaries and the database error all render role="alert".
        IWebElement? alert = Driver.FindElements(By.CssSelector("[role='alert']")).FirstOrDefault(element => element.Displayed);
        Assert.That(alert?.Text, Is.Null, "The page shows an error");

        Driver.Context = NativeContext;
        Assert.That(AppCrash(), Is.Null, "The app crashed during the first launch");
    }
}
