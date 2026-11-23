using Foundation;
using UIKit;

namespace OpenHabitTracker.Blazor.Maui;
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        // Before the first page, so no time input can open its wheels while the Mac idiom still forbids them.
        TimePickerStyle.AllowWheels();

        return base.FinishedLaunching(application, launchOptions);
    }
}
