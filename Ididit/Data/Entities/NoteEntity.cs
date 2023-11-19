using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ididit.Data.Entities;

public class NoteEntity : Entity
{
    public string Content { get; set; } = string.Empty;
}
