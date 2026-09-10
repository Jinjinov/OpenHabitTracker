using Microsoft.Extensions.Localization;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;

namespace OpenHabitTracker.App;

// Turns what NotificationSchedule says should fire into localized requests and hands the whole set
// to the platform. Everything that can change the answer calls Rebuild.
public class NotificationScheduler(ClientState clientState, INotifications notifications, IStringLocalizer loc) : INotificationScheduler
{
    private readonly ClientState _clientState = clientState;
    private readonly INotifications _notifications = notifications;
    private readonly IStringLocalizer _loc = loc;

    public async Task Rebuild()
    {
        if (!_notifications.CanNotify)
            return;

        SettingsModel settings = _clientState.Settings;

        if (settings.NotificationHour is null && settings.NotificationLeadMinutes is null)
        {
            await _notifications.CancelAll();
            return;
        }

        await _clientState.LoadTasks();
        await _clientState.LoadHabits();

        List<ScheduledNotification> scheduled = NotificationSchedule.Build(
            _clientState.Tasks?.Values ?? Enumerable.Empty<TaskModel>(),
            _clientState.Habits?.Values ?? Enumerable.Empty<HabitModel>(),
            settings,
            DateTime.Now);

        await _notifications.Replace(scheduled.Select(ToRequest).ToList());
    }

    private NotificationRequest ToRequest(ScheduledNotification scheduled)
    {
        if (scheduled.Kind == NotificationKind.TaskReminder)
        {
            _clientState.Tasks!.TryGetValue(scheduled.TaskId, out TaskModel? task);

            return new NotificationRequest
            {
                Id = $"task-{scheduled.TaskId}",
                Title = task?.Title ?? _loc["Tasks"],
                Body = scheduled.NotifyAt == task?.PlannedAt ? string.Empty : task?.PlannedAt?.ToString("t") ?? string.Empty,
                NotifyAt = scheduled.NotifyAt,
                Route = $"/tasks/{scheduled.TaskId}"
            };
        }

        return new NotificationRequest
        {
            Id = $"digest-{scheduled.NotifyAt:yyyyMMdd}",
            Title = _loc["Due today"],
            Body = DigestBody(scheduled),
            NotifyAt = scheduled.NotifyAt,
            Route = DigestRoute(scheduled)
        };
    }

    // Label and count, never a count in front of a noun: the app has no pluralization mechanism
    // and most of its twenty languages would need agreement.
    private string DigestBody(ScheduledNotification scheduled)
    {
        List<string> parts = new();

        if (scheduled.TaskCount > 0)
            parts.Add($"{_loc["Tasks"]}: {scheduled.TaskCount}");

        if (scheduled.HabitCount > 0)
            parts.Add($"{_loc["Habits"]}: {scheduled.HabitCount}");

        return string.Join(", ", parts);
    }

    private string DigestRoute(ScheduledNotification scheduled)
    {
        if (scheduled.HabitCount == 0)
            return "/tasks";

        if (scheduled.TaskCount == 0)
            return "/habits";

        // Both kinds: the page the user already chose to land on.
        return string.IsNullOrEmpty(_clientState.Settings.StartPage) ? "/" : _clientState.Settings.StartPage;
    }
}
