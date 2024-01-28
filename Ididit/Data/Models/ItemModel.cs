namespace Ididit.Data.Models;

public class ItemModel
{
    public long Id { get; set; }

    public long ParentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }
}
