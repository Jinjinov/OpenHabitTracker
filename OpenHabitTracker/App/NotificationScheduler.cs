using Microsoft.Extensions.Localization;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
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

        // A rebuild from outside the UI runs in a scope of its own, whose ClientState has loaded
        // nothing. The settings are read rather than loaded, so that a first launch, where the UI
        // is creating the settings row at this moment, does not create a second one here.
        if (_clientState.Settings.Id == 0)
        {
            IReadOnlyList<SettingsEntity> saved = await _clientState.DataAccess.GetSettings();

            if (saved.Count == 0)
                return;

            _clientState.Settings = saved[0].ToModel();
        }

        SettingsModel settings = _clientState.Settings;

        if (settings.NotificationTime is null && settings.NotificationLeadMinutes is null)
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

    // One line per kind, label and titles, so the collapsed notification already reads as a list
    // of what is due. A label with the titles behind it needs no plural form, which the app has no
    // mechanism for in its twenty languages.
    private string DigestBody(ScheduledNotification scheduled)
    {
        List<string> lines = new();

        if (scheduled.TaskCount > 0)
            lines.Add($"{_loc["Tasks"]}: {string.Join(", ", scheduled.TaskIds.Select(taskId => TaskLabel(taskId, scheduled.NotifyAt)))}");

        if (scheduled.HabitCount > 0)
            lines.Add($"{_loc["Habits"]}: {string.Join(", ", scheduled.HabitIds.Select(HabitLabel))}");

        return string.Join("\n", lines);
    }

    // The title alone for a task planned for the digest's day; the time when it has one, and the
    // date when it is overdue from an earlier day, since either is what the reader would ask next.
    private string TaskLabel(long taskId, DateTime notifyAt)
    {
        if (!_clientState.Tasks!.TryGetValue(taskId, out TaskModel? task) || task.PlannedAt is not DateTime plannedAt)
            return _loc["Tasks"];

        List<string> parts = new() { task.Title };

        if (plannedAt.Date != notifyAt.Date)
            parts.Add(plannedAt.ToString("d"));

        if (task.PlannedTime is not null)
            parts.Add(plannedAt.ToString("t"));

        return string.Join(" ", parts);
    }

    private string HabitLabel(long habitId)
    {
        return _clientState.Habits!.TryGetValue(habitId, out HabitModel? habit) ? habit.Title : _loc["Habits"];
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
