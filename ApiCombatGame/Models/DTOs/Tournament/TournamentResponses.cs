namespace ApiCombatGame.Models.DTOs.Tournament;

/// <summary>Tournament information with your entry status.</summary>
public class TournamentInfoResponse
{
    public Guid TournamentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public int EntryFee { get; set; }
    public List<TournamentPrize> Prizes { get; set; } = new();
    public DateTime StartsAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Whether you are registered in this tournament.</summary>
    public bool IsRegistered { get; set; }
    public int? YourSeed { get; set; }
    public bool? IsEliminated { get; set; }
}

public class TournamentPrize
{
    public int Place { get; set; }
    public int Currency { get; set; }
    public int Xp { get; set; }
    public string? Title { get; set; }
}

/// <summary>Tournament bracket view.</summary>
public class TournamentBracketResponse
{
    public Guid TournamentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalRounds { get; set; }
    public List<BracketRound> Rounds { get; set; } = new();
    public string? WinnerUsername { get; set; }
}

public class BracketRound
{
    public int RoundNumber { get; set; }
    public string RoundName { get; set; } = string.Empty;
    public List<BracketMatch> Matches { get; set; } = new();
}

public class BracketMatch
{
    public int MatchNumber { get; set; }
    public string? Player1Username { get; set; }
    public string? Player2Username { get; set; }
    public string? WinnerUsername { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Request to enter a tournament.</summary>
public class TournamentEntryRequest
{
    /// <summary>The team to compete with.</summary>
    public Guid TeamId { get; set; }
}
