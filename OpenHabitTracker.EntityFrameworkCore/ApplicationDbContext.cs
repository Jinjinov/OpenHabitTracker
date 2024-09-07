using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;

namespace OpenHabitTracker.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<ContentEntity> Contents { get; set; }
    public DbSet<HabitEntity> Habits { get; set; }
    public DbSet<NoteEntity> Notes { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<TimeEntity> Times { get; set; }
    public DbSet<ItemEntity> Items { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }
    public DbSet<PriorityEntity> Priorities { get; set; }
    public DbSet<SettingsEntity> Settings { get; set; }

    // Constructor with no argument is required and it is used when adding/removing migrations from class library
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        //Database.EnsureCreated();
        Database.Migrate();
    }

    // It is required to override this method when adding/removing migrations from class library
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentEntity>().HasIndex(x => x.CategoryId);

        modelBuilder.Entity<TimeEntity>().HasIndex(x => x.HabitId);

        modelBuilder.Entity<ItemEntity>().HasIndex(x => x.ParentId);

        var dictionaryComparer = new ValueComparer<Dictionary<ContentType, Sort>>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
        );

        modelBuilder.Entity<SettingsEntity>()
            .Property(e => e.SortBy)
            .HasColumnName("SortBy")
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<ContentType, Sort>>(json, (JsonSerializerOptions?)null)!,
                dictionaryComparer);

        var boolDictionaryComparer = new ValueComparer<Dictionary<Priority, bool>>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
        );

        modelBuilder.Entity<SettingsEntity>()
            .Property(e => e.ShowPriority)
            .HasColumnName("ShowPriority")
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<Priority, bool>>(json, (JsonSerializerOptions?)null)!,
                boolDictionaryComparer);

        base.OnModelCreating(modelBuilder);
    }

    public void ClearAllTables()
    {
        foreach (IEntityType entityType in Model.GetEntityTypes())
        {
            string? tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
#pragma warning disable EF1002 // Risk of vulnerability to SQL injection.
                Database.ExecuteSqlRaw($"DELETE FROM {tableName}");
                Database.ExecuteSqlRaw($"DELETE FROM sqlite_sequence WHERE name = '{tableName}'");
#pragma warning restore EF1002 // Risk of vulnerability to SQL injection.
            }
        }

        foreach (EntityEntry entry in ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
