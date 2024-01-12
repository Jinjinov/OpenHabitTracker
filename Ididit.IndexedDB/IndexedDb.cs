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
            UseKeyGenerator = false // Unable to use AutoIncrement = false and AutoIncrement = true in the same IndexedDbDatabaseModel
        };

        indexedDbDatabaseModel.AddStore(nameof(HabitEntity))
            .WithKey(nameof(HabitEntity.Id))
            .AddUniqueIndex(nameof(HabitEntity.Id));

        indexedDbDatabaseModel.AddStore(nameof(NoteEntity))
            .WithKey(nameof(NoteEntity.Id))
            .AddUniqueIndex(nameof(NoteEntity.Id));

        indexedDbDatabaseModel.AddStore(nameof(TaskEntity))
            .WithKey(nameof(TaskEntity.Id))
            .AddUniqueIndex(nameof(TaskEntity.Id));

        indexedDbDatabaseModel.AddStore(nameof(TimeEntity))
            .WithKey(nameof(TimeEntity.StartedAt))
            .AddUniqueIndex(nameof(TimeEntity.StartedAt))
            .AddIndex(nameof(TimeEntity.HabitId)); // warning: name.ToCamelCase();

        return indexedDbDatabaseModel;
    }
}
