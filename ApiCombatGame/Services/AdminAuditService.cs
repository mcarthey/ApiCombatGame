using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Services.Interfaces;

namespace ApiCombatGame.Services;

public class AdminAuditService : IAdminAuditService
{
    private readonly GameDbContext _context;

    public AdminAuditService(GameDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void AddEntry(Guid adminPlayerId, string action, Guid? targetPlayerId = null, string? detailsJson = null)
    {
        _context.AdminAuditLogs.Add(new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminPlayerId = adminPlayerId,
            Action = action,
            TargetPlayerId = targetPlayerId,
            DetailsJson = detailsJson,
            CreatedAt = DateTime.UtcNow
        });
    }
}
