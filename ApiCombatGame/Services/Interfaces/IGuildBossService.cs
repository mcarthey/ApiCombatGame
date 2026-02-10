using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Services.Interfaces;

public interface IGuildBossService
{
    Task<GuildBoss?> GetActiveGuildBoss(Guid guildId);
    Task<GuildBossAttempt> AttemptBoss(Guid guildBossId, Guid playerId, Guid teamId);
    Task<List<GuildBossAttempt>> GetBossLeaderboard(Guid guildBossId);
    Task SpawnBossForGuild(Guid guildId);
}
