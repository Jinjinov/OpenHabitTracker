using OpenHabitTracker.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OpenHabitTracker.CloudStorage.Mobile;

internal class GoogleDriveBackup : GoogleDriveBase, IGoogleDriveBackup
{
    public GoogleDriveBackup(ClientState clientState) : base(clientState)
    {
    }

    protected override Task<string> CreateFile(string folderId, string content)
    {
        throw new NotImplementedException();
    }

    protected override Task<string> CreateFolder()
    {
        throw new NotImplementedException();
    }

    protected override Task<string> GetFile(string fileId)
    {
        throw new NotImplementedException();
    }

    protected override Task<string> GetFileId(string folderId)
    {
        throw new NotImplementedException();
    }

    protected override Task<DateTime> GetFileModifiedTime(string fileId)
    {
        throw new NotImplementedException();
    }

    protected override Task<string> GetFolderId()
    {
        throw new NotImplementedException();
    }

    protected override Task<string> UpdateFile(string fileId, string content)
    {
        throw new NotImplementedException();
    }
}
