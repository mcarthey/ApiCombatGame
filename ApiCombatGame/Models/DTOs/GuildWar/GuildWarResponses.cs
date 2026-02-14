namespace ApiCombatGame.Models.DTOs.GuildWar;

/// <summary>Current guild war status.</summary>
public class GuildWarStatusResponse
{
    /// <summary>Whether your guild is currently in a war.</summary>
    public bool IsAtWar { get; set; }

    public Guid? WarId { get; set; }

    /// <summary>Your guild's name and score.</summary>
    public string YourGuild { get; set; } = string.Empty;
    public int YourScore { get; set; }

    /// <summary>Opposing guild's name and score.</summary>
    public string OpponentGuild { get; set; } = string.Empty;
    public int OpponentScore { get; set; }

    /// <summary>When the war ends.</summary>
    public DateTime? EndsAt { get; set; }
    public int DaysRemaining { get; set; }

    /// <summary>Treasury bonus for winning.</summary>
    public int TreasuryReward { get; set; }

    /// <summary>Top contributors from your guild.</summary>
    public List<WarContributorDto> TopContributors { get; set; } = new();
}

public class WarContributorDto
{
    public string Username { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Wins { get; set; }
}

/// <summary>Guild war history entry.</summary>
public class GuildWarHistoryResponse
{
    public Guid WarId { get; set; }
    public string OpponentGuild { get; set; } = string.Empty;
    public int YourScore { get; set; }
    public int OpponentScore { get; set; }
    public string Result { get; set; } = string.Empty;
    public int TreasuryReward { get; set; }
    public DateTime EndsAt { get; set; }
}
