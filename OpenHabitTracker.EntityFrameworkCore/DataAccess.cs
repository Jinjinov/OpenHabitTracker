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
