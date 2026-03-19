using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;
using OpenHabitTracker.Query;

namespace OpenHabitTracker.App;

public class ClientData(IDataAccess dataAccess, MarkdownToHtml markdownToHtml)
{
    private readonly IDataAccess _dataAccess = dataAccess;
    private readonly MarkdownToHtml _markdownToHtml = markdownToHtml;

    public UserModel User { get; set; } = new();
    public SettingsModel Settings { get; set; } = new();
    public Dictionary<long, HabitModel>? Habits { get; set; }
    public Dictionary<long, NoteModel>? Notes { get; set; }
    public Dictionary<long, TaskModel>? Tasks { get; set; }
    public Dictionary<long, TimeModel>? Times { get; set; }
    public Dictionary<long, ItemModel>? Items { get; set; }
    public Dictionary<long, CategoryModel>? Categories { get; set; }
    public List<HabitModel>? TrashedHabits { get; set; }
    public List<NoteModel>? TrashedNotes { get; set; }
    public List<TaskModel>? TrashedTasks { get; set; }

    public async Task<IEnumerable<NoteModel>> GetNotes(QueryParameters queryParameters)
    {
        // TODO:: first filter with queryParameters, then use _dataAccess
        if (Notes is null)
        {
            Notes = (await _dataAccess.GetNotes()).Select(x => x.ToModel()).ToDictionary(x => x.Id);

            foreach (NoteModel note in Notes.Values)
            {
                note.ContentMarkdown = _markdownToHtml.GetMarkdown(note.Content);
            }
        }

        return Notes.Values.FilterNotes(queryParameters);
    }

    public async Task<IEnumerable<TaskModel>> GetTasks(QueryParameters queryParameters)
    {
        // TODO:: first filter with queryParameters, then use _dataAccess
        if (Tasks is null)
        {
            Tasks = (await _dataAccess.GetTasks()).Select(x => x.ToModel()).ToDictionary(x => x.Id);

            if (Items is null)
            {
                Items = (await _dataAccess.GetItems()).Select(x => x.ToModel()).ToDictionary(x => x.Id);
            }

            foreach (TaskModel task in Tasks.Values)
            {
                task.Items = Items.Values.Where(x => x.ParentId == task.Id).ToList();
            }
        }

        return Tasks.Values.FilterTasks(queryParameters);
    }

    public async Task<IEnumerable<HabitModel>> GetHabits(QueryParameters queryParameters)
    {
        // TODO:: first filter with queryParameters, then use _dataAccess
        if (Habits is null)
        {
            Habits = (await _dataAccess.GetHabits()).Select(x => x.ToModel()).ToDictionary(x => x.Id);

            if (Items is null)
            {
                Items = (await _dataAccess.GetItems()).Select(x => x.ToModel()).ToDictionary(x => x.Id);
            }

            if (Times is null)
            {
                Times = (await _dataAccess.GetTimes()).Select(x => x.ToModel()).ToDictionary(x => x.Id);
            }

            foreach (HabitModel habit in Habits.Values)
            {
                habit.Items = Items.Values.Where(x => x.ParentId == habit.Id).ToList();

                habit.TimesDone = Times.Values.Where(x => x.HabitId == habit.Id).ToList();
            }

            foreach (HabitModel habit in Habits.Values)
            {
                habit.RefreshTimesDoneByDay();
            }
        }

        return Habits.Values.FilterHabits(queryParameters);
    }
}
