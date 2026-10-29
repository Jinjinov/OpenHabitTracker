using OpenHabitTracker.Localization;
using System.Globalization;

namespace OpenHabitTracker.UnitTests.Localization;

[TestFixture]
public class LocTests
{
    private static readonly DateTime _sample = new(2026, 1, 5, 13, 5, 7);

    [Test]
    public void CreateFormattingCulture_NoPatterns_KeepsTheLanguagesOwn()
    {
        CultureInfo culture = Loc.CreateFormattingCulture(new CultureInfo("en"), null, null);

        Assert.That(_sample.ToString("d", culture), Is.EqualTo("1/5/2026"));
        Assert.That(_sample.ToString("t", culture), Is.EqualTo("1:05 PM"));
    }

    [Test]
    public void CreateFormattingCulture_DayFirstAndTwentyFourHour_ChangesEnglishFormats()
    {
        CultureInfo culture = Loc.CreateFormattingCulture(new CultureInfo("en"), "dd/MM/yyyy", "HH:mm");

        Assert.That(_sample.ToString("d", culture), Is.EqualTo("05/01/2026"));
        Assert.That(_sample.ToString("t", culture), Is.EqualTo("13:05"));
        Assert.That(_sample.ToString("T", culture), Is.EqualTo("13:05:07"));
    }

    [Test]
    public void CreateFormattingCulture_SlashesInAPattern_StaySlashesInGerman()
    {
        CultureInfo culture = Loc.CreateFormattingCulture(new CultureInfo("de"), "dd/MM/yyyy", "h:mm tt");

        Assert.That(_sample.ToString("d", culture), Is.EqualTo("05/01/2026"));
        Assert.That(_sample.ToString("t", culture), Does.StartWith("1:05"));
    }

    [Test]
    public void CreateFormattingCulture_APattern_LeavesMonthNamesInTheLanguage()
    {
        CultureInfo culture = Loc.CreateFormattingCulture(new CultureInfo("sl"), "M/d/yyyy", null);

        Assert.That(_sample.ToString("MMMM", culture), Is.EqualTo("januar"));
    }

    [Test]
    public void CreateFormattingCulture_SerbianLatin_SurvivesTheClone()
    {
        CultureInfo culture = Loc.CreateFormattingCulture(Loc.Cultures["sr"].Culture, "dd.MM.yyyy", null);

        Assert.That(culture.Name, Is.EqualTo("sr-Latn"));
        Assert.That(_sample.ToString("d", culture), Is.EqualTo("05.01.2026"));
    }

    [Test]
    public void CreateFormattingCulture_DoesNotChangeTheSharedLanguageCulture()
    {
        CultureInfo language = Loc.Cultures["en"].Culture;

        Loc.CreateFormattingCulture(language, "yyyy-MM-dd", "HH:mm");

        Assert.That(language.DateTimeFormat.ShortDatePattern, Is.EqualTo("M/d/yyyy"));
    }

    [Test]
    public void LiteralSeparators_QuotesOnlyUnquotedSeparators()
    {
        Assert.That(Loc.LiteralSeparators("dd/MM/yyyy"), Is.EqualTo("dd'/'MM'/'yyyy"));
        Assert.That(Loc.LiteralSeparators("HH:mm"), Is.EqualTo("HH':'mm"));
        Assert.That(Loc.LiteralSeparators("dd'/'MM"), Is.EqualTo("dd'/'MM"));
        Assert.That(Loc.LiteralSeparators("d\\/M"), Is.EqualTo("d\\/M"));
    }
}
