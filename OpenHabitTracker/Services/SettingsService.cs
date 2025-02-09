using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class SettingsService(ClientSideData appData, IDataAccess dataAccess)
{
    private readonly ClientSideData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public SettingsModel Settings => _appData.Settings;

    public async Task Initialize()
    {
        await _appData.LoadSettings();
    }

    public async Task UpdateSettings()
    {
        if (await _dataAccess.GetSettings(Settings.Id) is SettingsEntity settings)
        {
            Settings.CopyToEntity(settings);

            await _dataAccess.UpdateSettings(settings);
        }
    }
}
