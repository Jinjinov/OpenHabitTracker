using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Backup;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class ImportSummaryComponentTests
{
    private BunitContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    private static PendingImport Pending(ImportCounts found, ImportCounts added, SettingsModel? settings = null, ImportFormat format = ImportFormat.GoogleKeep) => new()
    {
        Format = format,
        Filename = "takeout.zip",
        Categories = [],
        Settings = settings,
        Found = found,
        New = added
    };

    private IRenderedComponent<ImportSummary> Render(PendingImport pendingImport, Action? onImport = null, Action? onCancel = null)
    {
        return _ctx.Render<ImportSummary>(parameters => parameters
            .Add(p => p.PendingImport, pendingImport)
            .Add(p => p.OnImport, onImport ?? (() => { }))
            .Add(p => p.OnCancel, onCancel ?? (() => { })));
    }

    [Test]
    public void Summary_ShowsTheFormatAndOnlyTheRowsTheFileHas()
    {
        IRenderedComponent<ImportSummary> cut = Render(Pending(new(2, 28, 2, 0, 7, 0), new(2, 3, 0, 0, 1, 0)));

        IReadOnlyList<IElement> rows = cut.FindAll("tbody tr");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Google Keep (.zip): takeout.zip"));
            Assert.That(rows.Select(x => x.TextContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                Is.EqualTo(new[] { new[] { "Categories", "2", "2" }, new[] { "Notes", "28", "3" }, new[] { "Tasks", "2", "0" }, new[] { "Items", "7", "1" } }));
        });
    }

    [TestCase(ImportFormat.Json, "JSON (.json)")]
    [TestCase(ImportFormat.Tsv, "TSV (.tsv)")]
    [TestCase(ImportFormat.Yaml, "YAML (.yaml)")]
    [TestCase(ImportFormat.Markdown, "Markdown (.md)")]
    [TestCase(ImportFormat.GoogleKeep, "Google Keep (.zip)")]
    [TestCase(ImportFormat.LoopDatabase, "Loop Habit Tracker (.db)")]
    [TestCase(ImportFormat.LoopCsv, "Loop Habit Tracker (.zip)")]
    [TestCase(ImportFormat.HabitNow, "HabitNow (.hn)")]
    [TestCase(ImportFormat.Habitica, "Habitica (.json)")]
    [TestCase(ImportFormat.HabitifyDatabase, "Habitify (.db)")]
    public void Summary_NamesTheFormat(ImportFormat format, string name)
    {
        PendingImport pending = Pending(new(1, 0, 0, 0, 0, 0), new(1, 0, 0, 0, 0, 0), format: format);

        IRenderedComponent<ImportSummary> cut = Render(pending);

        Assert.That(cut.Find("small").TextContent, Is.EqualTo($"{name}: takeout.zip"));
    }

    // An Unknown file is refused before a summary is shown.
    [Test]
    public void Summary_EveryFormatHasAName()
    {
        foreach (ImportFormat format in Enum.GetValues<ImportFormat>().Where(x => x != ImportFormat.Unknown))
        {
            PendingImport pending = Pending(new(1, 0, 0, 0, 0, 0), new(1, 0, 0, 0, 0, 0), format: format);

            IRenderedComponent<ImportSummary> cut = Render(pending);

            Assert.That(cut.Find("small").TextContent, Does.Not.StartWith($"{format}:"), format.ToString());
        }
    }

    [Test]
    public void Summary_ShowsHabitsAndCompletions()
    {
        IRenderedComponent<ImportSummary> cut = Render(Pending(new(0, 0, 0, 3, 0, 40), new(0, 0, 0, 1, 0, 12)));

        Assert.That(cut.FindAll("tbody tr").Select(x => x.TextContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            Is.EqualTo(new[] { new[] { "Habits", "3", "1" }, new[] { "Completions", "40", "12" } }));
    }

    [Test]
    public async Task Summary_WithSomethingNew_ImportsOnImport()
    {
        bool imported = false;

        IRenderedComponent<ImportSummary> cut = Render(Pending(new(1, 1, 0, 0, 0, 0), new(1, 1, 0, 0, 0, 0)), onImport: () => imported = true);

        await cut.Find("button.btn-primary").ClickAsync(new MouseEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(imported, Is.True);
            Assert.That(cut.Markup, Does.Not.Contain("Nothing new to import."));
        });
    }

    [Test]
    public async Task Summary_WithNothingNew_OffersOnlyCancel()
    {
        bool cancelled = false;

        IRenderedComponent<ImportSummary> cut = Render(Pending(new(1, 1, 0, 0, 0, 0), new(0, 0, 0, 0, 0, 0)), onCancel: () => cancelled = true);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Nothing new to import."));
            Assert.That(cut.FindAll("button.btn-primary"), Is.Empty);
        });

        await cut.Find("button.btn-secondary").ClickAsync(new MouseEventArgs());

        Assert.That(cancelled, Is.True);
    }

    [Test]
    public void Summary_OfAFileWithSettings_SaysTheyWillBeReplaced()
    {
        IRenderedComponent<ImportSummary> cut = Render(Pending(new(0, 0, 0, 0, 0, 0), new(0, 0, 0, 0, 0, 0), new SettingsModel()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Markup, Does.Contain("The settings will be replaced."));
            Assert.That(cut.Markup, Does.Not.Contain("Nothing new to import."));
            Assert.That(cut.FindAll("button.btn-primary"), Has.Count.EqualTo(1));
        });
    }
}
