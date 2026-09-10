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

    // DailyDigest only.
    public int TaskCount { get; set; }

    public int HabitCount { get; set; }
}

// Describes what should fire and when; the text and the route are composed by the caller,
// so these results stay assertable without any wording in them.
public static class NotificationSchedule
{
    // Long enough to outlast a holiday; past that, silence rather than nagging.
    public const int HorizonDays = 14;

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

                int taskCount = candidateTasks.Count(task => IsInDigest(task, day, notifyAt, settings));
                int habitCount = candidateHabits.Count(habit => IsDue(habit, notifyAt, settings));

                if (taskCount + habitCount > 0)
                    result.Add(new ScheduledNotification { NotifyAt = notifyAt, Kind = NotificationKind.DailyDigest, TaskCount = taskCount, HabitCount = habitCount });
            }
        }

        return result.OrderBy(notification => notification.NotifyAt).ToList();
    }

    // The digest covers what the lead time will not, so a timed task later the same day is left
    // to its own reminder and never announced twice.
    private static bool IsInDigest(TaskModel task, DateTime day, DateTime notifyAt, SettingsModel settings)
    {
        if (task.PlannedTime is null && task.PlannedAt!.Value.Date == day)
            return true;

        return settings.NotificationIncludeOverdueTasks && task.PlannedAt!.Value < notifyAt;
    }

    private static bool IsDue(HabitModel habit, DateTime notifyAt, SettingsModel settings)
    {
        double ratio = GetRatioAt(habit, settings.SelectedRatio, notifyAt);

        // An average interval of zero makes the elapsed-to-average ratio non-finite; a habit with
        // no history to average has no meaningful urgency, so it is not something to notify about.
        return double.IsFinite(ratio) && ratio >= settings.NotificationHabitThreshold;
    }

    // The ratios only move one way with time, so the value at a future instant is exact arithmetic
    // over the same inputs HabitModel uses; a completion is the only thing that lowers it, and a
    // completion rebuilds the schedule.
    private static double GetRatioAt(HabitModel habit, Ratio ratio, DateTime at)
    {
        TimeSpan elapsed = habit.LastTimeDoneAt.HasValue
            ? at - habit.LastTimeDoneAt.Value
            : new TimeSpan(Math.Max(0L, (at - (habit.StartAt ?? habit.CreatedAt)).Ticks));

        return ratio switch
        {
            Ratio.ElapsedToAverage => elapsed / habit.AverageInterval * 100.0,
            Ratio.ElapsedToDesired => elapsed / habit.GetRepeatInterval() * 100.0,
            Ratio.AverageToDesired => habit.AverageInterval / habit.GetRepeatInterval() * 100.0,
            _ => throw new ArgumentOutOfRangeException(nameof(ratio))
        };
    }
}
