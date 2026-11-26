using System.Text.RegularExpressions;

namespace OpenHabitTracker.EndToEndTests.CRUD;

// Prerequisite: publish OpenHabitTracker.Blazor.Wasm to IIS at http://localhost before running tests.
// See: OpenHabitTracker.EndToEndTests/TODO.md for Playwright quirks and locator guidelines.

// The days a habit is meant for. The week starts on Monday by default, so the first weekday button is Monday.
[TestFixture]
public class HabitScheduleTests : BaseTest
{
    private const string Title = "Schedule Habit";

    [SetUp]
    public async Task SetUp()
    {
        await GotoAsync();
        await Page.Locator("[data-main-step-5]").ClickAsync();
        await Expect(Page.Locator("button.btn-plain.input-group")).ToBeVisibleAsync();
    }

    private ILocator TitleButton() => Page.Locator("[data-habits-step-2]").Filter(new LocatorFilterOptions { HasText = Title });

    private ILocator Days() => Page.Locator("#habit-component [data-habits-step-16]");

    private ILocator Weekdays() => Days().Locator("[role='group']").Last.Locator("button");

    private ILocator MonthDays() => Days().Locator("[role='toolbar'] button");

    private ILocator Count() => Page.Locator("#habit-component [data-habits-step-15] input[type='number']").First;

    private ILocator Badge() => Page.Locator("div.input-group.flex-nowrap").Filter(new LocatorFilterOptions { Has = TitleButton() }).Locator("[data-habits-step-3]");

    private async Task OpenHabitAsync()
    {
        await TitleButton().ClickAsync();
        await Expect(Page.Locator("#habit-component")).ToBeVisibleAsync();
    }

    private async Task ReloadAndOpenAsync()
    {
        await Page.ReloadAsync();
        await Expect(Page.Locator("nav[aria-label]")).ToBeVisibleAsync();
        await NavigateToAsync("[data-main-step-5]");
        await OpenHabitAsync();
    }

    // A predicate over the stored habit; the JS interop serializer camelCases the names, with a PascalCase fallback.
    private Task WaitForStoredHabitAsync(string condition) =>
        WaitForIndexedDbAsync("HabitEntity", $$"""
            habits => habits.some(h => {
                const weekdays = h.repeatWeekdays ?? h.RepeatWeekdays ?? 0;
                const monthDays = h.repeatMonthDays ?? h.RepeatMonthDays ?? 0;
                const monthWeeks = h.repeatMonthWeeks ?? h.RepeatMonthWeeks ?? 0;
                const months = h.repeatMonths ?? h.RepeatMonths ?? 0;
                const count = h.repeatCount ?? h.RepeatCount;
                return {{condition}};
            })
            """);

    private static async Task<string[]> PressedAsync(ILocator buttons) =>
        [.. (await buttons.AllAsync()).Select(x => x.GetAttributeAsync("aria-pressed").Result!)];

    [Test]
    public async Task NewHabit_ThreeWeekdays_AreThereAfterAReloadWithTheirCountAndBoldDays()
    {
        await Page.Locator("button.btn-plain.input-group").ClickAsync();
        await Page.Locator("input[aria-required='true']").FillAsync(Title);
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");

        ILocator formWeekdays = Page.Locator("[role='group'] button[aria-pressed]");
        await formWeekdays.Nth(0).ClickAsync();
        await formWeekdays.Nth(2).ClickAsync();
        await formWeekdays.Nth(4).ClickAsync();

        await Expect(Page.Locator("#countInput")).ToHaveValueAsync("3");
        await Expect(Page.Locator("#countInput")).ToHaveAttributeAsync("readonly", "");

        await Page.Locator("button:has(i.bi-floppy)").ClickAsync();
        await WaitForStoredHabitAsync("weekdays == 21 && count == 3");

        await ReloadAndOpenAsync();

        Assert.That(await PressedAsync(Weekdays()), Is.EqualTo(new[] { "true", "false", "true", "false", "true", "false", "false" }));
        await Expect(Count()).ToHaveValueAsync("3");

        ILocator bold = Page.Locator("[data-habits-step-6] button[role='gridcell'].fw-bold");
        await Expect(bold.First).ToBeVisibleAsync();

        foreach (ILocator cell in await bold.AllAsync())
            Assert.That(await cell.GetAttributeAsync("aria-label"), Does.Match("^(Monday|Wednesday|Friday),"));
    }

    [TestCase("month days")]
    [TestCase("month weekdays")]
    [TestCase("year")]
    public async Task DaysOfEachPeriod_AreThereAfterAReload(string rule)
    {
        await AddItemAsync(Title);
        await OpenHabitAsync();

        switch (rule)
        {
            case "month days":
                await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Month");
                await MonthDays().Nth(0).ClickAsync();
                await MonthDays().Nth(14).ClickAsync();
                await WaitForStoredHabitAsync("monthDays == ((1 << 0) | (1 << 14)) && count == 2");
                break;
            case "month weekdays":
                await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Month");
                await Days().Locator("select").SelectOptionAsync("weekdays");
                await Days().Locator("[role='group']").First.Locator("button").Nth(1).ClickAsync();
                await Weekdays().Nth(0).ClickAsync();
                await WaitForStoredHabitAsync("monthWeeks == 2 && weekdays == 1 && count == 1");
                break;
            case "year":
                await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Year");
                await Days().Locator("[role='group']").First.Locator("button").Nth(2).ClickAsync();
                await MonthDays().Nth(13).ClickAsync();
                await WaitForStoredHabitAsync("months == 4 && monthDays == (1 << 13) && count == 1");
                break;
        }

        await ReloadAndOpenAsync();

        switch (rule)
        {
            case "month days":
                await Expect(Days().Locator("select")).ToHaveValueAsync("days");
                await Expect(MonthDays().Nth(0)).ToHaveAttributeAsync("aria-pressed", "true");
                await Expect(MonthDays().Nth(14)).ToHaveAttributeAsync("aria-pressed", "true");
                await Expect(Count()).ToHaveValueAsync("2");
                break;
            case "month weekdays":
                await Expect(Days().Locator("select")).ToHaveValueAsync("weekdays");
                await Expect(Days().Locator("[role='group']").First.Locator("button").Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
                await Expect(Weekdays().Nth(0)).ToHaveAttributeAsync("aria-pressed", "true");
                break;
            case "year":
                await Expect(Days().Locator("[role='group']").First.Locator("button").Nth(2)).ToHaveAttributeAsync("aria-pressed", "true");
                await Expect(MonthDays().Nth(13)).ToHaveAttributeAsync("aria-pressed", "true");
                break;
        }
    }

    // A habit saved by a version before the repeat days has none of the four properties in its record.
    [Test]
    public async Task RecordWithoutTheDays_OpensWithoutDaysAndCanBeGivenOne()
    {
        await AddItemAsync(Title);
        await WaitForIndexedDbAsync("HabitEntity", "habits => habits.length == 1");

        await Page.EvaluateAsync("""
            async () => {
                const db = await new Promise((resolve, reject) => {
                    const request = indexedDB.open('OpenHabitTracker');
                    request.onsuccess = () => resolve(request.result);
                    request.onerror = () => reject(request.error);
                });
                const store = db.transaction('HabitEntity', 'readwrite').objectStore('HabitEntity');
                const habits = await new Promise(resolve => { const request = store.getAll(); request.onsuccess = () => resolve(request.result); });
                for (const habit of habits) {
                    for (const name of ['repeatWeekdays', 'repeatMonthDays', 'repeatMonthWeeks', 'repeatMonths', 'RepeatWeekdays', 'RepeatMonthDays', 'RepeatMonthWeeks', 'RepeatMonths'])
                        delete habit[name];
                    habit.repeatPeriod = 1;
                    store.put(habit);
                }
                await new Promise(resolve => store.transaction.oncomplete = resolve);
                db.close();
            }
            """);

        await ReloadAndOpenAsync();

        Assert.That(await PressedAsync(Weekdays()), Is.All.EqualTo("false"));

        await Weekdays().Nth(6).ClickAsync();
        await WaitForStoredHabitAsync("weekdays == 64 && count == 1");
    }

    // Done on Monday at 08:00, meant for Monday, Wednesday and Friday: due by the end of Wednesday.
    [Test]
    public async Task Badge_MeasuresAgainstTheEndOfTheNextScheduledDay()
    {
        DateTime monday = new(2026, 11, 23, 8, 0, 0);

        await Page.Clock.InstallAsync(new ClockInstallOptions { TimeDate = monday });
        await GotoAsync();
        await NavigateToAsync("[data-main-step-5]");

        await AddItemAsync(Title);
        await OpenHabitAsync();
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");
        await Weekdays().Nth(0).ClickAsync();
        await Weekdays().Nth(2).ClickAsync();
        await Weekdays().Nth(4).ClickAsync();

        await Page.Locator("[data-habits-step-6] button[role='gridcell'].border-primary-subtle").ClickAsync();
        await Expect(Badge()).ToContainTextAsync("(0%)");

        List<(string Text, string Color)> readings = [];

        foreach (string at in new[] { "2026-11-24T12:00:00", "2026-11-25T12:00:00", "2026-11-25T23:00:00", "2026-11-26T01:00:00", "2026-11-26T12:00:00" })
        {
            await Page.Clock.SetFixedTimeAsync(DateTime.Parse(at));

            // opening and closing the habit repaints the list
            await Page.Locator("[data-habits-step-11]").ClickAsync();
            await OpenHabitAsync();

            string text = (await Badge().InnerTextAsync()).Trim();
            string color = (await Badge().GetAttributeAsync("class"))!;

            readings.Add((Regex.Match(text, @"\(\d+%\)").Value, Regex.Match(color, @"bg-(success|warning|danger)-subtle").Value));
        }

        Assert.That(readings, Is.EqualTo(new[]
        {
            ("(44%)", "bg-success-subtle"),
            ("(81%)", "bg-warning-subtle"),
            ("(98%)", "bg-warning-subtle"),
            ("(102%)", "bg-danger-subtle"),
            ("(119%)", "bg-danger-subtle")
        }));
    }

    [Test]
    public async Task MonthDays_ByKeyboardAlone()
    {
        await AddItemAsync(Title);
        await OpenHabitAsync();
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Month");

        await Days().Locator("select").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(MonthDays().Nth(0)).ToBeFocusedAsync();

        string scrollBefore = await Page.EvaluateAsync<string>("() => [...document.querySelectorAll('.child-column')].map(x => x.scrollTop).join(',') + ',' + window.scrollY");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(MonthDays().Nth(9)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(MonthDays().Nth(30)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(MonthDays().Nth(23)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(MonthDays().Nth(0)).ToBeFocusedAsync();

        string scrollAfter = await Page.EvaluateAsync<string>("() => [...document.querySelectorAll('.child-column')].map(x => x.scrollTop).join(',') + ',' + window.scrollY");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("Space");
        await Expect(MonthDays().Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
        await WaitForStoredHabitAsync("monthDays == 2");

        await Page.Keyboard.PressAsync("Tab");

        bool focusInGrid = await Page.EvaluateAsync<bool>("() => document.activeElement.closest(\"[role='toolbar']\") !== null");

        Assert.Multiple(() =>
        {
            Assert.That(scrollAfter, Is.EqualTo(scrollBefore));
            Assert.That(focusInGrid, Is.False);
        });
    }

    [Test]
    public async Task TwoTogglesFasterThanTheSave_AreBothStored()
    {
        await AddItemAsync(Title);
        await OpenHabitAsync();
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");

        // the second click does not wait for the first save
        await Weekdays().Nth(0).ClickAsync();
        await Weekdays().Nth(1).ClickAsync();

        await WaitForStoredHabitAsync("weekdays == 3 && count == 2");

        await ReloadAndOpenAsync();

        Assert.That(await PressedAsync(Weekdays()), Is.EqualTo(new[] { "true", "true", "false", "false", "false", "false", "false" }));
    }

    [TestCase("[data-data-step-3]")]
    [TestCase("[data-data-step-5]")]
    public async Task Backup_ExportedAndImportedAfterDeleteAllData_KeepsTheDays(string exportButton)
    {
        await AddItemAsync(Title);
        await OpenHabitAsync();
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");
        await Weekdays().Nth(1).ClickAsync();
        await Weekdays().Nth(3).ClickAsync();
        await WaitForStoredHabitAsync("weekdays == 10");
        await Page.Locator("[data-habits-step-11]").ClickAsync();

        await OpenSidebarAsync("bi-database");
        IDownload download = await Page.RunAndWaitForDownloadAsync(() => Page.Locator(exportButton).ClickAsync());
        string exportedFilePath = Path.Combine(Path.GetTempPath(), $"schedule-{Guid.NewGuid():N}-{download.SuggestedFilename}");
        await download.SaveAsAsync(exportedFilePath);

        await Page.Locator("[data-data-step-2]").ClickAsync();
        await CloseSidebarAsync();
        await Expect(TitleButton()).ToHaveCountAsync(0);

        await OpenSidebarAsync("bi-database");
        await Page.Locator("input[type='file'].d-none").SetInputFilesAsync(exportedFilePath);
        await Page.Locator("[data-import-summary] button.btn-primary").ClickAsync();
        await WaitForStoredHabitAsync("weekdays == 10 && count == 2");
        await CloseSidebarAsync();

        await OpenHabitAsync();

        Assert.That(await PressedAsync(Weekdays()), Is.EqualTo(new[] { "false", "true", "false", "true", "false", "false", "false" }));

        File.Delete(exportedFilePath);
    }

    // What does not fit in the days row: a button or a label whose text runs past it, or anything past the window's right edge.
    private Task<string[]> OverflowsAsync(string root) => Page.EvaluateAsync<string[]>($$"""
        () => {
            const root = document.querySelector("{{root}}");
            const failures = [];
            for (const element of root.querySelectorAll('button, .input-group-text, select')) {
                const rect = element.getBoundingClientRect();
                if (element.scrollWidth > element.clientWidth + 1)
                    failures.push(`text past its box: ${element.textContent.trim() || element.value}`);
                if (rect.right > window.innerWidth + 0.5)
                    failures.push(`past the window: ${element.textContent.trim() || element.value}`);
            }
            return failures;
        }
        """);

    // Every language at the narrowest width the app is laid out for, in the detail and in the new habit form.
    [Test]
    public async Task EveryLanguage_TheDaysFitAtPhoneWidth()
    {
        await Page.SetViewportSizeAsync(384, 800);

        await AddItemAsync(Title);

        await OpenSidebarAsync("bi-gear");
        string[] languages = await Page.Locator("[data-settings-step-18] select option").EvaluateAllAsync<string[]>("options => options.map(x => x.value)");
        await CloseSidebarAsync();

        List<string> failures = [];

        foreach (string language in languages)
        {
            await OpenSidebarAsync("bi-gear");
            await Page.Locator("[data-settings-step-18] select").SelectOptionAsync(language);
            await CloseSidebarAsync();

            await OpenHabitAsync();

            foreach ((string period, string? form) in new (string, string?)[] { ("Week", null), ("Month", "days"), ("Month", "weekdays"), ("Year", "days") })
            {
                await Page.Locator("#habit-component [data-habits-step-15] select").SelectOptionAsync(period);

                if (form is not null)
                    await Days().Locator("select").SelectOptionAsync(form);

                failures.AddRange((await OverflowsAsync("#habit-component [data-habits-step-16]")).Select(x => $"{language} detail {period} {form}: {x}"));
            }

            await Page.Locator("[data-habits-step-11]").ClickAsync();

            await Page.Locator("button.btn-plain.input-group").ClickAsync();

            foreach ((string period, string? form) in new (string, string?)[] { ("Week", null), ("Month", "days"), ("Month", "weekdays"), ("Year", "days") })
            {
                await Page.Locator("div.input-group:has(#countInput) select").SelectOptionAsync(period);

                if (form is not null)
                    await Page.Locator("div.input-group:has(#countInput) + div.input-group select").SelectOptionAsync(form);

                // the days follow the repeat row: one row for a week, and the row with a block under it for a month or a year
                string[] overflows = await Page.EvaluateAsync<string[]>("""
                    () => {
                        const label = document.querySelector('div.input-group:has(#countInput) + div.input-group');
                        const parts = [label, label.nextElementSibling].filter(x => x && (x === label || x.classList.contains('mx-auto')));
                        const failures = [];
                        for (const element of parts.flatMap(x => [...x.querySelectorAll('button, .input-group-text, select')])) {
                            const rect = element.getBoundingClientRect();
                            if (element.scrollWidth > element.clientWidth + 1)
                                failures.push(`text past its box: ${element.textContent.trim() || element.value}`);
                            if (rect.right > window.innerWidth + 0.5)
                                failures.push(`past the window: ${element.textContent.trim() || element.value}`);
                        }
                        return failures;
                    }
                    """);

                failures.AddRange(overflows.Select(x => $"{language} form {period} {form}: {x}"));
            }

            await Page.Locator("button:has(i.bi-x-square)").ClickAsync();
        }

        Assert.Multiple(() =>
        {
            Assert.That(languages, Has.Length.EqualTo(20));
            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        });
    }

    [Test]
    public async Task Themes_APressedAndAnUnpressedButtonLookDifferent()
    {
        await AddItemAsync(Title);
        await OpenHabitAsync();
        await Page.Locator("select[aria-label='Repeat period']").SelectOptionAsync("Week");
        await Weekdays().Nth(0).ClickAsync();
        await Page.Locator("[data-habits-step-11]").ClickAsync();

        await OpenSidebarAsync("bi-gear");
        string[] themes = await Page.Locator("[data-settings-step-2] select option").EvaluateAllAsync<string[]>("options => options.map(x => x.value)");
        await CloseSidebarAsync();

        // Bootstrap buttons fade between colors, so a style read right after the theme changes is part of the way there.
        await Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "* { transition: none !important; }" });

        List<string> same = [];

        foreach (string theme in themes)
        {
            await OpenSidebarAsync("bi-gear");
            await Page.Locator("[data-settings-step-2] select").SelectOptionAsync(theme);
            await CloseSidebarAsync();
            await OpenHabitAsync();

            // the stylesheet of the theme loads after the select changes, and the buttons are drawn by the browser's
            // own style until it applies, whose outset border no theme uses
            await Page.WaitForFunctionAsync($$"""
                () => {
                    const sheet = document.getElementById('theme-link').sheet;
                    const buttons = [...document.querySelectorAll("#habit-component [data-habits-step-16] button")];
                    return sheet !== null && (sheet.href ?? '').includes('/{{theme}}/') && buttons.length > 0 && buttons.every(x => getComputedStyle(x).borderTopStyle !== 'outset');
                }
                """);

            // a theme can show the pressed state by its background, its text or its shadow
            const string look = "x => { const s = getComputedStyle(x); return [s.backgroundColor, s.color, s.boxShadow].join('|'); }";
            string pressed = await Weekdays().Nth(0).EvaluateAsync<string>(look);
            string unpressed = await Weekdays().Nth(1).EvaluateAsync<string>(look);

            if (pressed == unpressed)
                same.Add($"{theme}: {pressed}");

            await Page.Locator("[data-habits-step-11]").ClickAsync();
        }

        Assert.Multiple(() =>
        {
            Assert.That(themes, Has.Length.EqualTo(26));
            Assert.That(same, Is.Empty);
        });
    }
}
