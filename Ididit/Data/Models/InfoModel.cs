namespace Ididit.Data.Models;

public class InfoModel
{
    internal long Id { get; set; }

    internal long CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
