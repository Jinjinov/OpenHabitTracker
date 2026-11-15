using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

// The ratios read the clock, so every habit here is hours or days from its threshold.
[TestFixture]
[SetCulture("en-US")]
public class HabitsPageTests
{
    private AppTestContext _app = null!;

    [SetUp]
    public void SetUp()
    {
        _app = new AppTestContext();

        // The list waits for the column width, and a narrow one keeps the calendar under each row.
        _app.JsInterop.GetElementDimensions(Arg.Any<ElementReference>()).Returns(Task.FromResult(new Dimensions { Width = 300, Height = 800 }));
    }

    [TearDown]
    public void TearDown()
    {
        _app.Dispose();
    }

    private static HabitModel Daily(string title, double daysSinceDone) =>
        new() { Title = title, LastTimeDoneAt = DateTime.Now.AddDays(-daysSinceDone), CreatedAt = DateTime.Now.AddDays(-30), TimesDone = [] };

    private async Task<ClientState> Seed(Action<SettingsModel>? configure = null, params HabitModel[] habits)
    {
        ClientState clientState = await _app.Start(configure);

        await clientState.AddUserData([new CategoryModel { Title = "Health", Habits = [.. habits] }]);

        return clientState;
    }

    private long IdOf(string title) => _app.ClientState.Habits!.Values.Single(x => x.Title == title).Id;

    private static List<string> Titles(IRenderedComponent<Habits> cut) =>
        cut.FindAll("button[id^='habit-item-']").Select(x => x.TextContent.Trim()).ToList();

    private IElement Badge(IRenderedComponent<Habits> cut, string title) =>
        cut.Find($"#habit-item-{IdOf(title)}").ParentElement!.QuerySelector("[data-habits-step-3]")!;

    private static string Text(IElement element) => string.Join(" ", element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [Test]
    public async Task List_SortsByTheRatioAndColorsTheBadgeByHowOverdue()
    {
        await Seed(null, Daily("Overdue", 2), Daily("Fresh", 0.5), Daily("Nearly", 0.85));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        Assert.Multiple(() =>
        {
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Overdue", "Nearly", "Fresh" }));
            Assert.That(Text(Badge(cut, "Overdue")), Is.EqualTo("2 d (200%)"));
            Assert.That(Badge(cut, "Overdue").ClassList, Does.Contain("bg-danger-subtle"));
            Assert.That(Text(Badge(cut, "Nearly")), Is.EqualTo("20 h (85%)"));
            Assert.That(Badge(cut, "Nearly").ClassList, Does.Contain("bg-warning-subtle"));
            Assert.That(Text(Badge(cut, "Fresh")), Is.EqualTo("12 h (50%)"));
            Assert.That(Badge(cut, "Fresh").ClassList, Does.Contain("bg-success-subtle"));
        });
    }

    [Test]
    public async Task List_ANeverDoneHabitShowsNoElapsedTime()
    {
        await Seed(null, new HabitModel { Title = "New", CreatedAt = DateTime.Now.AddHours(-6), TimesDone = [] });

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        Assert.That(Text(Badge(cut, "New")), Is.EqualTo("⊘ (25%)"));
    }

    [Test]
    public async Task SmallCalendarOn_ShowsTheCalendarInsteadOfTheButton()
    {
        await Seed(null, Daily("Run", 2));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("[data-habits-step-6]"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("[data-habits-step-4]"), Is.Empty);
        });
    }

    [Test]
    public async Task MarkAsDone_WithTheSmallCalendarOff_StoresACompletionAndResetsTheBadge()
    {
        await Seed(settings => settings.ShowSmallCalendar = false, Daily("Run", 2));
        long runId = IdOf("Run");

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        await cut.Find("[data-habits-step-4]").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetTimes(runId)).Count, Is.EqualTo(1));
            Assert.That(Text(Badge(cut, "Run")), Is.EqualTo("0 m (0%)"));
            Assert.That(Badge(cut, "Run").ClassList, Does.Contain("bg-success-subtle"));
        });
    }

    [Test]
    public async Task MarkAsDone_QuantityHabit_AsksForTheQuantityAndStoresIt()
    {
        HabitModel water = Daily("Water", 2);
        water.DisplayMetric = DisplayMetric.Quantity;
        water.TargetQuantity = 2;
        await Seed(settings => settings.ShowSmallCalendar = false, water);
        long waterId = IdOf("Water");

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        await cut.Find("[data-habits-step-4]").ClickAsync(new MouseEventArgs());

        Assert.That(cut.Find("#quantity-modal-input").GetAttribute("value"), Is.EqualTo("2"));

        cut.Find("#quantity-modal-input").Change("1.5");
        await cut.Find("#quantity-modal-ok").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((await _app.Store.GetTimes(waterId)).Single().Quantity, Is.EqualTo(1.5));
            Assert.That(cut.FindAll("#quantity-modal"), Is.Empty);
        });
    }

    [Test]
    public async Task MarkAsDone_QuantityHabitCancelled_StoresNothing()
    {
        HabitModel water = Daily("Water", 2);
        water.DisplayMetric = DisplayMetric.Quantity;
        await Seed(settings => settings.ShowSmallCalendar = false, water);
        long waterId = IdOf("Water");

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        await cut.Find("[data-habits-step-4]").ClickAsync(new MouseEventArgs());
        await cut.Find("button[aria-label='Cancel']").ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await _app.Store.GetTimes(waterId), Is.Empty);
            Assert.That(cut.FindAll("#quantity-modal"), Is.Empty);
        });
    }

    [Test]
    public async Task AddNew_RepeatDisplayAndTarget_AreStored()
    {
        await Seed(null);

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        await cut.Find("button.btn-plain.input-group").ClickAsync(new MouseEventArgs());

        Assert.That(cut.FindAll("input[aria-label='Target quantity']"), Is.Empty);

        cut.Find("input[placeholder='Add new habit']").Input("Pushups");
        cut.Find("#countInput").Change("3");
        cut.Find("#intervalInput").Change("2");
        cut.Find("select[aria-label='Repeat period']").Change(nameof(Period.Week));
        cut.Find("select[aria-label='Display']").Change(nameof(DisplayMetric.Quantity));
        cut.Find("input[aria-label='Target quantity']").Change("20");
        await cut.Find("button[aria-label='Save']").ClickAsync(new MouseEventArgs());

        HabitEntity stored = (await _app.Store.GetHabits()).Single();

        Assert.Multiple(() =>
        {
            Assert.That((stored.Title, stored.RepeatCount, stored.RepeatInterval, stored.RepeatPeriod), Is.EqualTo(("Pushups", 3, 2, Period.Week)));
            Assert.That((stored.DisplayMetric, stored.TargetQuantity), Is.EqualTo((DisplayMetric.Quantity, 20.0)));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Pushups" }));
        });
    }

    [Test]
    public async Task GroupedByCategory_TheCompletionRuleColorsTheButtonAndAClickStoresTheOtherRule()
    {
        HabitModel doneThisWeek = Daily("Done", 0);
        doneThisWeek.TimesDone = [new TimeModel { StartedAt = DateTime.Now, CompletedAt = DateTime.Now }];

        await Seed(settings => settings.ShowGroupedByCategory = true, doneThisWeek, Daily("Not done", 30));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        IElement rule() => cut.Find("button.btn-sm");

        Assert.Multiple(() =>
        {
            Assert.That(rule().TextContent, Is.EqualTo("all habits are done"));
            Assert.That(rule().ClassList, Does.Contain("btn-outline-warning"));
        });

        await rule().ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(rule().TextContent, Is.EqualTo("any habit is done"));
            Assert.That(rule().ClassList, Does.Contain("btn-outline-success"));
            Assert.That((await _app.Store.GetCategories()).Single().CompletionRule, Is.EqualTo(CompletionRule.Any));
        });
    }

    [Test]
    public async Task GroupedByCategory_NothingDoneThisWeekIsDanger()
    {
        await Seed(settings => settings.ShowGroupedByCategory = true, Daily("Not done", 30));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        Assert.That(cut.Find("button.btn-sm").ClassList, Does.Contain("btn-outline-danger"));
    }

    // Blazor renders a bool attribute value as an empty attribute when true and drops it when false,
    // so aria-pressed never carries "true" or "false".
    [Test]
    [Explicit("Bug: aria-pressed on the completion rule button is bound to a bool, so it renders empty or not at all")]
    public async Task GroupedByCategory_TheCompletionRuleButtonReportsItsPressedState()
    {
        await Seed(settings => settings.ShowGroupedByCategory = true, Daily("Not done", 30));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        Assert.That(cut.Find("button.btn-sm").GetAttribute("aria-pressed"), Is.EqualTo("true"));

        await cut.Find("button.btn-sm").ClickAsync(new MouseEventArgs());

        Assert.That(cut.Find("button.btn-sm").GetAttribute("aria-pressed"), Is.EqualTo("false"));
    }

    [Test]
    public async Task Id_OfAHabit_OpensItsEditorInPlaceOfItsRow()
    {
        await Seed(null, Daily("Run", 2), Daily("Read", 1));
        long runId = IdOf("Run");

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.Id, runId));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#habit-component"), Has.Count.EqualTo(1));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Read" }));
            Assert.That(_app.Get<IHabitService>().SelectedHabit!.Id, Is.EqualTo(runId));
        });
    }

    [Test]
    public async Task Title_Click_NavigatesToTheHabit()
    {
        await Seed(null, Daily("Run", 2));
        long runId = IdOf("Run");

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        await cut.Find($"#habit-item-{runId}").ClickAsync(new MouseEventArgs());

        Assert.That(_app.Get<NavigationManager>().Uri, Does.EndWith($"/habits/{runId}"));
    }

    private IRenderedComponent<Habits> RenderWide(long? id = null) =>
        _app.Ctx.Render<Habits>(parameters => parameters
            .Add(p => p.Id, id)
            .AddCascadingValue("WindowDimensions", new Dimensions { Width = 1400, Height = 900 }));

    [Test]
    public async Task WideWindow_OpenAndClose_TheEditorIsInTheSecondColumnAndTheFocusComesBack()
    {
        await Seed(null, Daily("Run", 2), Daily("Read", 1));
        long runId = IdOf("Run");

        IRenderedComponent<Habits> cut = RenderWide();

        Assert.That(cut.FindAll(".child-column #habit-component"), Is.Empty);

        await cut.Find($"#habit-item-{runId}").ClickAsync(new MouseEventArgs());
        cut.Render(parameters => parameters.Add(p => p.Id, runId));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.FindAll(".child-column #habit-component"), Has.Count.EqualTo(1));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Run", "Read" }));
            await _app.JsInterop.Received(1).FocusElementById("habit-component");
        });

        await cut.Find("[data-habits-step-11]").ClickAsync(new MouseEventArgs());

        Assert.That(_app.Get<NavigationManager>().Uri, Does.EndWith("/habits"));

        cut.Render(parameters => parameters.Add(p => p.Id, (long?)null));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(cut.FindAll("#habit-component"), Is.Empty);
            await _app.JsInterop.Received(1).FocusElementById($"habit-item-{runId}");
        });
    }

    [Test]
    public async Task Embedded_OpenAndClose_StayOnTheSamePage()
    {
        await Seed(null, Daily("Run", 2), Daily("Read", 1));
        long runId = IdOf("Run");

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.IsEmbedded, true));
        string startUri = _app.Get<NavigationManager>().Uri;

        await cut.Find($"#habit-item-{runId}").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#habit-component"), Has.Count.EqualTo(1));
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Read" }));
        });

        await cut.Find("[data-habits-step-11]").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("#habit-component"), Is.Empty);
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Run", "Read" }));
            Assert.That(_app.Get<NavigationManager>().Uri, Is.EqualTo(startUri));
        });
    }

    [Test]
    public async Task WideColumn_TheCalendarStripFillsTheRestOfTheRow()
    {
        _app.JsInterop.GetElementDimensions(Arg.Any<ElementReference>()).Returns(Task.FromResult(new Dimensions { Width = 800, Height = 800 }));
        await Seed(null, Daily("Run", 2));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        IElement strip = cut.Find("[data-habits-step-6]");

        Assert.Multiple(() =>
        {
            Assert.That(strip.ClassList, Does.Contain("d-flex"));
            Assert.That(strip.QuerySelectorAll("[role='gridcell']"), Has.Length.EqualTo((800 - 350) / 50));
            Assert.That(cut.Find($"#habit-item-{IdOf("Run")}").ParentElement!.GetAttribute("style"), Is.EqualTo("height: 50px;"));
        });
    }

    [Test]
    public async Task SmallCalendar_ClickToday_UpdatesTheBadge()
    {
        await Seed(null, Daily("Run", 2));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        await cut.Find("[data-habits-step-6] button.border-primary-subtle").ClickAsync(new MouseEventArgs());

        Assert.That(Text(Badge(cut, "Run")), Is.EqualTo("0 m (0%)"));
    }

    private async Task SeedTwoCategories(Action<SettingsModel> configure)
    {
        ClientState clientState = await _app.Start(settings =>
        {
            settings.ShowGroupedByCategory = true;
            configure(settings);
        });

        await clientState.AddUserData(
        [
            new CategoryModel { Title = "Health", Habits = [Daily("Run", 2)] },
            new CategoryModel { Title = "Home", Habits = [Daily("Dust", 1)] }
        ]);
    }

    private long CategoryIdOf(string title) => _app.ClientState.Categories!.Values.Single(x => x.Title == title).Id;

    [Test]
    public async Task GroupedByCategory_HeaderClick_FoldsTheGroupStoresItAndReportsIt()
    {
        await SeedTwoCategories(_ => { });
        int reported = 0;

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>(parameters => parameters.Add(p => p.OnIsCollapsedChanged, () => reported++));

        await cut.FindAll("button.btn-plain.border-0").First(x => x.TextContent.Contains("Health")).ClickAsync(new MouseEventArgs());

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Titles(cut), Is.EqualTo(new[] { "Dust" }));
            Assert.That((await _app.Store.GetCategory(CategoryIdOf("Health")))!.IsCollapsed, Is.True);
            Assert.That(reported, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GroupedByCategory_HiddenCategory_IsLeftOut()
    {
        await SeedTwoCategories(_ => { });
        _app.ClientState.Settings.HiddenCategoryIds.Add(CategoryIdOf("Home"));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        Assert.That(Titles(cut), Is.EqualTo(new[] { "Run" }));
    }

    [TestCase(FilterDisplay.RadioButtons)]
    [TestCase(FilterDisplay.SelectOptions)]
    public async Task GroupedByCategory_OneSelectedCategory_IsTheOnlyGroup(FilterDisplay display)
    {
        await SeedTwoCategories(settings => settings.CategoryFilterDisplay = display);
        _app.ClientState.Settings.SelectedCategoryId = CategoryIdOf("Home");

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        Assert.That(Titles(cut), Is.EqualTo(new[] { "Dust" }));
    }

    [Test]
    public async Task Help_StartsTheHabitsTour()
    {
        await Seed(settings => settings.ShowHelp = true, Daily("Run", 2));

        IRenderedComponent<Habits> cut = _app.Ctx.Render<Habits>();

        await cut.Find("button[aria-label='Help']").ClickAsync(new MouseEventArgs());

        _app.GTourService.Received(1).StartTour("habits");
    }
}
