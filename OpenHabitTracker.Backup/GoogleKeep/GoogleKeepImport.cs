using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace OpenHabitTracker.Backup.GoogleKeep;

public class GoogleKeepImport(IStringLocalizer loc)
{
    private readonly IStringLocalizer _loc = loc;

    //readonly Dictionary<string, string> _namedColors = new()
    //{
    //    { "DEFAULT", "transparent" },
    //    { "BLUE", "blue" },
    //    { "BROWN", "brown" },
    //    { "CERULEAN", "steelblue" },
    //    { "GRAY", "gray" },
    //    { "GREEN", "green" },
    //    { "ORANGE", "orange" },
    //    { "PINK", "pink" },
    //    { "PURPLE", "purple" },
    //    { "RED", "red" },
    //    { "TEAL", "teal" },
    //    { "YELLOW", "yellow" },
    //};

    readonly Dictionary<string, string> _bootstrapClass = new()
    {
        { "DEFAULT", "bg-transparent" },
        { "BLUE", "bg-primary-subtle" },
        { "BROWN", "bg-dark-subtle" },
        { "CERULEAN", "bg-light-subtle" },
        { "GRAY", "bg-secondary-subtle" },
        { "GREEN", "bg-success-subtle" },
        { "ORANGE", "bg-body-tertiary" },
        { "PINK", "bg-body-secondary" },
        { "PURPLE", "bg-body" },
        { "RED", "bg-danger-subtle" },
        { "TEAL", "bg-info-subtle" },
        { "YELLOW", "bg-warning-subtle" },
    };

    public async Task<ImportedData> ReadDataFile(Stream stream)
    {
        (List<GoogleKeepNote> googleKeepNotes, Dictionary<string, Reminder> reminders) = await GetGoogleKeepNotes(stream);

        List<CategoryModel> categories = [];
        Dictionary<NoteModel, string> formerContents = [];

        CategoryModel? uncategorized = null;
        CategoryModel? archived = null;
        NoteModel? note = null;
        TaskModel? task = null;
        HabitModel? habit = null;

        DateTime now = DateTime.Now;

        foreach (GoogleKeepNote googleKeepNote in googleKeepNotes.OrderByDescending(gkn => gkn.CreatedTimestampUsec))
        {
            CategoryModel category;

            if (googleKeepNote.IsArchived)
            {
                if (archived is null)
                {
                    archived = new() { Title = _loc["Archived"] };

                    categories.Add(archived);
                }

                category = archived;
            }
            else if (googleKeepNote.Labels.Count > 0)
            {
                Label label = googleKeepNote.Labels.First();

                if (categories.FirstOrDefault(x => x.Title == label.Name) is CategoryModel categoryModel)
                {
                    category = categoryModel;
                }
                else
                {
                    category = new() { Title = label.Name };

                    categories.Add(category);
                }
            }
            else
            {
                if (uncategorized is null)
                {
                    uncategorized = new();

                    categories.Add(uncategorized);
                }

                category = uncategorized;
            }

            // Without Tasks.json in the Takeout, a note with a reminder is imported without it.
            Reminder? reminder = googleKeepNote.Tasks.Select(x => reminders.GetValueOrDefault(GetTaskId(x.Id))).FirstOrDefault(x => x is not null);

            if (reminder?.Interval is Interval interval)
            {
                habit = new()
                {
                    Title = googleKeepNote.Title,
                    Priority = googleKeepNote.IsPinned ? Priority.VeryHigh : Priority.None,
                    IsDeleted = googleKeepNote.IsTrashed,
                    Color = GetBootstrapClass(googleKeepNote.Color),
                    CreatedAt = DateTime.UnixEpoch.AddTicks(googleKeepNote.CreatedTimestampUsec * 10).ToLocalTime(),
                    UpdatedAt = DateTime.UnixEpoch.AddTicks(googleKeepNote.UserEditedTimestampUsec * 10).ToLocalTime(),
                    StartAt = GetLocalTime(reminder.Start),
                    Items = GetItems(googleKeepNote, now)
                };

                SetRepeat(habit, interval);

                category.Habits.Add(habit);
            }
            else if (googleKeepNote.ListContent.Count == 0 && reminder is null)
            {
                note = new()
                {
                    Title = googleKeepNote.Title,
                    Content = GetContent(googleKeepNote),
                    Priority = googleKeepNote.IsPinned ? Priority.VeryHigh : Priority.None,
                    IsDeleted = googleKeepNote.IsTrashed,
                    Color = GetBootstrapClass(googleKeepNote.Color),
                    CreatedAt = DateTime.UnixEpoch.AddTicks(googleKeepNote.CreatedTimestampUsec * 10).ToLocalTime(),
                    UpdatedAt = DateTime.UnixEpoch.AddTicks(googleKeepNote.UserEditedTimestampUsec * 10).ToLocalTime()
                };

                // The importer used to take textContent as it is, without the formatting and the links.
                if (note.Content != googleKeepNote.TextContent)
                    formerContents[note] = googleKeepNote.TextContent;

                category.Notes.Add(note);
            }
            else
            {
                task = new()
                {
                    Title = googleKeepNote.Title,
                    Priority = googleKeepNote.IsPinned ? Priority.VeryHigh : Priority.None,
                    IsDeleted = googleKeepNote.IsTrashed,
                    Color = GetBootstrapClass(googleKeepNote.Color),
                    CreatedAt = DateTime.UnixEpoch.AddTicks(googleKeepNote.CreatedTimestampUsec * 10).ToLocalTime(),
                    UpdatedAt = DateTime.UnixEpoch.AddTicks(googleKeepNote.UserEditedTimestampUsec * 10).ToLocalTime(),
                    PlannedAt = reminder is null ? null : GetPlannedAt(reminder.Start),
                    Items = GetItems(googleKeepNote, now)
                };

                category.Tasks.Add(task);
            }
        }

        MergeOptions options = new()
        {
            HiddenCategories = archived is null ? [] : [archived],
            FormerContents = formerContents
        };

        return new(categories, options);
    }

    // Tasks and habits have no text, so a text note brings its lines as items.
    private static List<ItemModel> GetItems(GoogleKeepNote googleKeepNote, DateTime now)
    {
        if (googleKeepNote.ListContent.Count == 0)
            return googleKeepNote.TextContent.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Select(x => new ItemModel { Title = x }).ToList();

        // Keep keeps an empty item at the end of a checklist for the next entry.
        return googleKeepNote.ListContent
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => new ItemModel { Title = x.Text, DoneAt = x.IsChecked ? now : null })
            .ToList();
    }

    // Tasks.json names the task of a reminder by the id the note gives it, in base64url without padding.
    private static string GetTaskId(string keepTaskId)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(keepTaskId)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    // The start is in UTC, with a fraction of a second the app does not show.
    private static DateTime GetLocalTime(DateTime start)
    {
        DateTime local = start.ToLocalTime();

        return local.AddTicks(-(local.Ticks % TimeSpan.TicksPerMinute));
    }

    // An all-day reminder starts at local midnight.
    private static DateTime GetPlannedAt(DateTime start)
    {
        DateTime local = GetLocalTime(start);

        return local.TimeOfDay == TimeSpan.Zero ? local.Date + TaskModel.DateOnlySentinel : local;
    }

    // A habit repeats a number of times per interval, so the weekdays of a weekly reminder become its count,
    // and the day of the month, the day of the year and the end of the repeat are left out.
    private static void SetRepeat(HabitModel habit, Interval interval)
    {
        habit.RepeatInterval = Math.Max(1, interval.IntervalMultiplier);

        (habit.RepeatCount, habit.RepeatPeriod) = interval switch
        {
            { Weekly: WeeklyInterval weekly } => (Math.Max(1, weekly.DayOfWeek.Count), Period.Week),
            { Monthly: not null } => (1, Period.Month),
            { Yearly: not null } => (1, Period.Year),
            _ => (1, Period.Day)
        };
    }

    private static string GetContent(GoogleKeepNote googleKeepNote)
    {
        // textContent is the same text without the formatting.
        string text = string.IsNullOrEmpty(googleKeepNote.TextContentHtml) ? googleKeepNote.TextContent : GoogleKeepMarkdown.FromHtml(googleKeepNote.TextContentHtml);

        // Keep stores the preview of a link as an annotation, mostly of a link that is in the text already.
        IEnumerable<string> links = googleKeepNote.Annotations
            .Where(annotation => !string.IsNullOrWhiteSpace(annotation.Url) && !ContainsUrl(text, annotation.Url))
            .Select(annotation => string.IsNullOrWhiteSpace(annotation.Title) ? annotation.Url : $"[{EscapeLinkText(annotation.Title)}]({annotation.Url})");

        // A category holds the first label, or no label when the note is archived, and the last line lists the labels it does not hold.
        IEnumerable<Label> otherLabels = googleKeepNote.IsArchived ? googleKeepNote.Labels : googleKeepNote.Labels.Skip(1);

        string labels = string.Join(" ", otherLabels.Select(label => $"`{label.Name}`"));

        return string.Join("\n", new[] { text }.Concat(links).Append(labels).Where(line => !string.IsNullOrEmpty(line)));
    }

    // The text can hold the link without its scheme, www or the trailing slash.
    private static bool ContainsUrl(string text, string url)
    {
        string address = url.Trim().TrimEnd('/');

        int scheme = address.IndexOf("://", StringComparison.Ordinal);

        if (scheme >= 0)
            address = address[(scheme + 3)..];

        if (address.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            address = address[4..];

        return text.Contains(address, StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeLinkText(string text)
    {
        return text.Replace("[", "\\[").Replace("]", "\\]");
    }

    // A color name Keep adds later falls back to the default instead of failing the whole import.
    private string GetBootstrapClass(string color)
    {
        return _bootstrapClass.GetValueOrDefault(color, _bootstrapClass["DEFAULT"]);
    }

    private static async Task<(List<GoogleKeepNote> Notes, Dictionary<string, Reminder> Reminders)> GetGoogleKeepNotes(Stream stream)
    {
        using MemoryStream memoryStream = new();

        await stream.CopyToAsync(memoryStream);

        using ZipArchive archive = new(memoryStream);

        List<GoogleKeepNote> googleKeepNotes = [];
        Dictionary<string, Reminder> reminders = [];

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                await using Stream jsonStream = entry.Open();

                using StreamReader streamReader = new(jsonStream);

                string jsonText = await streamReader.ReadToEndAsync();

                if (GoogleKeepNote.IsNote(jsonText))
                {
                    GoogleKeepNote? googleKeepNote = JsonSerializer.Deserialize<GoogleKeepNote>(jsonText);

                    if (googleKeepNote is not null)
                    {
                        googleKeepNotes.Add(googleKeepNote);
                    }
                }
                else if (GoogleTaskLists.IsTaskLists(jsonText) && JsonSerializer.Deserialize<GoogleTaskLists>(jsonText) is GoogleTaskLists taskLists)
                {
                    AddReminders(taskLists, reminders);
                }
            }
        }

        return (googleKeepNotes, reminders);
    }

    // A repeating task names its repeat rule among the recurrences of its list.
    private static void AddReminders(GoogleTaskLists taskLists, Dictionary<string, Reminder> reminders)
    {
        foreach (GoogleTaskList taskList in taskLists.Items)
        {
            Dictionary<string, Interval> intervals = [];

            foreach (GoogleTaskRecurrence recurrence in taskList.Recurrences)
                intervals[recurrence.Id] = recurrence.Schedule.Interval;

            foreach (GoogleTask googleTask in taskList.Items)
            {
                if ((googleTask.ScheduledTime.FirstOrDefault(x => x.Current) ?? googleTask.ScheduledTime.FirstOrDefault()) is ScheduledTime scheduledTime)
                {
                    Interval? interval = googleTask.TaskRecurrenceId is string recurrenceId ? intervals.GetValueOrDefault(recurrenceId) : null;

                    reminders[googleTask.Id] = new(scheduledTime.Start, interval);
                }
            }
        }
    }

    private sealed record Reminder(DateTime Start, Interval? Interval);
}
