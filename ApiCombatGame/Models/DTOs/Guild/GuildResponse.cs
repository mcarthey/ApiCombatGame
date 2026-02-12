namespace ApiCombatGame.Models.DTOs.Guild;

/// <summary>Guild information and membership details.</summary>
public class GuildResponse
{
    /// <summary>Unique guild identifier.</summary>
    public Guid GuildId { get; set; }

    /// <summary>Guild name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short guild tag shown next to member names (3-5 characters).</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Guild's public description and recruitment message.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Username of the guild leader.</summary>
    public string LeaderName { get; set; } = string.Empty;

    /// <summary>Guild level. Higher levels unlock more member slots and boss encounters.</summary>
    public int Level { get; set; }

    /// <summary>Current number of guild members.</summary>
    public int MemberCount { get; set; }

    /// <summary>Maximum members allowed at the guild's current level.</summary>
    public int MaxMembers { get; set; }

    /// <summary>When the guild was founded.</summary>
    public DateTime CreatedAt { get; set; }
}
