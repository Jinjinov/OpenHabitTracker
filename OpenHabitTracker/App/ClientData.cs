using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.App;

public class ClientData
{
    public UserModel User { get; set; } = new();
    public SettingsModel Settings { get; set; } = new();
    public Dictionary<long, HabitModel>? Habits { get; set; }
    public Dictionary<long, NoteModel>? Notes { get; set; }
    public Dictionary<long, TaskModel>? Tasks { get; set; }
    public Dictionary<long, TimeModel>? Times { get; set; }
    public Dictionary<long, ItemModel>? Items { get; set; }
    public Dictionary<long, CategoryModel>? Categories { get; set; }
    public Dictionary<long, PriorityModel>? Priorities { get; set; }
    public List<ContentModel>? Trash { get; set; }
}
