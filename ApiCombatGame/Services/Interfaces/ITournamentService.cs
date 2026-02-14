using ApiCombatGame.Models.DTOs.Tournament;

namespace ApiCombatGame.Services.Interfaces;

public interface ITournamentService
{
    Task<TournamentInfoResponse> GetActiveTournamentAsync(Guid playerId);
    Task<TournamentInfoResponse> EnterTournamentAsync(Guid playerId, TournamentEntryRequest request);
    Task<TournamentBracketResponse> GetBracketAsync(Guid tournamentId);
    Task CreateWeeklyTournamentAsync();
    Task ProcessTournamentMatchesAsync();
}
