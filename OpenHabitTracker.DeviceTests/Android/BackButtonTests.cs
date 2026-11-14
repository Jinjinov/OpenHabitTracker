namespace OpenHabitTracker.DeviceTests.Android;

// What the Android back button does today, so that any change to it is a deliberate one.
// Back walks the page's browser history, and leaves the app once there is none; it does not close a sidebar or a modal first.
[TestFixture]
public class BackButtonTests : AndroidTest
{
    [Test]
    public void StartPage_Back_LeavesTheApp()
    {
        StartFresh();

        PressBack();

        Assert.That(WaitUntil(() => Driver.CurrentPackage != AppPackage, DefaultTimeout), Is.True, "Back on the start page stayed in the app");
    }

    [Test]
    public void PageReachedFromAnother_Back_ReturnsToThePreviousPage()
    {
        StartFresh();
        OpenPage(TasksPage);

        PressBack();
        SwitchToWebView();

        Assert.That(WaitUntil(() => Path() == "/notes", DefaultTimeout), Is.True, $"Back went to {Path()}");
    }

    [Test]
    public void OpenDetail_Back_ClosesTheDetail()
    {
        StartFresh();
        WaitForElement(NoteRows).Click();
        WaitForElement("#note-component");

        PressBack();
        SwitchToWebView();

        Assert.That(WaitUntil(() => Path() == "/notes" && !IsShown("#note-component"), DefaultTimeout), Is.True, $"Back went to {Path()}");
    }

    // The sidebar stays open; the page behind it goes back, here to the root route the start page was redirected from.
    [Test]
    public void OpenSidebar_Back_GoesBackBehindTheOpenSidebar()
    {
        StartFresh();
        OpenSidebar("bi-gear");

        PressBack();
        SwitchToWebView();

        Assert.That(WaitUntil(() => Path() == "/", DefaultTimeout), Is.True, $"Back went to {Path()}");
        Assert.That(IsShown("#closeSidebar"), Is.True, "Back closed the sidebar");
    }

    // The modal stays open through Back; a bounded wait, because what is checked is that nothing changes.
    [Test]
    public void OpenModal_Back_LeavesTheModalOpen()
    {
        StartFresh();
        OpenQuantityModal();

        PressBack();
        SwitchToWebView();

        Assert.That(WaitUntil(() => !IsShown("#quantity-modal"), TimeSpan.FromSeconds(5)), Is.False, "Back closed the modal");
    }

    // The app starts on the notes page; tapping its nav link there would add a history entry of its own.
    private void StartFresh()
    {
        ClearApp();
        LaunchApp();
        SwitchToWebView();
        WaitForElement(NoteRows);
    }

    // A Quantity habit's small calendar asks for the quantity in a modal.
    private void OpenQuantityModal()
    {
        OpenPage(HabitsPage);
        AddItem("Quantity habit");
        Row(HabitRows, "Quantity habit")!.Click();

        IWebElement display = WaitForElement("#habit-component select:has(option[value='Quantity'])");
        RunScript("arguments[0].value = 'Quantity'; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", display);
        WaitForElement("[data-habits-step-11]").Click();

        WaitForElement("[data-habits-step-6] button[role='gridcell'].border-primary-subtle").Click();
        WaitForElement("#quantity-modal");
    }

    // Leaves the native context active: Back can leave the app, and the WebView with it.
    private void PressBack()
    {
        Driver.Context = NativeContext;
        Driver.ExecuteScript("mobile: pressKey", new Dictionary<string, object> { ["keycode"] = 4 });
    }

    private string Path()
    {
        return RunScript("return location.pathname;") as string ?? "";
    }

    private bool IsShown(string selector)
    {
        return Driver.FindElements(By.CssSelector(selector)).Any(element => element.Displayed);
    }
}
