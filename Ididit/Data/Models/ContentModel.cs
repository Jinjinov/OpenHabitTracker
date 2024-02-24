namespace Ididit.Data.Models;

public class ContentModel
{
    internal long Id { get; set; }

    internal long CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Color { get; set; } = "bg-body-secondary";

    public Priority Priority { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
