using OpenQA.Selenium.Support.UI;

namespace OpenHabitTracker.DeviceTests;

// What every fixture does the same way whatever host it drives: the waits, and the page in the web context,
// reached with the selectors the e2e uses.
public abstract class DeviceTest<TDriver> where TDriver : AppiumDriver
{
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    protected TDriver Driver = null!;

    // Polls the condition and returns whether it became true in time, so the caller can assert with the values it measured.
    // Needs no session, so it also waits on what happens outside one, like a file the app writes.
    protected static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        DefaultWait<bool> wait = new(true) { Timeout = timeout, PollingInterval = TimeSpan.FromMilliseconds(500) };
        wait.IgnoreExceptionTypes(typeof(NotFoundException));

        try
        {
            return wait.Until(_ => condition());
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    // Native context only.
    protected IWebElement WaitForNativeElement(By locator, TimeSpan? timeout = null)
    {
        WebDriverWait wait = new(Driver, timeout ?? DefaultTimeout);
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        return wait.Until(driver => driver.FindElement(locator));
    }

    // Web context only: waits until the selector matches a displayed element.
    protected IWebElement WaitForElement(string cssSelector, TimeSpan? timeout = null)
    {
        WebDriverWait wait = new(Driver, timeout ?? DefaultTimeout);
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        return wait.Until(driver =>
        {
            IWebElement element = driver.FindElement(By.CssSelector(cssSelector));
            return element.Displayed ? element : null;
        })!;
    }

    // Web context only.
    protected object? RunScript(string script, params object[] args)
    {
        return Driver.ExecuteScript(script, args);
    }

    // Main nav steps, as the e2e uses them.
    protected const string HomePage = "[data-main-step-2]";
    protected const string NotesPage = "[data-main-step-3]";
    protected const string TasksPage = "[data-main-step-4]";
    protected const string HabitsPage = "[data-main-step-5]";

    // Title buttons of the list rows, per page.
    protected const string NoteRows = "[data-notes-step-2]";
    protected const string TaskRows = "[data-tasks-step-2]";
    protected const string HabitRows = "[data-habits-step-2]";

    // The elapsed time a habit row shows before its first completion.
    protected const string NeverDone = "⊘";

    // Web context only: a page through its main nav step.
    protected void OpenPage(string navStep)
    {
        WaitForElement(navStep).Click();
        WaitForElement("main#main-content");
    }

    // Web context only.
    protected void OpenNotesPage()
    {
        OpenPage(NotesPage);
    }

    // Web context only: sets a field's value and raises the input event that typing raises, without typing.
    // Chromedriver's typing reaches Android as hardware key events, after which Gboard opens as its small
    // hardware-keyboard toolbar instead of the soft keyboard, for every later field.
    protected void SetValue(IWebElement field, string value)
    {
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true }));", field, value);
    }

    // Web context only: sets a field's value and raises the change event, for the fields that save on change.
    protected void SetChangedValue(IWebElement field, string value)
    {
        RunScript("arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));", field, value);
    }

    // Web context only: adds a note, task or habit through the current page's add form, the same way the e2e adds items.
    // The form closes only after the save has finished.
    protected void AddItem(string title)
    {
        WaitForElement("button.btn-plain.input-group").Click();
        SetValue(WaitForElement("input[aria-required='true']"), title);

        IWebElement save = WaitForElement("button:has(i.bi-floppy)");
        new WebDriverWait(Driver, DefaultTimeout).Until(driver => save.Enabled);
        save.Click();

        new WebDriverWait(Driver, DefaultTimeout).Until(driver => driver.FindElements(By.CssSelector("button:has(i.bi-floppy)")).Count == 0);
    }

    // Web context only: clicks today's cell in the habit row's small calendar, which records a completion.
    // The cell shows the count before the save finishes; the elapsed time changes only after it, so that is what the wait is on.
    protected void CompleteToday(string habitTitle)
    {
        Assert.That(Row(HabitRows, habitTitle), Is.Not.Null, $"The habit {habitTitle} is not in the list");

        WaitForElement("[data-habits-step-6] button[role='gridcell'].border-primary-subtle").Click();

        Assert.That(WaitUntil(() => !WaitForElement("[data-habits-step-3]").Text.Contains(NeverDone), DefaultTimeout), Is.True, "The completion was not saved");
    }

    // Web context only: fails if the page shows an error. The error boundaries and the database error render role="alert",
    // and an exception outside them shows Blazor's own #blazor-error-ui bar.
    protected void AssertNoErrorShown()
    {
        IWebElement? alert = Driver.FindElements(By.CssSelector("[role='alert']")).FirstOrDefault(element => element.Displayed);
        Assert.That(alert?.Text, Is.Null, "The page shows an error");

        bool errorBar = Driver.FindElements(By.CssSelector("#blazor-error-ui")).Any(element => element.Displayed);
        Assert.That(errorBar, Is.False, "The page shows Blazor's unhandled error bar");
    }

    // Web context only: a sidebar from the menu, by the icon on its menu entry, as the e2e opens them.
    protected void OpenSidebar(string iconClass)
    {
        WaitForElement("[data-main-step-1]").Click();
        WaitForElement($"button:has(i.{iconClass})").Click();
        WaitForElement("#closeSidebar");
    }

    // Web context only: the list row title whose text contains the title, or null once the wait runs out.
    protected IWebElement? Row(string rows, string title, TimeSpan? timeout = null)
    {
        IWebElement? row = null;

        WaitUntil(() =>
        {
            try
            {
                row = Driver.FindElements(By.CssSelector(rows)).FirstOrDefault(element => element.Text.Contains(title));
            }
            catch (StaleElementReferenceException)
            {
                row = null;
            }

            return row is not null;
        }, timeout ?? DefaultTimeout);

        return row;
    }

    // Web context only: waits for the import summary, confirms it, and returns the format it named.
    // The summary names the format it recognized, in text that is not localized.
    protected string ConfirmImport()
    {
        WaitUntil(() => Driver.FindElements(By.CssSelector("[data-import-summary], [role='alert'], #blazor-error-ui")).Any(element => element.Displayed), DefaultTimeout);
        AssertNoErrorShown();

        string format = WaitForElement("[data-import-summary] small").Text;

        WaitForElement("[data-import-summary] .btn-primary").Click();
        Assert.That(WaitUntil(() => Driver.FindElements(By.CssSelector("[data-import-summary]")).Count == 0, DefaultTimeout), Is.True, "The import did not finish");
        AssertNoErrorShown();

        return format;
    }
}
