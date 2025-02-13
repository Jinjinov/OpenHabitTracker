using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
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
        if (await _appData.DataAccess.GetSettings(Settings.Id) is SettingsEntity settings)
        {
            Settings.CopyToEntity(settings);

            await _appData.DataAccess.UpdateSettings(settings);
        }
    }
}
