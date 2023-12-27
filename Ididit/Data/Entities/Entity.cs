namespace Ididit.Data.Entities;

public class Entity
{
    public long Id { get; init; }

    public bool IsDeleted { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Priority Priority { get; set; }
}
