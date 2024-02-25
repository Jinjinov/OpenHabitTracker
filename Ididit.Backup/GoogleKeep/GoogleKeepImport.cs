using Ididit.Data;
using Ididit.Data.Models;
using System.IO.Compression;
using System.Text.Json;

namespace Ididit.Backup.GoogleKeep;

public class GoogleKeepImport(AppData appData)
{
    private readonly AppData _appData = appData;

    public async Task ImportDataFile(Stream stream)
    {
        List<GoogleKeepNote> googleKeepNotes = await GetGoogleKeepNotes(stream);

        UserData userData = new();

        CategoryModel category = new();
        NoteModel? note = null;
        TaskModel? task = null;
        HabitModel? habit = null;

        foreach (GoogleKeepNote googleKeepNote in googleKeepNotes.OrderByDescending(gkn => gkn.CreatedTimestampUsec))
        {
            category.Notes ??= new();
            category.Tasks ??= new();
            category.Habits ??= new();

            note = new()
            {
                Title = googleKeepNote.Title,
                Content = googleKeepNote.TextContent,
                IsDeleted = googleKeepNote.IsTrashed,
                //Color = googleKeepNote.Color
                CreatedAt = new DateTime(googleKeepNote.CreatedTimestampUsec),
                UpdatedAt = new DateTime(googleKeepNote.UserEditedTimestampUsec)
            };

            category.Notes.Add(note);
        }

        await _appData.SetUserData(userData);
    }

    private static async Task<List<GoogleKeepNote>> GetGoogleKeepNotes(Stream stream)
    {
        MemoryStream memoryStream = new();

        await stream.CopyToAsync(memoryStream);

        ZipArchive archive = new(memoryStream);

        List<GoogleKeepNote> googleKeepNotes = new();

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
