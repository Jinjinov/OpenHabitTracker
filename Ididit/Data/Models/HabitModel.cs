using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data.Models;

public class HabitModel : Model
{
    public TimeSpan AverageInterval { get; set; }

    public TimeSpan DesiredInterval { get; set; }

    public DateTime? LastTimeDoneAt { get; set; }

    public List<DateTime>? TimesDone { get; set; }
}
