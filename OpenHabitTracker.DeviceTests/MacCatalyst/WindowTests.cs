namespace OpenHabitTracker.DeviceTests.MacCatalyst;

[TestFixture]
public class WindowTests : MacCatalystTest
{
    // The window comes back where the user left it and at the size they left it, through Window.yaml.
    // The window is moved by its title bar and resized by its bottom-right corner, as a user does.
    [Test]
    public void MovedAndResizedWindow_Relaunched_ComesBackWithTheSameFrame()
    {
        ResetApp();
        LaunchApp();

        System.Drawing.Rectangle start = WindowFrame();

        Drag(start.Left + start.Width / 2, start.Top + 10, start.Left + start.Width / 2 + 100, start.Top + 90);
        System.Drawing.Rectangle moved = WindowFrame();
        Assert.That(moved.Location, Is.Not.EqualTo(start.Location), "The window did not move");

        Drag(moved.Right - 1, moved.Bottom - 1, moved.Right - 101, moved.Bottom - 51);
        System.Drawing.Rectangle left = WindowFrame();
        Assert.That(left.Size, Is.Not.EqualTo(moved.Size), "The window was not resized");

        TerminateApp();
        LaunchApp();

        // The window settles once the page has loaded.
        WaitUntil(() => WindowFrame() == left, DefaultTimeout);

        Assert.That(WindowFrame(), Is.EqualTo(left), "The window did not come back with the frame it was left with");
    }
}
