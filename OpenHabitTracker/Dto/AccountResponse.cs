namespace OpenHabitTracker.Dto;

public class AccountResponse
{
    public required string Username { get; set; }

    public required string Email { get; set; }

    public required bool IsAdministrator { get; set; }
}
