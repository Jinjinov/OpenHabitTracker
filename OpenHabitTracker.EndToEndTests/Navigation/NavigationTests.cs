namespace OpenHabitTracker.EndToEndTests.Navigation;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class NavigationTests : BaseTest
{
    [SetUp]
    public async Task SetUp() => await GotoAsync();

    [Test]
    public async Task NotesNavLink_Click_NavigatesToNotesPage()
    {
        await Page.Locator("[data-main-step-3]").ClickAsync();
        await Page.WaitForURLAsync(new Regex("/notes$"));

        Assert.That(Page.Url, Does.EndWith("/notes"));
        await Expect(Page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TasksNavLink_Click_NavigatesToTasksPage()
    {
        await Page.Locator("[data-main-step-4]").ClickAsync();
        await Page.WaitForURLAsync(new Regex("/tasks$"));

        Assert.That(Page.Url, Does.EndWith("/tasks"));
        await Expect(Page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HabitsNavLink_Click_NavigatesToHabitsPage()
    {
        await Page.Locator("[data-main-step-5]").ClickAsync();
        await Page.WaitForURLAsync(new Regex("/habits$"));

        Assert.That(Page.Url, Does.EndWith("/habits"));
        await Expect(Page.Locator("main#main-content")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HomeNavLink_Click_NavigatesToHomePage()
    {
        await Page.Locator("[data-main-step-3]").ClickAsync();
        await Page.WaitForURLAsync(new Regex("/notes$"));

        await Page.Locator("[data-main-step-2]").ClickAsync();
        await Page.WaitForURLAsync(new Regex("/$"));

        string relative = new Uri(Page.Url).AbsolutePath.TrimEnd('/');
        Assert.That(relative, Is.EqualTo(""));
    }

    [Test]
    public async Task BrowserBack_AfterNavigation_ReturnsToPreviousPage()
    {
        await Page.Locator("[data-main-step-3]").ClickAsync();
        await Page.WaitForURLAsync(new Regex("/notes$"));

        await Page.Locator("[data-main-step-4]").ClickAsync();
        await Page.WaitForURLAsync(new Regex("/tasks$"));

        await Page.GoBackAsync();
        await Page.WaitForURLAsync(new Regex("/notes$"));

        Assert.That(Page.Url, Does.EndWith("/notes"));
    }
}
