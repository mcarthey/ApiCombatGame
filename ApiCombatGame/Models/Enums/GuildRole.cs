namespace ApiCombatGame.Models.Enums;

/// <summary>
/// A player's role and permission level within a guild.
/// </summary>
public enum GuildRole
{
    /// <summary>Standard guild member. Can participate in boss raids and view guild info.</summary>
    Member,
    /// <summary>Promoted member with management permissions such as invite and kick.</summary>
    Officer,
    /// <summary>Guild founder and owner. Full control over settings and officer promotion.</summary>
    Leader
}
