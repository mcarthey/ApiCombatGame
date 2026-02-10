using System.Security.Claims;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly IConfiguration _config;

    public DashboardModel(GameDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public DashboardViewModel Dashboard { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var playerId = GetPlayerId();
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return RedirectToPage("/Auth/Login");

        // Reset daily battles if new day
        if (player.LastBattleResetDate.Date < DateTime.UtcNow.Date)
        {
            player.DailyBattlesUsed = 0;
            player.LastBattleResetDate = DateTime.UtcNow.Date;
            await _context.SaveChangesAsync();
        }

        var totalBattles = await _context.Battles.CountAsync(b =>
            (b.Player1Id == playerId || b.Player2Id == playerId) &&
            b.Status == BattleStatus.Completed);

        var wins = await _context.Battles.CountAsync(b => b.WinnerId == playerId);

        var rank = await _context.Players.CountAsync(p => p.Rating > player.Rating) + 1;

        var recentBattles = await _context.Battles
            .Where(b => (b.Player1Id == playerId || b.Player2Id == playerId) && b.Status == BattleStatus.Completed)
            .OrderByDescending(b => b.CompletedAt)
            .Take(10)
            .Select(b => new
            {
                b.Id,
                b.Player1Id,
                b.Player2Id,
                b.WinnerId,
                b.Player1RatingChange,
                b.Player2RatingChange,
                b.CompletedAt
            })
            .ToListAsync();

        var opponentIds = recentBattles
            .Select(b => b.Player1Id == playerId ? b.Player2Id : b.Player1Id)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var opponentNames = await _context.Players
            .Where(p => opponentIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Username);

        var dailyLimit = player.CurrentTier == SubscriptionTier.Free
            ? _config.GetValue<int>("GameSettings:FreeTierDailyBattleLimit", 10)
            : int.MaxValue;

        Dashboard = new DashboardViewModel
        {
            PlayerId = playerId,
            Username = player.Username,
            CurrentTier = player.CurrentTier.ToString(),
            BattlesToday = player.DailyBattlesUsed,
            DailyLimit = player.CurrentTier == SubscriptionTier.Free ? dailyLimit : -1,
            WinRate = totalBattles > 0 ? Math.Round((double)wins / totalBattles * 100, 1) : 0,
            GlobalRank = rank,
            Rating = player.Rating,
            RatingChange = 0,
            Currency = player.Currency,
            Level = player.Level,
            RecentBattles = recentBattles.Select(b =>
            {
                var opponentId = b.Player1Id == playerId ? b.Player2Id : b.Player1Id;
                var opponentName = opponentId.HasValue && opponentNames.TryGetValue(opponentId.Value, out var name) ? name : "Unknown";
                var ratingChange = b.Player1Id == playerId ? (b.Player1RatingChange ?? 0) : (b.Player2RatingChange ?? 0);

                return new RecentBattleInfo
                {
                    BattleId = b.Id,
                    OpponentName = opponentName,
                    IsWin = b.WinnerId == playerId,
                    RatingChange = ratingChange,
                    CompletedAt = b.CompletedAt ?? DateTime.UtcNow
                };
            }).ToList()
        };

        return Page();
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }
}
