namespace OpenHabitTracker.Blazor.Web.Data;

public class RefreshToken
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiryDate { get; set; }
}
