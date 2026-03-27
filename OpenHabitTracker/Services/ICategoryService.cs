using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public interface ICategoryService
{
    IReadOnlyCollection<CategoryModel>? Categories { get; }
    CategoryModel? SelectedCategory { get; set; }
    CategoryModel? NewCategory { get; set; }
    string GetCategoryTitle(long category);
    Task Initialize();
    void SetSelectedCategory(long? id);
    Task AddCategory();
    Task UpdateCategory(string title);
    Task DeleteCategory(CategoryModel category);
    Task ToggleCollapsed(CategoryModel category);
    void ChangeCategory(ContentModel model, long newCategoryId);
}
