using Microsoft.AspNetCore.Identity;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Blazor.Web.Data;

public class ApplicationUser : IdentityUser<long>, IUserEntity
{
    string IUserEntity.UserName
    {
        get => base.UserName!;
        set => base.UserName = value;
    }

    string IUserEntity.Email
    {
        get => base.Email!;
        set => base.Email = value;
    }

    string IUserEntity.PasswordHash
    {
        get => base.PasswordHash!;
        set => base.PasswordHash = value;
    }

    public DateTime LastChangeAt { get; set; }
}
