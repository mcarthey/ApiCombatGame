using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages;

[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public new int StatusCode { get; set; }
    public string? SupportId { get; set; }

    public void OnGet(int? code)
    {
        StatusCode = code ?? HttpContext.Response.StatusCode;
        SupportId = HttpContext.Items["SupportId"] as string;
    }
}
