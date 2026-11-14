namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class ExternalLinkTests : AndroidTest
{
    // A link must leave for the system browser; navigating the WebView itself would replace the app with the website.
    [TestCase("https://github.com/Jinjinov/OpenHabitTracker")]
    [TestCase("https://www.reddit.com/r/OpenHabitTracker")]
    public void AboutLink_Tapped_OpensOutsideTheApp(string url)
    {
        OpenAbout();
        MarkPage();

        TapNatively(WaitForElement($"a[href='{url}']"));

        Assert.That(WaitUntil(() => Driver.CurrentPackage != AppPackage, DefaultTimeout), Is.True, "The link did not leave the app");

        LaunchApp();
        SwitchToWebView();
        Assert.That(IsPageMarked(), Is.True, "The WebView navigated away from the app");
    }

    // Rate this app opens the store's write-a-review page: in the Play Store app, or, on a device where the Play Store
    // has no account signed in, such as this emulator, on the Play website in the browser.
    // The automatic review prompt is quota-limited by the OS and is left out.
    [Test]
    public void RateThisApp_Tapped_OpensTheStore()
    {
        OpenAbout();

        TapNatively(WaitForElement("button:has(i.bi-star)"));

        string[] stores = ["com.android.vending", "com.android.chrome"];
        Assert.That(WaitUntil(() => stores.Contains(Driver.CurrentPackage), DefaultTimeout), Is.True, $"The foreground app is {Driver.CurrentPackage}");
    }

    private void OpenAbout()
    {
        ClearApp();
        LaunchApp();
        SwitchToWebView();

        OpenSidebar("bi-info-circle");
    }
}
