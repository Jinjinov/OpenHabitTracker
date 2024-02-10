namespace Ididit.Data.Models;

public class SettingsModel
{
    internal long Id { get; set; }

    public DayOfWeek FirstDayOfWeek { get; set; }

    public bool ShowItemList { get; set; }
}
