using Microsoft.Data.Sqlite;

namespace OpenHabitTracker.Backup;

// SQLite opens files, not streams, so another app's database is copied to a temporary file and opened read-only.
internal sealed class TemporaryDatabase : IDisposable
{
    private readonly string _path;

    public SqliteConnection Connection { get; }

    private TemporaryDatabase(string path, SqliteConnection connection)
    {
        _path = path;
        Connection = connection;
    }

    public static async Task<TemporaryDatabase> Open(Stream stream)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

        await using (FileStream file = System.IO.File.Create(path))
        {
            await stream.CopyToAsync(file);
        }

        // Without pooling the file is closed with the connection, so it can be deleted.
        SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());

        try
        {
            connection.Open();
        }
        catch
        {
            connection.Dispose();
            System.IO.File.Delete(path);

            throw;
        }

        return new TemporaryDatabase(path, connection);
    }

    public HashSet<string> Columns(string table)
    {
        using SqliteCommand command = Connection.CreateCommand();

        command.CommandText = "SELECT name FROM pragma_table_info($table)";
        command.Parameters.AddWithValue("$table", table);

        using SqliteDataReader reader = command.ExecuteReader();

        HashSet<string> columns = new(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
            columns.Add(reader.GetString(0));

        return columns;
    }

    public void Dispose()
    {
        Connection.Dispose();

        System.IO.File.Delete(_path);
    }
}
