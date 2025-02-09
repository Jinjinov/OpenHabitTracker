using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.Data.Models;

namespace OpenHabitTracker.Services;

public class SettingsService(AppData appData, IDataAccess dataAccess)
{
    private readonly AppData _appData = appData;
    private readonly IDataAccess _dataAccess = dataAccess;

    public SettingsModel Settings => _appData.Settings;

    public async Task Initialize()
    {
        await _appData.InitializeSettings();
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
