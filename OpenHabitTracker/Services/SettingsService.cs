using OpenHabitTracker.App;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class SettingsService(ClientState appData)
{
    private readonly ClientState _appData = appData;

    public SettingsModel Settings => _appData.Settings;

    public async Task Initialize()
    {
        await _appData.LoadSettings();
    }

    public async Task UpdateSettings()
    {
        await _appData.UpdateSettings();
    }
}
