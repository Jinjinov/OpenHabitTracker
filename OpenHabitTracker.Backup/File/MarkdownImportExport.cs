using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.Text;

namespace OpenHabitTracker.Backup.File;

public class MarkdownImportExport(ClientData appData)
{
    private readonly ClientData _appData = appData;

    public async Task<string> GetDataExportFileString()
    {
        UserImportExportData userData = await _appData.GetUserData();

        StringBuilder stringBuilder = new();

        foreach (CategoryModel category in userData.Categories)
        {
            stringBuilder.AppendLine($"# {category.Title}");
            stringBuilder.AppendLine();

            if (category.Notes is not null)
            {
                foreach (NoteModel note in category.Notes)
                {
                    stringBuilder.AppendLine($"## {note.Title}");
                    stringBuilder.AppendLine();

                    stringBuilder.AppendLine(note.Content.Replace("\n", "\n\n"));
                    stringBuilder.AppendLine();
                }
            }

            if (category.Tasks is not null)
            {
                foreach (TaskModel task in category.Tasks)
                {
                    stringBuilder.AppendLine($"## {task.Title}");
                    stringBuilder.AppendLine();

                    if (task.Items is not null)
                    {
                        foreach (ItemModel item in task.Items)
                        {
                            stringBuilder.AppendLine($"- {item.Title}");
                        }

                        if (task.Items.Count > 0)
                            stringBuilder.AppendLine();
                    }
                }
            }

            if (category.Habits is not null)
            {
                foreach (HabitModel habit in category.Habits)
                {
                    stringBuilder.AppendLine($"## {habit.Title}");
                    stringBuilder.AppendLine();

                    if (habit.Items is not null)
                    {
                        foreach (ItemModel item in habit.Items)
                        {
                            stringBuilder.AppendLine($"- {item.Title}");
                        }

                        if (habit.Items.Count > 0)
                            stringBuilder.AppendLine();
                    }
                }
            }
        }

        return stringBuilder.ToString();
    }

    public async Task ImportDataFile(Stream stream)
    {
        using StreamReader streamReader = new(stream);

        string content = await streamReader.ReadToEndAsync();

        using StringReader stringReader = new(content);

        UserImportExportData userData = new();

        CategoryModel category = new();
        NoteModel? note = null;
        TaskModel? task = null;
        //HabitModel? habit = null;
        string? newTitle = null;

        string? line;
        while ((line = stringReader.ReadLine()) is not null)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            if (line.StartsWith("# "))
            {
                category = new()
                {
                    Title = line[2..],
                    Notes = [],
                    Tasks = [],
                    Habits = []
                };

                userData.Categories.Add(category);

                continue;
            }

            category.Notes ??= [];
            category.Tasks ??= [];
            category.Habits ??= [];

            if (line.StartsWith("## "))
            {
                newTitle = line[3..];
                continue;
            }

            if (newTitle is not null)
            {
                if (line.StartsWith("- "))
                {
                    task = new() { Title = newTitle };
                    category.Tasks.Add(task);

                    note = null;
                    //habit = null;
                }
                else
                {
                    note = new() { Title = newTitle };
                    category.Notes.Add(note);

                    task = null;
                    //habit = null;
                }

                newTitle = null;
            }

            if (task is not null)
            {
                if (line.StartsWith("- "))
                {
                    task.Items ??= [];

                    ItemModel item = new() { Title = line[2..] };
                    task.Items.Add(item);
                }
            }

            if (note is not null)
            {
                if (!string.IsNullOrEmpty(note.Content))
                    note.Content += "\n";

                note.Content += line;
            }
        }

        await _appData.SetUserData(userData);
    }
}
