using Bunit;
using OpenHabitTracker.Blazor.Layout;
using OpenHabitTracker.Blazor.Web.Components;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class AccountNavBarFragmentTests
{
    private BunitContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new BunitContext();
    }

    [TearDown]
    public void TearDown()
    {
        _ctx.Dispose();
    }

    [Test]
    public void GetNavBarFragment_Server_LinksToTheAccountPageWithAFullPageLoad()
    {
        IRenderedComponent<Bunit.Rendering.ContainerFragment> cut = _ctx.Render(new AccountNavBarFragment().GetNavBarFragment());

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("a").GetAttribute("href"), Is.EqualTo("/account"));
            Assert.That(cut.Find("a").GetAttribute("data-enhance-nav"), Is.EqualTo("false"));
            Assert.That(cut.Find("a").GetAttribute("aria-label"), Is.EqualTo("Account"));
        });
    }

    [Test]
    public void GetNavBarFragment_OtherHosts_RendersNothing()
    {
        IRenderedComponent<Bunit.Rendering.ContainerFragment> cut = _ctx.Render(new NavBarFragment().GetNavBarFragment());

        Assert.That(cut.Markup, Is.Empty);
    }
}
