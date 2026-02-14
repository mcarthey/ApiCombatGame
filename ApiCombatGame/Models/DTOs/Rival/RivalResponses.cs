namespace ApiCombatGame.Models.DTOs.Rival;

/// <summary>Your current rival assignment.</summary>
public class RivalInfoResponse
{
    /// <summary>Your rival's player ID.</summary>
    public Guid RivalId { get; set; }

    /// <summary>Your rival's username.</summary>
    public string RivalUsername { get; set; } = string.Empty;

    /// <summary>Your rival's current API rating.</summary>
    public int RivalRating { get; set; }

    /// <summary>Your rival's level.</summary>
    public int RivalLevel { get; set; }

    /// <summary>Wins you've scored against this rival.</summary>
    public int WinsAgainstRival { get; set; }

    /// <summary>Losses against this rival.</summary>
    public int LossesAgainstRival { get; set; }

    /// <summary>Bonus gold earned from rival victories so far.</summary>
    public int BonusGoldEarned { get; set; }

    /// <summary>Bonus gold awarded per win against your rival.</summary>
    public int BonusPerWin { get; set; } = 100;

    /// <summary>When this rival assignment expires and a new one is assigned.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Whether you currently have a rival assigned.</summary>
    public bool HasRival { get; set; }
}
