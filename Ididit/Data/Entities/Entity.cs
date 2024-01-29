namespace Ididit.Data.Entities;

public class Entity
{
    public long Id { get; set; }

    public long CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
