namespace OpenHabitTracker.Blazor.Web;

public class AppSettings
{
    public string UserName { get; set; } = "admin";

    public string Email { get; set; } = "admin@admin.com";

    public string Password { get; set; } = "admin";

    public string JwtSecret { get; set; } = "your-extremely-strong-secret-key";
}
