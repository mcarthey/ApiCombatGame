using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class GuildBossAttempt
{
    [Key]
    public Guid Id { get; set; }

    public Guid GuildBossId { get; set; }
    public GuildBoss GuildBoss { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int DamageDealt { get; set; }
    public bool WasKillingBlow { get; set; } = false;

    /// <summary>
    /// Full battle log JSON for the boss attempt.
    /// </summary>
    public string BattleLogJson { get; set; } = "[]";

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
