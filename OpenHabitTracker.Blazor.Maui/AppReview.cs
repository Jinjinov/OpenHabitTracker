using OpenHabitTracker.Services;
using Plugin.Maui.AppRating;

namespace OpenHabitTracker.Blazor.Maui;

public class AppReview : IAppReview
{
    // State is device-local (Preferences, not the synced database) on purpose:
    // a review is wanted once per store, so each device counts its own engagement.
    private const string EngagementPointsKey = "ReviewPromptEngagementPoints";
    private const string ActiveDaysKey = "ReviewPromptActiveDays";
    private const string LastActiveDayKey = "ReviewPromptLastActiveDay";
    private const string PromptShownKey = "ReviewPromptShown";

    // 10 habit/task completions, or 30 notes/tasks/habits created, or any mix
    private const int EngagementPointsTarget = 30;
    // distinct local dates with at least one engagement event - keeps a day-one
    // "played with the examples" burst from triggering the once-ever prompt
    private const int ActiveDaysTarget = 5;

    public async Task RecordEngagement(EngagementKind kind)
    {
        if (Preferences.Default.Get(PromptShownKey, false))
            return;

        DateTime today = DateTime.Today;
        int activeDays = Preferences.Default.Get(ActiveDaysKey, 0);

        if (Preferences.Default.Get(LastActiveDayKey, DateTime.MinValue) != today)
        {
            activeDays++;
            Preferences.Default.Set(LastActiveDayKey, today);
            Preferences.Default.Set(ActiveDaysKey, activeDays);
        }

        int points = Preferences.Default.Get(EngagementPointsKey, 0) + (kind == EngagementKind.Completed ? 3 : 1);
        Preferences.Default.Set(EngagementPointsKey, points);

        if (points < EngagementPointsTarget || activeDays < ActiveDaysTarget)
            return;

        Preferences.Default.Set(PromptShownKey, true);

        // On Android the dialog appears only when the app is installed from Google Play; the OS may also silently skip it (quota)
        await MainThread.InvokeOnMainThreadAsync(() => AppRating.Default.PerformInAppRateAsync());
    }
}
