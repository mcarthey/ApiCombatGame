using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// A weekly single-elimination tournament bracket.
/// </summary>
public class Tournament
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>"registration", "in_progress", "completed"</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "registration";

    public int MaxParticipants { get; set; } = 16;
    public int EntryFee { get; set; } = 100;

    /// <summary>JSON array of prize pool: [{place, currency, xp, title}]</summary>
    public string PrizePoolJson { get; set; } = "[]";

    public Guid? WinnerId { get; set; }

    public DateTime RegistrationOpens { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TournamentEntry> Entries { get; set; } = new();
    public List<TournamentMatch> Matches { get; set; } = new();
}

/// <summary>
/// A player's entry in a tournament.
/// </summary>
public class TournamentEntry
{
    [Key]
    public Guid Id { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public Guid TeamId { get; set; }

    /// <summary>Seed position in the bracket (1-based, assigned at tournament start).</summary>
    public int Seed { get; set; }

    /// <summary>How far the player advanced (1 = first round exit, higher = further).</summary>
    public int RoundsWon { get; set; } = 0;

    public bool IsEliminated { get; set; } = false;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A single match within a tournament bracket.
/// </summary>
public class TournamentMatch
{
    [Key]
    public Guid Id { get; set; }

    public Guid TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    /// <summary>Round number (1 = first round, 2 = quarterfinals, etc.)</summary>
    public int Round { get; set; }

    /// <summary>Match position within the round (1-based).</summary>
    public int MatchNumber { get; set; }

    public Guid? Player1Id { get; set; }
    public Guid? Player2Id { get; set; }
    public Guid? WinnerId { get; set; }

    /// <summary>Reference to the Battle record for this match.</summary>
    public Guid? BattleId { get; set; }

    /// <summary>"pending", "in_progress", "completed", "bye"</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "pending";
}
