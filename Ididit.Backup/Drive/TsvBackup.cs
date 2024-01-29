using CsvHelper;
using CsvHelper.Configuration;
using Ididit.Data;
using System.Globalization;

namespace Ididit.Backup.Drive;

public class TsvBackup(AppData appData)
{
    private readonly AppData _appData = appData;

    private readonly CsvConfiguration _csvConfiguration = new(CultureInfo.InvariantCulture)
    {
        Delimiter = "\t",
        Quote = (char)1,
        Mode = CsvMode.NoEscape
    };

    public async Task<string> GetDataExportFileString()
    {
        UserData userData = await _appData.GetUserData();

        using StringWriter stringWriter = new();

        using (CsvWriter csvWriter = new(stringWriter, _csvConfiguration))
        {
            csvWriter.WriteRecords(userData.Categories);
        }

        return stringWriter.ToString();
    }
}
