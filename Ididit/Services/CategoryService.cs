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
}
