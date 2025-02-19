using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class CategoryService(ClientState appData)
{
    private readonly ClientState _appData = appData;

    public IReadOnlyCollection<CategoryModel>? Categories => _appData.Categories?.Values;

    public CategoryModel? SelectedCategory { get; set; }

    public CategoryModel? NewCategory { get; set; }

    public string GetCategoryTitle(long category)
    {
        return _appData.Categories?.GetValueOrDefault(category)?.Title ?? category.ToString();
    }

    public async Task Initialize()
    {
        await _appData.LoadCategories();

        NewCategory ??= new() { UserId = _appData.User.Id };
    }

    public void SetSelectedCategory(long? id)
    {
        if (_appData.Categories is null)
            return;

        SelectedCategory = id.HasValue && _appData.Categories.TryGetValue(id.Value, out CategoryModel? category) ? category : null;
    }

    public async Task AddCategory()
    {
        if (_appData.Categories is null || NewCategory is null)
            return;

        CategoryEntity category = NewCategory.ToEntity();

        await _appData.DataAccess.AddCategory(category);

        NewCategory.Id = category.Id;

        _appData.Categories.Add(NewCategory.Id, NewCategory);

        NewCategory = new() { UserId = _appData.User.Id };
    }

    public async Task UpdateCategory(string title)
    {
        if (Categories is null || SelectedCategory is null)
            return;

        SelectedCategory.Title = title;

        if (await _appData.DataAccess.GetCategory(SelectedCategory.Id) is CategoryEntity category)
        {
            category.Title = SelectedCategory.Title;

            await _appData.DataAccess.UpdateCategory(category);
        }

        SelectedCategory = null;
    }

    public async Task DeleteCategory(CategoryModel category)
    {
        if (_appData.Categories is null)
            return;

        category.Notes?.ForEach(async x => { x.CategoryId = 0; x.IsDeleted = true; await _appData.UpdateModel(x); });
        category.Tasks?.ForEach(async x => { x.CategoryId = 0; x.IsDeleted = true; await _appData.UpdateModel(x); });
        category.Habits?.ForEach(async x => { x.CategoryId = 0; x.IsDeleted = true; await _appData.UpdateModel(x); });

        _appData.Categories.Remove(category.Id);

        await _appData.DataAccess.RemoveCategory(category.Id);

        if (_appData.Settings.HiddenCategoryIds.Contains(category.Id))
        {
            _appData.Settings.HiddenCategoryIds.Remove(category.Id);

            await UpdateSettings();
        }
    }

    private async Task UpdateSettings()
    {
        await _appData.UpdateSettings();
    }
}
