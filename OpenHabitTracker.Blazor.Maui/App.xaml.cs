using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace OpenHabitTracker.Blazor.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Current.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = new Window(new MainPage())
        {
            Title = "OpenHabitTracker",
            X = 0,
            Y = 0,
            // https://stackoverflow.com/questions/67972372/why-are-window-height-and-window-width-not-exact-c-wpf
            Width = 1680 + 14,
            Height = 1050 + 7 + 31
        };

        return window;
    }
}
