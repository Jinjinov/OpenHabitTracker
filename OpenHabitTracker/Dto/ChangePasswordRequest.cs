namespace OpenHabitTracker.Dto;

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }

    public required string NewPassword { get; set; }

    // A device sends its own refresh token, which stays valid while the other devices' are revoked.
    public string? RefreshToken { get; set; }
}
