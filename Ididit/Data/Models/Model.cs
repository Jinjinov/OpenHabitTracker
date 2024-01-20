namespace Ididit.Data.Models;

public class Model
{
    public long Id { get; set; }

    public long CategoryId { get; set; }

    public long PriorityId { get; set; }

    public bool IsDeleted { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public CategoryModel? Category { get; set; }

    public PriorityModel? Priority { get; set; }
}
