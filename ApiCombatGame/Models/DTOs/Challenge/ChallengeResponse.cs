namespace ApiCombatGame.Models.DTOs.Challenge;

public class ChallengeResponse
{
    public Guid ChallengeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int RequiredProgress { get; set; }
    public double ProgressPercentage => RequiredProgress > 0 ? (double)Progress / RequiredProgress * 100 : 0;
    public bool IsCompleted { get; set; }
    public int RewardCurrency { get; set; }
    public int RewardExperience { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class ClaimRequest
{
    public Guid ChallengeId { get; set; }
}
