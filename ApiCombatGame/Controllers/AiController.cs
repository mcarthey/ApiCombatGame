using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.AI;
using ApiCombatGame.Models.DTOs.Battle;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers;

/// <summary>
/// AI practice opponents for learning and strategy testing.
/// </summary>
[ApiCategoryMeta("robot", "#8b5cf6", Order = 13)]
[ApiController]
[Route("api/v1/ai")]
[Authorize]
[Tags("AI Practice")]
public class AiController : ControllerBase
{
    private readonly IAiOpponentService _aiService;

    public AiController(IAiOpponentService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>List all available AI opponents.</summary>
    /// <remarks>
    /// Returns AI opponents across three difficulty tiers: novice, intermediate, and expert.
    /// Each opponent has a fixed team composition and strategy. Practice battles award 50% of
    /// normal gold and XP, never affect your API rating, and don't count against your daily battle limit.
    ///
    /// Use AI opponents to learn the combat system, test new team compositions, or experiment
    /// with strategy configurations before risking your rating in ranked matches.
    /// </remarks>
    /// <response code="200">List of AI opponents with difficulty and team information.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Start with 'Training Dummy' (novice-1) to learn how the battle system works. Once you win consistently, move up to intermediate opponents to test real strategies.")]
    [ApiGameTip("Expert AI opponents use the same max-tier units and optimized strategies that top-ranked players use. If you can beat 'The Undefeated', you're ready for competitive play.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("List AI Opponents", Response = "{\n  \"opponents\": [\n    {\n      \"id\": \"novice-1\",\n      \"name\": \"Training Dummy\",\n      \"difficulty\": \"novice\",\n      \"approximateRating\": 600,\n      \"teamSize\": 3,\n      \"teamClasses\": [\"Warrior\", \"Ranger\", \"Mage\"],\n      \"formation\": \"balanced\"\n    }\n  ],\n  \"rewardMultiplier\": 0.5,\n  \"ratingAffected\": false,\n  \"countsTowardDailyLimit\": false\n}")]
    [HttpGet("opponents")]
    [ProducesResponseType(typeof(AiOpponentListResponse), StatusCodes.Status200OK)]
    public IActionResult GetOpponents()
    {
        var result = _aiService.GetAvailableOpponents();
        return Ok(result);
    }

    /// <summary>Fight an AI opponent in a practice battle.</summary>
    /// <remarks>
    /// Instantly resolves a battle between your team and the selected AI opponent — no matchmaking
    /// queue or waiting. The full turn-by-turn combat log is returned immediately.
    ///
    /// **Rewards**: 50% of normal gold and XP. No rating change. No daily limit consumed.
    ///
    /// **Strategy**: Your team's strategy configuration is used exactly as configured. The AI uses
    /// a fixed strategy appropriate to its difficulty level. This makes practice battles ideal for
    /// A/B testing strategy changes — fight the same AI opponent with different configs and compare results.
    /// </remarks>
    /// <param name="request">Your team ID and the AI opponent to fight.</param>
    /// <response code="200">Complete battle results with turn-by-turn log and rewards.</response>
    /// <response code="400">Invalid team or team has no units.</response>
    /// <response code="404">AI opponent ID not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Fight the same AI opponent multiple times with different strategy configs to see which formation and ability conditions work best. The AI always uses the same strategy, making it a perfect control variable.")]
    [ApiGameTip("Practice battles are the safest way to test expensive team compositions. Unlock a new 600g unit? Test it against AI before risking your ranked rating.")]
    [ApiPrerequisite("Register and login", "Build a team", "Choose an AI opponent")]
    [ApiExample("Fight AI Opponent", Request = "{\n  \"teamId\": \"a1b2c3d4-5678-9abc-def0-1234567890ab\",\n  \"opponentId\": \"novice-1\"\n}", Response = "{\n  \"battleId\": \"f47ac10b-58cc-4372-a567-0e02b2c3d479\",\n  \"status\": \"completed\",\n  \"winnerId\": \"your-player-id\",\n  \"turns\": 8,\n  \"battleLog\": [...],\n  \"rewards\": {\n    \"currency\": 25,\n    \"ratingChange\": 0,\n    \"experienceEarned\": 50,\n    \"tierMultiplier\": 1.0\n  }\n}")]
    [HttpPost("practice")]
    [ProducesResponseType(typeof(BattleResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PracticeBattle([FromBody] PracticeBattleRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _aiService.FightAiOpponentAsync(playerId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Run batch practice battles for statistical analysis.</summary>
    /// <remarks>
    /// Runs N practice battles server-side and returns aggregate statistics. No gold, XP, or
    /// rating changes — this is a pure simulation for strategy analysis.
    ///
    /// Ideal for classrooms: students can test their strategies against AI opponents and compare
    /// win rates without any economy impact. Rate limited to 1 request per minute.
    /// </remarks>
    /// <param name="request">Team ID, optional opponent ID, and battle count (max 200).</param>
    /// <response code="200">Aggregate battle statistics.</response>
    /// <response code="400">Invalid team or parameters.</response>
    /// <response code="404">AI opponent not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Use batch practice to A/B test strategy changes. Run 100 battles with your current config, tweak it, run 100 more, and compare win rates.")]
    [ApiPrerequisite("Register and login", "Build a team")]
    [HttpPost("batch-practice")]
    [ProducesResponseType(typeof(ApiCombatGame.Models.DTOs.Education.BatchPracticeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BatchPractice([FromBody] ApiCombatGame.Models.DTOs.Education.BatchPracticeRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var result = await _aiService.BatchPracticeAsync(playerId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
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
