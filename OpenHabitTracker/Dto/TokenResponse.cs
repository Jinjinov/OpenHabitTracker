namespace OpenHabitTracker.Dto;

public class TokenResponse
{
    public required string JwtToken { get; set; }

    public required string RefreshToken { get; set; }

    public required DateTimeOffset JwtTokenExpiresAt { get; set; }
}
