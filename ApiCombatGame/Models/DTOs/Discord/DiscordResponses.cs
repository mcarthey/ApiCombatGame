namespace ApiCombatGame.Models.DTOs.Discord;

public class DiscordLinkResponse
{
    public string DiscordUserId { get; set; } = string.Empty;
    public string? DiscordUsername { get; set; }
    public bool IsVerified { get; set; }
    public bool NotificationsEnabled { get; set; }
    public string? VerificationCode { get; set; }
    public DateTime LinkedAt { get; set; }
}

public class LinkDiscordRequest
{
    public string DiscordUserId { get; set; } = string.Empty;
    public string? DiscordUsername { get; set; }
}

public class VerifyDiscordRequest
{
    public string VerificationCode { get; set; } = string.Empty;
}

public class RegisterWebhookRequest
{
    public string WebhookUrl { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<string> EventTypes { get; set; } = new() { "battle_won", "battle_lost", "level_up" };
}

public class WebhookResponse
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string EventTypes { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DiscordProfileResponse
{
    public string Username { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Rating { get; set; }
    public int WinStreak { get; set; }
    public string CurrentTier { get; set; } = "Free";
    public string? Badge { get; set; }
    public string? GuildName { get; set; }
}
