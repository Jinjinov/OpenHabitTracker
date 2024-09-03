using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Data;

public class UserData
{
    public SettingsModel Settings { get; set; } = new();

    public List<CategoryModel> Categories { get; set; } = [];
}
