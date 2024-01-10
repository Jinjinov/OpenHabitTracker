using Ididit.Data.Entities;

namespace Ididit.Data;

public class UserData
{
    public List<HabitEntity>? Habits { get; set; }
    public List<NoteEntity>? Notes { get; set; }
    public List<TaskEntity>? Tasks { get; set; }
}
