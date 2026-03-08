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

        if (category.Notes is not null)
            foreach (NoteModel note in category.Notes)
            {
                note.CategoryId = 0;
                note.IsDeleted = true;
                await _clientState.UpdateModel(note);
            }

        if (category.Tasks is not null)
            foreach (TaskModel task in category.Tasks)
            {
                task.CategoryId = 0;
                task.IsDeleted = true;
                await _clientState.UpdateModel(task);
            }

        if (category.Habits is not null)
            foreach(HabitModel habit in category.Habits)
            {
                habit.CategoryId = 0;
                habit.IsDeleted = true;
                await _clientState.UpdateModel(habit);
            }

        _clientState.Categories.Remove(category.Id);

        await _clientState.DataAccess.RemoveCategory(category.Id);

        if (_clientState.Settings.HiddenCategoryIds.Contains(category.Id))
        {
            _clientState.Settings.HiddenCategoryIds.Remove(category.Id);

            await UpdateSettings();
        }
    }

    private async Task UpdateSettings()
    {
        await _clientState.UpdateSettings();
    }
}
