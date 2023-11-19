using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data.Models;

public class Model
{
    public long Id { get; init; }

    public bool IsDeleted { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Priority Priority { get; set; }
}
