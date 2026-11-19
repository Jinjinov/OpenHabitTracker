namespace OpenHabitTracker.DeviceTests.MacCatalyst;

[TestFixture]
public class FileDialogTests : MacCatalystTest
{
    // The export goes through the system save panel, which macOS shows a sandboxed app only with the
    // user-selected read-write entitlement. The file it writes is read back and checked.
    // The page is reachable only through the accessibility tree, so its English labels are the locators;
    // the save panel carries identifiers of its own.
    [Test]
    public void ExportJson_SavedInTheSavePanel_FileHoldsTheData()
    {
        ResetApp();
        LaunchApp();

        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Add new note'"), StartupTimeout).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 49 AND label == 'Note title'")).SendKeys("Exported note");
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Save'")).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Exported note'"));

        // The menu entry carries its text as a label; the welcome note's markdown carries the same word as a value.
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Toggle menu'")).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 48 AND label == 'Data'")).Click();
        WaitForNativeElement(MobileBy.IosNSPredicate("elementType == 9 AND label == 'Export JSON'")).Click();

        WaitForNativeElement(MobileBy.AccessibilityId("save-panel"));
        string fileName = WaitForNativeElement(MobileBy.AccessibilityId("saveAsNameTextField")).GetAttribute("value") ?? "";
        Assert.That(fileName, Does.StartWith("OpenHabitTracker.").And.EndWith(".json"));

        // The panel opens on Documents, where the file is read back from.
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", fileName);
        File.Delete(path);

        WaitForNativeElement(MobileBy.AccessibilityId("OKButton")).Click();

        Assert.That(WaitUntil(() => File.Exists(path), DefaultTimeout), Is.True, $"{path} was not written");

        string json = File.ReadAllText(path);
        File.Delete(path);

        Assert.That(json, Does.Contain("Exported note"), "The exported file does not hold the note");
    }
}
