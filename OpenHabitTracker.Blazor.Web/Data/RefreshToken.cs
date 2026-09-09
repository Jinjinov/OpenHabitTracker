namespace OpenHabitTracker.Blazor.Web.Data;

public class RefreshToken
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiryDate { get; set; }

    // The value this row held before the last rotation. Accepted for a short grace window, so a
    // refresh whose response never arrived can be retried; presented after that, it is a replay.
    public string? PreviousToken { get; set; }

    public DateTime? PreviousTokenValidUntil { get; set; }
}
