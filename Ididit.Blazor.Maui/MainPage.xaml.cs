using Microsoft.AspNetCore.Components.WebView;

namespace Ididit.Blazor.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        blazorWebView.UrlLoading += OnUrlLoading;
    }

    private void OnUrlLoading(object? sender, UrlLoadingEventArgs e)
    {
        Uri uri = e.Url;

        if (!uri.Host.Contains("0.0.0.0"))
        {
            e.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;

            Browser.OpenAsync(uri, BrowserLaunchMode.External);
        }
    }
}
