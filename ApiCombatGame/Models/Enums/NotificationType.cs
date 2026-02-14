namespace ApiCombatGame.Models.Enums;

public enum NotificationType
{
    // Battle
    BattleCompleted,
    WinStreakMilestone,
    RatingMilestone,

    // Guild
    GuildInvited,
    GuildInviteResponse,
    GuildKicked,
    GuildPromoted,
    GuildBossSpawned,
    GuildBossDefeated,
    GuildChatMention,
    GuildStrategyPublished,
    GuildTreasuryUpgrade,

    // Progression
    LevelUp,
    AchievementUnlocked,
    DailyChallengesAvailable,
    MasteryRankUp,

    // Marketplace
    StrategyRated,
    StrategyDownloadMilestone,

    // Seasons
    SeasonReward,
    SeasonRankChange,

    // Competitive
    RevengeOpportunity,
    RivalDefeatedYou,
    TournamentResult,
    GuildWarResult,
    RankUp,
    RankDown,

    // System
    NewModifierActive,
    AdminAnnouncement,

    // Security (always on)
    ApiKeyNewIp,
    PasswordChanged,
    TierChanged,
    AdminActionOnAccount
}
