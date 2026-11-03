using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenHabitTracker.Data;
using OpenHabitTracker.Data.Entities;
using OpenHabitTracker.EntityFrameworkCore;

namespace OpenHabitTracker.Blazor.Web.Data;

// No migration in here: this class is constructed for every request and every circuit, and Program
// migrates through Initialize() before the server accepts the first one.
public class ApplicationDataAccess(IDbContextFactory<ApplicationDbContext> dbContextFactory, UserManager<ApplicationUser> userManager, ICurrentUser currentUser) : DataAccessBase, IDataAccess
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public bool MultipleServicesCanModifyData { get; } = true;

    public DataLocation DataLocation { get; } = DataLocation.Local;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;

    private readonly ICurrentUser _currentUser = currentUser;

    // The user is resolved per operation rather than once in the constructor, because startup constructs
    // this class to migrate, where there is no user.
    protected override async Task ExecuteWithDbContext(Func<IApplicationDbContext, Task> action)
    {
        using ApplicationDbContext context = _dbContextFactory.CreateDbContext();
        context.CurrentUserId = await _currentUser.GetUserId();
        await action(context);
    }

    protected override async Task<T> ExecuteWithDbContext<T>(Func<IApplicationDbContext, Task<T>> action)
    {
        using ApplicationDbContext context = _dbContextFactory.CreateDbContext();
        context.CurrentUserId = await _currentUser.GetUserId();
        return await action(context);
    }

    // Each device polls its own account's LastChangeAt, so a change stamps the account that made it
    // rather than whichever account happens to come first.
    protected override async Task SaveChanges(IApplicationDbContext dataContext)
    {
        ApplicationDbContext context = (ApplicationDbContext)dataContext;

        if (await context.Users.FindAsync(context.CurrentUserId) is ApplicationUser user)
        {
            user.LastChangeAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    // The server's own UI calls the account methods from the circuit, past the controller's attributes,
    // so they check the role themselves.
    private async Task EnsureAdministrator()
    {
        if (!await _currentUser.IsAdministrator())
            throw new UnauthorizedAccessException("Only the administrator can manage accounts.");
    }

    public async Task AddUser(UserEntity user)
    {
        await EnsureAdministrator();

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

    // Only the caller's own account, and never its password hash: devices read users[0] as their own
    // account, and the API returns whatever this returns.
    public async Task<IReadOnlyList<UserEntity>> GetUsers() => await ExecuteWithDbContext(async dataContext =>
    {
        long currentUserId = ((ApplicationDbContext)dataContext).CurrentUserId;

        //List<ApplicationUser> users = await _userManager.Users.ToListAsync();
        List<IUserEntity> users = await dataContext.Users.Where(u => u.Id == currentUserId).ToListAsync();

        return users.Select(u => new UserEntity
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            LastChangeAt = u.LastChangeAt
        }).ToList();
    });

    public async Task<UserEntity?> GetUser(long id) => await ExecuteWithDbContext(async dataContext =>
    {
        if (id != ((ApplicationDbContext)dataContext).CurrentUserId)
            return null;

        //ApplicationUser? user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        IUserEntity? user = await dataContext.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return null;

        return new UserEntity
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            LastChangeAt = user.LastChangeAt
        };
    });

    public async Task UpdateUser(UserEntity user)
    {
        await EnsureAdministrator();

        ApplicationUser? applicationUser = _userManager.Users.FirstOrDefault(u => u.Id == user.Id);

        if (applicationUser is not null)
        {
            applicationUser.UserName = user.UserName;
            applicationUser.Email = user.Email;
            applicationUser.LastChangeAt = user.LastChangeAt;

            await _userManager.UpdateAsync(applicationUser);
        }
    }

    public async Task RemoveUser(long id)
    {
        await EnsureAdministrator();

        ApplicationUser? applicationUser = _userManager.Users.FirstOrDefault(u => u.Id == id);

        if (applicationUser is not null)
        {
            await RemoveAccount(applicationUser);
        }
    }

    public async Task RemoveUsers()
    {
        await EnsureAdministrator();

        foreach (ApplicationUser applicationUser in _userManager.Users)
        {
            await RemoveAccount(applicationUser);
        }
    }

    // Refresh tokens are keyed by username, so one left behind would sign its device in to a new account
    // created with the same name.
    private async Task RemoveAccount(ApplicationUser applicationUser)
    {
        string? userName = applicationUser.UserName;

        await _userManager.DeleteAsync(applicationUser);

        using ApplicationDbContext context = _dbContextFactory.CreateDbContext();

        await context.RefreshTokens.Where(x => x.Username == userName).ExecuteDeleteAsync();
    }
}
