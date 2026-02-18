using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.ViewModels;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Admin;

[Authorize(Policy = "Admin")]
public class LogsModel : PageModel
{
    private readonly IAppLogService _logService;

    public LogsModel(IAppLogService logService)
    {
        _logService = logService;
    }

    public AdminLogData Data { get; set; } = new();
    public AppLog? SelectedLog { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Level { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public Guid? DetailId { get; set; }

    public async Task OnGetAsync()
    {
        DateTime? from = null;
        DateTime? to = null;

        if (DateTime.TryParse(DateFrom, out var parsedFrom))
            from = parsedFrom;
        if (DateTime.TryParse(DateTo, out var parsedTo))
            to = parsedTo;

        if (Page < 1) Page = 1;

        Data = await _logService.GetLogsAsync(Search, Level, from, to, Page);

        if (DetailId.HasValue)
            SelectedLog = await _logService.GetLogByIdAsync(DetailId.Value);
    }
}
