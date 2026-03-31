using Microsoft.Playwright;

namespace OpenHabitTracker.EndToEndTests.Smoke;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class StartupTests : BaseTest
{
    private List<string> _jsErrors = [];

    [SetUp]
    public void AttachJsErrorListener()
    {
        Page.PageError += (_, error) => _jsErrors.Add(error);
    }

    private void AssertNoJsErrors()
    {
        Assert.That(_jsErrors, Is.Empty,
            $"JavaScript errors detected: {string.Join("; ", _jsErrors)}");
    }

    [Test]
    public async Task HomePage_Loads_WithoutJsErrors()
    {
        await GotoAsync();

        AssertNoJsErrors();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();
    }

    [Test]
    public async Task NotesPage_Loads_WithoutJsErrors()
    {
        await GotoAsync("notes");

        AssertNoJsErrors();
        await Expect(Page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TasksPage_Loads_WithoutJsErrors()
    {
        await GotoAsync("tasks");

        AssertNoJsErrors();
        await Expect(Page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HabitsPage_Loads_WithoutJsErrors()
    {
        await GotoAsync("habits");

        AssertNoJsErrors();
        await Expect(Page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HomePage_AfterReload_StillLoads_WithoutJsErrors()
    {
        await GotoAsync();
        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(500);

        AssertNoJsErrors();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();
    }
}
