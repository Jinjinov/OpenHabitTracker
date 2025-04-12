using System;
using System.Threading.Tasks;

namespace OpenHabitTracker.CloudStorage;

public interface IGoogleDriveBackup : IDataExport
{
    Task ImportData();

    Task<DateTime> GetFileModifiedTime();
}
