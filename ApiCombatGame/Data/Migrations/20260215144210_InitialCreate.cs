using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCombatGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    RequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSecret = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActionRequired = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CosmeticItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Rarity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GemPrice = table.Column<int>(type: "int", nullable: false),
                    GoldPrice = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    IsLimited = table.Column<bool>(type: "bit", nullable: false),
                    RequiredTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CosmeticItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EasterEggs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PagePath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CssSelector = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OffsetX = table.Column<double>(type: "float", nullable: false),
                    OffsetY = table.Column<double>(type: "float", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HintText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EasterEggs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentalModifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ModifierType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentalModifiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UnlockRequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTitles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SeasonNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RewardConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    EntryFee = table.Column<int>(type: "int", nullable: false),
                    PrizePoolJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WinnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistrationOpens = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentTier = table.Column<int>(type: "int", nullable: false),
                    DailyBattlesUsed = table.Column<int>(type: "int", nullable: false),
                    LastBattleResetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExperiencePoints = table.Column<int>(type: "int", nullable: false),
                    WinStreak = table.Column<int>(type: "int", nullable: false),
                    LastFirstBattleBonus = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoginStreak = table.Column<int>(type: "int", nullable: false),
                    LastLoginRewardDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginRewardClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    AdminRole = table.Column<int>(type: "int", nullable: false),
                    NotificationPreferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveTitleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AchievementPoints = table.Column<int>(type: "int", nullable: false),
                    Gems = table.Column<int>(type: "int", nullable: false),
                    GemsEarnedTotal = table.Column<int>(type: "int", nullable: false),
                    GemsSpentTotal = table.Column<int>(type: "int", nullable: false),
                    ActiveProfileBorderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActiveCardBackId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Badge = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LastGemStipendClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalBattlesPlayed = table.Column<int>(type: "int", nullable: false),
                    TotalBattlesWon = table.Column<int>(type: "int", nullable: false),
                    TotalGoldEarned = table.Column<long>(type: "bigint", nullable: false),
                    TotalXpEarned = table.Column<long>(type: "bigint", nullable: false),
                    HighestRating = table.Column<int>(type: "int", nullable: false),
                    HighestWinStreak = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_PlayerTitles_ActiveTitleId",
                        column: x => x.ActiveTitleId,
                        principalTable: "PlayerTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BattlePasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxLevel = table.Column<int>(type: "int", nullable: false),
                    XpPerLevel = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RewardsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattlePasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattlePasses_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    MatchNumber = table.Column<int>(type: "int", nullable: false),
                    Player1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Player2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WinnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityFeedEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityFeedEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityFeedEntries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Players_AdminPlayerId",
                        column: x => x.AdminPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_Players_TargetPlayerId",
                        column: x => x.TargetPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Battles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Player1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Player2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Team1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Team2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WinnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Turns = table.Column<int>(type: "int", nullable: false),
                    BattleLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Team1ClassesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Team2ClassesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Player1RatingChange = table.Column<int>(type: "int", nullable: true),
                    Player2RatingChange = table.Column<int>(type: "int", nullable: true),
                    CurrencyReward = table.Column<int>(type: "int", nullable: true),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Battles_Players_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Battles_Players_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContentCreators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    GemsEarned = table.Column<int>(type: "int", nullable: false),
                    TotalDownloads = table.Column<int>(type: "int", nullable: false),
                    ModulesCreated = table.Column<int>(type: "int", nullable: false),
                    IsSpotlighted = table.Column<bool>(type: "bit", nullable: false),
                    SpotlightMonth = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentCreators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentCreators_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstructorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LessonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumModules_Players_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    RequiredProgress = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RewardCurrency = table.Column<int>(type: "int", nullable: false),
                    RewardExperience = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyChallenges_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscordLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscordUserId = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiscordUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerificationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordLinks_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscordWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventTypes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordWebhooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordWebhooks_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EggClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EasterEggId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LootType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LootDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LootAmount = table.Column<int>(type: "int", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EggClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EggClaims_EasterEggs_EasterEggId",
                        column: x => x.EasterEggId,
                        principalTable: "EasterEggs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EggClaims_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ExperiencePoints = table.Column<int>(type: "int", nullable: false),
                    MaxMembers = table.Column<int>(type: "int", nullable: false),
                    TreasuryBalance = table.Column<int>(type: "int", nullable: false),
                    GoldBonusPercent = table.Column<int>(type: "int", nullable: false),
                    MaxRaidAttempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guilds_Players_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LootDrops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DropType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
                    Claimed = table.Column<bool>(type: "bit", nullable: false),
                    DroppedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LootDrops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LootDrops_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Progress = table.Column<int>(type: "int", nullable: false),
                    RequiredProgress = table.Column<int>(type: "int", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "bit", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    LastRequestAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerActivities_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCosmetics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CosmeticItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEquipped = table.Column<bool>(type: "bit", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCosmetics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerCosmetics_CosmeticItems_CosmeticItemId",
                        column: x => x.CosmeticItemId,
                        principalTable: "CosmeticItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerCosmetics_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSeasonRanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentTier = table.Column<int>(type: "int", nullable: false),
                    SeasonRating = table.Column<int>(type: "int", nullable: false),
                    PeakRating = table.Column<int>(type: "int", nullable: false),
                    PeakTier = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    Draws = table.Column<int>(type: "int", nullable: false),
                    RewardsClaimed = table.Column<bool>(type: "bit", nullable: false),
                    LastRankedBattleAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSeasonRanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonRanks_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSeasonRanks_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferrerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferredPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferrerRewardClaimed = table.Column<bool>(type: "bit", nullable: false),
                    ReferredRewardClaimed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Players_ReferredPlayerId",
                        column: x => x.ReferredPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_Players_ReferrerId",
                        column: x => x.ReferrerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RivalAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RivalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WinsAgainstRival = table.Column<int>(type: "int", nullable: false),
                    LossesAgainstRival = table.Column<int>(type: "int", nullable: false),
                    BonusGoldEarned = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RivalAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RivalAssignments_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RivalAssignments_Players_RivalId",
                        column: x => x.RivalId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StrategyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    WinCount = table.Column<int>(type: "int", nullable: false),
                    LossCount = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    EffectivenessMultiplier = table.Column<double>(type: "float", nullable: false),
                    LastDecayUpdate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Strategies_Players_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldTier = table.Column<int>(type: "int", nullable: true),
                    NewTier = table.Column<int>(type: "int", nullable: true),
                    AmountUsd = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionEvents_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StripePriceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AmountUsd = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrategyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TournamentEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    RoundsWon = table.Column<int>(type: "int", nullable: false),
                    IsEliminated = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentEntries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentEntries_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnitMasteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ExperiencePoints = table.Column<int>(type: "int", nullable: false),
                    BattlesUsed = table.Column<int>(type: "int", nullable: false),
                    WinsWithUnit = table.Column<int>(type: "int", nullable: false),
                    FirstUsed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitMasteries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitMasteries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Class = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Health = table.Column<int>(type: "int", nullable: false),
                    Attack = table.Column<int>(type: "int", nullable: false),
                    Defense = table.Column<int>(type: "int", nullable: false),
                    Speed = table.Column<int>(type: "int", nullable: false),
                    UnlockCost = table.Column<int>(type: "int", nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    CustomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsGolden = table.Column<bool>(type: "bit", nullable: false),
                    RerollCount = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerBattlePasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BattlePassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false),
                    CurrentXp = table.Column<int>(type: "int", nullable: false),
                    HasPremium = table.Column<bool>(type: "bit", nullable: false),
                    ClaimedLevelsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBattlePasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerBattlePasses_BattlePasses_BattlePassId",
                        column: x => x.BattlePassId,
                        principalTable: "BattlePasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerBattlePasses_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeyUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeyUsageLogs_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BattleReplays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShareUrl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeaturedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleReplays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleReplays_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentLesson = table.Column<int>(type: "int", nullable: false),
                    LessonsCompleted = table.Column<int>(type: "int", nullable: false),
                    CompletedLessonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_CurriculumModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "CurriculumModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildBosses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BossType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AbilitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxHp = table.Column<int>(type: "int", nullable: false),
                    CurrentHp = table.Column<int>(type: "int", nullable: false),
                    Attack = table.Column<int>(type: "int", nullable: false),
                    Defense = table.Column<int>(type: "int", nullable: false),
                    SpawnedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDefeated = table.Column<bool>(type: "bit", nullable: false),
                    DefeatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RewardCurrency = table.Column<int>(type: "int", nullable: false),
                    RewardExperience = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBosses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildBosses_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildChatMessages_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildChatMessages_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GuildInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedByPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildInvites_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildInvites_Players_InvitedByPlayerId",
                        column: x => x.InvitedByPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildInvites_Players_InvitedPlayerId",
                        column: x => x.InvitedPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContributionPoints = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMemberships_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildMemberships_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildStrategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StrategyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildStrategies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildStrategies_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildStrategies_Players_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildWars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Guild1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Guild2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Guild1Score = table.Column<int>(type: "int", nullable: false),
                    Guild2Score = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WinnerGuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TreasuryReward = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildWars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildWars_Guilds_Guild1Id",
                        column: x => x.Guild1Id,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildWars_Guilds_Guild2Id",
                        column: x => x.Guild2Id,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategyRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyRatings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StrategyRatings_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Abilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false),
                    Healing = table.Column<int>(type: "int", nullable: false),
                    CooldownTurns = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TargetsAllies = table.Column<bool>(type: "bit", nullable: false),
                    IsAoE = table.Column<bool>(type: "bit", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Abilities_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildBossAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildBossId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DamageDealt = table.Column<int>(type: "int", nullable: false),
                    WasKillingBlow = table.Column<bool>(type: "bit", nullable: false),
                    BattleLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBossAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildBossAttempts_GuildBosses_GuildBossId",
                        column: x => x.GuildBossId,
                        principalTable: "GuildBosses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildBossAttempts_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildWarContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildWarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildWarContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildWarContributions_GuildWars_GuildWarId",
                        column: x => x.GuildWarId,
                        principalTable: "GuildWars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildWarContributions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abilities_UnitId",
                table: "Abilities",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Category",
                table: "Achievements",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFeedEntries_ActivityType",
                table: "ActivityFeedEntries",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityFeedEntries_PlayerId_CreatedAt",
                table: "ActivityFeedEntries",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAlerts_IsAcknowledged_CreatedAt",
                table: "AdminAlerts",
                columns: new[] { "IsAcknowledged", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAlerts_Severity",
                table: "AdminAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminPlayerId",
                table: "AdminAuditLogs",
                column: "AdminPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_CreatedAt",
                table: "AdminAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_TargetPlayerId",
                table: "AdminAuditLogs",
                column: "TargetPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_PlayerId_IsActive",
                table: "ApiKeys",
                columns: new[] { "PlayerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyUsageLogs_ApiKeyId_IpAddress",
                table: "ApiKeyUsageLogs",
                columns: new[] { "ApiKeyId", "IpAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyUsageLogs_ApiKeyId_Timestamp",
                table: "ApiKeyUsageLogs",
                columns: new[] { "ApiKeyId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_BattlePasses_IsActive",
                table: "BattlePasses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BattlePasses_SeasonId",
                table: "BattlePasses",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleReplays_BattleId",
                table: "BattleReplays",
                column: "BattleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleReplays_ShareUrl",
                table: "BattleReplays",
                column: "ShareUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Battles_Player1Id",
                table: "Battles",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_Player2Id",
                table: "Battles",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_ContentCreators_IsVerified",
                table: "ContentCreators",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_ContentCreators_PlayerId",
                table: "ContentCreators",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CosmeticItems_Category",
                table: "CosmeticItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CosmeticItems_IsAvailable",
                table: "CosmeticItems",
                column: "IsAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumModules_InstructorId",
                table: "CurriculumModules",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumModules_IsPublished",
                table: "CurriculumModules",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumModules_JoinCode",
                table: "CurriculumModules",
                column: "JoinCode",
                unique: true,
                filter: "[JoinCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyChallenges_PlayerId_ExpiresAt",
                table: "DailyChallenges",
                columns: new[] { "PlayerId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordLinks_DiscordUserId",
                table: "DiscordLinks",
                column: "DiscordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordLinks_PlayerId",
                table: "DiscordLinks",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordWebhooks_PlayerId",
                table: "DiscordWebhooks",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_EasterEggs_Code",
                table: "EasterEggs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EasterEggs_PagePath_IsActive",
                table: "EasterEggs",
                columns: new[] { "PagePath", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EasterEggs_WeekStart_WeekEnd",
                table: "EasterEggs",
                columns: new[] { "WeekStart", "WeekEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_EggClaims_EasterEggId",
                table: "EggClaims",
                column: "EasterEggId");

            migrationBuilder.CreateIndex(
                name: "IX_EggClaims_PlayerId_EasterEggId",
                table: "EggClaims",
                columns: new[] { "PlayerId", "EasterEggId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalModifiers_IsActive_StartDate_EndDate",
                table: "EnvironmentalModifiers",
                columns: new[] { "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildBossAttempts_GuildBossId",
                table: "GuildBossAttempts",
                column: "GuildBossId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildBossAttempts_PlayerId",
                table: "GuildBossAttempts",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildBosses_GuildId",
                table: "GuildBosses",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildChatMessages_GuildId_CreatedAt",
                table: "GuildChatMessages",
                columns: new[] { "GuildId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildChatMessages_PlayerId",
                table: "GuildChatMessages",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildInvites_GuildId_InvitedPlayerId_Status",
                table: "GuildInvites",
                columns: new[] { "GuildId", "InvitedPlayerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildInvites_InvitedByPlayerId",
                table: "GuildInvites",
                column: "InvitedByPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildInvites_InvitedPlayerId",
                table: "GuildInvites",
                column: "InvitedPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildMemberships_GuildId_PlayerId",
                table: "GuildMemberships",
                columns: new[] { "GuildId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMemberships_PlayerId",
                table: "GuildMemberships",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_LeaderId",
                table: "Guilds",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_Name",
                table: "Guilds",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_Tag",
                table: "Guilds",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildStrategies_CreatorId",
                table: "GuildStrategies",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildStrategies_GuildId_CreatedAt",
                table: "GuildStrategies",
                columns: new[] { "GuildId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildWarContributions_GuildWarId_PlayerId",
                table: "GuildWarContributions",
                columns: new[] { "GuildWarId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildWarContributions_PlayerId",
                table: "GuildWarContributions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildWars_Guild1Id",
                table: "GuildWars",
                column: "Guild1Id");

            migrationBuilder.CreateIndex(
                name: "IX_GuildWars_Guild2Id",
                table: "GuildWars",
                column: "Guild2Id");

            migrationBuilder.CreateIndex(
                name: "IX_GuildWars_Status",
                table: "GuildWars",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LootDrops_BattleId",
                table: "LootDrops",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_LootDrops_PlayerId_Claimed",
                table: "LootDrops",
                columns: new[] { "PlayerId", "Claimed" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ExpiresAt",
                table: "Notifications",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PlayerId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "PlayerId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_AchievementId",
                table: "PlayerAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_PlayerId_AchievementId",
                table: "PlayerAchievements",
                columns: new[] { "PlayerId", "AchievementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActivities_ActivityDate",
                table: "PlayerActivities",
                column: "ActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerActivities_PlayerId_ActivityDate",
                table: "PlayerActivities",
                columns: new[] { "PlayerId", "ActivityDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBattlePasses_BattlePassId",
                table: "PlayerBattlePasses",
                column: "BattlePassId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBattlePasses_PlayerId_BattlePassId",
                table: "PlayerBattlePasses",
                columns: new[] { "PlayerId", "BattlePassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCosmetics_CosmeticItemId",
                table: "PlayerCosmetics",
                column: "CosmeticItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCosmetics_PlayerId_CosmeticItemId",
                table: "PlayerCosmetics",
                columns: new[] { "PlayerId", "CosmeticItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_ActiveTitleId",
                table: "Players",
                column: "ActiveTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Email",
                table: "Players",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonRanks_PlayerId_SeasonId",
                table: "PlayerSeasonRanks",
                columns: new[] { "PlayerId", "SeasonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSeasonRanks_SeasonId_SeasonRating",
                table: "PlayerSeasonRanks",
                columns: new[] { "SeasonId", "SeasonRating" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTitles_Name",
                table: "PlayerTitles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Code",
                table: "Referrals",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredPlayerId",
                table: "Referrals",
                column: "ReferredPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerId",
                table: "Referrals",
                column: "ReferrerId");

            migrationBuilder.CreateIndex(
                name: "IX_RivalAssignments_PlayerId_ExpiresAt",
                table: "RivalAssignments",
                columns: new[] { "PlayerId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RivalAssignments_RivalId",
                table: "RivalAssignments",
                column: "RivalId");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_IsActive",
                table: "Seasons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_SeasonNumber",
                table: "Seasons",
                column: "SeasonNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_CreatorId",
                table: "Strategies",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_DownloadCount",
                table: "Strategies",
                column: "DownloadCount");

            migrationBuilder.CreateIndex(
                name: "IX_Strategies_IsPublic",
                table: "Strategies",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRatings_PlayerId",
                table: "StrategyRatings",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyRatings_StrategyId_PlayerId",
                table: "StrategyRatings",
                columns: new[] { "StrategyId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_ModuleId",
                table: "StudentEnrollments",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_PlayerId_ModuleId",
                table: "StudentEnrollments",
                columns: new[] { "PlayerId", "ModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionEvents_PlayerId_CreatedAt",
                table: "SubscriptionEvents",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlayerId",
                table: "Subscriptions",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeCustomerId",
                table: "Subscriptions",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StripeSubscriptionId",
                table: "Subscriptions",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_PlayerId",
                table: "Teams",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntries_PlayerId",
                table: "TournamentEntries",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentEntries_TournamentId_PlayerId",
                table: "TournamentEntries",
                columns: new[] { "TournamentId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_Round_MatchNumber",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "Round", "MatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Status",
                table: "Tournaments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UnitMasteries_PlayerId_UnitId",
                table: "UnitMasteries",
                columns: new[] { "PlayerId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_PlayerId",
                table: "Units",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Abilities");

            migrationBuilder.DropTable(
                name: "ActivityFeedEntries");

            migrationBuilder.DropTable(
                name: "AdminAlerts");

            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "ApiKeyUsageLogs");

            migrationBuilder.DropTable(
                name: "BattleReplays");

            migrationBuilder.DropTable(
                name: "ContentCreators");

            migrationBuilder.DropTable(
                name: "DailyChallenges");

            migrationBuilder.DropTable(
                name: "DiscordLinks");

            migrationBuilder.DropTable(
                name: "DiscordWebhooks");

            migrationBuilder.DropTable(
                name: "EggClaims");

            migrationBuilder.DropTable(
                name: "EnvironmentalModifiers");

            migrationBuilder.DropTable(
                name: "GuildBossAttempts");

            migrationBuilder.DropTable(
                name: "GuildChatMessages");

            migrationBuilder.DropTable(
                name: "GuildInvites");

            migrationBuilder.DropTable(
                name: "GuildMemberships");

            migrationBuilder.DropTable(
                name: "GuildStrategies");

            migrationBuilder.DropTable(
                name: "GuildWarContributions");

            migrationBuilder.DropTable(
                name: "LootDrops");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PlayerAchievements");

            migrationBuilder.DropTable(
                name: "PlayerActivities");

            migrationBuilder.DropTable(
                name: "PlayerBattlePasses");

            migrationBuilder.DropTable(
                name: "PlayerCosmetics");

            migrationBuilder.DropTable(
                name: "PlayerSeasonRanks");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "RivalAssignments");

            migrationBuilder.DropTable(
                name: "StrategyRatings");

            migrationBuilder.DropTable(
                name: "StudentEnrollments");

            migrationBuilder.DropTable(
                name: "SubscriptionEvents");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "TournamentEntries");

            migrationBuilder.DropTable(
                name: "TournamentMatches");

            migrationBuilder.DropTable(
                name: "UnitMasteries");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "Battles");

            migrationBuilder.DropTable(
                name: "EasterEggs");

            migrationBuilder.DropTable(
                name: "GuildBosses");

            migrationBuilder.DropTable(
                name: "GuildWars");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "BattlePasses");

            migrationBuilder.DropTable(
                name: "CosmeticItems");

            migrationBuilder.DropTable(
                name: "Strategies");

            migrationBuilder.DropTable(
                name: "CurriculumModules");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "PlayerTitles");
        }
    }
}
