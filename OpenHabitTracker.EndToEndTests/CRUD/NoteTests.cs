namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class NoteTests : BaseTest
{
    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-3]");
    }

    [Test]
    public async Task AddNote_TypeTitle_NoteAppearsInList()
    {
        await AddItemAsync("My Note");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "My Note" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddNote_EmptyTitle_NoteIsNotAdded()
    {
        int initialCount = await Page.Locator("[data-notes-step-2]").CountAsync();

        await Page.Locator("button.btn-plain.input-group").ClickAsync();

        // Save button is disabled when title is empty
        await Expect(Page.Locator("button:has(i.bi-floppy)")).ToBeDisabledAsync();

        await Page.Locator("button:has(i.bi-x-square)").ClickAsync();

        await Expect(Page.Locator("[data-notes-step-2]")).ToHaveCountAsync(initialCount);
    }

    [Test]
    public async Task DeleteNote_AfterAdd_NoteDisappearsFromList()
    {
        await AddItemAsync("Delete Me");

        // Open note detail (second column at 1920px width)
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Delete Me" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-notes-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Delete Me" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteNote_MovesToTrash()
    {
        await AddItemAsync("Trashed Note");

        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Trashed Note" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-notes-step-6]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open Trash sidebar
        await OpenSidebarAsync("bi-trash");

        await Expect(Page.Locator("span.input-group-text").Filter(new LocatorFilterOptions { HasText = "Trashed Note" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditNote_ChangesTitle_NewTitleVisible()
    {
        await AddItemAsync("Original");

        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Original" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // data-notes-step-5 wraps the title input in NoteComponent
        await Page.Locator("[data-notes-step-5] input").FillAsync("Updated");
        await Page.Locator("[data-notes-step-5] input").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.Locator("[data-notes-step-7]").ClickAsync(); // Close note detail
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Updated" })).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Original" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EditNote_ChangeAllFields_CloseWorksOnFirstClick()
    {
        await AddItemAsync("Note To Edit");

        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Note To Edit" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change title
        await Page.Locator("[data-notes-step-5] input").FillAsync("Note Edited Title");
        await Page.Locator("[data-notes-step-5] input").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Change content
        await Page.Locator("[data-notes-step-8] textarea").FillAsync("Note edited content text");
        await Page.Locator("[data-notes-step-8] textarea").BlurAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click Close once — must close the detail panel immediately (no second click required)
        await Page.Locator("[data-notes-step-7]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-notes-step-7]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Note Edited Title" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditNote_TwiceThenClose_CloseWorksOnFirstClick()
    {
        await AddItemAsync("Note Edit Twice");

        // First edit — fill with enough lines to make the note require vertical scroll
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Note Edit Twice" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("[data-notes-step-8] textarea").FillAsync("line1\nline2\nline3\nline4\nline5\nline6\nline7\nline8\nline9\nline10\nline11\nline12\nline13\nline14\nline15\nline16\nline17\nline18\nline19\nline20\nline21\nline22\nline23\nline24\nline25\nline26\nline27\nline28\nline29\nline30\nline31\nline32\nline33\nline34\nline35\nline36\nline37\nline38\nline39\nline40\nline41\nline42\nline43\nline44\nline45\nline46\nline47\nline48\nline49\nline50\nline51\nline52\nline53\nline54\nline55\nline56\nline57\nline58\nline59\nline60\nline61\nline62\nline63\nline64\nline65\nline66\nline67\nline68\nline69\nline70\nline71\nline72\nline73\nline74\nline75\nline76\nline77\nline78\nline79\nline80\nline81\nline82\nline83\nline84\nline85\nline86\nline87\nline88\nline89\nline90\nline91\nline92\nline93\nline94\nline95\nline96\nline97\nline98\nline99");
        await Page.Locator("[data-notes-step-7]").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Second edit — add one more line (changes row count), scroll to Close, click without blur
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Note Edit Twice" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.Locator("[data-notes-step-8] textarea").FillAsync("line1\nline2\nline3\nline4\nline5\nline6\nline7\nline8\nline9\nline10\nline11\nline12\nline13\nline14\nline15\nline16\nline17\nline18\nline19\nline20\nline21\nline22\nline23\nline24\nline25\nline26\nline27\nline28\nline29\nline30\nline31\nline32\nline33\nline34\nline35\nline36\nline37\nline38\nline39\nline40\nline41\nline42\nline43\nline44\nline45\nline46\nline47\nline48\nline49\nline50\nline51\nline52\nline53\nline54\nline55\nline56\nline57\nline58\nline59\nline60\nline61\nline62\nline63\nline64\nline65\nline66\nline67\nline68\nline69\nline70\nline71\nline72\nline73\nline74\nline75\nline76\nline77\nline78\nline79\nline80\nline81\nline82\nline83\nline84\nline85\nline86\nline87\nline88\nline89\nline90\nline91\nline92\nline93\nline94\nline95\nline96\nline97\nline98\nline99\nline100");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Scroll the correct child-column (the one containing the editor) to the bottom
        await Page.EvaluateAsync("document.querySelector('[data-notes-step-7]').closest('.child-column').scrollTop = document.querySelector('[data-notes-step-7]').closest('.child-column').scrollHeight");

        // Click with raw mouse coordinates — no Playwright auto-scroll, matches real user behavior
        LocatorBoundingBoxResult? box = await Page.Locator("[data-notes-step-7]").BoundingBoxAsync();
        await Page.Mouse.ClickAsync(box!.X + box.Width / 2, box.Y + box.Height / 2, new MouseClickOptions { Delay = 300 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.Locator("[data-notes-step-7]")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task AddNote_PersistedAfterReload()
    {
        await AddItemAsync("Persistent Note");

        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(500);

        await NavigateToAsync("[data-main-step-3]");

        await Expect(Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Persistent Note" })).ToBeVisibleAsync();
    }
}
