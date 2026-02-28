namespace ApiCombatGame.Services.Interfaces;

/// <summary>
/// Shared service for recording admin audit log entries.
/// Call <see cref="AddEntry"/> to stage an entry, then save with the caller's <c>SaveChangesAsync</c>
/// so audit logs are atomic with the operation they describe.
/// </summary>
public interface IAdminAuditService
{
    /// <summary>Stages an audit log entry on the current DbContext (does NOT save).</summary>
    void AddEntry(Guid adminPlayerId, string action, Guid? targetPlayerId = null, string? detailsJson = null);
}
