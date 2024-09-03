namespace OpenHabitTracker.Blazor.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new MainPage();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = base.CreateWindow(activationState);

        window.Title = "OpenHabitTracker";

        window.X = 0;
        window.Y = 0;

        // https://stackoverflow.com/questions/67972372/why-are-window-height-and-window-width-not-exact-c-wpf
        window.Width = 1680 + 14;
        window.Height = 1050 + 7 + 31;

        return window;
    }
}
