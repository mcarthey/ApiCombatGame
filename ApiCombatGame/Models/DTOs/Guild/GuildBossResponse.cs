namespace ApiCombatGame.Models.DTOs.Guild;

public class GuildBossResponse
{
    public Guid BossId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public double HpPercentage => MaxHp > 0 ? (double)CurrentHp / MaxHp * 100 : 0;
    public DateTime ExpiresAt { get; set; }
    public bool IsDefeated { get; set; }
    public int RewardCurrency { get; set; }
    public int RewardExperience { get; set; }
}

public class BossAttemptRequest
{
    public Guid BossId { get; set; }
    public Guid TeamId { get; set; }
}

public class BossAttemptResponse
{
    public Guid AttemptId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int DamageDealt { get; set; }
    public bool WasKillingBlow { get; set; }
    public DateTime AttemptedAt { get; set; }
}
