namespace OpenHabitTracker.DeviceTests.Windows;

// The client's WindowsDriver reports NATIVE_APP as its only context and refuses to switch,
// so WebView2 pages are reached through AppiumDriver's own context commands, which WinCore answers.
// Replace with the client's WincoreDriver once a release ships one.
public sealed class WincoreDriver(Uri appiumServer, AppiumOptions options, TimeSpan commandTimeout)
    : AppiumDriver(appiumServer, options.ToCapabilities(), commandTimeout);
