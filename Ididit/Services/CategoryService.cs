using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class CategoryService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public IReadOnlyCollection<CategoryModel>? Categories => _appData.Categories?.Values;

    public CategoryModel? SelectedCategory { get; set; }

    public CategoryModel? NewCategory { get; set; }

    public void SetCategory(Model model, long categoryId)
    {
        model.Category = _appData.Categories?.GetValueOrDefault(categoryId);
    }

    public async Task Initialize()
    {
        await _appData.InitializeCategories();

        NewCategory ??= new();
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

        CategoryEntity category = new()
        {
            Title = NewCategory.Title
        };

        await _dataAccess.AddCategory(category);

        NewCategory.Id = category.Id;

        _appData.Categories.Add(NewCategory.Id, NewCategory);

        NewCategory = new();
    }

    public async Task UpdateCategory(string title)
    {
        if (Categories is null || SelectedCategory is null)
            return;

        SelectedCategory.Title = title;

        if (await _dataAccess.GetCategory(SelectedCategory.Id) is CategoryEntity category)
        {
            category.Title = SelectedCategory.Title;

            await _dataAccess.UpdateCategory(category);
        }

        SelectedCategory = null;
    }

    public async Task DeleteCategory(CategoryModel category)
    {
        if (_appData.Categories is null)
            return;

        _appData.Categories.Remove(category.Id);

        await _dataAccess.RemoveCategory(category.Id);
    }
}
