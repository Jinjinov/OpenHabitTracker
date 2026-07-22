using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using OpenHabitTracker.Backup;
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
        string windowSettingsPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Window.yaml");

        Window window = new Window(new MainPage())
        {
            Title = "OpenHabitTracker"
        };

        WindowSettings? saved = WindowSettings.Load(windowSettingsPath);

        if (saved is not null)
        {
            window.X = saved.X;
            window.Y = saved.Y;
            window.Width = saved.Width;
            window.Height = saved.Height;
        }
        else
        {
            // First run: min(a screen fraction, the 1680x1050 cap), top-left. DIPs are the native unit.
            // No #if WINDOWS, so no taskbar-excluded work area; a 0.85 fraction of the full display absorbs the taskbar.
            DisplayInfo display = DeviceDisplay.Current.MainDisplayInfo;
            double density = display.Density > 0 ? display.Density : 1;
            window.X = 0;
            window.Y = 0;
            window.Width = Math.Min(display.Width / density * 0.85, 1680);
            window.Height = Math.Min(display.Height / density * 0.85, 1050);
        }

        window.SizeChanged += (sender, e) => SaveWindowSettings(window, windowSettingsPath);
        window.Destroying += (sender, e) => SaveWindowSettings(window, windowSettingsPath);

        return window;
    }

    private static void SaveWindowSettings(Window window, string path)
    {
        // Geometry is NaN until the window is laid out (and on mobile, where it is not applicable).
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
