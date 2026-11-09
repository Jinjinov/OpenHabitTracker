using Microsoft.Extensions.Localization;
using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using System.IO.Compression;
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
        List<GoogleKeepNote> googleKeepNotes = await GetGoogleKeepNotes(stream);

        List<CategoryModel> categories = [];
        Dictionary<NoteModel, string> formerContents = [];

        CategoryModel? uncategorized = null;
        CategoryModel? archived = null;
        NoteModel? note = null;
        TaskModel? task = null;
        //HabitModel? habit = null;

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

            if (googleKeepNote.ListContent.Count == 0)
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
                    UpdatedAt = DateTime.UnixEpoch.AddTicks(googleKeepNote.UserEditedTimestampUsec * 10).ToLocalTime()
                };

                if (googleKeepNote.ListContent.Count > 0)
                {
                    task.Items = [];

                    // Keep keeps an empty item at the end of a checklist for the next entry.
                    foreach (ListContent listContent in googleKeepNote.ListContent.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
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

        MergeOptions options = new()
        {
            HiddenCategories = archived is null ? [] : [archived],
            FormerContents = formerContents
        };

        return new(categories, options);
    }

    private static string GetContent(GoogleKeepNote googleKeepNote)
    {
        // textContent is the same text without the formatting.
        string text = string.IsNullOrEmpty(googleKeepNote.TextContentHtml) ? googleKeepNote.TextContent : GoogleKeepMarkdown.FromHtml(googleKeepNote.TextContentHtml);

        // Keep stores the preview of a link as an annotation, mostly of a link that is in the text already.
        IEnumerable<string> links = googleKeepNote.Annotations
            .Where(annotation => !string.IsNullOrWhiteSpace(annotation.Url) && !ContainsUrl(text, annotation.Url))
            .Select(annotation => string.IsNullOrWhiteSpace(annotation.Title) ? annotation.Url : $"[{EscapeLinkText(annotation.Title)}]({annotation.Url})");

        return string.Join("\n", new[] { text }.Concat(links).Where(line => !string.IsNullOrEmpty(line)));
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

    private static async Task<List<GoogleKeepNote>> GetGoogleKeepNotes(Stream stream)
    {
        using MemoryStream memoryStream = new();

        await stream.CopyToAsync(memoryStream);

        using ZipArchive archive = new(memoryStream);

        List<GoogleKeepNote> googleKeepNotes = [];

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                await using Stream jsonStream = entry.Open();

                using StreamReader streamReader = new(jsonStream);

                string jsonText = await streamReader.ReadToEndAsync();

                if (!GoogleKeepNote.IsNote(jsonText))
                    continue;

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
