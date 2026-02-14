namespace ApiCombatGame.Models.DTOs.Creator;

public class CreatorProfileResponse
{
    public Guid Id { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public int GemsEarned { get; set; }
    public int TotalDownloads { get; set; }
    public int ModulesCreated { get; set; }
    public bool IsSpotlighted { get; set; }
    public DateTime AppliedAt { get; set; }
}

public class ApplyCreatorRequest
{
    public string CreatorName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
}

public class SpotlightResponse
{
    public List<CreatorProfileResponse> FeaturedCreators { get; set; } = new();
    public string Month { get; set; } = string.Empty;
}

public class CreatorStatsResponse
{
    public string CreatorName { get; set; } = string.Empty;
    public int TotalStrategiesUploaded { get; set; }
    public int TotalStrategyDownloads { get; set; }
    public double AverageStrategyRating { get; set; }
    public int ModulesCreated { get; set; }
    public int StudentsEnrolled { get; set; }
    public int GemsEarned { get; set; }
    public bool IsVerified { get; set; }
}
