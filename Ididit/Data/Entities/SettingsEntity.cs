namespace Ididit.Data.Entities;

public class SettingsEntity
{
    public long Id { get; set; }

    public DayOfWeek FirstDayOfWeek { get; set; }

    public bool ShowItemList { get; set; }

    public Sort NotesSort { get; set; }

    public Sort TasksSort { get; set; }

    public Sort HabitsSort { get; set; }
}
