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

        List<CsvRow> csvRows = new();

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
                            csvRows.Add(new CsvRow
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
                        csvRows.Add(new CsvRow
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
            csvWriter.WriteRecords(csvRows);
        }

        return stringWriter.ToString();
    }

    public async Task ImportDataFile(Stream stream)
    {
        UserData userData = null;

        await _appData.SetUserData(userData);
    }

    class CsvRow
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
