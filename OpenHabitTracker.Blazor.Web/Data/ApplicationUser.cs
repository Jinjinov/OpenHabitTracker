using Microsoft.AspNetCore.Identity;
using OpenHabitTracker.Data.Entities;

namespace OpenHabitTracker.Blazor.Web.Data;

public class ApplicationUser : IdentityUser<long>, IUserEntity
{
}
