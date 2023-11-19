using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data.Entities;

public class TaskEntity : Entity
{
    public bool IsDone { get; set; }

    public DateTime? Date { get; set; }
}
