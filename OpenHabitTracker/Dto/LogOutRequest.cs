namespace OpenHabitTracker.Dto;

public class LogOutRequest
{
    // A device sends its own refresh token, which is revoked; a browser sends none.
    public string? RefreshToken { get; set; }
}
