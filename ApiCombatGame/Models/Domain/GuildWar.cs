using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// A weekly guild vs guild war matchup. Each member's ranked wins contribute points.
/// </summary>
public class GuildWar
{
    [Key]
    public Guid Id { get; set; }

    public Guid Guild1Id { get; set; }
    public Guild Guild1 { get; set; } = null!;

    public Guid Guild2Id { get; set; }
    public Guild Guild2 { get; set; } = null!;

    public int Guild1Score { get; set; } = 0;
    public int Guild2Score { get; set; } = 0;

    /// <summary>"active", "completed", "cancelled"</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "active";

    /// <summary>ID of the winning guild (null if ongoing or draw).</summary>
    public Guid? WinnerGuildId { get; set; }

    /// <summary>Treasury bonus awarded to the winner.</summary>
    public int TreasuryReward { get; set; } = 500;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<GuildWarContribution> Contributions { get; set; } = new();
}

/// <summary>
/// Tracks individual player contributions to a guild war.
/// </summary>
public class GuildWarContribution
{
    [Key]
    public Guid Id { get; set; }

    public Guid GuildWarId { get; set; }
    public GuildWar GuildWar { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public Guid GuildId { get; set; }

    /// <summary>Points earned from ranked battle wins during the war.</summary>
    public int Points { get; set; } = 0;

    /// <summary>Number of ranked wins during the war.</summary>
    public int Wins { get; set; } = 0;
}
