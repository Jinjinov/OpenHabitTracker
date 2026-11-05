using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenHabitTracker.Blazor.Web;
using OpenHabitTracker.Localization;
using System.Globalization;

namespace OpenHabitTracker.UnitTests.Web;

[TestFixture]
public class CircuitCultureTests
{
    private static readonly CultureInfo _german = new("de");
    private static readonly CultureInfo _english = new("en");

    private CultureInfo? _defaultCulture;
    private CultureInfo? _defaultUiCulture;
    private CultureInfo _culture = null!;
    private CultureInfo _uiCulture = null!;

    [SetUp]
    public void SetUp()
    {
        _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        _defaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;
    }

    [TearDown]
    public void TearDown()
    {
        CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultUiCulture;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }

    private static async Task<(CultureInfo Culture, CultureInfo UiCulture)> DuringNextEvent(CircuitCulture circuit)
    {
        (CultureInfo, CultureInfo) seen = default;

        Func<CircuitInboundActivityContext, Task> handler = circuit.CreateInboundActivityHandler(_ =>
        {
            seen = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
            return Task.CompletedTask;
        });

        await handler(null!);

        return seen;
    }

    [Test]
    public void Apply_NeverTouchesTheProcessDefaults()
    {
        CultureInfo.DefaultThreadCurrentCulture = _english;
        CultureInfo.DefaultThreadCurrentUICulture = _english;

        new CircuitCulture().Apply(_german, _german);

        Assert.Multiple(() =>
        {
            Assert.That(CultureInfo.DefaultThreadCurrentCulture, Is.EqualTo(_english));
            Assert.That(CultureInfo.DefaultThreadCurrentUICulture, Is.EqualTo(_english));
            Assert.That(CultureInfo.CurrentUICulture, Is.EqualTo(_german));
        });
    }

    [Test]
    public async Task NextEvent_CultureWasLostBetweenEvents_RestoresTheCircuitsOwn()
    {
        CircuitCulture circuit = new();
        circuit.Apply(_german, _german);

        CultureInfo.CurrentCulture = _english;
        CultureInfo.CurrentUICulture = _english;

        (CultureInfo culture, CultureInfo uiCulture) = await DuringNextEvent(circuit);

        Assert.Multiple(() =>
        {
            Assert.That(culture, Is.EqualTo(_german));
            Assert.That(uiCulture, Is.EqualTo(_german));
        });
    }

    [Test]
    public async Task NextEvent_TwoCircuits_EachKeepsItsOwn()
    {
        CircuitCulture anna = new(), bert = new();

        anna.Apply(_english, _english);
        bert.Apply(_german, _german);

        Assert.Multiple(async () =>
        {
            Assert.That((await DuringNextEvent(anna)).UiCulture, Is.EqualTo(_english));
            Assert.That((await DuringNextEvent(bert)).UiCulture, Is.EqualTo(_german));
        });
    }

    [Test]
    public async Task NextEvent_NothingAppliedYet_LeavesTheCultureAlone()
    {
        CultureInfo.CurrentUICulture = _german;

        Assert.That((await DuringNextEvent(new CircuitCulture())).UiCulture, Is.EqualTo(_german));
    }

    [Test]
    public void ProcessCulture_SetsTheDefaultsForTheSingleUserHosts()
    {
        new ProcessCulture().Apply(_german, _german);

        Assert.Multiple(() =>
        {
            Assert.That(CultureInfo.DefaultThreadCurrentCulture, Is.EqualTo(_german));
            Assert.That(CultureInfo.DefaultThreadCurrentUICulture, Is.EqualTo(_german));
            Assert.That(CultureInfo.CurrentCulture, Is.EqualTo(_german));
        });
    }
}
