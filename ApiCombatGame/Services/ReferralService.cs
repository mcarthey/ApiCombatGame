using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Referral;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ReferralService : IReferralService
{
    private readonly GameDbContext _context;
    private readonly INotificationService _notifications;
    private readonly ILogger<ReferralService> _logger;

    private const int ReferrerReward = 500;
    private const int ReferredBonus = 300;

    public ReferralService(GameDbContext context, INotificationService notifications, ILogger<ReferralService> logger)
    {
        _context = context;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<ReferralInfoResponse> GetReferralInfoAsync(Guid playerId)
    {
        // Get or create referral code
        var referral = await _context.Referrals
            .FirstOrDefaultAsync(r => r.ReferrerId == playerId && r.ReferredPlayerId == null);

        if (referral == null)
        {
            referral = new Referral
            {
                Id = Guid.NewGuid(),
                ReferrerId = playerId,
                Code = GenerateCode(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Referrals.Add(referral);
            await _context.SaveChangesAsync();
        }

        var totalReferrals = await _context.Referrals
            .CountAsync(r => r.ReferrerId == playerId && r.ReferredPlayerId != null);

        var totalGold = totalReferrals * ReferrerReward;

        return new ReferralInfoResponse
        {
            ReferralCode = referral.Code,
            TotalReferrals = totalReferrals,
            TotalGoldEarned = totalGold,
            RewardPerReferral = ReferrerReward,
            ReferredPlayerBonus = ReferredBonus
        };
    }

    public async Task<RedeemReferralResponse> RedeemCodeAsync(Guid newPlayerId, string code)
    {
        var referral = await _context.Referrals
            .Include(r => r.Referrer)
            .FirstOrDefaultAsync(r => r.Code == code && r.ReferredPlayerId == null)
            ?? throw new KeyNotFoundException("Invalid or already-used referral code.");

        if (referral.ReferrerId == newPlayerId)
            throw new InvalidOperationException("You cannot use your own referral code.");

        var newPlayer = await _context.Players.FindAsync(newPlayerId)
            ?? throw new KeyNotFoundException("Player not found.");

        // Mark referral as redeemed
        referral.ReferredPlayerId = newPlayerId;
        referral.RedeemedAt = DateTime.UtcNow;
        referral.ReferredRewardClaimed = true;

        // Award bonus to new player
        newPlayer.Currency += ReferredBonus;

        // Award reward to referrer
        var referrer = referral.Referrer;
        referrer.Currency += ReferrerReward;
        referral.ReferrerRewardClaimed = true;

        // Create a fresh unused code for the referrer (so they can refer more people)
        var newCode = new Referral
        {
            Id = Guid.NewGuid(),
            ReferrerId = referrer.Id,
            Code = GenerateCode(),
            CreatedAt = DateTime.UtcNow
        };
        _context.Referrals.Add(newCode);

        await _context.SaveChangesAsync();

        // Notify referrer
        await _notifications.SendAsync(referrer.Id, NotificationType.AdminAnnouncement,
            "Referral Reward!",
            $"{newPlayer.Username} joined using your referral code! +{ReferrerReward}g");

        _logger.LogInformation("Referral redeemed: {Referrer} referred {NewPlayer} (code: {Code})",
            referrer.Username, newPlayer.Username, code);

        return new RedeemReferralResponse
        {
            Success = true,
            BonusGoldAwarded = ReferredBonus,
            ReferrerUsername = referrer.Username
        };
    }

    public async Task<ReferralLeaderboardResponse> GetLeaderboardAsync(Guid playerId, int limit)
    {
        var referralCounts = await _context.Referrals
            .Where(r => r.ReferredPlayerId != null)
            .GroupBy(r => r.ReferrerId)
            .Select(g => new { ReferrerId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync();

        var playerIds = referralCounts.Select(r => r.ReferrerId).ToList();
        var players = await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Username);

        var rankings = referralCounts.Select((r, i) => new ReferralLeaderboardEntry
        {
            Rank = i + 1,
            Username = players.GetValueOrDefault(r.ReferrerId, "Unknown"),
            TotalReferrals = r.Count
        }).ToList();

        var yourCount = await _context.Referrals
            .CountAsync(r => r.ReferrerId == playerId && r.ReferredPlayerId != null);

        int? yourRank = null;
        if (yourCount > 0)
        {
            yourRank = await _context.Referrals
                .Where(r => r.ReferredPlayerId != null)
                .GroupBy(r => r.ReferrerId)
                .CountAsync(g => g.Count() > yourCount) + 1;
        }

        return new ReferralLeaderboardResponse
        {
            Rankings = rankings,
            YourRank = yourRank,
            YourReferrals = yourCount
        };
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
