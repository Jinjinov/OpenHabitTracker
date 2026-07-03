using OpenHabitTracker.Services;
using Plugin.Maui.AppRating;

namespace OpenHabitTracker.Blazor.Maui;

public class AppReview : IAppReview
{
    // State is device-local (Preferences, not the synced database) on purpose:
    // a review is wanted once per store, so each device counts its own completions.
    private const string CompletionCountKey = "ReviewPromptCompletionCount";
    private const string PromptShownKey = "ReviewPromptShown";
    private const int CompletionCountTarget = 10;

    public async Task RecordHabitCompletion()
    {
        if (Preferences.Default.Get(PromptShownKey, false))
            return;

        int completionCount = Preferences.Default.Get(CompletionCountKey, 0) + 1;
        Preferences.Default.Set(CompletionCountKey, completionCount);

        if (completionCount < CompletionCountTarget)
            return;

        Preferences.Default.Set(PromptShownKey, true);

        // On Android the dialog appears only when the app is installed from Google Play; the OS may also silently skip it (quota)
        await MainThread.InvokeOnMainThreadAsync(() => AppRating.Default.PerformInAppRateAsync());
    }
}
