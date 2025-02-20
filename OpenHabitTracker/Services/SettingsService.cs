using OpenHabitTracker.App;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class SettingsService(ClientState appData)
{
    private readonly ClientState _appData = appData;

    public SettingsModel Settings => _appData.Settings;
}
