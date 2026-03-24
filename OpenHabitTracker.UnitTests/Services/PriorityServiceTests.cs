using OpenHabitTracker.Data;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.UnitTests.Services;

[TestFixture]
public class PriorityServiceTests
{
    private PriorityService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new();
    }

    // --- GetPriorityTitle tests ---

    [TestCase(Priority.None, "⊘")]
    [TestCase(Priority.VeryLow, "︾")]
    [TestCase(Priority.Low, "﹀")]
    [TestCase(Priority.Medium, "—")]
    [TestCase(Priority.High, "︿")]
    [TestCase(Priority.VeryHigh, "︽")]
    public void GetPriorityTitle_ReturnsCorrectSymbol(Priority priority, string expected)
    {
        string result = _sut.GetPriorityTitle(priority);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void GetPriorityTitle_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetPriorityTitle((Priority)99));
    }

    // --- GetPriorityName tests ---

    [TestCase(Priority.None, "No priority")]
    [TestCase(Priority.VeryLow, "Very low priority")]
    [TestCase(Priority.Low, "Low priority")]
    [TestCase(Priority.Medium, "Medium priority")]
    [TestCase(Priority.High, "High priority")]
    [TestCase(Priority.VeryHigh, "Very high priority")]
    public void GetPriorityName_ReturnsCorrectName(Priority priority, string expected)
    {
        string result = _sut.GetPriorityName(priority);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void GetPriorityName_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetPriorityName((Priority)99));
    }
}
