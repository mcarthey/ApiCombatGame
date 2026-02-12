namespace ApiCombatGame.Models.DTOs.Auth;

/// <summary>Authentication result containing a JWT token for API access.</summary>
public class AuthResponse
{
    /// <summary>Your unique player identifier. Used in leaderboard lookups and battle references.</summary>
    public Guid PlayerId { get; set; }

    /// <summary>JWT Bearer token. Include as <c>Authorization: Bearer &lt;token&gt;</c> in all authenticated requests.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>UTC timestamp when this token expires. Refresh before this time to maintain your session.</summary>
    public DateTime ExpiresAt { get; set; }
}
