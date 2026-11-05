using System.Globalization;

namespace OpenHabitTracker.Localization;

public interface ICultureTarget
{
    void Apply(CultureInfo culture, CultureInfo uiCulture);
}

// One user per process, so the defaults make background threads format correctly too.
public class ProcessCulture : ICultureTarget
{
    public void Apply(CultureInfo culture, CultureInfo uiCulture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;
    }
}
