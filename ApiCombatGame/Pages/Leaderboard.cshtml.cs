using ApiCombatGame.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages;

public class LeaderboardModel : PageModel
{
    private readonly GameDbContext _context;

    public LeaderboardModel(GameDbContext context)
    {
        _context = context;
    }

    public List<LeaderboardEntry> Players { get; set; } = new();

    public async Task OnGetAsync()
    {
        Players = await _context.Players
            .Where(p => !p.IsBot && !p.IsDeleted)
            .OrderByDescending(p => p.Rating)
            .Take(50)
            .Select(p => new LeaderboardEntry
            {
                Username = p.Username,
                Rating = p.Rating,
                Level = p.Level,
                TotalBattles = p.TotalBattlesPlayed,
                TotalWins = p.TotalBattlesWon
            })
            .ToListAsync();
    }

    public class LeaderboardEntry
    {
        public string Username { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int Level { get; set; }
        public int TotalBattles { get; set; }
        public int TotalWins { get; set; }
        public decimal WinRate => TotalBattles > 0 ? Math.Round((decimal)TotalWins / TotalBattles * 100, 1) : 0;
    }
}
