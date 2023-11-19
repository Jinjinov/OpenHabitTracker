using DnetIndexedDb;
using DnetIndexedDb.Fluent;
using DnetIndexedDb.Models;
using Ididit.Data.Entities;
using Microsoft.JSInterop;

namespace Ididit.IndexedDB;

public class IndexedDb(IJSRuntime jsRuntime, IndexedDbOptions<IndexedDb> options) : IndexedDbInterop(jsRuntime, options)
{
    public static IndexedDbDatabaseModel GetDatabaseModel()
    {
        IndexedDbDatabaseModel indexedDbDatabaseModel = new()
        {
            Name = "Ididit",
            Version = 1,
            DbModelId = 0,
            UseKeyGenerator = true // Unable to use AutoIncrement = false and AutoIncrement = true in the same IndexedDbDatabaseModel
        };

        indexedDbDatabaseModel.AddStore(nameof(HabitEntity))
            .WithAutoIncrementingKey(nameof(HabitEntity.Id))
            .AddUniqueIndex(nameof(HabitEntity.Id))
            .AddIndex(nameof(HabitEntity.Title));

        return indexedDbDatabaseModel;
    }
}
