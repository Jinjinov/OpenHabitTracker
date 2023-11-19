using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data.Entities;

public class HabitEntity : Entity
{
    public TimeSpan AverageInterval { get; set; }

    public TimeSpan DesiredInterval { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }
}
