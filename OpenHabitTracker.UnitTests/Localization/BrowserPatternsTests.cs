using OpenHabitTracker.Localization;
using System.Globalization;

namespace OpenHabitTracker.UnitTests.Localization;

// The parts are real Edge output for 2026-01-05 09:05.
[TestFixture]
public class BrowserPatternsTests
{
    private static readonly DateTime _sample = new(2026, 1, 5, 9, 5, 0);
    private static readonly DateTime _afternoon = new(2026, 1, 5, 13, 5, 0);

    private static List<DateTimePart> Parts(params (string Type, string Value)[] parts) =>
        parts.Select(x => new DateTimePart { Type = x.Type, Value = x.Value }).ToList();

    private static string FormatDate(string pattern, string language) =>
        _sample.ToString("d", Loc.CreateFormattingCulture(new CultureInfo(language), pattern, null));

    private static string FormatTime(DateTime time, string pattern, string language) =>
        time.ToString("t", Loc.CreateFormattingCulture(new CultureInfo(language), null, pattern));

    [Test]
    public void ToDatePattern_EnUs_IsMonthFirstWithAFullYear()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("month", "1"), ("literal", "/"), ("day", "5"), ("literal", "/"), ("year", "26")));

        Assert.That(FormatDate(pattern!, "en"), Is.EqualTo("1/5/2026"));
    }

    [Test]
    public void ToDatePattern_EnGb_IsDayFirstAndPadded()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("day", "05"), ("literal", "/"), ("month", "01"), ("literal", "/"), ("year", "2026")));

        Assert.That(FormatDate(pattern!, "en"), Is.EqualTo("05/01/2026"));
    }

    [Test]
    public void ToDatePattern_EnCa_IsIso()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("year", "2026"), ("literal", "-"), ("month", "01"), ("literal", "-"), ("day", "05")));

        Assert.That(FormatDate(pattern!, "en"), Is.EqualTo("2026-01-05"));
    }

    [Test]
    public void ToDatePattern_DeDe_KeepsItsDotsInAnotherLanguage()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("day", "05"), ("literal", "."), ("month", "01"), ("literal", "."), ("year", "26")));

        Assert.That(FormatDate(pattern!, "en"), Is.EqualTo("05.01.2026"));
    }

    [Test]
    public void ToDatePattern_SlSi_KeepsTheSpaceAfterEachDot()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("day", "5"), ("literal", ". "), ("month", "1"), ("literal", ". "), ("year", "2026")));

        Assert.That(FormatDate(pattern!, "sl"), Is.EqualTo("5. 1. 2026"));
    }

    [Test]
    public void ToDatePattern_SrLatn_KeepsTheTrailingDot()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("day", "5"), ("literal", ". "), ("month", "1"), ("literal", ". "), ("year", "2026"), ("literal", ".")));

        Assert.That(FormatDate(pattern!, "sr-Latn"), Is.EqualTo("5. 1. 2026."));
    }

    [Test]
    public void ToDatePattern_KoKr_IsYearFirstWithDots()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("year", "26"), ("literal", ". "), ("month", "1"), ("literal", ". "), ("day", "5"), ("literal", ".")));

        Assert.That(FormatDate(pattern!, "ko"), Is.EqualTo("2026. 1. 5."));
    }

    [Test]
    public void ToDatePattern_JaJp_IsYearFirstWithSlashes()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("year", "2026"), ("literal", "/"), ("month", "01"), ("literal", "/"), ("day", "05")));

        Assert.That(FormatDate(pattern!, "ja"), Is.EqualTo("2026/01/05"));
    }

    [Test]
    public void ToDatePattern_ArEg_ArabicDigitsFallBack()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("day", "\u0665"), ("literal", "\u200F/"), ("month", "\u0661"), ("literal", "\u200F/"), ("year", "\u0662\u0660\u0662\u0666")));

        Assert.That(pattern, Is.Null);
    }

    [Test]
    public void ToDatePattern_AnUnknownPart_FallsBack()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("weekday", "Mon"), ("literal", " "), ("day", "5"), ("literal", "/"), ("month", "1"), ("literal", "/"), ("year", "2026")));

        Assert.That(pattern, Is.Null);
    }

    [Test]
    public void ToDatePattern_AMissingPart_FallsBack()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("day", "5"), ("literal", "/"), ("month", "1")));

        Assert.That(pattern, Is.Null);
    }

    [Test]
    public void ToDatePattern_AnApostropheInALiteral_IsWrittenOut()
    {
        string? pattern = BrowserPatterns.ToDatePattern(Parts(("day", "5"), ("literal", "'"), ("month", "1"), ("literal", "'"), ("year", "2026")));

        Assert.That(FormatDate(pattern!, "en"), Is.EqualTo("5'1'2026"));
    }

    [Test]
    public void ToTimePattern_EnUs_IsTwelveHour()
    {
        string? pattern = BrowserPatterns.ToTimePattern(Parts(("hour", "9"), ("literal", ":"), ("minute", "05"), ("literal", " "), ("dayPeriod", "AM")), "h12");

        Assert.That(FormatTime(_sample, pattern!, "en"), Is.EqualTo("9:05 AM"));
        Assert.That(FormatTime(_afternoon, pattern!, "en"), Is.EqualTo("1:05 PM"));
    }

    [Test]
    public void ToTimePattern_EnGb_IsPaddedTwentyFourHour()
    {
        string? pattern = BrowserPatterns.ToTimePattern(Parts(("hour", "09"), ("literal", ":"), ("minute", "05")), "h23");

        Assert.That(FormatTime(_sample, pattern!, "en"), Is.EqualTo("09:05"));
        Assert.That(FormatTime(_afternoon, pattern!, "en"), Is.EqualTo("13:05"));
    }

    [Test]
    public void ToTimePattern_FiFi_KeepsItsDotSeparator()
    {
        string? pattern = BrowserPatterns.ToTimePattern(Parts(("hour", "9"), ("literal", "."), ("minute", "05")), "h23");

        Assert.That(FormatTime(_afternoon, pattern!, "fi"), Is.EqualTo("13.05"));
    }

    [Test]
    public void ToTimePattern_KoKr_PutsTheDayPeriodFirst()
    {
        string? pattern = BrowserPatterns.ToTimePattern(Parts(("dayPeriod", "\uC624\uC804"), ("literal", " "), ("hour", "9"), ("literal", ":"), ("minute", "05")), "h12");

        Assert.That(pattern, Is.EqualTo("tt' 'h':'mm"));
    }

    [Test]
    public void ToTimePattern_AnUnknownHourCycle_FallsBack()
    {
        string? pattern = BrowserPatterns.ToTimePattern(Parts(("hour", "09"), ("literal", ":"), ("minute", "05")), "");

        Assert.That(pattern, Is.Null);
    }

    [Test]
    public void ToTimePattern_TwelveHourWithoutADayPeriod_FallsBack()
    {
        string? pattern = BrowserPatterns.ToTimePattern(Parts(("hour", "9"), ("literal", ":"), ("minute", "05")), "h12");

        Assert.That(pattern, Is.Null);
    }
}
