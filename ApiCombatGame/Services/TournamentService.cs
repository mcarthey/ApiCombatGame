using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Tournament;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class TournamentService : ITournamentService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IActivityLedger _ledger;
    private readonly ILogger<TournamentService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TournamentService(GameDbContext context, INotificationService notifications, IActivityLedger ledger, ILogger<TournamentService> logger)
    {
        _context = context;
        _notifications = notifications;
        _ledger = ledger;
        _logger = logger;
    }

    public async Task<TournamentInfoResponse> GetActiveTournamentAsync(Guid playerId)
    {
        var tournament = await _context.Set<Tournament>()
            .Include(t => t.Entries)
            .Where(t => t.Status == "registration" || t.Status == "in_progress")
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (tournament == null)
        {
            // Return info about the most recent completed tournament
            tournament = await _context.Set<Tournament>()
                .Include(t => t.Entries)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (tournament == null)
                return new TournamentInfoResponse { Name = "No tournaments available" };
        }

        var entry = tournament.Entries.FirstOrDefault(e => e.PlayerId == playerId);
        var prizes = JsonSerializer.Deserialize<List<TournamentPrize>>(tournament.PrizePoolJson, JsonOptions) ?? new();

        return new TournamentInfoResponse
        {
            TournamentId = tournament.Id,
            Name = tournament.Name,
            Status = tournament.Status,
            MaxParticipants = tournament.MaxParticipants,
            CurrentParticipants = tournament.Entries.Count,
            EntryFee = tournament.EntryFee,
            Prizes = prizes,
            StartsAt = tournament.StartsAt,
            CompletedAt = tournament.CompletedAt,
            IsRegistered = entry != null,
            YourSeed = entry?.Seed,
            IsEliminated = entry?.IsEliminated
        };
    }

    public async Task<TournamentInfoResponse> EnterTournamentAsync(Guid playerId, TournamentEntryRequest request)
    {
        var tournament = await _context.Set<Tournament>()
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Status == "registration");

        if (tournament == null)
            throw new InvalidOperationException("No tournament currently accepting registrations.");

        if (tournament.Entries.Count >= tournament.MaxParticipants)
            throw new InvalidOperationException("Tournament is full.");

        if (tournament.Entries.Any(e => e.PlayerId == playerId))
            throw new InvalidOperationException("You are already registered for this tournament.");

        // Class tournaments require enrollment in the associated module
        if (tournament.ModuleId.HasValue)
        {
            var isEnrolled = await _context.Set<StudentEnrollment>()
                .AnyAsync(e => e.PlayerId == playerId && e.ModuleId == tournament.ModuleId.Value);
            if (!isEnrolled)
                throw new InvalidOperationException("This is a class tournament. You must be enrolled in the module to participate.");
        }

        // Verify team ownership
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId && t.PlayerId == playerId);
        if (team == null)
            throw new InvalidOperationException("Team not found or doesn't belong to you.");

        // Charge entry fee
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new InvalidOperationException("Player not found.");

        if (player.Currency < tournament.EntryFee)
            throw new InvalidOperationException($"Insufficient currency. Need {tournament.EntryFee}g, have {player.Currency}g.");

        var oldCurrency = player.Currency;
        player.Currency -= tournament.EntryFee;
        _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "Tournament", "TournamentEntry", tournament.Id);

        // Re-check participant count to prevent over-capacity race condition
        var currentCount = await _context.Set<TournamentEntry>()
            .CountAsync(e => e.TournamentId == tournament.Id);
        if (currentCount >= tournament.MaxParticipants)
            throw new InvalidOperationException("Tournament is full.");

        var entry = new TournamentEntry
        {
            Id = Guid.NewGuid(),
            TournamentId = tournament.Id,
            PlayerId = playerId,
            TeamId = request.TeamId
        };

        _context.Set<TournamentEntry>().Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} entered tournament {TournamentId}", playerId, tournament.Id);

        return await GetActiveTournamentAsync(playerId);
    }

    public async Task<TournamentBracketResponse> GetBracketAsync(Guid tournamentId)
    {
        var tournament = await _context.Set<Tournament>()
            .Include(t => t.Matches)
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        if (tournament == null)
            throw new KeyNotFoundException("Tournament not found.");

        // Resolve player usernames
        var playerIds = tournament.Matches
            .SelectMany(m => new[] { m.Player1Id, m.Player2Id, m.WinnerId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var usernames = await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Username);

        int totalRounds = tournament.Matches.Any() ? tournament.Matches.Max(m => m.Round) : 0;
        string? winnerName = tournament.WinnerId.HasValue
            ? usernames.GetValueOrDefault(tournament.WinnerId.Value)
            : null;

        var rounds = Enumerable.Range(1, totalRounds).Select(r =>
        {
            var roundMatches = tournament.Matches
                .Where(m => m.Round == r)
                .OrderBy(m => m.MatchNumber)
                .ToList();

            return new BracketRound
            {
                RoundNumber = r,
                RoundName = GetRoundName(r, totalRounds),
                Matches = roundMatches.Select(m => new BracketMatch
                {
                    MatchNumber = m.MatchNumber,
                    Player1Username = m.Player1Id.HasValue ? usernames.GetValueOrDefault(m.Player1Id.Value) : null,
                    Player2Username = m.Player2Id.HasValue ? usernames.GetValueOrDefault(m.Player2Id.Value) : null,
                    WinnerUsername = m.WinnerId.HasValue ? usernames.GetValueOrDefault(m.WinnerId.Value) : null,
                    Status = m.Status
                }).ToList()
            };
        }).ToList();

        return new TournamentBracketResponse
        {
            TournamentId = tournament.Id,
            Name = tournament.Name,
            TotalRounds = totalRounds,
            Rounds = rounds,
            WinnerUsername = winnerName
        };
    }

    public async Task CreateWeeklyTournamentAsync()
    {
        // Don't create if one already exists in registration or in_progress
        var existing = await _context.Set<Tournament>()
            .AnyAsync(t => t.Status == "registration" || t.Status == "in_progress");

        if (existing) return;

        var prizes = new List<TournamentPrize>
        {
            new() { Place = 1, Currency = 5000, Xp = 1000, Title = "Tournament Champion" },
            new() { Place = 2, Currency = 2500, Xp = 500 },
            new() { Place = 3, Currency = 1000, Xp = 250 },
            new() { Place = 4, Currency = 1000, Xp = 250 }
        };

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = $"Weekly Tournament #{DateTime.UtcNow:yyyyMMdd}",
            Status = "registration",
            MaxParticipants = 16,
            EntryFee = 100,
            PrizePoolJson = JsonSerializer.Serialize(prizes, JsonOptions),
            RegistrationOpens = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow.AddDays(1)
        };

        _context.Set<Tournament>().Add(tournament);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created weekly tournament: {Name}", tournament.Name);
    }

    public async Task ProcessTournamentMatchesAsync()
    {
        // Check for tournaments ready to start
        var readyToStart = await _context.Set<Tournament>()
            .Include(t => t.Entries)
            .Where(t => t.Status == "registration" && t.StartsAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var tournament in readyToStart)
        {
            if (tournament.Entries.Count < 2)
            {
                // Not enough participants — cancel and refund
                tournament.Status = "completed";
                tournament.CompletedAt = DateTime.UtcNow;
                foreach (var entry in tournament.Entries)
                {
                    var player = await _context.Players.FindAsync(entry.PlayerId);
                    if (player != null)
                    {
                        var oldCurrency = player.Currency;
                        player.Currency += tournament.EntryFee;
                        _ledger.LogPlayer(entry.PlayerId, "Currency", oldCurrency, player.Currency, "Tournament", "TournamentRefund", tournament.Id);
                    }
                }
                continue;
            }

            await GenerateBracket(tournament);
            tournament.Status = "in_progress";
        }

        // Process pending matches in active tournaments
        var activeTournaments = await _context.Set<Tournament>()
            .Include(t => t.Matches)
            .Include(t => t.Entries)
            .Where(t => t.Status == "in_progress")
            .ToListAsync();

        foreach (var tournament in activeTournaments)
        {
            await ProcessNextRound(tournament);
        }

        await _context.SaveChangesAsync();
    }

    private async Task GenerateBracket(Tournament tournament)
    {
        // Seed by rating (highest rated gets seed 1)
        var entries = tournament.Entries.ToList();
        var playerIds = entries.Select(e => e.PlayerId).ToList();
        var players = await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Rating);

        var seeded = entries
            .OrderByDescending(e => players.GetValueOrDefault(e.PlayerId, 1000))
            .ToList();

        for (int i = 0; i < seeded.Count; i++)
            seeded[i].Seed = i + 1;

        // Pad to next power of 2
        int bracketSize = 1;
        while (bracketSize < seeded.Count) bracketSize *= 2;

        int totalRounds = (int)Math.Log2(bracketSize);

        // Create first round matches
        for (int i = 0; i < bracketSize / 2; i++)
        {
            var p1 = i < seeded.Count ? seeded[i].PlayerId : (Guid?)null;
            var p2 = (bracketSize - 1 - i) < seeded.Count ? seeded[bracketSize - 1 - i].PlayerId : (Guid?)null;

            var match = new TournamentMatch
            {
                Id = Guid.NewGuid(),
                TournamentId = tournament.Id,
                Round = 1,
                MatchNumber = i + 1,
                Player1Id = p1,
                Player2Id = p2
            };

            // Handle byes (only one player)
            if (p1.HasValue && !p2.HasValue)
            {
                match.WinnerId = p1;
                match.Status = "bye";
            }
            else if (!p1.HasValue && p2.HasValue)
            {
                match.WinnerId = p2;
                match.Status = "bye";
            }

            _context.Set<TournamentMatch>().Add(match);
        }

        // Create placeholder matches for subsequent rounds
        for (int round = 2; round <= totalRounds; round++)
        {
            int matchesInRound = bracketSize / (int)Math.Pow(2, round);
            for (int m = 0; m < matchesInRound; m++)
            {
                _context.Set<TournamentMatch>().Add(new TournamentMatch
                {
                    Id = Guid.NewGuid(),
                    TournamentId = tournament.Id,
                    Round = round,
                    MatchNumber = m + 1,
                    Status = "pending"
                });
            }
        }

        _logger.LogInformation("Generated bracket for tournament {TournamentId}: {Players} players, {Rounds} rounds",
            tournament.Id, seeded.Count, totalRounds);
    }

    private async Task ProcessNextRound(Tournament tournament)
    {
        var currentRound = tournament.Matches
            .Where(m => m.Status != "pending")
            .Select(m => m.Round)
            .DefaultIfEmpty(0)
            .Max();

        if (currentRound == 0) currentRound = 1;

        // Check if all matches in current round are done
        var roundMatches = tournament.Matches.Where(m => m.Round == currentRound).ToList();
        bool roundComplete = roundMatches.All(m => m.Status == "completed" || m.Status == "bye");

        if (!roundComplete)
        {
            // Resolve unresolved matches (simulate battle results)
            foreach (var match in roundMatches.Where(m => m.Status == "pending" && m.Player1Id.HasValue && m.Player2Id.HasValue))
            {
                // Use rating to determine winner (higher rated = more likely to win)
                var p1 = await _context.Players.FindAsync(match.Player1Id!.Value);
                var p2 = await _context.Players.FindAsync(match.Player2Id!.Value);

                if (p1 == null || p2 == null) continue;

                double p1WinChance = 1.0 / (1.0 + Math.Pow(10, (p2.Rating - p1.Rating) / 400.0));
                match.WinnerId = Random.Shared.NextDouble() < p1WinChance ? match.Player1Id : match.Player2Id;
                match.Status = "completed";

                // Mark loser as eliminated
                var loserId = match.WinnerId == match.Player1Id ? match.Player2Id : match.Player1Id;
                var loserEntry = tournament.Entries.FirstOrDefault(e => e.PlayerId == loserId);
                if (loserEntry != null) loserEntry.IsEliminated = true;

                var winnerEntry = tournament.Entries.FirstOrDefault(e => e.PlayerId == match.WinnerId);
                if (winnerEntry != null) winnerEntry.RoundsWon++;

                _logger.LogInformation("Tournament match: {P1} vs {P2} — Winner: {Winner}",
                    p1.Username, p2.Username, match.WinnerId == match.Player1Id ? p1.Username : p2.Username);
            }

            roundComplete = roundMatches.All(m => m.Status == "completed" || m.Status == "bye");
        }

        if (roundComplete)
        {
            int nextRound = currentRound + 1;
            var nextRoundMatches = tournament.Matches.Where(m => m.Round == nextRound).OrderBy(m => m.MatchNumber).ToList();

            if (nextRoundMatches.Count == 0)
            {
                // Tournament is over — determine winner
                var finalMatch = roundMatches.FirstOrDefault(m => m.Status == "completed");
                if (finalMatch?.WinnerId != null)
                {
                    tournament.WinnerId = finalMatch.WinnerId;
                    tournament.Status = "completed";
                    tournament.CompletedAt = DateTime.UtcNow;

                    // Award prizes
                    await AwardPrizes(tournament);
                }
                return;
            }

            // Advance winners to next round
            var winners = roundMatches
                .Where(m => m.WinnerId.HasValue)
                .OrderBy(m => m.MatchNumber)
                .Select(m => m.WinnerId!.Value)
                .ToList();

            for (int i = 0; i < nextRoundMatches.Count && i * 2 < winners.Count; i++)
            {
                nextRoundMatches[i].Player1Id = winners[i * 2];
                nextRoundMatches[i].Player2Id = (i * 2 + 1 < winners.Count) ? winners[i * 2 + 1] : null;

                if (nextRoundMatches[i].Player1Id.HasValue && !nextRoundMatches[i].Player2Id.HasValue)
                {
                    nextRoundMatches[i].WinnerId = nextRoundMatches[i].Player1Id;
                    nextRoundMatches[i].Status = "bye";
                }
            }
        }
    }

    private async Task AwardPrizes(Tournament tournament)
    {
        var prizes = JsonSerializer.Deserialize<List<TournamentPrize>>(tournament.PrizePoolJson, JsonOptions) ?? new();

        // Rank players by rounds won
        var ranked = tournament.Entries
            .OrderByDescending(e => e.RoundsWon)
            .ToList();

        for (int i = 0; i < Math.Min(prizes.Count, ranked.Count); i++)
        {
            var entry = ranked[i];
            var prize = prizes[i];

            var player = await _context.Players.FindAsync(entry.PlayerId);
            if (player == null) continue;

            var oldCurrency = player.Currency;
            var oldXp = player.ExperiencePoints;
            player.Currency += prize.Currency;
            player.ExperiencePoints += prize.Xp;
            _ledger.LogPlayer(entry.PlayerId, "Currency", oldCurrency, player.Currency, "Tournament", "TournamentPrize", tournament.Id);
            _ledger.LogPlayer(entry.PlayerId, "ExperiencePoints", oldXp, player.ExperiencePoints, "Tournament", "TournamentPrize", tournament.Id);

            if (!string.IsNullOrEmpty(prize.Title))
            {
                var title = await _context.Set<PlayerTitle>()
                    .FirstOrDefaultAsync(t => t.Name == prize.Title);
                if (title == null)
                {
                    title = new PlayerTitle
                    {
                        Id = Guid.NewGuid(),
                        Name = prize.Title,
                        Description = $"Won {tournament.Name}",
                        ColorHex = "#FFD700"
                    };
                    _context.Set<PlayerTitle>().Add(title);
                }
                player.ActiveTitleId = title.Id;
            }

            await _notifications.SendAsync(entry.PlayerId, NotificationType.AchievementUnlocked,
                i == 0 ? "Tournament Champion!" : $"Tournament #{i + 1}",
                $"You placed #{i + 1} in {tournament.Name}! Prize: {prize.Currency}g + {prize.Xp} XP",
                $"/api/v1/tournament/bracket/{tournament.Id}");
        }

        _logger.LogInformation("Tournament {TournamentId} completed. Winner: {WinnerId}",
            tournament.Id, tournament.WinnerId);
    }

    private static string GetRoundName(int round, int totalRounds)
    {
        if (round == totalRounds) return "Finals";
        if (round == totalRounds - 1) return "Semifinals";
        if (round == totalRounds - 2) return "Quarterfinals";
        return $"Round {round}";
    }
}
