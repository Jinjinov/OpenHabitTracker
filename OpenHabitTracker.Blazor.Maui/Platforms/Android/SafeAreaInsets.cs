using Android.Webkit;
using AndroidX.Core.View;
using Java.Interop;
using Insets = AndroidX.Core.Graphics.Insets;
using WebView = Android.Webkit.WebView;

namespace OpenHabitTracker.Blazor.Maui;

// The page is drawn under the navigation bar and the WebView reports 0 for env(safe-area-inset-bottom),
// so the bar's height reaches the page as the --safe-area-inset-bottom CSS variable instead.
// index.html reads it through the JavaScript interface when the page loads, and every insets change
// after that - rotation, a navigation mode change - pushes the new value into the loaded page.
// An insets listener rather than a layout listener: switching between three-button and gesture
// navigation changes the inset without changing the WebView's size.
public class SafeAreaInsets : Java.Lang.Object, IOnApplyWindowInsetsListener
{
    private readonly WebView _webView;

    private int _bottom;

    public SafeAreaInsets(WebView webView)
    {
        _webView = webView;

        _webView.AddJavascriptInterface(this, "safeAreaInsets");

        ViewCompat.SetOnApplyWindowInsetsListener(_webView, this);
        ViewCompat.RequestApplyInsets(_webView);
    }

    // Runs on the WebView's JavaScript thread, so it only reads the value cached on the UI thread.
    [JavascriptInterface]
    [Export("bottom")]
    public int Bottom() => _bottom;

    public WindowInsetsCompat? OnApplyWindowInsets(Android.Views.View? view, WindowInsetsCompat? insets)
    {
        Insets? bars = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());

        if (bars is not null)
            Push((int)Math.Round(bars.Bottom / _webView.Resources!.DisplayMetrics!.Density));

        return insets;
    }

    private void Push(int bottom)
    {
        if (bottom == _bottom)
            return;

        _bottom = bottom;

        _webView.EvaluateJavascript($"document.documentElement.style.setProperty('--safe-area-inset-bottom', '{bottom}px')", null);
    }
}
