using System.Drawing;

namespace OpenHabitTracker.DeviceTests.Windows;

[TestFixture(WindowsHost.Msix)]
[TestFixture(WindowsHost.WinForms)]
public class WindowTests(WindowsHost host) : WindowsTest(host)
{
    // The app saves its window's place and size when it closes, in Window.yaml beside the database, and puts the window back there on the next start.
    // The rectangle is away from the first start's top-left corner and size, so a start that ignores the file fails.
    [Test]
    public void Window_MovedAndResized_IsRestoredAfterRestart()
    {
        ClearData();
        StartApp();

        Driver.Manage().Window.Position = new Point(160, 120);
        Driver.Manage().Window.Size = new Size(1100, 760);
        Rectangle placed = WindowRectangle();

        CloseApp();
        Assert.That(File.Exists(Path.Combine(DataFolder, "Window.yaml")), Is.True, "Closing the app saved no Window.yaml");

        StartApp();

        Assert.That(WindowRectangle(), Is.EqualTo(placed));
    }

    private Rectangle WindowRectangle()
    {
        IWindow window = Driver.Manage().Window;

        return new Rectangle(window.Position, window.Size);
    }
}
