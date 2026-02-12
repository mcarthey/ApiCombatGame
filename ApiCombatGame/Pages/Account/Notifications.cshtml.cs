using System.Security.Claims;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ApiCombatGame.Pages.Account;

[Authorize]
public class NotificationsModel : PageModel
{
    private readonly INotificationService _notifications;

    public NotificationsModel(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public List<Notification> NotificationList { get; set; } = new();
    public int UnreadCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public bool UnreadOnly { get; set; }

    public async Task OnGetAsync()
    {
        var playerId = GetPlayerId();
        NotificationList = await _notifications.GetNotificationsAsync(playerId, Page, 20, UnreadOnly);
        UnreadCount = await _notifications.GetUnreadCountAsync(playerId);
    }

    public async Task<IActionResult> OnPostMarkReadAsync(Guid notificationId)
    {
        var playerId = GetPlayerId();
        await _notifications.MarkReadAsync(notificationId, playerId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var playerId = GetPlayerId();
        await _notifications.MarkAllReadAsync(playerId);
        return RedirectToPage();
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim!.Value);
    }
}
