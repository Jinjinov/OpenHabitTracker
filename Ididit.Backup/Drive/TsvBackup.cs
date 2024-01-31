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
                                Category = category.Title,
                                Habit = habit.Title,
                                Item = item.Title,
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
                            Category = category.Title,
                            Habit = habit.Title,
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

        await foreach(Record record in records)
        {
            if (userData.Categories.FirstOrDefault(x => x.Title == record.Category) is not CategoryModel category)
            {
                category = new() { Title = record.Category };

                userData.Categories.Add(category);
            }

            category.Habits ??= new();

            if (category.Habits.FirstOrDefault(x => x.Title == record.Habit) is not HabitModel habit)
            {
                habit = new()
                {
                    Title = record.Habit,
                    Priority = record.Priority,
                    RepeatCount = record.RepeatCount,
                    RepeatInterval = record.RepeatInterval,
                    RepeatPeriod = record.RepeatPeriod,
                    Duration = record.Duration
                };

                category.Habits.Add(habit);
            }

            if (string.IsNullOrEmpty(record.Item))
                continue;

            habit.Items ??= new();

            ItemModel item = new() { Title = record.Item };

            habit.Items.Add(item);
        }

        await _appData.SetUserData(userData);
    }

    class Record
    {
        public string Category { get; set; } = string.Empty;
        public string Habit { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public int RepeatCount { get; set; }
        public int RepeatInterval { get; set; }
        public Period RepeatPeriod { get; set; }
        public TimeOnly? Duration { get; set; }
    }
}
