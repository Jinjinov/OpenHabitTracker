using System.ComponentModel.DataAnnotations;

namespace OpenHabitTracker.Dto;

public class CreateAccountRequest
{
    public required string Username { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    public required string Password { get; set; }
}
