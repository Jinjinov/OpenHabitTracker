using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.EntityFrameworkCore;

public class DataAccess : DataAccessBase, IDataAccess
{
    public bool MultipleServicesCanModifyData { get; } = false;

    public DataLocation DataLocation { get; } = DataLocation.Local;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public DataAccess(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;

        using ApplicationDbContext context = _dbContextFactory.CreateDbContext();

        // Migrate() is called here in the constructor as a safety net: without an explicit Initialize() call at
        // startup, DI resolves DataAccess on first use (e.g. the first Blazor component that needs data), which
        // could be deep into the app's lifecycle with the UI already loading. Having it here guarantees the schema
        // is always ready regardless of when DI first resolves this service.
        // Initialize() is also called explicitly at startup to guarantee migration runs early and predictably.

        // Delete any stale migration lock left behind by a previous run that was killed mid-migration.
        // Without this, the app hangs forever on startup trying to acquire a lock that will never be released.
        // Safe because MAUI/desktop apps are single-instance, so there is never a legitimate concurrent migration.
        try
        {
            context.Database.ExecuteSqlRaw("DELETE FROM \"__EFMigrationsLock\"");
        }
        catch (Exception)
        {
            // Table does not exist on a fresh install - that is fine, Migrate() will create it.
        }

        context.Database.Migrate();
    }

    protected override async Task ExecuteWithDbContext(Func<IApplicationDbContext, Task> action)
    {
        using ApplicationDbContext context = _dbContextFactory.CreateDbContext();
        await action(context);
    }

    protected override async Task<T> ExecuteWithDbContext<T>(Func<IApplicationDbContext, Task<T>> action)
    {
        using ApplicationDbContext context = _dbContextFactory.CreateDbContext();
        return await action(context);
    }

    public async Task AddUser(UserEntity user) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Add(user);
        await SaveChanges(dataContext);
    });

    public async Task AddUsers(IReadOnlyList<UserEntity> users) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.AddRange(users);
        await SaveChanges(dataContext);
    });

    public async Task<IReadOnlyList<UserEntity>> GetUsers() => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Set<UserEntity>().ToListAsync();
    });

    public async Task<UserEntity?> GetUser(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        return await dataContext.Set<UserEntity>().FindAsync(id);
    });

    public async Task UpdateUser(UserEntity user) => await ExecuteWithDbContext(async dataContext =>
    {
        dataContext.Update(user);
        await SaveChanges(dataContext);
    });

    public async Task RemoveUser(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        var entity = dataContext.Set<UserEntity>().Find(id);
        if (entity is not null)
            dataContext.Set<UserEntity>().Remove(entity);
        await SaveChanges(dataContext);
    });

    public async Task RemoveUsers() => await ExecuteWithDbContext(async dataContext =>
    {
        await dataContext.Users.ExecuteDeleteAsync();
        await SaveChanges(dataContext);
    });
}
