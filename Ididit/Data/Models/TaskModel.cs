using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data.Models;

public class TaskModel : Model
{
    public bool IsDone { get; set; }

    public DateTime? Date { get; set; }
}
