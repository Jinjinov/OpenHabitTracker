using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.App;

// What an import adds to the data already there. A category, note, task, habit, item or completion
// the data already has, in the trash too, is matched instead of added, so a second import of the
// same file adds nothing, and nothing that already exists is changed.
public class UserDataMerge
{
    // New categories, with all their notes, tasks and habits.
    internal List<CategoryModel> Categories { get; } = [];

    // New notes, tasks and habits of categories that already exist, or of no category.
    internal List<NoteModel> Notes { get; } = [];
    internal List<TaskModel> Tasks { get; } = [];
    internal List<HabitModel> Habits { get; } = [];

    // New items and completions of tasks and habits that already exist.
    internal List<ItemModel> Items { get; } = [];
    internal List<TimeModel> Times { get; } = [];

    // New categories that are hidden from the lists once they exist.
    internal List<CategoryModel> HiddenCategories { get; } = [];

    public ImportCounts Found { get; private init; } = new(0, 0, 0, 0, 0, 0);

    public ImportCounts New
    {
        get
        {
            ImportCounts inNewCategories = Count(Categories);

            return new(
                inNewCategories.Categories,
                inNewCategories.Notes + Notes.Count,
                inNewCategories.Tasks + Tasks.Count,
                inNewCategories.Habits + Habits.Count,
                inNewCategories.Items + Tasks.Sum(x => x.Items?.Count ?? 0) + Habits.Sum(x => x.Items?.Count ?? 0) + Items.Count,
                inNewCategories.Completions + Habits.Sum(x => x.TimesDone?.Count ?? 0) + Times.Count);
        }
    }

    // Everything is added, nothing is matched.
    internal static UserDataMerge AllNew(List<CategoryModel> categories)
    {
        UserDataMerge merge = new() { Found = Count(categories) };

        merge.Categories.AddRange(categories);

        return merge;
    }

    internal static UserDataMerge Create(
        List<CategoryModel> categories,
        IEnumerable<CategoryModel> existingCategories,
        IEnumerable<NoteModel> existingNotes,
        IEnumerable<TaskModel> existingTasks,
        IEnumerable<HabitModel> existingHabits,
        IEnumerable<ItemModel> existingItems,
        IEnumerable<TimeModel> existingTimes,
        MergeOptions? options = null)
    {
        options ??= new();

        UserDataMerge merge = new() { Found = Count(categories) };

        // Each existing model can be matched once, so a file holding two equal notes adds the second
        // one when the data has only the first.
        List<CategoryModel> categoryPool = existingCategories.ToList();
        Dictionary<long, List<NoteModel>> notePools = existingNotes.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<TaskModel>> taskPools = existingTasks.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        Dictionary<long, List<HabitModel>> habitPools = existingHabits.GroupBy(x => x.CategoryId).ToDictionary(g => g.Key, g => g.ToList());
        ILookup<long, ItemModel> itemsByParentId = existingItems.ToLookup(x => x.ParentId);
        ILookup<long, TimeModel> timesByHabitId = existingTimes.ToLookup(x => x.HabitId);

        foreach (CategoryModel category in categories)
        {
            long categoryId;

            // A category without a title holds what has no category.
            if (string.IsNullOrEmpty(category.Title))
            {
                categoryId = 0;
            }
            else if (Take(categoryPool, x => SameTitle(x.Title, category.Title)) is CategoryModel existingCategory)
            {
                categoryId = existingCategory.Id;
            }
            else
            {
                merge.Categories.Add(category);

                if (options.HiddenCategories.Contains(category))
                    merge.HiddenCategories.Add(category);

                continue;
            }

            List<NoteModel> notePool = notePools.GetValueOrDefault(categoryId) ?? [];
            List<TaskModel> taskPool = taskPools.GetValueOrDefault(categoryId) ?? [];
            List<HabitModel> habitPool = habitPools.GetValueOrDefault(categoryId) ?? [];

            foreach (NoteModel note in category.Notes)
            {
                if (Take(notePool, x => SameTitle(x.Title, note.Title) && SameContent(x.Content, note.Content)) is null)
                {
                    note.CategoryId = categoryId;

                    merge.Notes.Add(note);
                }
            }

            foreach (TaskModel task in category.Tasks)
            {
                if (Take(taskPool, x => SameTitle(x.Title, task.Title) && x.PlannedAt == task.PlannedAt) is TaskModel existingTask)
                {
                    merge.AddMissingItems(existingTask.Id, task.Items, itemsByParentId[existingTask.Id]);
                }
                else
                {
                    task.CategoryId = categoryId;

                    merge.Tasks.Add(task);
                }
            }

            foreach (HabitModel habit in category.Habits)
            {
                if (Take(habitPool, x => SameTitle(x.Title, habit.Title)) is HabitModel existingHabit)
                {
                    merge.AddMissingItems(existingHabit.Id, habit.Items, itemsByParentId[existingHabit.Id]);

                    if (options.MatchCompletionsByDay)
                        merge.AddTimesOnMissingDays(existingHabit.Id, habit.TimesDone, timesByHabitId[existingHabit.Id]);
                    else
                        merge.AddMissingTimes(existingHabit.Id, habit.TimesDone, timesByHabitId[existingHabit.Id]);
                }
                else
                {
                    habit.CategoryId = categoryId;

                    merge.Habits.Add(habit);
                }
            }
        }

        return merge;
    }

    private void AddMissingItems(long parentId, List<ItemModel>? items, IEnumerable<ItemModel> existingItems)
    {
        List<ItemModel> pool = existingItems.ToList();

        foreach (ItemModel item in items ?? [])
        {
            if (Take(pool, x => SameTitle(x.Title, item.Title)) is null)
            {
                item.ParentId = parentId;

                Items.Add(item);
            }
        }
    }

    // A completion matches at the precision this app records, the start and the completion time.
    private void AddMissingTimes(long habitId, List<TimeModel>? times, IEnumerable<TimeModel> existingTimes)
    {
        List<TimeModel> pool = existingTimes.ToList();

        foreach (TimeModel time in times ?? [])
        {
            if (Take(pool, x => x.StartedAt == time.StartedAt && x.CompletedAt == time.CompletedAt) is null)
            {
                time.HabitId = habitId;

                Times.Add(time);
            }
        }
    }

    private void AddTimesOnMissingDays(long habitId, List<TimeModel>? times, IEnumerable<TimeModel> existingTimes)
    {
        HashSet<DateTime> doneDays = existingTimes.Select(x => x.StartedAt.Date).ToHashSet();

        foreach (TimeModel time in times ?? [])
        {
            if (doneDays.Add(time.StartedAt.Date))
            {
                time.HabitId = habitId;

                Times.Add(time);
            }
        }
    }

    private static T? Take<T>(List<T> pool, Func<T, bool> match) where T : class
    {
        int index = pool.FindIndex(x => match(x));

        if (index < 0)
            return null;

        T taken = pool[index];

        pool.RemoveAt(index);

        return taken;
    }

    private static bool SameTitle(string a, string b)
    {
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    // Blank lines and the whitespace around lines are not compared, because the Markdown and TSV
    // round trips do not keep them.
    private static bool SameContent(string a, string b)
    {
        return NormalizeContent(a) == NormalizeContent(b);
    }

    private static string NormalizeContent(string content)
    {
        return string.Join('\n', content.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0));
    }

    private static ImportCounts Count(List<CategoryModel> categories)
    {
        List<TaskModel> tasks = categories.SelectMany(x => x.Tasks).ToList();
        List<HabitModel> habits = categories.SelectMany(x => x.Habits).ToList();

        return new(
            categories.Count(x => !string.IsNullOrEmpty(x.Title)),
            categories.Sum(x => x.Notes.Count),
            tasks.Count,
            habits.Count,
            tasks.Sum(x => x.Items?.Count ?? 0) + habits.Sum(x => x.Items?.Count ?? 0),
            habits.Sum(x => x.TimesDone?.Count ?? 0));
    }
}
