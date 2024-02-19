using Ididit.Data;
using Ididit.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;

namespace Ididit.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<InfoEntity> Infos { get; set; }
    public DbSet<HabitEntity> Habits { get; set; }
    public DbSet<NoteEntity> Notes { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<TimeEntity> Times { get; set; }
    public DbSet<ItemEntity> Items { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }
    public DbSet<PriorityEntity> Priorities { get; set; }
    public DbSet<SettingsEntity> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InfoEntity>().HasIndex(x => x.CategoryId);

        modelBuilder.Entity<TimeEntity>().HasIndex(x => x.HabitId);

        modelBuilder.Entity<ItemEntity>().HasIndex(x => x.ParentId);

        modelBuilder.Entity<SettingsEntity>()
            .Property(e => e.SortBy)
            .HasColumnName("SortBy")
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<ContentType, Sort>>(json, (JsonSerializerOptions?)null)!);

        modelBuilder.Entity<SettingsEntity>()
            .Property(e => e.ShowPriority)
            .HasColumnName("ShowPriority")
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<Priority, bool>>(json, (JsonSerializerOptions?)null)!);

        base.OnModelCreating(modelBuilder);
    }

    public void ClearAllTables()
    {
        foreach (IEntityType entityType in Model.GetEntityTypes())
        {
            string? tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                Database.ExecuteSqlRaw($"DELETE FROM {tableName}");
                Database.ExecuteSqlRaw($"DELETE FROM sqlite_sequence WHERE name = '{tableName}'");
            }
        }

        foreach (EntityEntry entry in ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
