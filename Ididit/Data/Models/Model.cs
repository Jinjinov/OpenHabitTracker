namespace Ididit.Data.Models;

public class Model
{
    public long Id { get; set; }

    public long CategoryId { get; set; }

    public bool IsDeleted { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Priority Priority { get; set; }

    public Importance Importance { get; set; }
}
