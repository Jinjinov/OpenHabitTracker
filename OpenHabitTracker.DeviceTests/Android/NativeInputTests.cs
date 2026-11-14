namespace OpenHabitTracker.DeviceTests.Android;

[TestFixture]
public class NativeInputTests : AndroidTest
{
    // input type=time opens Android's clock dialog, and the value it hands back must reach the task.
    // Reopening the detail renders the field from the task again, not from what the input last showed.
    // Persisting the value is the e2e's EditTask_PlannedAt_PersistedAfterReload.
    [Test]
    public void PlannedTime_SetInTheSystemClockDialog_ReachesTheTask()
    {
        OpenNewTask("Timed task");

        TapNatively(WaitForElement("[data-tasks-step-12] input[type='time']"));

        // The dial's numbers are found by their digits, which do not change with the system language.
        WaitForNativeElement(By.Id("android:id/timePicker"));
        DialNumber("9").Click();
        DialNumber("30").Click();
        Driver.FindElement(By.Id("android:id/pm_label")).Click();
        Driver.FindElement(By.Id("android:id/button1")).Click();

        AssertTaskField("input[type='time']", "21:30");
    }

    // input type=date opens Android's calendar dialog.
    [Test]
    public void PlannedDate_SetInTheSystemCalendarDialog_ReachesTheTask()
    {
        OpenNewTask("Dated task");

        TapNatively(WaitForElement("[data-tasks-step-12] input[type='date']"));

        // The 15th of the month the dialog opens on, which is the current one for an empty field.
        WaitForNativeElement(By.Id("android:id/datePicker"));
        Driver.FindElement(MobileBy.AndroidUIAutomator("new UiSelector().resourceId(\"android:id/month_view\").childSelector(new UiSelector().text(\"15\"))")).Click();
        Driver.FindElement(By.Id("android:id/button1")).Click();

        AssertTaskField("input[type='date']", $"{DateTime.Today:yyyy-MM}-15");
    }

    // A select element opens as Android's own list; the option picked there must reach the note.
    [Test]
    public void Category_PickedInTheSystemList_ReachesTheNote()
    {
        ClearApp();
        LaunchApp();
        SwitchToWebView();

        // A category of its own, whose name, unlike the Welcome category's, does not change with the language.
        OpenSidebar("bi-tag");
        IWebElement categoryTitle = WaitForElement("[data-categories-step-1] input");
        SetValue(categoryTitle, "Picked category");
        WaitForElement("[data-categories-step-1] button:has(i.bi-plus-square)").Click();
        WaitForElement("#closeSidebar").Click();

        AddLongNoteAndOpenIt("Categorized note", lines: 1);

        TapNatively(WaitForElement("[data-notes-step-9] select"));
        WaitForNativeElement(MobileBy.AndroidUIAutomator("new UiSelector().text(\"Picked category\")")).Click();

        SwitchToWebView();
        Assert.That(WaitUntil(() => SelectedText("[data-notes-step-9] select") == "Picked category", DefaultTimeout), Is.True,
            $"The select shows {SelectedText("[data-notes-step-9] select")}");

        // Reopened, the detail renders the select from the note again.
        WaitForElement("[data-notes-step-7]").Click();
        Row(NoteRows, "Categorized note")!.Click();

        Assert.That(SelectedText("[data-notes-step-9] select"), Is.EqualTo("Picked category"), "The note did not keep the category");
    }

    private string? SelectedText(string select)
    {
        return RunScript("const s = document.querySelector(arguments[0]); return s.options[s.selectedIndex].text;", select) as string;
    }

    private void OpenNewTask(string title)
    {
        ClearApp();
        LaunchApp();
        SwitchToWebView();

        OpenPage(TasksPage);
        AddItem(title);

        IWebElement? row = Row(TaskRows, title);
        Assert.That(row, Is.Not.Null, $"The task {title} is not in the list");
        row!.Click();
    }

    private IWebElement DialNumber(string number)
    {
        return WaitForNativeElement(MobileBy.AndroidUIAutomator($"new UiSelector().resourceId(\"android:id/radial_picker\").childSelector(new UiSelector().description(\"{number}\"))"));
    }

    // Closes and reopens the task's detail and reads the field again.
    private void AssertTaskField(string input, string expected)
    {
        SwitchToWebView();
        Assert.That(WaitUntil(() => WaitForElement($"[data-tasks-step-12] {input}").GetDomProperty("value") == expected, DefaultTimeout), Is.True,
            $"The field shows {WaitForElement($"[data-tasks-step-12] {input}").GetDomProperty("value")}");

        WaitForElement("[data-tasks-step-10]").Click();
        Row(TaskRows, "task")!.Click();

        Assert.That(WaitForElement($"[data-tasks-step-12] {input}").GetDomProperty("value"), Is.EqualTo(expected), "The task did not keep the value");
    }
}
