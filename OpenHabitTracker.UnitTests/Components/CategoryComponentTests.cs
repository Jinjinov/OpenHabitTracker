using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using OpenHabitTracker.Blazor.Components;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Components;

[TestFixture]
public class CategoryComponentTests
{
    private BunitContext _ctx = null!;
    private ICategoryService _categoryService = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();

        _categoryService = Substitute.For<ICategoryService>();
        _categoryService.Categories.Returns([TestData.Category(id: 1, title: "Work")]);

        IStringLocalizer loc = Substitute.For<IStringLocalizer>();
        loc[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.RequiredArg<string>(), callInfo.RequiredArg<string>()));

        _ctx.Services.AddScoped(_ => _categoryService);
        _ctx.Services.AddSingleton(loc);
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    // An item in a category can be moved back to no category.
    [Test]
    public void Select_ItemInACategory_MovesBackToNone()
    {
        NoteModel note = TestData.Note(id: 5, categoryId: 1);

        IRenderedComponent<CategoryComponent> cut = _ctx.Render<CategoryComponent>(
            parameters => parameters.Add(p => p.Model, note));

        cut.Find("select").Change("0");

        Assert.That(cut.Find("option[value='0']").HasAttribute("disabled"), Is.False);
        _categoryService.Received(1).ChangeCategory(note, 0);
    }
}
