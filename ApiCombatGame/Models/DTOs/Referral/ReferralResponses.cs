namespace ApiCombatGame.Models.DTOs.Referral;

/// <summary>Your referral code and stats.</summary>
public class ReferralInfoResponse
{
    /// <summary>Your unique referral code. Share this with friends.</summary>
    public string ReferralCode { get; set; } = string.Empty;

    /// <summary>Total players who signed up using your code.</summary>
    public int TotalReferrals { get; set; }

    /// <summary>Gold earned from referral rewards so far.</summary>
    public int TotalGoldEarned { get; set; }

    /// <summary>Gold reward per successful referral.</summary>
    public int RewardPerReferral { get; set; } = 500;

    /// <summary>Bonus gold the referred player receives.</summary>
    public int ReferredPlayerBonus { get; set; } = 300;
}

/// <summary>A leaderboard entry for the referral program.</summary>
public class ReferralLeaderboardEntry
{
    public int Rank { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TotalReferrals { get; set; }
}

/// <summary>Referral leaderboard response.</summary>
public class ReferralLeaderboardResponse
{
    public List<ReferralLeaderboardEntry> Rankings { get; set; } = new();
    public int? YourRank { get; set; }
    public int YourReferrals { get; set; }
}

/// <summary>Result of redeeming a referral code during registration.</summary>
public class RedeemReferralResponse
{
    public bool Success { get; set; }
    public int BonusGoldAwarded { get; set; }
    public string ReferrerUsername { get; set; } = string.Empty;
}
