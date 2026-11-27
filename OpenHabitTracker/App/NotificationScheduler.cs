using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Services;
using System.Text;
using System.Xml;

namespace OpenHabitTracker.App;

// Turns what NotificationSchedule says should fire into localized requests and hands the whole set
// to the platform. Everything that can change the answer calls Rebuild.
public class NotificationScheduler(ClientState clientState, INotifications notifications, IStringLocalizer loc, ILogger<NotificationScheduler> logger) : INotificationScheduler
{
    private readonly ClientState _clientState = clientState;
    private readonly INotifications _notifications = notifications;
    private readonly IStringLocalizer _loc = loc;
    private readonly ILogger<NotificationScheduler> _logger = logger;

    // Android and Apple schedule one request at a time, so a rebuild that started while another was still
    // adding would cancel half of it and leave the other half in place.
    private readonly SemaphoreSlim _rebuildLock = new(1, 1);

    private int _rebuildsAsked;
    private int _rebuildsDone;

    private Task _lastRebuild = Task.CompletedTask;

    public Task Rebuild()
    {
        Task rebuild = RebuildInTurn();

        _lastRebuild = rebuild;

        return rebuild;
    }

    // Ends when every rebuild asked for so far has ended, without asking for another.
    public Task WhenIdle()
    {
        return _lastRebuild;
    }

    // One rebuild at a time, and the calls made while one runs share the next one, which starts after them.
    private async Task RebuildInTurn()
    {
        int asked = Interlocked.Increment(ref _rebuildsAsked);

        await _rebuildLock.WaitAsync();

        try
        {
            if (_rebuildsDone >= asked)
                return;

            int covered = Volatile.Read(ref _rebuildsAsked);

            await RebuildNow();

            _rebuildsDone = covered;
        }
        catch (Exception exception)
        {
            // Most callers do not wait for the task, so the failure is logged here.
            _logger.LogError(exception, "Notification schedule rebuild failed");

            throw;
        }
        finally
        {
            _rebuildLock.Release();
        }
    }

    private async Task RebuildNow()
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

            // No page has applied the app's language in this scope, so the times would take the device's format.
            _clientState.SetCulture();
        }

        SettingsModel settings = _clientState.Settings;

        if (settings.NotificationTime is null && settings.NotificationLeadMinutes is null)
        {
            _logger.LogInformation("Notifications off, cancelling all");

            await Task.Run(_notifications.CancelAll);
            return;
        }

        await _clientState.LoadTasks();
        await _clientState.LoadHabits();

        List<ScheduledNotification> scheduled = NotificationSchedule.Build(
            _clientState.Tasks?.Values ?? Enumerable.Empty<TaskModel>(),
            _clientState.Habits?.Values ?? Enumerable.Empty<HabitModel>(),
            settings,
            DateTime.Now);

        List<NotificationRequest> requests = scheduled.Select(ToRequest).Select(WithoutInvalidCharacters).ToList();

        _logger.LogInformation("Scheduling {Count} notifications from {Tasks} tasks and {Habits} habits, lead {Lead}, summary {Summary}",
            requests.Count, _clientState.Tasks?.Count, _clientState.Habits?.Count, settings.NotificationLeadMinutes, settings.NotificationTime);

        // The requests are worked out above, in the caller's context, where the UI changes the models.
        // The platform's calls go into other processes and take up to seconds, so they run on the thread pool.
        await Task.Run(() => _notifications.Replace(requests));
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

        if (scheduled.Kind == NotificationKind.HabitReminder)
        {
            _clientState.Habits!.TryGetValue(scheduled.HabitId, out HabitModel? habit);

            return new NotificationRequest
            {
                Id = $"habit-{scheduled.HabitId}-{scheduled.NotifyAt:yyyyMMdd}",
                Title = string.IsNullOrEmpty(habit?.Title) ? _loc["Habits"] : habit.Title,
                Body = _clientState.Settings.NotificationLeadMinutes is 0 or null ? string.Empty : habit?.PlannedTime?.ToString("t") ?? string.Empty,
                NotifyAt = scheduled.NotifyAt,
                Route = $"/habits/{scheduled.HabitId}"
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

    // Windows builds each notification as XML, which refuses control characters and a lone half of a
    // surrogate pair, and a pasted title can hold either.
    private static NotificationRequest WithoutInvalidCharacters(NotificationRequest request)
    {
        request.Title = WithoutInvalidCharacters(request.Title);
        request.Body = WithoutInvalidCharacters(request.Body);

        return request;
    }

    private static string WithoutInvalidCharacters(string text)
    {
        StringBuilder result = new(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            if (i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], text[i]))
            {
                result.Append(text, i, 2);
                i++;
            }
            else if (XmlConvert.IsXmlChar(text[i]))
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
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
