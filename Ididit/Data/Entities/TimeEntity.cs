using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data.Entities;

public class TimeEntity
{
    public long HabitId { get; init; }

    public DateTime Time { get; set; }
}
