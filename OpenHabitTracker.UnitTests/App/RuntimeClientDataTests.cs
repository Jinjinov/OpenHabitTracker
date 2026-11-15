using NSubstitute;
using OpenHabitTracker.Blazor;

namespace OpenHabitTracker.UnitTests.App;

[TestFixture]
public class RuntimeClientDataTests
{
    private IJsInterop _jsInterop = null!;
    private IPreRenderService _preRenderService = null!;
    private RuntimeClientData _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _jsInterop = Substitute.For<IJsInterop>();
        _jsInterop.GetWindowDimensions().Returns(new Dimensions { Width = 1280, Height = 720 });
        _preRenderService = Substitute.For<IPreRenderService>();
        _sut = new RuntimeClientData(_jsInterop, _preRenderService);
    }

    [Test]
    public async Task GetWindowWidth_ReadsTheWidthOnceAndKeepsIt()
    {
        int first = await _sut.GetWindowWidth();

        _jsInterop.GetWindowDimensions().Returns(new Dimensions { Width = 400, Height = 800 });

        int second = await _sut.GetWindowWidth();

        await Assert.MultipleAsync(async () =>
        {
            Assert.That((first, second), Is.EqualTo((1280, 1280)));
            await _jsInterop.Received(1).GetWindowDimensions();
        });
    }

    // A prerender has no browser to ask.
    [Test]
    public async Task GetWindowWidth_WhilePrerendering_IsZeroAndAsksAgainLater()
    {
        _preRenderService.IsPreRendering.Returns(true);

        int prerendered = await _sut.GetWindowWidth();

        _preRenderService.IsPreRendering.Returns(false);

        int rendered = await _sut.GetWindowWidth();

        Assert.That((prerendered, rendered), Is.EqualTo((0, 1280)));
    }
}
