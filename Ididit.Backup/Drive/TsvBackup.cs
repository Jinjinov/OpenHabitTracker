using CsvHelper;
using CsvHelper.Configuration;
using Ididit.Data;
using Ididit.Data.Models;
using System.Globalization;

namespace Ididit.Backup.Drive;

public class TsvBackup(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly CsvConfiguration _csvConfiguration = new(CultureInfo.InvariantCulture)
    {
        Delimiter = "\t",
        Quote = (char)1,
        Mode = CsvMode.NoEscape
    };

    public async Task<string> GetDataExportFileString()
    {
        UserData userData = await _appData.GetUserData();

        List<Record> records = new();

        foreach (CategoryModel category in userData.Categories)
        {
            if (category.Notes?.Count > 0)
            {
                foreach (NoteModel note in category.Notes)
                {
                    records.Add(new Record
                    {
                        ContentType = ContentType.Note,
                        Category = category.Title,
                        Title = note.Title,
                        Content = note.Content,
                        Priority = note.Priority
                    });
                }
            }
            if (category.Tasks?.Count > 0)
            {
                foreach (TaskModel task in category.Tasks)
                {
                    if (task.Items?.Count > 0)
                    {
                        foreach (ItemModel item in task.Items)
                        {
                            records.Add(new Record
                            {
                                ContentType = ContentType.Task,
                                Category = category.Title,
                                Title = task.Title,
                                Content = item.Title,
                                Priority = task.Priority,
                                PlannedAt = task.PlannedAt,
                                Duration = task.Duration
                            });
                        }
                    }
                    else
                    {
                        records.Add(new Record
                        {
                            ContentType = ContentType.Task,
                            Category = category.Title,
                            Title = task.Title,
                            Priority = task.Priority,
                            PlannedAt = task.PlannedAt,
                            Duration = task.Duration
                        });
                    }
                }
            }
            if (category.Habits?.Count > 0)
            {
                foreach (HabitModel habit in category.Habits)
                {
                    if (habit.Items?.Count > 0)
                    {
                        foreach (ItemModel item in habit.Items)
                        {
                            records.Add(new Record
                            {
                                ContentType = ContentType.Habit,
                                Category = category.Title,
                                Title = habit.Title,
                                Content = item.Title,
                                Priority = habit.Priority,
                                RepeatCount = habit.RepeatCount,
                                RepeatInterval = habit.RepeatInterval,
                                RepeatPeriod = habit.RepeatPeriod,
                                Duration = habit.Duration
                            });
                        }
                    }
                    else
                    {
                        records.Add(new Record
                        {
                            ContentType = ContentType.Habit,
                            Category = category.Title,
                            Title = habit.Title,
                            Priority = habit.Priority,
                            RepeatCount = habit.RepeatCount,
                            RepeatInterval = habit.RepeatInterval,
                            RepeatPeriod = habit.RepeatPeriod,
                            Duration = habit.Duration
                        });
                    }
                }
            }
        }

        using StringWriter stringWriter = new();

        using (CsvWriter csvWriter = new(stringWriter, _csvConfiguration))
        {
            csvWriter.WriteRecords(records);
        }

        return stringWriter.ToString();
    }

    public async Task ImportDataFile(Stream stream)
    {
        UserData userData = new();

        using StreamReader reader = new(stream);

        using CsvReader csv = new(reader, _csvConfiguration);

        // Microsoft.AspNetCore.Components.Forms
        // internal sealed class BrowserFileStream : Stream
        // public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Synchronous reads are not supported.");

        IAsyncEnumerable<Record> records = csv.GetRecordsAsync<Record>();

        DateTime now = DateTime.Now;

        await foreach (Record record in records)
        {
            if (userData.Categories.FirstOrDefault(x => x.Title == record.Category) is not CategoryModel category)
            {
                category = new() { Title = record.Category };

                userData.Categories.Add(category);
            }

            category.Notes ??= new();
            category.Tasks ??= new();
            category.Habits ??= new();

            if (record.ContentType == ContentType.Note && category.Notes.FirstOrDefault(x => x.Title == record.Title) is not NoteModel note)
            {
                note = new()
                {
                    Title = record.Title,
                    Priority = record.Priority,
                    Content = record.Content,

                    CreatedAt = now,
                    UpdatedAt = now
                };

                category.Notes.Add(note);

                continue;
            }

            ItemsModel? items = null;

            if (record.ContentType == ContentType.Task && category.Tasks.FirstOrDefault(x => x.Title == record.Title) is not TaskModel task)
            {
                task = new()
                {
                    Title = record.Title,
                    Priority = record.Priority,
                    PlannedAt = record.PlannedAt,
                    Duration = record.Duration,

                    CreatedAt = now,
                    UpdatedAt = now
                };

                category.Tasks.Add(task);

                items = task;
            }
            else if (record.ContentType == ContentType.Habit && category.Habits.FirstOrDefault(x => x.Title == record.Title) is not HabitModel habit)
            {
                habit = new()
                {
                    Title = record.Title,
                    Priority = record.Priority,
                    RepeatCount = record.RepeatCount,
                    RepeatInterval = record.RepeatInterval,
                    RepeatPeriod = record.RepeatPeriod,
                    Duration = record.Duration,

                    CreatedAt = now,
                    UpdatedAt = now
                };

                category.Habits.Add(habit);

                items = habit;
            }

            if (items is not null && !string.IsNullOrEmpty(record.Content))
            {
                items.Items ??= new();

                ItemModel item = new() { Title = record.Content };

                items.Items.Add(item);
            }
        }

        await _appData.SetUserData(userData);
    }

    class Record
    {
        public ContentType ContentType { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public TimeOnly? Duration { get; set; }
        public DateTime? PlannedAt { get; set; }
        public int RepeatCount { get; set; }
        public int RepeatInterval { get; set; }
        public Period RepeatPeriod { get; set; }
    }
}
