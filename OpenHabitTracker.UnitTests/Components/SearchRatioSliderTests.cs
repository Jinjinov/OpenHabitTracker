using Bunit;
using GTour.Abstractions;
using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.App;
using OpenHabitTracker.Blazor;
using OpenHabitTracker.Blazor.Pages;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class SearchRatioSliderTests
{
    private BunitContext _ctx = null!;
    private IDataAccess _dataAccess = null!;
    private ClientState _clientState = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _dataAccess = Substitute.For<IDataAccess>();
        _dataAccess.DataLocation.Returns(DataLocation.Local);
        _dataAccess.GetSettings(Arg.Any<long>()).Returns(Task.FromResult<SettingsEntity?>(new SettingsEntity()));

        _clientState = new(new[] { _dataAccess }, new MarkdownToHtml(new MarkdownPipelineBuilder().Build()));
        _clientState.Settings.ShowHelp = false;

        ICategoryService categoryService = Substitute.For<ICategoryService>();
        categoryService.Categories.Returns(new List<CategoryModel>());

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _ctx.Services.AddScoped(_ => _clientState);
        _ctx.Services.AddScoped(_ => categoryService);
        _ctx.Services.AddScoped<ISearchFilterService>(_ => new SearchFilterService());
        _ctx.Services.AddScoped(_ => Substitute.For<INotificationScheduler>());
        _ctx.Services.AddScoped(_ => Substitute.For<IJsInterop>());
        _ctx.Services.AddSingleton(Substitute.For<IGTourService>());
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    // A drag moves the slider through many values, and the settings are written once, when it is released.
    [TestCase("[data-search-step-19]")]
    [TestCase("[data-search-step-21]")]
    public void RatioSlider_Drag_SavesTheSettingsOnceOnRelease(string slider)
    {
        IRenderedComponent<Search> cut = _ctx.Render<Search>();

        cut.Find(slider).Input("60");
        cut.Find(slider).Input("70");
        cut.Find(slider).Input("80");

        _dataAccess.DidNotReceive().UpdateSettings(Arg.Any<SettingsEntity>());

        cut.Find(slider).Change("80");

        _dataAccess.Received(1).UpdateSettings(Arg.Any<SettingsEntity>());
    }

    [Test]
    public void RatioSlider_Drag_FiltersWithEachValue()
    {
        IRenderedComponent<Search> cut = _ctx.Render<Search>();

        cut.Find("[data-search-step-19]").Input("70");

        Assert.That(_clientState.Settings.SelectedRatioMin, Is.EqualTo(70));
    }
}
