using ApiCombatGame.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Pages;

public class LandingModel : PageModel
{
    private readonly GameDbContext _context;

    public LandingModel(GameDbContext context)
    {
        _context = context;
    }

    public int PlayerCount { get; set; }
    public int TotalBattles { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UtmSource { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UtmMedium { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UtmCampaign { get; set; }

    public string RegisterUrl { get; set; } = "/Auth/Register";

    public async Task OnGetAsync()
    {
        PlayerCount = await _context.Players.CountAsync(p => !p.IsBot && !p.IsDeleted);
        TotalBattles = await _context.Battles.CountAsync();

        // Pass UTM params through to Register link
        var queryParts = new List<string>();
        if (!string.IsNullOrEmpty(UtmSource)) queryParts.Add($"utm_source={Uri.EscapeDataString(UtmSource)}");
        if (!string.IsNullOrEmpty(UtmMedium)) queryParts.Add($"utm_medium={Uri.EscapeDataString(UtmMedium)}");
        if (!string.IsNullOrEmpty(UtmCampaign)) queryParts.Add($"utm_campaign={Uri.EscapeDataString(UtmCampaign)}");

        if (queryParts.Count > 0)
            RegisterUrl = $"/Auth/Register?{string.Join("&", queryParts)}";
    }
}
