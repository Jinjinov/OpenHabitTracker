using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.EntityFrameworkCore;

public class DataAccess : DataAccessBase, IDataAccess
{
    public DataLocation DataLocation { get; } = DataLocation.Local;

    public DataAccess(ApplicationDbContext dataContext)
    {
        _dataContext = dataContext;
        _dataContext.Database.Migrate();
    }

    public async Task AddUser(UserEntity user)
    {
        _dataContext.Add(user);
        await SaveChanges();
    }

    public async Task AddUsers(IReadOnlyList<UserEntity> users)
    {
        _dataContext.AddRange(users);
        await SaveChanges();
    }

    public async Task<IReadOnlyList<UserEntity>> GetUsers()
    {
        return await _dataContext.Set<UserEntity>().ToListAsync();
    }

    public async Task<UserEntity?> GetUser(long id)
    {
        return await _dataContext.Set<UserEntity>().FindAsync(id);
    }

    public async Task UpdateUser(UserEntity user)
    {
        _dataContext.Update(user);
        await SaveChanges();
    }

    public async Task RemoveUser(long id)
    {
        var entity = _dataContext.Set<UserEntity>().Find(id);
        if (entity is not null)
            _dataContext.Set<UserEntity>().Remove(entity);
        await SaveChanges();
    }

    public async Task RemoveUsers()
    {
        await _dataContext.Users.ExecuteDeleteAsync();
        await SaveChanges();
    }
}
