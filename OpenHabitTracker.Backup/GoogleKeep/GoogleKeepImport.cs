using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.IO.Compression;
using System.Text.Json;

namespace OpenHabitTracker.Backup.GoogleKeep;

public class GoogleKeepImport(AppData appData)
{
    private readonly AppData _appData = appData;

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

    public async Task ImportDataFile(Stream stream)
    {
        List<GoogleKeepNote> googleKeepNotes = await GetGoogleKeepNotes(stream);

        UserData userData = new();

        CategoryModel? category = null;
        NoteModel? note = null;
        TaskModel? task = null;
        //HabitModel? habit = null;

        DateTime now = DateTime.Now;

        foreach (GoogleKeepNote googleKeepNote in googleKeepNotes.OrderByDescending(gkn => gkn.CreatedTimestampUsec))
        {
            if (googleKeepNote.Labels.Count > 0)
            {
                Label label = googleKeepNote.Labels.First();

                if (userData.Categories.FirstOrDefault(x => x.Title == label.Name) is CategoryModel categoryModel)
                {
                    category = categoryModel;
                }
                else
                {
                    category = new() { Title = label.Name };

                    userData.Categories.Add(category);
                }
            }

            if (category is null)
            {
                category = new();

                userData.Categories.Add(category);
            }

            category.Notes ??= [];
            category.Tasks ??= [];
            category.Habits ??= [];

            if (googleKeepNote.ListContent.Count == 0)
            {
                note = new()
                {
                    Title = googleKeepNote.Title,
                    Content = googleKeepNote.TextContent,
                    IsDeleted = googleKeepNote.IsTrashed,
                    Color = _bootstrapClass[googleKeepNote.Color],
                    CreatedAt = new DateTime(googleKeepNote.CreatedTimestampUsec),
                    UpdatedAt = new DateTime(googleKeepNote.UserEditedTimestampUsec)
                };

                category.Notes.Add(note);
            }
            else
            {
                task = new()
                {
                    Title = googleKeepNote.Title,
                    IsDeleted = googleKeepNote.IsTrashed,
                    Color = _bootstrapClass[googleKeepNote.Color],
                    CreatedAt = new DateTime(googleKeepNote.CreatedTimestampUsec),
                    UpdatedAt = new DateTime(googleKeepNote.UserEditedTimestampUsec)
                };

                if (googleKeepNote.ListContent.Count > 0)
                {
                    task.Items = [];

                    foreach (ListContent listContent in googleKeepNote.ListContent)
                    {
                        ItemModel item = new()
                        {
                            Title = listContent.Text,
                            DoneAt = listContent.IsChecked ? now : null
                        };

                        task.Items.Add(item);
                    }
                }

                category.Tasks.Add(task);
            }
        }

        await _appData.SetUserData(userData);
    }

    private static async Task<List<GoogleKeepNote>> GetGoogleKeepNotes(Stream stream)
    {
        MemoryStream memoryStream = new();

        await stream.CopyToAsync(memoryStream);

        ZipArchive archive = new(memoryStream);

        List<GoogleKeepNote> googleKeepNotes = [];

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                await using Stream jsonStream = entry.Open();

                using StreamReader streamReader = new(jsonStream);

                string jsonText = await streamReader.ReadToEndAsync();

                GoogleKeepNote? googleKeepNote = JsonSerializer.Deserialize<GoogleKeepNote>(jsonText);

                if (googleKeepNote is not null)
                {
                    googleKeepNotes.Add(googleKeepNote);
                }
            }
        }

        return googleKeepNotes;
    }
}
