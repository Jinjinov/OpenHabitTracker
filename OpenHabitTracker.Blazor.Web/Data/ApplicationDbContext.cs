using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.Blazor.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long>, IApplicationDbContext
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

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    IQueryable<IUserEntity> IApplicationDbContext.Users => Users;

    // Constructor with no argument is required and it is used when adding/removing migrations from class library
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        //Database.EnsureCreated();
        //Database.Migrate();
    }

    // It is required to override this method when adding/removing migrations from class library
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.OnModelCreating();

        base.OnModelCreating(modelBuilder);
    }
}
