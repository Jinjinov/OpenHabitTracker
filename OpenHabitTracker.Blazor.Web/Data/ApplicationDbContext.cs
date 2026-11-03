using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

    // The account this context acts for, set by ApplicationDataAccess for every operation.
    public long CurrentUserId { get; set; }

    // Constructor with no argument is required and it is used when adding/removing migrations from class library
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        // this is very bad practice and causes many different problems even in long lived DbContext
        // in short lived DbContext this would try to migrate every time DB is used
        // this could also cause problems for "dotnet ef migrations add"

        //Database.EnsureCreated();
        //Database.Migrate();
    }

    // It is required to override this method when adding/removing migrations from class library
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.OnModelCreating();

        // Only the server filters by owner, so only the server indexes the owner column.
        modelBuilder.Entity<ContentEntity>().HasIndex(x => x.UserId);

        modelBuilder.Entity<CategoryEntity>().HasIndex(x => x.UserId);

        modelBuilder.Entity<SettingsEntity>().HasIndex(x => x.UserId);

        // The per-parent lazy loads query both columns at once.
        modelBuilder.Entity<TimeEntity>().HasIndex(x => new { x.UserId, x.HabitId });

        modelBuilder.Entity<ItemEntity>().HasIndex(x => new { x.UserId, x.ParentId });

        // EF reads CurrentUserId from the context instance running the query, not from this one.
        // ContentEntity is the root of the habit, note and task hierarchy, the only place EF allows
        // its filter. Never filter ApplicationUser: sign-in looks users up before there is a current one.
        modelBuilder.Entity<ContentEntity>().HasQueryFilter(x => x.UserId == CurrentUserId);
        modelBuilder.Entity<ItemEntity>().HasQueryFilter(x => x.UserId == CurrentUserId);
        modelBuilder.Entity<TimeEntity>().HasQueryFilter(x => x.UserId == CurrentUserId);
        modelBuilder.Entity<CategoryEntity>().HasQueryFilter(x => x.UserId == CurrentUserId);
        modelBuilder.Entity<SettingsEntity>().HasQueryFilter(x => x.UserId == CurrentUserId);

        base.OnModelCreating(modelBuilder);
    }

    // Rows from before accounts existed are at zero. Nothing else can leave a row there, because a write
    // without an account throws, so this finds nothing on every start after the first.
    public async Task AssignUnownedRows(long userId)
    {
        await Contents.IgnoreQueryFilters().Where(x => x.UserId == 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UserId, userId));
        await Items.IgnoreQueryFilters().Where(x => x.UserId == 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UserId, userId));
        await Times.IgnoreQueryFilters().Where(x => x.UserId == 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UserId, userId));
        await Categories.IgnoreQueryFilters().Where(x => x.UserId == 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UserId, userId));
        await Settings.IgnoreQueryFilters().Where(x => x.UserId == 0).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UserId, userId));
    }

    // Query filters do not apply to writes, so every write to a row that belongs to an account is
    // checked here. The API saves entities straight from the request body, so neither the submitted
    // UserId nor the submitted Id can be trusted.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        foreach (EntityEntry<IUserScopedEntity> entry in GetUserScopedWrites())
        {
            if (entry.State != EntityState.Added && !IsCurrentUsers(entry.GetDatabaseValues()))
                throw NotCurrentUsers(entry);

            StampCurrentUser(entry);
        }

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (EntityEntry<IUserScopedEntity> entry in GetUserScopedWrites())
        {
            if (entry.State != EntityState.Added && !IsCurrentUsers(await entry.GetDatabaseValuesAsync(cancellationToken)))
                throw NotCurrentUsers(entry);

            StampCurrentUser(entry);
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private List<EntityEntry<IUserScopedEntity>> GetUserScopedWrites()
    {
        List<EntityEntry<IUserScopedEntity>> entries = ChangeTracker.Entries<IUserScopedEntity>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        // A row written without an account would be owned by nobody and never shown again.
        if (entries.Count > 0 && CurrentUserId == 0)
            throw new InvalidOperationException("Rows that belong to an account cannot be written without a signed-in account.");

        return entries;
    }

    // Reads the stored row, not the submitted one, and ignores the query filters while doing it, so a
    // row that exists but belongs to another account is told apart from the current account's own.
    private bool IsCurrentUsers(PropertyValues? storedValues) =>
        storedValues is not null && storedValues.GetValue<long>(nameof(IUserScopedEntity.UserId)) == CurrentUserId;

    private void StampCurrentUser(EntityEntry<IUserScopedEntity> entry)
    {
        if (entry.State is EntityState.Added or EntityState.Modified)
            entry.Entity.UserId = CurrentUserId;
    }

    private static InvalidOperationException NotCurrentUsers(EntityEntry<IUserScopedEntity> entry) =>
        new($"{entry.Metadata.ClrType.Name} {entry.Property("Id").CurrentValue} does not belong to the signed-in account.");
}
