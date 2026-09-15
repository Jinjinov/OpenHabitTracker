using Microsoft.AspNetCore.Components.WebView;

namespace OpenHabitTracker.Blazor.Maui;

public partial class MainPage
{
    private SafeAreaInsets? _safeAreaInsets;

    partial void OnBlazorWebViewInitialized(BlazorWebViewInitializedEventArgs e)
    {
        _safeAreaInsets = new(e.WebView);
    }
}
