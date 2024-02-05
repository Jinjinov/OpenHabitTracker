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
            .AddUniqueIndex(nameof(HabitEntity.Id))
            .AddIndex(nameof(HabitEntity.CategoryId)) // warning: name.ToCamelCase();
            .AddIndex(nameof(HabitEntity.Priority)); // warning: name.ToCamelCase();

        indexedDbDatabaseModel.AddStore(nameof(NoteEntity))
            .WithKey(nameof(NoteEntity.Id))
            .AddUniqueIndex(nameof(NoteEntity.Id))
            .AddIndex(nameof(NoteEntity.CategoryId)) // warning: name.ToCamelCase();
            .AddIndex(nameof(NoteEntity.Priority)); // warning: name.ToCamelCase();

        indexedDbDatabaseModel.AddStore(nameof(TaskEntity))
            .WithKey(nameof(TaskEntity.Id))
            .AddUniqueIndex(nameof(TaskEntity.Id))
            .AddIndex(nameof(TaskEntity.CategoryId)) // warning: name.ToCamelCase();
            .AddIndex(nameof(TaskEntity.Priority)); // warning: name.ToCamelCase();

        indexedDbDatabaseModel.AddStore(nameof(TimeEntity))
            .WithKey(nameof(TimeEntity.Id))
            .AddUniqueIndex(nameof(TimeEntity.Id))
            .AddIndex(nameof(TimeEntity.HabitId)); // warning: name.ToCamelCase();

        indexedDbDatabaseModel.AddStore(nameof(ItemEntity))
            .WithKey(nameof(ItemEntity.Id))
            .AddUniqueIndex(nameof(ItemEntity.Id))
            .AddIndex(nameof(ItemEntity.ParentId)); // warning: name.ToCamelCase();

        indexedDbDatabaseModel.AddStore(nameof(CategoryEntity))
            .WithKey(nameof(CategoryEntity.Id))
            .AddUniqueIndex(nameof(CategoryEntity.Id));

        indexedDbDatabaseModel.AddStore(nameof(PriorityEntity))
            .WithKey(nameof(PriorityEntity.Id))
            .AddUniqueIndex(nameof(PriorityEntity.Id));

        indexedDbDatabaseModel.AddStore(nameof(SettingsEntity))
            .WithKey(nameof(SettingsEntity.Id))
            .AddUniqueIndex(nameof(SettingsEntity.Id));

        return indexedDbDatabaseModel;
    }
}
