using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.Blazor.Web.Data;

public class ApplicationDataAccess : DataAccessBase, IDataAccess
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DataLocation DataLocation { get; } = DataLocation.Local;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public ApplicationDataAccess(IDbContextFactory<ApplicationDbContext> dbContextFactory, UserManager<ApplicationUser> userManager)
    {
        _dbContextFactory = dbContextFactory;

        using ApplicationDbContext context = _dbContextFactory.CreateDbContext();
        context.Database.Migrate();

        _userManager = userManager;
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

    public async Task AddUser(UserEntity user)
    {
        ApplicationUser applicationUser = new()
        {
            UserName = user.UserName,
            Email = user.Email,
        };

        await _userManager.CreateAsync(applicationUser, user.PasswordHash);
    }

    public async Task AddUsers(IReadOnlyList<UserEntity> users)
    {
        foreach (UserEntity user in users)
        {
            await AddUser(user);
        }
    }

    public async Task<IReadOnlyList<UserEntity>> GetUsers()
    {
        List<ApplicationUser> users = await _userManager.Users.ToListAsync();

        return users.Select(u => new UserEntity
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            PasswordHash = u.PasswordHash ?? string.Empty
        }).ToList();
    }

    public async Task<UserEntity?> GetUser(long id)
    {
        ApplicationUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return null;

        return new UserEntity
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PasswordHash = user.PasswordHash ?? string.Empty
        };
    }

    public async Task UpdateUser(UserEntity user)
    {
        ApplicationUser? applicationUser = _userManager.Users.FirstOrDefault(u => u.Id == user.Id);

        if (applicationUser is not null)
        {
            applicationUser.UserName = user.UserName;
            applicationUser.Email = user.Email;

            await _userManager.UpdateAsync(applicationUser);
        }
    }

    public async Task RemoveUser(long id)
    {
        ApplicationUser? applicationUser = _userManager.Users.FirstOrDefault(u => u.Id == id);

        if (applicationUser is not null)
        {
            await _userManager.DeleteAsync(applicationUser);
        }
    }

    public async Task RemoveUsers()
    {
        foreach (ApplicationUser applicationUser in _userManager.Users)
        {
            await _userManager.DeleteAsync(applicationUser);
        }
    }
}
