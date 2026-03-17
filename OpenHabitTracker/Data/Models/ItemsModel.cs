namespace OpenHabitTracker.Data.Models;

public class ItemsModel : ContentModel
{
    public List<ItemModel>? Items { get; set; }

    public TimeOnly? Duration { get; set; }

    internal TimeOnly DurationProxy
    {
        get => Duration ?? TimeOnly.MinValue;
        set => Duration = value == TimeOnly.MinValue ? null : value;
    }

    internal int DurationHour
    {
        get => DurationProxy.Hour;
        set => DurationProxy = new TimeOnly(value, DurationProxy.Minute);
    }

    internal int DurationMinute
    {
        get => DurationProxy.Minute;
        set => DurationProxy = new TimeOnly(DurationProxy.Hour, value);
    }
}
