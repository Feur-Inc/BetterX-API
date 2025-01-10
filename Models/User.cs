namespace BetterX_API.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Inactive;
    public string? AppVersion { get; set; }
    public bool UsedMoreThanHour { get; set; }
    public string? Country { get; set; }
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}

public enum UserStatus
{
    Active,
    Inactive,
    DoNotDisturb
}
