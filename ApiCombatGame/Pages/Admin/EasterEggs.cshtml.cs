using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class EasterEggsModel : PageModel
{
    private readonly GameDbContext _context;
    private readonly IEasterEggService _eggService;

    public EasterEggsModel(GameDbContext context, IEasterEggService eggService)
    {
        _context = context;
        _eggService = eggService;
    }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // Current week's eggs
    public List<EggWithStats> ActiveEggs { get; set; } = new();

    // Past eggs (last 4 weeks)
    public List<EggWithStats> PastEggs { get; set; } = new();

    // Recent claims across all eggs
    public List<ClaimEntry> RecentClaims { get; set; } = new();

    // Summary stats
    public int TotalEggsAllTime { get; set; }
    public int TotalClaimsAllTime { get; set; }
    public int TotalLootAwarded { get; set; }
    public int UniqueFinders { get; set; }

    // Edit form binding
    [BindProperty]
    public EggEditInput? EditInput { get; set; }

    public async Task OnGetAsync(string? message)
    {
        if (message == "created") SuccessMessage = "Egg created successfully.";
        else if (message == "updated") SuccessMessage = "Egg updated successfully.";
        else if (message == "deleted") SuccessMessage = "Egg deleted.";
        else if (message == "regenerated") SuccessMessage = "Weekly eggs regenerated.";
        else if (message == "error") ErrorMessage = "Something went wrong. Check the logs.";

        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostRegenerateAsync()
    {
        // Deactivate current week's eggs
        var now = DateTime.UtcNow;
        var monday = GetMondayOfWeek(now);
        var currentEggs = await _context.EasterEggs
            .Where(e => e.WeekStart == monday)
            .ToListAsync();

        foreach (var egg in currentEggs)
            egg.IsActive = false;

        await _context.SaveChangesAsync();

        // Generate fresh ones
        await _eggService.GenerateWeeklyEggsAsync(now);

        return RedirectToPage("EasterEggs", new { message = "regenerated" });
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (EditInput == null)
            return RedirectToPage("EasterEggs", new { message = "error" });

        var egg = new EasterEgg
        {
            Id = Guid.NewGuid(),
            Code = EditInput.Code.Trim().ToUpper(),
            PagePath = EditInput.PagePath.Trim(),
            CssSelector = EditInput.CssSelector?.Trim() ?? "main",
            OffsetX = EditInput.OffsetX,
            OffsetY = EditInput.OffsetY,
            WeekStart = EditInput.WeekStart,
            WeekEnd = EditInput.WeekEnd,
            Difficulty = EditInput.Difficulty,
            IconName = EditInput.IconName?.Trim() ?? "egg_alt",
            HintText = EditInput.HintText?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EasterEggs.Add(egg);
        await _context.SaveChangesAsync();

        return RedirectToPage("EasterEggs", new { message = "created" });
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (EditInput == null || EditInput.Id == Guid.Empty)
            return RedirectToPage("EasterEggs", new { message = "error" });

        var egg = await _context.EasterEggs.FindAsync(EditInput.Id);
        if (egg == null)
            return RedirectToPage("EasterEggs", new { message = "error" });

        egg.Code = EditInput.Code.Trim().ToUpper();
        egg.PagePath = EditInput.PagePath.Trim();
        egg.CssSelector = EditInput.CssSelector?.Trim() ?? "main";
        egg.OffsetX = EditInput.OffsetX;
        egg.OffsetY = EditInput.OffsetY;
        egg.WeekStart = EditInput.WeekStart;
        egg.WeekEnd = EditInput.WeekEnd;
        egg.Difficulty = EditInput.Difficulty;
        egg.IconName = EditInput.IconName?.Trim() ?? "egg_alt";
        egg.HintText = EditInput.HintText?.Trim();
        egg.IsActive = EditInput.IsActive;

        await _context.SaveChangesAsync();

        return RedirectToPage("EasterEggs", new { message = "updated" });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid eggId)
    {
        var egg = await _context.EasterEggs.FindAsync(eggId);
        if (egg != null)
        {
            egg.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("EasterEggs", new { message = "deleted" });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid eggId)
    {
        var egg = await _context.EasterEggs.FindAsync(eggId);
        if (egg != null)
        {
            egg.IsActive = !egg.IsActive;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("EasterEggs", new { message = "updated" });
    }

    private async Task LoadDataAsync()
    {
        var now = DateTime.UtcNow;
        var monday = GetMondayOfWeek(now);
        var fourWeeksAgo = monday.AddDays(-28);

        // Active eggs (this week)
        var activeRaw = await _context.EasterEggs
            .Include(e => e.Claims)
            .Where(e => e.WeekStart == monday)
            .OrderBy(e => e.PagePath)
            .ToListAsync();

        ActiveEggs = activeRaw.Select(e => new EggWithStats
        {
            Egg = e,
            ClaimCount = e.Claims.Count
        }).ToList();

        // Past eggs (previous 4 weeks, not including current)
        var pastRaw = await _context.EasterEggs
            .Include(e => e.Claims)
            .Where(e => e.WeekStart >= fourWeeksAgo && e.WeekStart < monday)
            .OrderByDescending(e => e.WeekStart)
            .ThenBy(e => e.PagePath)
            .ToListAsync();

        PastEggs = pastRaw.Select(e => new EggWithStats
        {
            Egg = e,
            ClaimCount = e.Claims.Count
        }).ToList();

        // Recent claims (last 50)
        RecentClaims = await _context.EggClaims
            .Include(c => c.EasterEgg)
            .Include(c => c.Player)
            .OrderByDescending(c => c.ClaimedAt)
            .Take(50)
            .Select(c => new ClaimEntry
            {
                PlayerName = c.Player.Username,
                EggCode = c.EasterEgg.Code,
                EggPage = c.EasterEgg.PagePath,
                LootDescription = c.LootDescription,
                LootType = c.LootType,
                LootAmount = c.LootAmount,
                ClaimedAt = c.ClaimedAt
            })
            .ToListAsync();

        // Summary stats
        TotalEggsAllTime = await _context.EasterEggs.CountAsync();
        TotalClaimsAllTime = await _context.EggClaims.CountAsync();
        TotalLootAwarded = await _context.EggClaims.SumAsync(c => (int?)c.LootAmount) ?? 0;
        UniqueFinders = await _context.EggClaims.Select(c => c.PlayerId).Distinct().CountAsync();
    }

    private static DateTime GetMondayOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.Date.AddDays(-diff);
    }

    // View models
    public class EggWithStats
    {
        public EasterEgg Egg { get; set; } = null!;
        public int ClaimCount { get; set; }
    }

    public class ClaimEntry
    {
        public string PlayerName { get; set; } = string.Empty;
        public string EggCode { get; set; } = string.Empty;
        public string EggPage { get; set; } = string.Empty;
        public string LootDescription { get; set; } = string.Empty;
        public string LootType { get; set; } = string.Empty;
        public int LootAmount { get; set; }
        public DateTime ClaimedAt { get; set; }
    }

    public class EggEditInput
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string PagePath { get; set; } = string.Empty;
        public string? CssSelector { get; set; }
        public double OffsetX { get; set; } = 50;
        public double OffsetY { get; set; } = 50;
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int Difficulty { get; set; } = 1;
        public string? IconName { get; set; } = "egg_alt";
        public string? HintText { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
