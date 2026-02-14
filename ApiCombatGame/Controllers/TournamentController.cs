using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Tournament;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// Weekly single-elimination tournaments with prize pools.
/// </summary>
[ApiCategoryMeta("trophy", "#f59e0b", Order = 21)]
[ApiController]
[Route("api/v1/tournament")]
[Authorize]
[Tags("Tournament")]
public class TournamentController : ControllerBase
{
    private readonly ITournamentService _tournamentService;

    public TournamentController(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    /// <summary>Get the current or most recent tournament.</summary>
    /// <remarks>
    /// Returns tournament info including registration status, participants, prize pool,
    /// and your entry status. Tournaments run weekly: registration opens, then brackets
    /// are generated and matches play out automatically.
    ///
    /// Entry fee is 100g. Top 4 finishers win prizes (1st: 5000g + title, 2nd: 2500g, 3rd/4th: 1000g).
    /// Brackets are seeded by API rating — higher rated players face lower seeds first.
    /// </remarks>
    /// <response code="200">Tournament info with your registration status.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Entry is only 100g but first place wins 5000g + a title. High risk, high reward — enter if you're confident in your strategy.")]
    [ApiGameTip("Tournaments seed by rating, so high-rated players get easier first-round matchups.")]
    [ApiPrerequisite("Register and login, have at least 100g")]
    [ApiExample("Get Tournament", Response = "{\n  \"tournamentId\": \"...\",\n  \"name\": \"Weekly Tournament #20260212\",\n  \"status\": \"registration\",\n  \"maxParticipants\": 16,\n  \"currentParticipants\": 7,\n  \"entryFee\": 100,\n  \"prizes\": [...],\n  \"isRegistered\": false\n}")]
    [HttpGet("current")]
    [ProducesResponseType(typeof(TournamentInfoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentTournament()
    {
        var playerId = GetPlayerId();
        var result = await _tournamentService.GetActiveTournamentAsync(playerId);
        return Ok(result);
    }

    /// <summary>Enter the current tournament.</summary>
    /// <remarks>
    /// Register for the active tournament with your team. Costs the entry fee (100g).
    /// You can only enter once per tournament. Registration closes when the tournament starts
    /// or when max participants is reached.
    /// </remarks>
    /// <param name="request">Your team ID to compete with.</param>
    /// <response code="200">Successfully registered. Returns updated tournament info.</response>
    /// <response code="400">Not enough currency, already registered, or tournament full/closed.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Pick your strongest team — you can't change it once registered.")]
    [ApiPrerequisite("Have a configured team and 100g")]
    [HttpPost("enter")]
    [ProducesResponseType(typeof(TournamentInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnterTournament([FromBody] TournamentEntryRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _tournamentService.EnterTournamentAsync(playerId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>View a tournament bracket.</summary>
    /// <remarks>
    /// Returns the full bracket with match results for each round. Shows all matchups,
    /// who won, and who's advancing. Use this to track the tournament's progress.
    /// </remarks>
    /// <param name="tournamentId">The tournament ID to view.</param>
    /// <response code="200">Full bracket with all rounds and matches.</response>
    /// <response code="404">Tournament not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Study the bracket to see who you might face next — prepare a counter-strategy!")]
    [ApiPrerequisite("Tournament must have started")]
    [HttpGet("bracket/{tournamentId}")]
    [ProducesResponseType(typeof(TournamentBracketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBracket(Guid tournamentId)
    {
        try
        {
            var result = await _tournamentService.GetBracketAsync(tournamentId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
