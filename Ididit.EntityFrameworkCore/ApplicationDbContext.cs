using Ididit.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ididit.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<HabitEntity> Habits { get; set; }
    public DbSet<NoteEntity> Notes { get; set; }
    public DbSet<TaskEntity> Tasks { get; set; }
    public DbSet<TimeEntity> Times { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Ididit.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimeEntity>().HasKey(x => x.Time);
        modelBuilder.Entity<TimeEntity>().HasIndex(x => x.HabitId);

        base.OnModelCreating(modelBuilder);
    }
}
