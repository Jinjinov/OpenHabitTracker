using Ididit.Data.Models;

namespace Ididit.Data;

public class UserData
{
    public SettingsModel Settings { get; set; } = new();

    public List<CategoryModel> Categories { get; set; } = [];
}
