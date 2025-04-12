using OpenHabitTracker.App;
using OpenHabitTracker.Data;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenHabitTracker.CloudStorage;

public abstract class GoogleDriveBase : IGoogleDriveBackup
{
    protected const string _fileName = "ididit.json";
    protected const string _fileDescription = "ididit backup";
    protected const string _fileMimeType = "application/json";

    protected const string _folderName = "ididit";
    protected const string _folderDescription = "ididit backup";
    protected const string _folderMimeType = "application/vnd.google-apps.folder";

    protected readonly JsonSerializerOptions _options = new() { IncludeFields = true, WriteIndented = true };

    public static bool IsGoogleDriveAvailable => OperatingSystem.IsBrowser() || OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst();

    public bool UnsavedChanges { get; private set; }

    public DataFormat DataFormat => DataFormat.GoogleDrive;

    private readonly ClientState _clientState;

    protected GoogleDriveBase(ClientState clientState)
    {
        _clientState = clientState;

        //_clientState.DataChanged += (sender, e) => UnsavedChanges = true;
    }

    public async Task ImportData()
    {
        string text = await LoadFile();

        UserImportExportData data = JsonSerializer.Deserialize<UserImportExportData>(text, _options) ?? throw new InvalidDataException("Can't deserialize JSON");

        await _clientState.SetUserData(data);
    }

    public async Task ExportData()
    {
        UserImportExportData data = await _clientState.GetUserData();

        string jsonString = JsonSerializer.Serialize(data, _options);

        await SaveFile(jsonString);

        UnsavedChanges = false;
    }

    protected async Task SaveFile(string content)
    {
        string folderId = await GetFolderId();

        if (string.IsNullOrEmpty(folderId))
        {
            folderId = await CreateFolder();
        }

        string fileId = await GetFileId(folderId);

        if (string.IsNullOrEmpty(fileId))
        {
            await CreateFile(folderId, content);
        }
        else
        {
            await UpdateFile(fileId, content);
        }
    }

    protected async Task<string> LoadFile()
    {
        string folderId = await GetFolderId();

        if (string.IsNullOrEmpty(folderId))
        {
            return string.Empty;
        }

        string fileId = await GetFileId(folderId);

        if (string.IsNullOrEmpty(fileId))
        {
            return string.Empty;
        }
        else
        {
            return await GetFile(fileId);
        }
    }

    public async Task<DateTime> GetFileModifiedTime()
    {
        string folderId = await GetFolderId();

        if (string.IsNullOrEmpty(folderId))
        {
            return DateTime.MinValue;
        }

        string fileId = await GetFileId(folderId);

        if (string.IsNullOrEmpty(fileId))
        {
            return DateTime.MinValue;
        }
        else
        {
            return await GetFileModifiedTime(fileId);
        }
    }

    protected abstract Task<string> GetFile(string fileId);

    protected abstract Task<DateTime> GetFileModifiedTime(string fileId);

    protected abstract Task<string> CreateFolder();

    protected abstract Task<string> CreateFile(string folderId, string content);

    protected abstract Task<string> UpdateFile(string fileId, string content);

    protected abstract Task<string> GetFolderId();

    protected abstract Task<string> GetFileId(string folderId);
}
