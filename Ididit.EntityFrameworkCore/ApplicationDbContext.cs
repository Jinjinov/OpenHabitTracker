using Ididit.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ididit.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Entity> Base { get; set; }
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
        modelBuilder.Entity<Entity>().HasIndex(x => x.CategoryId);

        modelBuilder.Entity<TimeEntity>().HasIndex(x => x.HabitId);

        modelBuilder.Entity<ItemEntity>().HasIndex(x => x.ParentId);

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

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
