namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class CultureTests : AndroidTest
{
    // The keys of the app's languages.
    private static readonly string[] Languages = ["cs", "da", "de", "en", "es", "fi", "fr", "hr", "it", "ja", "ko", "nl", "no", "pl", "pt", "sk", "sl", "sr", "sv", "zh"];

    // Each language, with the device's locale for the app set to it as well: on Android, culture data comes from the OS,
    // and the dev machine, where every other test runs, always has all of it.
    // Every page renders its dates, numbers and statistics with the examples loaded.
    [TestCaseSource(nameof(Languages))]
    public void Language_AppAndDeviceLocale_EveryPageRendersWithoutErrors(string language)
    {
        RestoreAfterTest(() => Shell("cmd", "locale", "set-app-locales", AppPackage, "--locales", "en"));

        ClearApp();
        Shell("cmd", "locale", "set-app-locales", AppPackage, "--locales", language == "sr" ? "sr-Latn" : language);

        LaunchApp();
        SwitchToWebView();

        OpenSidebar("bi-database");
        WaitForElement("[data-data-step-1]").Click();
        WaitForElement("#closeSidebar").Click();

        OpenSidebar("bi-gear");
        IWebElement select = WaitForElement("[data-settings-step-18] select");
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", select, language);
        WaitForElement("#closeSidebar").Click();

        // The page carries the culture's name, which for Serbian names the Latin script.
        string pageLanguage = language == "sr" ? "sr-Latn" : language;
        Assert.That(WaitUntil(() => RunScript("return document.documentElement.lang;") as string == pageLanguage, DefaultTimeout), Is.True,
            $"The page language is {RunScript("return document.documentElement.lang;")}");

        foreach (string page in new[] { HomePage, NotesPage, TasksPage, HabitsPage })
        {
            OpenPage(page);
            AssertNoErrorShown();
        }
    }
}
