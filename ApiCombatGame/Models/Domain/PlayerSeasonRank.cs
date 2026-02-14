using System.ComponentModel.DataAnnotations;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Models.Domain;

public class PlayerSeasonRank
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Guid SeasonId { get; set; }

    public SeasonTier CurrentTier { get; set; } = SeasonTier.Bronze;
    public int SeasonRating { get; set; } = 1000;
    public int PeakRating { get; set; } = 1000;
    public SeasonTier PeakTier { get; set; } = SeasonTier.Bronze;

    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }

    /// <summary>Whether end-of-season rewards have been claimed.</summary>
    public bool RewardsClaimed { get; set; }

    public DateTime LastRankedBattleAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player Player { get; set; } = null!;
    public Season Season { get; set; } = null!;
}
