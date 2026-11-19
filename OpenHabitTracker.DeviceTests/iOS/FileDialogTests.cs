namespace OpenHabitTracker.DeviceTests.iOS;

[TestFixture]
public class FileDialogTests : IosTest
{
    protected override IosBuild Build => IosBuild.Debug;

    // Every import format, read through the Files picker.
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
    public void Import_FilePickedInTheFilesPicker_IsReadAndImported(string fixture, string deviceName, string format)
    {
        PushToOnMyIPhone(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fixture), deviceName);

        ReinstallApp();
        LaunchApp();
        SwitchToWebView();

        OpenSidebar("bi-database");
        WaitForElement("button:has(i.bi-box-arrow-in-right)").Click();

        Driver.Context = NativeContext;
        PickFromOnMyIPhone(deviceName);

        SwitchToWebView();
        Assert.That(ConfirmImport(), Does.StartWith(format), "The summary names another format");
    }

    // The export goes through the Files save dialog into On My iPhone; the file it writes is read back and checked.
    [Test]
    public void ExportJson_SavedInTheFilesSaveDialog_FileHoldsTheData()
    {
        string folder = OnMyIPhoneOnHost();
        foreach (string earlier in Directory.GetFiles(folder, "OpenHabitTracker.*.json"))
            File.Delete(earlier);

        ReinstallApp();
        LaunchApp();
        SwitchToWebView();

        OpenNotesPage();
        AddItem("Exported note");

        // Export JSON
        OpenSidebar("bi-database");
        WaitForElement("[data-data-step-3]").Click();

        // The save dialog's navigation bar ends with its Save button.
        Driver.Context = NativeContext;
        OpenOnMyIPhoneInPicker();
        Driver.FindElement(MobileBy.IosClassChain("**/XCUIElementTypeNavigationBar[`name == 'FullDocumentManagerViewControllerNavigationBar'`]/**/XCUIElementTypeButton[-1]")).Click();

        string? path = null;
        Assert.That(WaitUntil(() => (path = Directory.GetFiles(folder, "OpenHabitTracker.*.json").FirstOrDefault()) is not null, DefaultTimeout), Is.True, $"Nothing was written to {folder}");

        string json = File.ReadAllText(path!);
        File.Delete(path!);

        Assert.That(json, Does.Contain("Exported note"), "The exported file does not hold the note");
    }
}
