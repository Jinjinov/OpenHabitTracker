using Foundation;
using UIKit;

namespace OpenHabitTracker.Blazor.Maui;
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        // Before the first page, so no time input can open the wheels style the Mac idiom forbids.
        TimePickerStyle.UseInlineInsteadOfWheels();

        return base.FinishedLaunching(application, launchOptions);
    }
}
