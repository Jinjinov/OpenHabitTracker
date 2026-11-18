namespace OpenHabitTracker.DeviceTests.Windows;

// The WinForms host opens and saves files through the Win32 common dialogs; the MSIX goes through WinRT's pickers.
[TestFixture]
public class FileDialogTests() : WindowsTest(WindowsHost.WinForms)
{
    // The file name box of the open dialog and of the save dialog, and the button of both that confirms the name.
    private const string OpenFileNameBox = "1148";
    private const string SaveFileNameBox = "1001";
    private const string ConfirmButton = "1";

    private string _folder = null!;

    // Deleted after the app has stopped, which can still hold the file it read open.
    [SetUp]
    public void CreateFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "oht-device-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);

        string folder = _folder;
        AfterTest(() => Directory.Delete(folder, true));
    }

    // Every import format, read through the open dialog in the published Release build.
    [TestCase("Examples/example.json", "JSON (.json)")]
    [TestCase("Examples/example.tsv", "TSV (.tsv)")]
    [TestCase("Examples/example.yaml", "YAML (.yaml)")]
    [TestCase("Examples/example.md", "Markdown (.md)")]
    [TestCase("Importers/takeout.zip", "Google Keep (.zip)")]
    [TestCase("Importers/loop-backup.db", "Loop Habit Tracker (.db)")]
    [TestCase("Importers/loop-export.zip", "Loop Habit Tracker (.zip)")]
    [TestCase("Importers/habitnow.hn", "HabitNow (.hn)")]
    [TestCase("Importers/habitica.json", "Habitica (.json)")]
    [TestCase("Importers/habitify.db", "Habitify (.db)")]
    public void Import_FileChosenInTheOpenDialog_IsReadAndImported(string fixture, string format)
    {
        string path = Path.Combine(_folder, Path.GetFileName(fixture));
        File.Copy(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fixture), path);

        ClearData();
        StartApp();
        SwitchToWebView();

        OpenSidebar("bi-database");
        WaitForElement("button:has(i.bi-box-arrow-in-right)").Click();

        ChooseInDialog(OpenFileNameBox, _ => path);

        SwitchToWebView();
        Assert.That(ConfirmImport(), Does.StartWith(format), "The summary names another format");
    }

    // The save dialog proposes the export's file name; the file it writes is read back.
    [Test]
    public void ExportJson_SavedInTheSaveDialog_FileHoldsTheData()
    {
        ClearData();
        StartApp();
        SwitchToWebView();

        OpenNotesPage();
        AddItem("Exported note");

        // Export JSON
        OpenSidebar("bi-database");
        WaitForElement("[data-data-step-3]").Click();

        string proposed = ChooseInDialog(SaveFileNameBox, fileName => Path.Combine(_folder, fileName));
        Assert.That(proposed, Does.StartWith("OpenHabitTracker.").And.EndWith(".json"));

        string path = Path.Combine(_folder, proposed);
        Assert.That(WaitUntil(() => File.Exists(path), DefaultTimeout), Is.True, $"{path} was not written");

        SwitchToWebView();
        AssertNoErrorShown();

        Assert.That(File.ReadAllText(path), Does.Contain("\"Title\": \"Exported note\""), "The exported file does not hold the note");
    }

    // Native context: in the common dialog the app opened, enters the path made from the file name the dialog proposes,
    // confirms it, and returns the proposed name. The dialog is a window of its own, so the session moves to it and back.
    private string ChooseInDialog(string fileNameBox, Func<string, string> path)
    {
        Driver.Context = NativeContext;
        string appWindow = Driver.CurrentWindowHandle;

        string? dialog = null;
        Assert.That(WaitUntil(() => (dialog = DialogHandle()) is not null, DefaultTimeout), Is.True, "The app opened no file dialog");
        Driver.SwitchTo().Window(dialog);

        IWebElement fileName = WaitForNativeElement(MobileBy.AccessibilityId(fileNameBox));
        string proposed = fileName.GetAttribute("Value");

        fileName.Clear();
        fileName.SendKeys(path(proposed));
        Driver.FindElement(MobileBy.AccessibilityId(ConfirmButton)).Click();

        Assert.That(WaitUntil(() => DialogHandle() is null, DefaultTimeout), Is.True, "The file dialog did not close");
        Driver.SwitchTo().Window(appWindow);

        return proposed;
    }

    // The common dialogs are windows of the class #32770.
    private string? DialogHandle()
    {
        IEnumerable<object> windows = (IEnumerable<object>)Driver.ExecuteScript("windows: getWindows")!;

        return windows.Cast<Dictionary<string, object>>().Where(window => (string)window["className"] == "#32770").Select(window => (string)window["handle"]).FirstOrDefault();
    }
}
