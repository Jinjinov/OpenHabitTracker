namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: start OpenHabitTracker.Blazor.Web at http://localhost before running tests.

[TestFixture]
public class TextareaTabTests : BaseTest
{
    private const string TextareaSelector = "[data-notes-step-8] textarea";

    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await NavigateToAsync("[data-main-step-3]");
        await AddItemAsync("Tab Test Note");
        await Page.Locator("[data-notes-step-2]").Filter(new LocatorFilterOptions { HasText = "Tab Test Note" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task SetTextareaValueAsync(string value)
    {
        await Page.Locator(TextareaSelector).ClickAsync();
        await Page.Keyboard.PressAsync("Control+A");
        await Page.Keyboard.PressAsync("Delete");
        await Page.Locator(TextareaSelector).FillAsync(value);
    }

    private async Task SetCursorPositionAsync(int position)
    {
        await Page.Locator(TextareaSelector).EvaluateAsync($"el => el.setSelectionRange({position}, {position})");
    }

    private async Task SetSelectionRangeAsync(int start, int end)
    {
        await Page.Locator(TextareaSelector).EvaluateAsync($"el => el.setSelectionRange({start}, {end})");
    }

    private async Task<string> GetTextareaValueAsync()
    {
        return await Page.Locator(TextareaSelector).InputValueAsync();
    }

    [Test]
    public async Task Tab_NoSelection_InsertsTabAtCursor()
    {
        await SetTextareaValueAsync("hello world");
        await SetCursorPositionAsync(5);

        await Page.Keyboard.PressAsync("Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("hello\t world"));
    }

    [Test]
    public async Task Tab_SingleLineSelection_ReplacesSelectionWithTab()
    {
        await SetTextareaValueAsync("hello world");
        await SetSelectionRangeAsync(6, 11); // selects "world"

        await Page.Keyboard.PressAsync("Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("hello \t"));
    }

    [Test]
    public async Task Tab_MultiLineSelection_IndentsEachLine()
    {
        await SetTextareaValueAsync("line one\nline two\nline three");
        await SetSelectionRangeAsync(0, 27); // select all

        await Page.Keyboard.PressAsync("Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("\tline one\n\tline two\n\tline three"));
    }

    [Test]
    public async Task Tab_MultiLineSelection_PartialFirstLine_IndentsAllAffectedLines()
    {
        await SetTextareaValueAsync("line one\nline two\nline three");
        await SetSelectionRangeAsync(5, 17); // "one\nline two" — mid first line to mid second line

        await Page.Keyboard.PressAsync("Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("\tline one\n\tline two\nline three"));
    }

    [Test]
    public async Task ShiftTab_SingleLineWithTab_RemovesLeadingTab()
    {
        await SetTextareaValueAsync("\thello");
        await SetCursorPositionAsync(3);

        await Page.Keyboard.PressAsync("Shift+Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task ShiftTab_SingleLineWithoutTab_NoChange()
    {
        await SetTextareaValueAsync("hello");
        await SetCursorPositionAsync(3);

        await Page.Keyboard.PressAsync("Shift+Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("hello"));
    }

    [Test]
    public async Task ShiftTab_MultiLineSelection_RemovesLeadingTabFromEachLine()
    {
        await SetTextareaValueAsync("\tline one\n\tline two\n\tline three");
        await SetSelectionRangeAsync(0, 31); // select all

        await Page.Keyboard.PressAsync("Shift+Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("line one\nline two\nline three"));
    }

    [Test]
    public async Task ShiftTab_MultiLineSelection_MixedIndentation_RemovesOnlyFromIndentedLines()
    {
        await SetTextareaValueAsync("\tline one\nline two\n\tline three");
        await SetSelectionRangeAsync(0, 30); // select all

        await Page.Keyboard.PressAsync("Shift+Tab");

        string value = await GetTextareaValueAsync();
        Assert.That(value, Is.EqualTo("line one\nline two\nline three"));
    }
}
