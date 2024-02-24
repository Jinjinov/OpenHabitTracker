using Ididit.Data;
using Ididit.Data.Models;
using System.Text;

namespace Ididit.Backup.Drive;

public class MarkdownBackup(AppData appData)
{
    private readonly AppData _appData = appData;

    public async Task<string> GetDataExportFileString()
    {
        UserData userData = await _appData.GetUserData();

        StringBuilder stringBuilder = new();

        foreach (CategoryModel category in userData.Categories)
        {
            stringBuilder.AppendLine($"# {category.Title}");
            stringBuilder.AppendLine();

            if (category.Notes is not null)
                foreach (NoteModel note in category.Notes)
                {
                    stringBuilder.AppendLine($"## {note.Title}");
                    stringBuilder.AppendLine();

                    stringBuilder.AppendLine(note.Content);
                    stringBuilder.AppendLine();
                }

            if (category.Tasks is not null)
                foreach (TaskModel task in category.Tasks)
                {
                    stringBuilder.AppendLine($"## {task.Title}");
                    stringBuilder.AppendLine();

                    if (task.Items is not null)
                        foreach (ItemModel item in task.Items)
                        {
                            stringBuilder.AppendLine(item.Title);
                            stringBuilder.AppendLine();
                        }
                }

            if (category.Habits is not null)
                foreach (HabitModel habit in category.Habits)
                {
                    stringBuilder.AppendLine($"## {habit.Title}");
                    stringBuilder.AppendLine();

                    if (habit.Items is not null)
                        foreach (ItemModel item in habit.Items)
                        {
                            stringBuilder.AppendLine(item.Title);
                            stringBuilder.AppendLine();
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

        UserData userData = new();

        CategoryModel category = new();
        NoteModel note = new();

        string? line;
        while ((line = stringReader.ReadLine()) is not null)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            if (line.StartsWith("# "))
            {
                category = new() { Title = line[2..] };
                userData.Categories.Add(category);
            }

            category.Notes ??= new();

            if (line.StartsWith("## "))
            {
                note = new() { Title = line[3..] };
                category.Notes.Add(note);
            }

            note.Content += line;
        }

        await _appData.SetUserData(userData);
    }
}
