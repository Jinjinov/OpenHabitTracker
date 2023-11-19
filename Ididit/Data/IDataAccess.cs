using Ididit.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data;

public interface IDataAccess
{
    Task<IReadOnlyList<HabitEntity>> GetHabits();

    Task<IReadOnlyList<NoteEntity>> GetNotes();

    Task<IReadOnlyList<TaskEntity>> GetTasks();

    Task<IReadOnlyList<TimeEntity>> GetTimes(long? habitId = null);
}
