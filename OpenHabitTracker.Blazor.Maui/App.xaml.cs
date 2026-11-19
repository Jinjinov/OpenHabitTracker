using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using OpenHabitTracker.App;
using Application = Microsoft.Maui.Controls.Application;

namespace OpenHabitTracker.Blazor.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Same per-platform dir the db and log use (see MauiProgram).
        string windowSettingsPath = Path.Combine(FileSystem.Current.AppDataDirectory, WindowSettings.FileName);

        Window window = new Window(new MainPage())
        {
            Title = "OpenHabitTracker"
        };

        WindowSettings? saved = WindowSettings.Load(windowSettingsPath);

        Rect frame;

        if (saved is not null)
        {
            frame = new Rect(saved.X, saved.Y, saved.Width, saved.Height);
        }
        else
        {
            // First run: min(a screen fraction, the 1680x1050 cap), top-left. DIPs are the native unit.
            // No #if WINDOWS, so no taskbar-excluded work area; a 0.85 fraction of the full display absorbs the taskbar.
            DisplayInfo display = DeviceDisplay.Current.MainDisplayInfo;
            double density = display.Density > 0 ? display.Density : 1;
            frame = new Rect(0, 0, Math.Min(display.Width / density * 0.85, 1680), Math.Min(display.Height / density * 0.85, 1050));
        }

        SetFrame(window, frame);

        // Mac Catalyst applies a frame only to a window that has its scene, which it gets after CreateWindow; until then
        // the window reports the scene's default bounds back as its frame. So the frame is set again on the first activation.
        // Even then macOS places the launching window itself a moment later, at its default frame, so on Mac Catalyst the frame
        // is set once more on the first resize the app did not cause. The window is saved only from then on,
        // so neither the default bounds nor the default placement overwrite the saved frame.
        // Elsewhere the frame is already applied, and setting the same values again changes nothing.
        bool activated = false;
        bool waitingForPlacement = DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst;
        bool settingFrame = false;

        void ApplyFrame()
        {
            settingFrame = true;
            SetFrame(window, frame);
            settingFrame = false;
        }

        void SaveFromNowOn()
        {
            window.SizeChanged += (sender, e) => SaveWindowSettings(window, windowSettingsPath);
            window.Destroying += (sender, e) => SaveWindowSettings(window, windowSettingsPath);
        }

        window.Activated += (sender, e) =>
        {
            if (activated)
                return;

            activated = true;

            ApplyFrame();

            if (!waitingForPlacement)
                SaveFromNowOn();
        };

        window.SizeChanged += (sender, e) =>
        {
            if (!activated || !waitingForPlacement || settingFrame)
                return;

            waitingForPlacement = false;

            ApplyFrame();
            SaveFromNowOn();
        };

        window.Resumed += async (sender, e) => await RebuildNotificationSchedule();

        return window;
    }

    // Time has passed and another device may have changed something, so the schedule is rebuilt on
    // resume as well as on launch.
    public static async Task RebuildNotificationSchedule()
    {
        if (IPlatformApplication.Current?.Services is IServiceProvider services)
            await NotificationRebuildRelay.Rebuild(services);
    }

    private static void SetFrame(Window window, Rect frame)
    {
        window.X = frame.X;
        window.Y = frame.Y;
        window.Width = frame.Width;
        window.Height = frame.Height;
    }

    private static void SaveWindowSettings(Window window, string path)
    {
        // Geometry is NaN until the window is laid out.
        if (double.IsNaN(window.Width) || double.IsNaN(window.Height) || double.IsNaN(window.X) || double.IsNaN(window.Y))
            return;

        new WindowSettings
        {
            X = window.X,
            Y = window.Y,
            Width = window.Width,
            Height = window.Height
        }.Save(path);
    }
}
