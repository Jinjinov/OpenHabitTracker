using System.Threading.Tasks;

namespace OpenHabitTracker.CloudStorage;

public interface IDataExport
{
    bool UnsavedChanges { get; }

    DataFormat DataFormat { get; }

    Task ExportData();
}
