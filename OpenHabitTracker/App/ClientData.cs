using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Models;

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
    public Dictionary<long, PriorityModel>? Priorities { get; set; }
    public List<ContentModel>? Trash { get; set; }

    public async Task<IEnumerable<NoteModel>> GetNotes(QueryParameters queryParameters)
    {
        if (Notes is null)
        {
            // ContentMarkdown = _markdownToHtml.GetMarkdown(x.Content)

            Notes = (await _dataAccess.GetNotes()).Select(x => x.ToModel()).ToDictionary(x => x.Id);
        }

        IEnumerable<NoteModel> notes = Notes.Values.Where(x => !x.IsDeleted);

        if (queryParameters.PriorityFilterDisplay == FilterDisplay.CheckBoxes)
        {
            notes = notes.Where(x => queryParameters.ShowPriority[x.Priority]);
        }
        else if (queryParameters.SelectedPriority is not null)
        {
            notes = notes.Where(x => x.Priority == queryParameters.SelectedPriority);
        }

        if (queryParameters.SearchTerm is not null)
        {
            StringComparison comparisonType = queryParameters.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            notes = notes.Where(x => x.Title.Contains(queryParameters.SearchTerm, comparisonType) || x.Content.Contains(queryParameters.SearchTerm, comparisonType));
        }

        if (queryParameters.CategoryFilterDisplay == FilterDisplay.CheckBoxes)
        {
            notes = notes.Where(x => !queryParameters.HiddenCategoryIds.Contains(x.CategoryId));
        }
        else if (queryParameters.SelectedCategoryId is not null)
        {
            notes = notes.Where(x => x.CategoryId == queryParameters.SelectedCategoryId);
        }

        return queryParameters.SortBy[ContentType.Note] switch
        {
            Sort.Category => notes.OrderBy(x => x.CategoryId),
            Sort.Priority => notes.OrderByDescending(x => x.Priority),
            Sort.Title => notes.OrderBy(x => x.Title),
            _ => notes
        };
    }

    public async Task<IEnumerable<TaskModel>> GetTasks(QueryParameters queryParameters)
    {
        if (Tasks is null)
        {
            // DataAccess.GetItems();

            Tasks = (await _dataAccess.GetTasks()).Select(x => x.ToModel()).ToDictionary(x => x.Id);
        }

        IEnumerable<TaskModel> tasks = Tasks.Values.Where(x => !x.IsDeleted);

        if (queryParameters.PriorityFilterDisplay == FilterDisplay.CheckBoxes)
        {
            tasks = tasks.Where(x => queryParameters.ShowPriority[x.Priority]);
        }
        else if (queryParameters.SelectedPriority is not null)
        {
            tasks = tasks.Where(x => x.Priority == queryParameters.SelectedPriority);
        }

        if (queryParameters.SearchTerm is not null)
        {
            StringComparison comparisonType = queryParameters.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            tasks = tasks.Where(x => x.Title.Contains(queryParameters.SearchTerm, comparisonType) || x.Items?.Any(i => i.Title.Contains(queryParameters.SearchTerm, comparisonType)) == true);
        }

        if (queryParameters.DoneAtFilter is not null)
        {
            tasks = queryParameters.DoneAtCompare switch
            {
                DateCompare.Before => tasks.Where(x => x.CompletedAt?.Date < queryParameters.DoneAtFilter.Value.Date),
                DateCompare.On => tasks.Where(x => x.CompletedAt?.Date == queryParameters.DoneAtFilter.Value.Date),
                DateCompare.After => tasks.Where(x => x.CompletedAt?.Date > queryParameters.DoneAtFilter.Value.Date),
                DateCompare.NotOn => tasks.Where(x => x.CompletedAt?.Date != queryParameters.DoneAtFilter.Value.Date),
                _ => throw new ArgumentOutOfRangeException(nameof(queryParameters.DoneAtCompare))
            };
        }

        if (queryParameters.PlannedAtFilter is not null)
        {
            tasks = queryParameters.PlannedAtCompare switch
            {
                DateCompare.Before => tasks.Where(x => x.PlannedAt?.Date < queryParameters.PlannedAtFilter.Value.Date),
                DateCompare.On => tasks.Where(x => x.PlannedAt?.Date == queryParameters.PlannedAtFilter.Value.Date),
                DateCompare.After => tasks.Where(x => x.PlannedAt?.Date > queryParameters.PlannedAtFilter.Value.Date),
                DateCompare.NotOn => tasks.Where(x => x.PlannedAt?.Date != queryParameters.PlannedAtFilter.Value.Date),
                _ => throw new ArgumentOutOfRangeException(nameof(queryParameters.PlannedAtCompare))
            };
        }

        if (queryParameters.CategoryFilterDisplay == FilterDisplay.CheckBoxes)
        {
            tasks = tasks.Where(x => !queryParameters.HiddenCategoryIds.Contains(x.CategoryId));
        }
        else if (queryParameters.SelectedCategoryId is not null)
        {
            tasks = tasks.Where(x => x.CategoryId == queryParameters.SelectedCategoryId);
        }

        if (queryParameters.HideCompletedTasks)
        {
            tasks = tasks.Where(x => x.CompletedAt is null);
        }

        return queryParameters.SortBy[ContentType.Task] switch
        {
            Sort.Category => tasks.OrderBy(x => x.CategoryId),
            Sort.Priority => tasks.OrderByDescending(x => x.Priority),
            Sort.Title => tasks.OrderBy(x => x.Title),
            Sort.Duration => tasks.OrderBy(x => x.Duration),
            Sort.ElapsedTime => tasks.OrderBy(x => x.CompletedAt),
            Sort.PlannedAt => tasks.OrderBy(x => x.PlannedAt),
            Sort.TimeSpent => tasks.OrderBy(x => x.TimeSpent),
            _ => tasks
        };
    }

    public async Task<IEnumerable<HabitModel>> GetHabits(QueryParameters queryParameters)
    {
        if (Habits is null)
        {
            // DataAccess.GetItems();

            // DataAccess.GetTimes();

            Habits = (await _dataAccess.GetHabits()).Select(x => x.ToModel()).ToDictionary(x => x.Id);
        }

        IEnumerable<HabitModel> habits = Habits.Values.Where(x => !x.IsDeleted);

        if (queryParameters.PriorityFilterDisplay == FilterDisplay.CheckBoxes)
        {
            habits = habits.Where(x => queryParameters.ShowPriority[x.Priority]);
        }
        else if (queryParameters.SelectedPriority is not null)
        {
            habits = habits.Where(x => x.Priority == queryParameters.SelectedPriority);
        }

        if (queryParameters.SearchTerm is not null)
        {
            StringComparison comparisonType = queryParameters.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            habits = habits.Where(x => x.Title.Contains(queryParameters.SearchTerm, comparisonType) || x.Items?.Any(i => i.Title.Contains(queryParameters.SearchTerm, comparisonType)) == true);
        }

        if (queryParameters.DoneAtFilter is not null)
        {
            habits = queryParameters.DoneAtCompare switch
            {
                DateCompare.Before => habits.Where(x => x.TimesDone?.Any(t => t.CompletedAt?.Date < queryParameters.DoneAtFilter.Value.Date) == true),
                DateCompare.On => habits.Where(x => x.TimesDone?.Any(t => t.CompletedAt?.Date == queryParameters.DoneAtFilter.Value.Date) == true),
                DateCompare.After => habits.Where(x => x.TimesDone?.Any(t => t.CompletedAt?.Date > queryParameters.DoneAtFilter.Value.Date) == true),
                DateCompare.NotOn => habits.Where(x => !x.TimesDone?.Any(t => t.CompletedAt?.Date == queryParameters.DoneAtFilter.Value.Date) == true),
                _ => throw new ArgumentOutOfRangeException(nameof(queryParameters.DoneAtCompare))
            };
        }

        if (queryParameters.CategoryFilterDisplay == FilterDisplay.CheckBoxes)
        {
            habits = habits.Where(x => !queryParameters.HiddenCategoryIds.Contains(x.CategoryId));
        }
        else if (queryParameters.SelectedCategoryId is not null)
        {
            habits = habits.Where(x => x.CategoryId == queryParameters.SelectedCategoryId);
        }

        if (queryParameters.ShowOnlyOverSelectedRatioMin)
        {
            habits = habits.Where(x => x.GetRatio(queryParameters.SelectedRatio) > queryParameters.SelectedRatioMin);
        }

        return queryParameters.SortBy[ContentType.Habit] switch
        {
            Sort.Category => habits.OrderBy(x => x.CategoryId),
            Sort.Priority => habits.OrderByDescending(x => x.Priority),
            Sort.Title => habits.OrderBy(x => x.Title),
            Sort.Duration => habits.OrderBy(x => x.Duration),
            Sort.RepeatInterval => habits.OrderBy(x => x.GetRepeatInterval() / x.NonZeroRepeatCount),
            Sort.AverageInterval => habits.OrderBy(x => x.AverageInterval / x.NonZeroRepeatCount),
            Sort.TimeSpent => habits.OrderBy(x => x.TotalTimeSpent),
            Sort.AverageTimeSpent => habits.OrderBy(x => x.AverageTimeSpent),
            Sort.ElapsedTime => habits.OrderBy(x => x.LastTimeDoneAt),
            Sort.SelectedRatio => habits.OrderByDescending(x => x.GetRatio(queryParameters.SelectedRatio) * x.NonZeroRepeatCount),
            _ => habits
        };
    }
}
