using Android.App;
using Android.Content;

namespace OpenHabitTracker.Blazor.Maui;

// Android sends this to the app when the user switches exact alarms on, whether or not the app is
// running, so the pending inexact alarms are replaced by exact ones right away. Nothing is sent
// when it is switched off: the system cancels the app's exact alarms itself, and the next resume
// schedules them again as inexact.
[BroadcastReceiver(Exported = true)]
[IntentFilter([AlarmManager.ActionScheduleExactAlarmPermissionStateChanged])]
public class ExactAlarmPermissionReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        // The broadcast, and with it the process, stays alive until Finish.
        PendingResult? pendingResult = GoAsync();

        Task.Run(async () =>
        {
            await App.RebuildNotificationSchedule();

            pendingResult?.Finish();
        });
    }
}
