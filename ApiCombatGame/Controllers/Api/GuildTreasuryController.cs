using System.Security.Claims;
using ApiCombatGame.Filters;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Guild;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Guild treasury — view balances, purchase upgrades, and deposit gold.
/// </summary>
[ApiController]
[Route("api/v1/guild")]
[Authorize]
[Tags("Guild")]
public class GuildTreasuryController : ControllerBase
{
    private readonly IGuildTreasuryService _guildTreasuryService;

    public GuildTreasuryController(IGuildTreasuryService guildTreasuryService)
    {
        _guildTreasuryService = guildTreasuryService;
    }

    /// <summary>View guild treasury and available upgrades.</summary>
    /// <remarks>
    /// Shows the guild's gold balance, current upgrade levels, and what upgrades can be purchased.
    /// </remarks>
    /// <param name="guildId">The guild to view treasury of.</param>
    /// <response code="200">Treasury balance with upgrade options.</response>
    /// <response code="404">Guild not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Coordinate deposits with your guildmates to save up for powerful upgrades like the 20% gold bonus.")]
    [ApiPrerequisite("Join a guild")]
    [HttpGet("{guildId}/treasury")]
    [ProducesResponseType(typeof(TreasuryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTreasury(Guid guildId)
    {
        try
        {
            var (guild, upgrades) = await _guildTreasuryService.GetTreasuryAsync(guildId);
            return Ok(new TreasuryResponse
            {
                Balance = guild.TreasuryBalance,
                GoldBonusPercent = guild.GoldBonusPercent,
                MaxRaidAttempts = guild.MaxRaidAttempts,
                MaxMembers = guild.MaxMembers,
                AvailableUpgrades = upgrades.Select(u => new GuildUpgradeOption
                {
                    Id = u.Id,
                    Name = u.Name,
                    Description = u.Description,
                    Cost = u.Cost,
                    CanAfford = u.CanAfford,
                    AlreadyPurchased = u.AlreadyPurchased
                }).ToList()
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Purchase a guild upgrade from the treasury.</summary>
    /// <remarks>
    /// Spend treasury gold on permanent guild upgrades. Only the guild leader can make purchases.
    /// Available upgrades: max_members_30, max_members_50, gold_bonus_10, gold_bonus_20,
    /// raid_attempts_4, raid_attempts_5.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">The upgrade to purchase.</param>
    /// <response code="200">Upgrade purchased. New treasury state returned.</response>
    /// <response code="400">Insufficient funds, already purchased, or not the leader.</response>
    /// <response code="404">Guild or upgrade not found.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("The gold_bonus_10 upgrade (30,000g) pays for itself over time as every member earns 10% more gold.")]
    [ApiPrerequisite("Create a guild", "Deposit gold into treasury")]
    [ApiExample("Purchase an upgrade", Request = "{\n  \"upgradeId\": \"gold_bonus_10\"\n}", Response = "{\n  \"balance\": 20000,\n  \"goldBonusPercent\": 10,\n  \"maxRaidAttempts\": 3,\n  \"maxMembers\": 20\n}")]
    [HttpPost("{guildId}/treasury/spend")]
    [ProducesResponseType(typeof(TreasuryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PurchaseUpgrade(Guid guildId, [FromBody] TreasurySpendRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var guild = await _guildTreasuryService.PurchaseUpgradeAsync(guildId, playerId, request.UpgradeId);

            return Ok(new
            {
                balance = guild.TreasuryBalance,
                goldBonusPercent = guild.GoldBonusPercent,
                maxRaidAttempts = guild.MaxRaidAttempts,
                maxMembers = guild.MaxMembers
            });
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

    /// <summary>Deposit personal gold into the guild treasury.</summary>
    /// <remarks>
    /// Transfer gold from your personal balance to the guild treasury. This increases your
    /// contribution points. Any guild member can deposit.
    /// </remarks>
    /// <param name="guildId">Your guild ID.</param>
    /// <param name="request">Amount of gold to deposit.</param>
    /// <response code="200">Deposit successful. New balances returned.</response>
    /// <response code="400">Insufficient personal gold or not a member.</response>
    /// <response code="404">Guild not found.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Depositing gold increases your contribution points — guilds often use this to track member participation.")]
    [ApiPrerequisite("Join a guild", "Have gold to deposit")]
    [HttpPost("{guildId}/treasury/deposit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DepositToTreasury(Guid guildId, [FromBody] TreasuryDepositRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var guild = await _guildTreasuryService.DepositAsync(guildId, playerId, request.Amount);

            return Ok(new
            {
                treasuryBalance = guild.TreasuryBalance,
                message = $"Deposited {request.Amount:N0} gold to the guild treasury."
            });
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
