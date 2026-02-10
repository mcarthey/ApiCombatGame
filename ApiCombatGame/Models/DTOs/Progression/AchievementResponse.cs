namespace ApiCombatGame.Models.DTOs.Progression;

public class AchievementResponse
{
    public Guid AchievementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Progress { get; set; }
    public int RequiredProgress { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsSecret { get; set; }
    public DateTime? UnlockedAt { get; set; }
}

public class ReplayResponse
{
    public Guid ReplayId { get; set; }
    public Guid BattleId { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
}

public class CreateReplayRequest
{
    public Guid BattleId { get; set; }
}
