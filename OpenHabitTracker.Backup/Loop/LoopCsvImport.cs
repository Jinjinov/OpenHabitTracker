using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.Globalization;
using System.IO.Compression;

namespace OpenHabitTracker.Backup.Loop;

// Reads the zip that Loop Habit Tracker writes with "Export as CSV": Habits.csv lists the habits, and each habit
// has a folder named by its position with a Checkmarks.csv of its entries.
// Questions, descriptions and entry notes have no place in this app and are left out.
public class LoopCsvImport(IStringLocalizer loc)
{
    private readonly IStringLocalizer _loc = loc;

    private readonly CsvConfiguration _csvConfiguration = new(CultureInfo.InvariantCulture);

    public async Task<ImportedData> ReadDataFile(Stream stream)
    {
        using MemoryStream memoryStream = new();

        await stream.CopyToAsync(memoryStream);

        using ZipArchive archive = new(memoryStream);

        ZipArchiveEntry habitsEntry = archive.GetEntry("Habits.csv") ?? throw new InvalidDataException("The zip has no Habits.csv");

        DateTime now = DateTime.Now;

        CategoryModel uncategorized = new();
        CategoryModel? archived = null;

        // Exports made with a version of Loop that has categories carry a Category column.
        Dictionary<string, CategoryModel> categoriesByTitle = [];

        List<CategoryModel> categories = [uncategorized];

        using (StreamReader streamReader = new(habitsEntry.Open()))
        using (CsvReader csv = new(streamReader, _csvConfiguration))
        {
            await csv.ReadAsync();
            csv.ReadHeader();

            bool hasCategories = csv.HeaderRecord!.Contains("Category");

            while (await csv.ReadAsync())
            {
                string position = csv.GetField("Position") ?? string.Empty;
                string name = (csv.GetField("Name") ?? string.Empty).Trim();
                bool isNumerical = csv.GetField("Type") == "NUMERICAL";
                string unit = isNumerical ? (csv.GetField("Unit") ?? string.Empty).Trim() : string.Empty;

                (int repeatCount, int repeatInterval, Period repeatPeriod) = LoopFormat.Frequency(
                    ParseInt64(csv.GetField("FrequencyNumerator"), 1),
                    ParseInt64(csv.GetField("FrequencyDenominator"), 1));

                HabitModel habit = new()
                {
                    Title = LoopFormat.Title(name, unit),
                    RepeatCount = repeatCount,
                    RepeatInterval = repeatInterval,
                    RepeatPeriod = repeatPeriod,
                    DisplayMetric = isNumerical ? DisplayMetric.Quantity : DisplayMetric.Repetitions,
                    TargetQuantity = isNumerical && double.TryParse(csv.GetField("Target Value"), NumberStyles.Float, CultureInfo.InvariantCulture, out double target) ? target : 1,
                    TimesDone = await ReadCompletions(archive, position, isNumerical),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                habit.LastTimeDoneAt = habit.TimesDone.LastOrDefault()?.CompletedAt;

                if (NearestColor.TryParseHex(csv.GetField("Color"), out string color))
                    habit.Color = color;

                string category = hasCategories ? (csv.GetField("Category") ?? string.Empty).Trim() : string.Empty;

                if (csv.GetField("Archived?") == "true")
                {
                    if (archived is null)
                    {
                        archived = new() { Title = _loc["Archived"] };

                        categories.Add(archived);
                    }

                    archived.Habits.Add(habit);
                }
                else if (category.Length > 0)
                {
                    if (!categoriesByTitle.TryGetValue(category, out CategoryModel? categoryModel))
                    {
                        categoryModel = new() { Title = category };

                        categoriesByTitle[category] = categoryModel;
                        categories.Add(categoryModel);
                    }

                    categoryModel.Habits.Add(habit);
                }
                else
                {
                    uncategorized.Habits.Add(habit);
                }
            }
        }

        MergeOptions options = new()
        {
            MatchCompletionsByDay = true,
            HiddenCategories = archived is null ? [] : [archived]
        };

        return new ImportedData(categories, options);
    }

    // The folder is found by the position, because its name drops characters such as commas, and names can repeat.
    private async Task<List<TimeModel>> ReadCompletions(ZipArchive archive, string position, bool isNumerical)
    {
        ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(x => x.Name == "Checkmarks.csv" && x.FullName.StartsWith(position + " "));

        List<TimeModel> times = [];

        if (entry is null)
            return times;

        using StreamReader streamReader = new(entry.Open());
        using CsvReader csv = new(streamReader, _csvConfiguration);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            if (!DateTime.TryParseExact(csv.GetField("Date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                continue;

            string value = csv.GetField("Value") ?? string.Empty;

            double quantity;

            // YES_AUTO is a day Loop fills in from the frequency, and NO, SKIP and UNKNOWN are not completions.
            if (isNumerical)
            {
                if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long thousandths) || thousandths <= 0)
                    continue;

                quantity = thousandths / LoopFormat.NumericalScale;
            }
            else
            {
                if (value != "YES_MANUAL")
                    continue;

                quantity = 1;
            }

            DateTime day = LoopFormat.Midday(date);

            times.Add(new TimeModel { StartedAt = day, CompletedAt = day, Quantity = quantity });
        }

        // The file lists the newest day first.
        return times.OrderBy(x => x.StartedAt).ToList();
    }

    private static long ParseInt64(string? value, long fallback)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result) ? result : fallback;
    }
}
