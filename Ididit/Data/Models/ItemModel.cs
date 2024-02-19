namespace Ididit.Data.Models;

public class ItemModel
{
    internal long Id { get; set; }

    internal long ParentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime? DoneAt { get; set; }

    internal bool IsDone => DoneAt is not null;
}
