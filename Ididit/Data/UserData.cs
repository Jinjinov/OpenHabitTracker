using Ididit.Data.Models;

namespace Ididit.Data;

public class UserData
{
    public SettingsModel Settings { get; set; } = new();
    public List<PriorityModel> Priorities { get; set; } = [];
    public List<CategoryModel> Categories { get; set; } = [];
    public List<NoteModel> Notes { get; set; } = [];
    public List<TaskModel> Tasks { get; set; } = [];
    public List<HabitModel> Habits { get; set; } = [];
}
