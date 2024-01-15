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

    public CategoryModel? EditCategory { get; set; }

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

        NewCategory = null;
    }

    public async Task UpdateCategory()
    {
        if (Categories is null || EditCategory is null)
            return;

        if (await _dataAccess.GetCategory(EditCategory.Id) is CategoryEntity category)
        {
            category.Title = EditCategory.Title;

            await _dataAccess.UpdateCategory(category);
        }

        EditCategory = null;
    }

    public async Task DeleteCategory(CategoryModel category)
    {
        if (Categories is null)
            return;

        Categories.Remove(category.Id);

        await _dataAccess.RemoveCategory(category.Id);
    }
}
