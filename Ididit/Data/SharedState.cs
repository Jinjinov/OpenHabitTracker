using Ididit.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data;

public class SharedState
{
    IReadOnlyDictionary<long, HabitModel> HabitsById { get; }

    IReadOnlyDictionary<long, NoteModel> NotesById { get; }

    IReadOnlyDictionary<long, TaskModel> TasksById { get; }
}
