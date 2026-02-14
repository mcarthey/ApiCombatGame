using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.GuildWar;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class GuildWarService : IGuildWarService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<GuildWarService> _logger;

    private const int WarDurationDays = 7;
    private const int PointsPerWin = 10;
    private const int BaseTreasuryReward = 500;

    public GuildWarService(GameDbContext context, INotificationService notifications, ILogger<GuildWarService> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<GuildWarStatusResponse> GetCurrentWarAsync(Guid playerId)
    {
        var membership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);

        if (membership == null)
            return new GuildWarStatusResponse { IsAtWar = false };

        var guildId = membership.GuildId;

        var war = await _context.Set<GuildWar>()
            .Include(w => w.Guild1)
            .Include(w => w.Guild2)
            .Include(w => w.Contributions)
            .FirstOrDefaultAsync(w => w.Status == "active"
                && (w.Guild1Id == guildId || w.Guild2Id == guildId));

        if (war == null)
            return new GuildWarStatusResponse { IsAtWar = false };

        bool isGuild1 = war.Guild1Id == guildId;
        var myGuild = isGuild1 ? war.Guild1 : war.Guild2;
        var opponentGuild = isGuild1 ? war.Guild2 : war.Guild1;

        var myContributions = war.Contributions
            .Where(c => c.GuildId == guildId)
            .OrderByDescending(c => c.Points)
            .Take(5)
            .ToList();

        // Resolve usernames
        var playerIds = myContributions.Select(c => c.PlayerId).ToList();
        var players = await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Username);

        return new GuildWarStatusResponse
        {
            IsAtWar = true,
            WarId = war.Id,
            YourGuild = myGuild.Name,
            YourScore = isGuild1 ? war.Guild1Score : war.Guild2Score,
            OpponentGuild = opponentGuild.Name,
            OpponentScore = isGuild1 ? war.Guild2Score : war.Guild1Score,
            EndsAt = war.EndsAt,
            DaysRemaining = Math.Max(0, (int)(war.EndsAt - DateTime.UtcNow).TotalDays),
            TreasuryReward = war.TreasuryReward,
            TopContributors = myContributions.Select(c => new WarContributorDto
            {
                Username = players.GetValueOrDefault(c.PlayerId, "Unknown"),
                Points = c.Points,
                Wins = c.Wins
            }).ToList()
        };
    }

    public async Task<List<GuildWarHistoryResponse>> GetWarHistoryAsync(Guid playerId, int limit)
    {
        var membership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);

        if (membership == null)
            return new List<GuildWarHistoryResponse>();

        var guildId = membership.GuildId;

        var wars = await _context.Set<GuildWar>()
            .Include(w => w.Guild1)
            .Include(w => w.Guild2)
            .Where(w => w.Status == "completed"
                && (w.Guild1Id == guildId || w.Guild2Id == guildId))
            .OrderByDescending(w => w.EndsAt)
            .Take(limit)
            .ToListAsync();

        return wars.Select(w =>
        {
            bool isGuild1 = w.Guild1Id == guildId;
            var opponent = isGuild1 ? w.Guild2 : w.Guild1;
            int myScore = isGuild1 ? w.Guild1Score : w.Guild2Score;
            int theirScore = isGuild1 ? w.Guild2Score : w.Guild1Score;
            string result = w.WinnerGuildId == guildId ? "victory"
                : w.WinnerGuildId == null ? "draw"
                : "defeat";

            return new GuildWarHistoryResponse
            {
                WarId = w.Id,
                OpponentGuild = opponent.Name,
                YourScore = myScore,
                OpponentScore = theirScore,
                Result = result,
                TreasuryReward = w.WinnerGuildId == guildId ? w.TreasuryReward : 0,
                EndsAt = w.EndsAt
            };
        }).ToList();
    }

    public async Task RecordWarContributionAsync(Guid playerId, Guid battleId)
    {
        var membership = await _context.GuildMemberships
            .FirstOrDefaultAsync(m => m.PlayerId == playerId);

        if (membership == null) return;

        var guildId = membership.GuildId;

        var war = await _context.Set<GuildWar>()
            .FirstOrDefaultAsync(w => w.Status == "active"
                && (w.Guild1Id == guildId || w.Guild2Id == guildId)
                && w.EndsAt > DateTime.UtcNow);

        if (war == null) return;

        // Get or create contribution record
        var contribution = await _context.Set<GuildWarContribution>()
            .FirstOrDefaultAsync(c => c.GuildWarId == war.Id && c.PlayerId == playerId);

        if (contribution == null)
        {
            contribution = new GuildWarContribution
            {
                Id = Guid.NewGuid(),
                GuildWarId = war.Id,
                PlayerId = playerId,
                GuildId = guildId
            };
            _context.Set<GuildWarContribution>().Add(contribution);
        }

        contribution.Points += PointsPerWin;
        contribution.Wins++;

        // Update guild score
        if (war.Guild1Id == guildId)
            war.Guild1Score += PointsPerWin;
        else
            war.Guild2Score += PointsPerWin;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Guild war contribution: Player {PlayerId} earned {Points} points for guild {GuildId}",
            playerId, PointsPerWin, guildId);
    }

    public async Task MatchGuildsForWarAsync()
    {
        // Find guilds not currently in a war
        var activeWars = await _context.Set<GuildWar>()
            .Where(w => w.Status == "active")
            .Select(w => new { w.Guild1Id, w.Guild2Id })
            .ToListAsync();
        var activeWarGuildIds = activeWars
            .SelectMany(w => new[] { w.Guild1Id, w.Guild2Id })
            .ToList();

        var availableGuilds = await _context.Guilds
            .Where(g => !activeWarGuildIds.Contains(g.Id))
            .Include(g => g.Members)
            .Where(g => g.Members.Count >= 3) // Need at least 3 members to war
            .OrderBy(g => g.Level)
            .ToListAsync();

        // Pair up guilds by similar level
        for (int i = 0; i + 1 < availableGuilds.Count; i += 2)
        {
            var guild1 = availableGuilds[i];
            var guild2 = availableGuilds[i + 1];

            var war = new GuildWar
            {
                Id = Guid.NewGuid(),
                Guild1Id = guild1.Id,
                Guild2Id = guild2.Id,
                TreasuryReward = BaseTreasuryReward + (Math.Min(guild1.Level, guild2.Level) * 50),
                StartsAt = DateTime.UtcNow,
                EndsAt = DateTime.UtcNow.AddDays(WarDurationDays)
            };

            _context.Set<GuildWar>().Add(war);

            _logger.LogInformation("Guild war matched: {Guild1} vs {Guild2}",
                guild1.Name, guild2.Name);

            // Notify both guilds
            foreach (var member in guild1.Members.Concat(guild2.Members))
            {
                var opponentName = member.GuildId == guild1.Id ? guild2.Name : guild1.Name;
                await _notifications.SendAsync(member.PlayerId, NotificationType.GuildBossSpawned,
                    "Guild War Declared!",
                    $"Your guild is at war with [{opponentName}]! Win ranked battles to earn points for your guild.",
                    "/api/v1/guildwar/status");
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task FinalizeExpiredWarsAsync()
    {
        var expiredWars = await _context.Set<GuildWar>()
            .Include(w => w.Guild1)
            .Include(w => w.Guild2)
            .Where(w => w.Status == "active" && w.EndsAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var war in expiredWars)
        {
            war.Status = "completed";

            if (war.Guild1Score > war.Guild2Score)
            {
                war.WinnerGuildId = war.Guild1Id;
                war.Guild1.TreasuryBalance += war.TreasuryReward;
            }
            else if (war.Guild2Score > war.Guild1Score)
            {
                war.WinnerGuildId = war.Guild2Id;
                war.Guild2.TreasuryBalance += war.TreasuryReward;
            }
            else
            {
                // Draw — split reward
                int halfReward = war.TreasuryReward / 2;
                war.Guild1.TreasuryBalance += halfReward;
                war.Guild2.TreasuryBalance += halfReward;
            }

            _logger.LogInformation("Guild war finalized: {Guild1} ({Score1}) vs {Guild2} ({Score2}). Winner: {Winner}",
                war.Guild1.Name, war.Guild1Score, war.Guild2.Name, war.Guild2Score,
                war.WinnerGuildId?.ToString() ?? "Draw");

            // Notify members of both guilds
            var allMembers = await _context.GuildMemberships
                .Where(m => m.GuildId == war.Guild1Id || m.GuildId == war.Guild2Id)
                .ToListAsync();

            foreach (var member in allMembers)
            {
                bool isGuild1 = member.GuildId == war.Guild1Id;
                bool won = war.WinnerGuildId == member.GuildId;
                string result = war.WinnerGuildId == null ? "ended in a draw"
                    : won ? "won!" : "lost.";

                await _notifications.SendAsync(member.PlayerId, NotificationType.GuildBossDefeated,
                    "Guild War Ended",
                    $"The guild war {result} Score: {(isGuild1 ? war.Guild1Score : war.Guild2Score)} vs {(isGuild1 ? war.Guild2Score : war.Guild1Score)}" +
                    (won ? $" +{war.TreasuryReward}g to treasury!" : ""));
            }
        }

        await _context.SaveChangesAsync();
    }
}
