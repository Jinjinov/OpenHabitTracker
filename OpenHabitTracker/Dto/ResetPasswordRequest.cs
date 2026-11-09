namespace OpenHabitTracker.Dto;

public class ResetPasswordRequest
{
    public required string Username { get; set; }

    public required string Password { get; set; }
}
