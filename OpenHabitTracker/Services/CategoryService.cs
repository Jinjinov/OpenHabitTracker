using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class CategoryService(ClientState clientState) : ICategoryService
{
    private readonly ClientState _clientState = clientState;

    public IReadOnlyCollection<CategoryModel>? Categories => _clientState.Categories?.Values;

    public CategoryModel? SelectedCategory { get; set; }

    public CategoryModel? NewCategory { get; set; }

    public string GetCategoryTitle(long category)
    {
        return _clientState.Categories?.GetValueOrDefault(category)?.Title ?? category.ToString();
    }

    public async Task Initialize()
    {
        await _clientState.LoadCategories();

        NewCategory ??= new() { UserId = _clientState.User.Id };
    }

    public void SetSelectedCategory(long? id)
    {
        if (_clientState.Categories is null)
            return;

        SelectedCategory = id.HasValue && _clientState.Categories.TryGetValue(id.Value, out CategoryModel? category) ? category : null;
    }

    public async Task AddCategory()
    {
        if (_clientState.Categories is null || NewCategory is null)
            return;

        CategoryEntity category = NewCategory.ToEntity();

        await _clientState.DataAccess.AddCategory(category);

        NewCategory.Id = category.Id;

        _clientState.Categories.Add(NewCategory.Id, NewCategory);

        NewCategory = new() { UserId = _clientState.User.Id };
    }

    public async Task UpdateCategory(string title)
    {
        if (Categories is null || SelectedCategory is null)
            return;

        SelectedCategory.Title = title;

        if (await _clientState.DataAccess.GetCategory(SelectedCategory.Id) is CategoryEntity category)
        {
            category.Title = SelectedCategory.Title;

            await _clientState.DataAccess.UpdateCategory(category);
        }

        SelectedCategory = null;
    }

    public async Task DeleteCategory(CategoryModel category)
    {
        if (_clientState.Categories is null)
            return;

        foreach (NoteModel note in category.Notes)
        {
            note.CategoryId = 0;
            if (!note.IsDeleted)
            {
                note.IsDeleted = true;
                _clientState.TrashedNotes?.Add(note);
            }
            if (await _clientState.DataAccess.GetNote(note.Id) is NoteEntity noteEntity)
            {
                note.CopyToEntity(noteEntity);
                await _clientState.DataAccess.UpdateNote(noteEntity);
            }
        }

        foreach (TaskModel task in category.Tasks)
        {
            task.CategoryId = 0;
            if (!task.IsDeleted)
            {
                task.IsDeleted = true;
                _clientState.TrashedTasks?.Add(task);
            }
            if (await _clientState.DataAccess.GetTask(task.Id) is TaskEntity taskEntity)
            {
                task.CopyToEntity(taskEntity);
                await _clientState.DataAccess.UpdateTask(taskEntity);
            }
        }

        foreach (HabitModel habit in category.Habits)
        {
            habit.CategoryId = 0;
            if (!habit.IsDeleted)
            {
                habit.IsDeleted = true;
                _clientState.TrashedHabits?.Add(habit);
            }
            if (await _clientState.DataAccess.GetHabit(habit.Id) is HabitEntity habitEntity)
            {
                habit.CopyToEntity(habitEntity);
                await _clientState.DataAccess.UpdateHabit(habitEntity);
            }
        }

        _clientState.Categories.Remove(category.Id);

        await _clientState.DataAccess.RemoveCategory(category.Id);

        if (_clientState.Settings.HiddenCategoryIds.Contains(category.Id))
        {
            _clientState.Settings.HiddenCategoryIds.Remove(category.Id);

            await UpdateSettings();
        }
    }

    public void ChangeCategory(ContentModel model, long newCategoryId)
    {
        if (_clientState.Categories is null)
            return;

        if (model.CategoryId != 0 && _clientState.Categories.TryGetValue(model.CategoryId, out CategoryModel? oldCategory))
        {
            if (model is NoteModel note)
                oldCategory.Notes.Remove(note);
            else if (model is TaskModel task)
                oldCategory.Tasks.Remove(task);
            else if (model is HabitModel habit)
                oldCategory.Habits.Remove(habit);
        }

        model.CategoryId = newCategoryId;

        if (newCategoryId != 0 && _clientState.Categories.TryGetValue(newCategoryId, out CategoryModel? newCategory))
        {
            if (model is NoteModel addedNote)
                newCategory.Notes.Add(addedNote);
            else if (model is TaskModel addedTask)
                newCategory.Tasks.Add(addedTask);
            else if (model is HabitModel addedHabit)
                newCategory.Habits.Add(addedHabit);
        }
    }

    private async Task UpdateSettings()
    {
        await _clientState.UpdateSettings();
    }
}
