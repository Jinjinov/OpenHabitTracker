using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Data;

public enum NotificationKind
{
    TaskReminder,
    DailyDigest,
}

public class ScheduledNotification
{
    public DateTime NotifyAt { get; set; }

    public NotificationKind Kind { get; set; }

    // TaskReminder only.
    public long TaskId { get; set; }

    // DailyDigest only: what it lists, most urgent first.
    public List<long> TaskIds { get; set; } = new();

    public List<long> HabitIds { get; set; } = new();

    public int TaskCount => TaskIds.Count;

    public int HabitCount => HabitIds.Count;
}

// Describes what should fire and when; the text and the route are composed by the caller,
// so these results stay assertable without any wording in them.
public static class NotificationSchedule
{
    // Long enough that an app left unopened for a month keeps summarising; past that, silence
    // rather than nagging. Also half of Apple's 64 pending requests, so at the limit the split is
    // even - and every day with nothing due leaves its slot to a task reminder instead.
    public const int HorizonDays = 32;

    public static List<ScheduledNotification> Build(IEnumerable<TaskModel> tasks, IEnumerable<HabitModel> habits, SettingsModel settings, DateTime now)
    {
        List<ScheduledNotification> result = new();

        List<TaskModel> candidateTasks = settings.NotificationContent == DigestContent.Habits
            ? new()
            : tasks.Where(task => !task.IsDeleted && task.CompletedAt is null && task.PlannedAt.HasValue && task.Priority >= settings.NotificationMinimumPriority).ToList();

        List<HabitModel> candidateHabits = settings.NotificationContent == DigestContent.Tasks
            ? new()
            : habits.Where(habit => !habit.IsDeleted && habit.Priority >= settings.NotificationMinimumPriority).ToList();

        if (settings.NotificationLeadMinutes is int leadMinutes)
        {
            foreach (TaskModel task in candidateTasks.Where(task => task.PlannedTime is not null))
            {
                DateTime notifyAt = task.PlannedAt!.Value.AddMinutes(-leadMinutes);

                // A moment already past is never scheduled, which is the same rule as announcing
                // nothing that fell due while the app was closed.
                if (notifyAt > now)
                    result.Add(new ScheduledNotification { NotifyAt = notifyAt, Kind = NotificationKind.TaskReminder, TaskId = task.Id });
            }
        }

        if (settings.NotificationTime is TimeOnly summaryTime)
        {
            for (int dayOffset = 0; dayOffset < HorizonDays; dayOffset++)
            {
                DateTime day = now.Date.AddDays(dayOffset);
                DateTime notifyAt = day.Add(summaryTime.ToTimeSpan());

                if (notifyAt <= now)
                    continue;

                List<long> taskIds = candidateTasks
                    .Where(task => IsInDigest(task, day, notifyAt, settings))
                    .OrderBy(task => task.PlannedAt)
                    .ThenBy(task => task.Title)
                    .Select(task => task.Id)
                    .ToList();

                List<long> habitIds = candidateHabits
                    .Where(habit => IsScheduledAndNotDone(habit, day) || IsDue(habit, notifyAt, settings))
                    .OrderByDescending(habit => habit.GetRatioAt(settings.SelectedRatio, notifyAt))
                    .ThenBy(habit => habit.Title)
                    .Select(habit => habit.Id)
                    .ToList();

                if (taskIds.Count + habitIds.Count > 0)
                    result.Add(new ScheduledNotification { NotifyAt = notifyAt, Kind = NotificationKind.DailyDigest, TaskIds = taskIds, HabitIds = habitIds });
            }
        }

        return result.OrderBy(notification => notification.NotifyAt).ToList();
    }

    // The digest covers what the lead time will not, so with reminders on a timed task later the
    // same day is left to its own reminder and never announced twice.
    private static bool IsInDigest(TaskModel task, DateTime day, DateTime notifyAt, SettingsModel settings)
    {
        if (task.PlannedAt!.Value.Date == day && (task.PlannedTime is null || settings.NotificationLeadMinutes is null))
            return true;

        return settings.NotificationIncludeOverdueTasks && task.PlannedAt!.Value < notifyAt;
    }

    // A completion is the only thing that lowers a ratio, and a completion rebuilds the schedule.
    private static bool IsDue(HabitModel habit, DateTime notifyAt, SettingsModel settings)
    {
        double ratio = habit.GetRatioAt(settings.SelectedRatio, notifyAt);

        // An average interval of zero makes the elapsed-to-average ratio non-finite; a habit with
        // no history to average has no meaningful urgency, so it is not something to notify about.
        return double.IsFinite(ratio) && ratio >= settings.NotificationHabitThreshold;
    }

    // A habit is in the summary of each day it is meant for, as an untimed task is on its planned day,
    // whatever its ratio.
    private static bool IsScheduledAndNotDone(HabitModel habit, DateTime day)
    {
        if (!HabitSchedule.IsScheduledOn(habit.Rule, day))
            return false;

        if (habit.StartAt is DateTime startAt && day < startAt.Date)
            return false;

        return habit.LastTimeDoneAt is not DateTime doneAt || doneAt.Date < day;
    }
}
