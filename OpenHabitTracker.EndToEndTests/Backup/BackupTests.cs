namespace OpenHabitTracker.EndToEndTests.Backup;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

[TestFixture]
public class BackupTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await LoadExamplesViaUiAsync();
    }

    [Test]
    public async Task ExportJson_ThenImport_RestoresAllContentTypeCounts()
    {
        await NavigateToAsync("[data-main-step-3]");
        int originalNoteCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        await NavigateToAsync("[data-main-step-4]");
        int originalTaskCount = await Page.Locator("[data-tasks-step-2]").CountAsync();

        await NavigateToAsync("[data-main-step-5]");
        int originalHabitCount = await Page.Locator("[data-habits-step-2]").CountAsync();

        // Export JSON
        await OpenSidebarAsync("bi-database");
        IDownload download = await Page.RunAndWaitForDownloadAsync(() =>
            Page.Locator("[data-data-step-3]").ClickAsync());
        string exportedFilePath = Path.Combine(Path.GetTempPath(), download.SuggestedFilename);
        await download.SaveAsAsync(exportedFilePath);

        // Delete all data
        await Page.Locator("[data-data-step-2]").ClickAsync();
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-3]");
        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(0);

        // Import the exported file
        await OpenSidebarAsync("bi-database");
        await Page.Locator("input[type='file'].d-none").SetInputFilesAsync(exportedFilePath);
        await Page.WaitForTimeoutAsync(2000); // allow all async DB writes to complete
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-3]");
        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(originalNoteCount);

        await NavigateToAsync("[data-main-step-4]");
        await Expect(Page.Locator("[data-tasks-step-2]")).ToHaveCountAsync(originalTaskCount);

        await NavigateToAsync("[data-main-step-5]");
        await Expect(Page.Locator("[data-habits-step-2]")).ToHaveCountAsync(originalHabitCount);
    }

    [Test]
    public async Task ExportJson_ThenDeleteAll_ThenImport_RestoresNoteCount()
    {
        await NavigateToAsync("[data-main-step-3]");
        int originalCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        // Export JSON and capture the downloaded file
        await OpenSidebarAsync("bi-database");
        IDownload download = await Page.RunAndWaitForDownloadAsync(() =>
            Page.Locator("[data-data-step-3]").ClickAsync());
        string suggestedName = download.SuggestedFilename;
        string exportedFilePath = Path.Combine(Path.GetTempPath(), suggestedName);
        await download.SaveAsAsync(exportedFilePath);

        // Delete all data
        await Page.Locator("[data-data-step-2]").ClickAsync();
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-3]");
        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(0);

        // Import the exported file
        await OpenSidebarAsync("bi-database");
        await Page.Locator("input[type='file'].d-none").SetInputFilesAsync(exportedFilePath);
        await Page.WaitForTimeoutAsync(2000); // allow all async DB writes to complete
        await CloseSidebarAsync();

        await NavigateToAsync("[data-main-step-3]");
        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(originalCount);
    }
}
