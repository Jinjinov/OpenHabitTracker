using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace OpenHabitTracker.Blazor.Maui;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Pin light status bar glyphs: MAUI's theme inherits Theme.MaterialComponents.DayNight,
        // so the glyph color would follow the SYSTEM light/dark setting rather than the app's,
        // and a user in light mode would get dark glyphs on the app's dark status bar.
        if (Window?.DecorView is Android.Views.View decorView)
        {
            WindowInsetsControllerCompat? insetsController = WindowCompat.GetInsetsController(Window, decorView);

            if (insetsController is not null)
                insetsController.AppearanceLightStatusBars = false;
        }
    }
}
