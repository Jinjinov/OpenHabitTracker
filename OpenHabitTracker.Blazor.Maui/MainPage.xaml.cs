using Microsoft.AspNetCore.Components.WebView;

namespace OpenHabitTracker.Blazor.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        blazorWebView.BlazorWebViewInitialized += (_, e) => OnBlazorWebViewInitialized(e);
    }

    // Implemented by the platforms that need the WebView itself: Android hands the navigation bar inset to the page.
    partial void OnBlazorWebViewInitialized(BlazorWebViewInitializedEventArgs e);
}
