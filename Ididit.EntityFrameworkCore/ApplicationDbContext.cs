using Ididit.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ididit.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<Entity> Base { get; set; }
    public DbSet<HabitEntity> Habits { get; set; }
    public DbSet<NoteEntity> Notes { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<TimeEntity> Times { get; set; }
    public DbSet<ItemEntity> Items { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }
    public DbSet<SettingsEntity> Settings { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Ididit.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entity>().HasIndex(x => x.CategoryId);

        modelBuilder.Entity<TimeEntity>().HasKey(x => x.StartedAt);
        modelBuilder.Entity<TimeEntity>().HasIndex(x => x.HabitId);

        modelBuilder.Entity<ItemEntity>().HasIndex(x => x.ParentId);

        base.OnModelCreating(modelBuilder);
    }
}
