namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class FileDialogTests : AndroidTest
{
    // Every import format, read through the system picker in the Release build, where trimming and AOT apply;
    // the unit tests read the same files untrimmed. The summary names the format it recognized, in text that is not localized.
    [TestCase("Examples/example.json", "oht-example.json", "JSON (.json)")]
    [TestCase("Examples/example.tsv", "oht-example.tsv", "TSV (.tsv)")]
    [TestCase("Examples/example.yaml", "oht-example.yaml", "YAML (.yaml)")]
    [TestCase("Examples/example.md", "oht-example.md", "Markdown (.md)")]
    [TestCase("Importers/takeout.zip", "oht-takeout.zip", "Google Keep (.zip)")]
    [TestCase("Importers/loop-backup.db", "oht-loop-backup.db", "Loop Habit Tracker (.db)")]
    [TestCase("Importers/loop-export.zip", "oht-loop-export.zip", "Loop Habit Tracker (.zip)")]
    [TestCase("Importers/habitnow.hn", "oht-habitnow.hn", "HabitNow (.hn)")]
    [TestCase("Importers/habitica.json", "oht-habitica.json", "Habitica (.json)")]
    [TestCase("Importers/habitify.db", "oht-habitify.db", "Habitify (.db)")]
    public void Import_FilePickedInTheSystemPicker_IsReadAndImported(string fixture, string deviceName, string format)
    {
        PushToDownloads(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fixture), deviceName);

        ClearApp();
        LaunchApp();
        SwitchToWebView();

        OpenSidebar("bi-database");
        WaitForElement("button:has(i.bi-box-arrow-in-right)").Click();

        Driver.Context = NativeContext;
        PickFromDownloads(deviceName);

        SwitchToWebView();
        WaitUntil(() => Driver.FindElements(By.CssSelector("[data-import-summary], [role='alert'], #blazor-error-ui")).Any(element => element.Displayed), DefaultTimeout);
        AssertNoErrorShown();

        Assert.That(WaitForElement("[data-import-summary] small").Text, Does.StartWith(format), "The summary names another format");

        WaitForElement("[data-import-summary] .btn-primary").Click();
        Assert.That(WaitUntil(() => Driver.FindElements(By.CssSelector("[data-import-summary]")).Count == 0, DefaultTimeout), Is.True, "The import did not finish");
        AssertNoErrorShown();
    }

    // The export goes through the system's create-document dialog; the file it writes is pulled back and checked.
    [Test]
    public void ExportJson_SavedInTheSystemSaveDialog_FileHoldsTheData()
    {
        ClearApp();
        LaunchApp();
        SwitchToWebView();

        OpenNotesPage();
        AddItem("Exported note");

        // Export JSON
        OpenSidebar("bi-database");
        WaitForElement("[data-data-step-3]").Click();

        Driver.Context = NativeContext;
        WaitForNativeElement(By.Id("com.google.android.documentsui:id/container_save"));
        OpenDownloadsInDocumentsUi();

        string fileName = Driver.FindElement(MobileBy.AndroidUIAutomator("new UiSelector().resourceId(\"com.google.android.documentsui:id/container_save\").childSelector(new UiSelector().resourceId(\"android:id/title\"))")).Text;
        Assert.That(fileName, Does.StartWith("OpenHabitTracker.").And.EndWith(".json"));

        Driver.FindElement(By.Id("android:id/button1")).Click();

        string path = $"/sdcard/Download/{fileName}";
        Assert.That(WaitUntil(() => Shell("ls", "/sdcard/Download").Contains(fileName), DefaultTimeout), Is.True, $"{path} was not written");

        string json = System.Text.Encoding.UTF8.GetString(Driver.PullFile(path));
        Shell("rm", "-f", path);

        Assert.That(json, Does.Contain("Exported note"), "The exported file does not hold the note");
    }
}
