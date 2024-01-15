using Ididit.Data;
using Ididit.Data.Entities;
using Ididit.Data.Models;

namespace Ididit.Services;

public class CategoryService(IDataAccess dataAccess)
{
    private readonly IDataAccess _dataAccess = dataAccess;

    public Dictionary<long, CategoryModel>? Categories { get; set; }

    public CategoryModel? SelectedCategory { get; set; }

    public CategoryModel? NewCategory { get; set; }

    public async Task Initialize()
    {
        if (Categories is null)
        {
            IReadOnlyList<CategoryEntity> categories = await _dataAccess.GetCategories();
            Categories = categories.Select(c => new CategoryModel
            {
                Id = c.Id,
                Title = c.Title,
            }).ToDictionary(x => x.Id);
        }

        NewCategory ??= new();
    }

    public void SetSelectedCategory(long? id)
    {
        if (Categories is null)
            return;

        SelectedCategory = id.HasValue && Categories.TryGetValue(id.Value, out CategoryModel? category) ? category : null;
    }

    public async Task AddCategory()
    {
        if (Categories is null || NewCategory is null)
            return;

        CategoryEntity category = new()
        {
            Title = NewCategory.Title
        };

        await _dataAccess.AddCategory(category);

        NewCategory.Id = category.Id;

        Categories.Add(NewCategory.Id, NewCategory);

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
        if (Categories is null)
            return;

        Categories.Remove(category.Id);

        await _dataAccess.RemoveCategory(category.Id);
    }
}
