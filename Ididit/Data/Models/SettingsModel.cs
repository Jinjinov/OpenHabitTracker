namespace Ididit.Data.Models;

public class SettingsModel
{
    internal long Id { get; set; }

    public DayOfWeek FirstDayOfWeek { get; set; }

    public bool ShowItemList { get; set; }

    public Sort NotesSort { get; set; }

    public Sort TasksSort { get; set; }

    public Sort HabitsSort { get; set; }
}
