using System.Reflection;
using OpenHabitTracker.Backup;

namespace OpenHabitTracker.UnitTests.Backup;

// NearestColor is internal to the backup assembly, which the importers reach it through.
[TestFixture]
public class NearestColorTests
{
    private static readonly Type _nearestColor = typeof(ImportExportService).Assembly.GetType("OpenHabitTracker.Backup.NearestColor", throwOnError: true)!;

    private static string OfRgb(int rgb) => (string)_nearestColor.GetMethod("OfRgb", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [rgb])!;

    private static bool TryParseHex(string? value, out string color)
    {
        object?[] arguments = [value, null];

        bool parsed = (bool)_nearestColor.GetMethod("TryParseHex", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, arguments)!;

        color = (string)arguments[1]!;

        return parsed;
    }

    // The Material palette Loop uses maps to the class of its own meaning.
    [TestCase(0xD32F2F, "bg-danger-subtle")]
    [TestCase(0xE64A19, "bg-warning-subtle")]
    [TestCase(0xF57C00, "bg-warning-subtle")]
    [TestCase(0xFF8F00, "bg-warning-subtle")]
    [TestCase(0xF9A825, "bg-warning-subtle")]
    [TestCase(0xAFB42B, "bg-success-subtle")]
    [TestCase(0x7CB342, "bg-success-subtle")]
    [TestCase(0x388E3C, "bg-success-subtle")]
    [TestCase(0x00897B, "bg-info-subtle")]
    [TestCase(0x00ACC1, "bg-info-subtle")]
    [TestCase(0x039BE5, "bg-info-subtle")]
    [TestCase(0x1976D2, "bg-primary-subtle")]
    [TestCase(0x303F9F, "bg-primary-subtle")]
    [TestCase(0x5E35B1, "bg-primary-subtle")]
    [TestCase(0x8E24AA, "bg-primary-subtle")]
    [TestCase(0xD81B60, "bg-danger-subtle")]
    [TestCase(0x5D4037, "bg-dark-subtle")]
    [TestCase(0x424242, "bg-dark-subtle")]
    [TestCase(0x757575, "bg-secondary-subtle")]
    [TestCase(0x9E9E9E, "bg-secondary-subtle")]
    public void OfRgb_PaletteColor_MapsToItsMeaning(int rgb, string color)
    {
        Assert.That(OfRgb(rgb), Is.EqualTo(color));
    }

    [TestCase(0xFF0000, "bg-danger-subtle")]
    [TestCase(0x00FF00, "bg-success-subtle")]
    [TestCase(0x0000FF, "bg-primary-subtle")]
    [TestCase(0xFFFF00, "bg-warning-subtle")]
    [TestCase(0x808080, "bg-secondary-subtle")]
    [TestCase(0x000000, "bg-dark-subtle")]
    [TestCase(0xFFFFFF, "bg-secondary-subtle")]
    public void OfRgb_OtherColor_MapsToTheNearest(int rgb, string color)
    {
        Assert.That(OfRgb(rgb), Is.EqualTo(color));
    }

    // Android stores colors with an alpha byte in front.
    [Test]
    public void OfRgb_WithAlpha_IgnoresIt()
    {
        Assert.That(OfRgb(unchecked((int)0xFF388E3C)), Is.EqualTo("bg-success-subtle"));
    }

    [TestCase("#D32F2F", "bg-danger-subtle")]
    [TestCase("#d32f2f", "bg-danger-subtle")]
    [TestCase("#1976D2", "bg-primary-subtle")]
    public void TryParseHex_Valid_MapsTheColor(string value, string color)
    {
        bool parsed = TryParseHex(value, out string result);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(result, Is.EqualTo(color));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("D32F2F")]
    [TestCase("#D32F2")]
    [TestCase("#D32F2F0")]
    [TestCase("#GGGGGG")]
    [TestCase("rgb(1,2,3)")]
    public void TryParseHex_Invalid_IsFalseWithNoColor(string? value)
    {
        bool parsed = TryParseHex(value, out string result);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.False);
            Assert.That(result, Is.Empty);
        });
    }
}
