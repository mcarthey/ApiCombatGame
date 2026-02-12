using ApiCombatGame.Models.DTOs.Progression;
using ApiCombatGame.Models.Enums;

namespace ApiCombatGame.Services.Interfaces;

public interface IPlayerProgressionService
{
    /// <summary>Process all post-battle rewards: gold, XP, streak tracking, first-battle-of-day bonus.</summary>
    Task<BattleRewardsSummary> ProcessBattleRewardsAsync(Guid playerId, bool won, int ratingChange);

    /// <summary>Award gold with tier multiplier applied. Returns actual gold awarded.</summary>
    Task<int> AwardCurrencyAsync(Guid playerId, int baseGold);

    /// <summary>Award XP and check for level-up. Returns new level if leveled up, null otherwise.</summary>
    Task<int?> AwardExperienceAsync(Guid playerId, int baseXp);

    /// <summary>Calculate XP needed to reach the next level.</summary>
    int GetXpForLevel(int level);

    /// <summary>Get the gold multiplier for a subscription tier.</summary>
    decimal GetGoldMultiplier(SubscriptionTier tier);
}
