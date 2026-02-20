# API Combat Game - Implementation Task List

**Complete audit record of all features and implementations across all phases.**
**Source of truth for what has been built, what's in progress, and what's planned.**

---

## Phase 1: Core API ✅ COMPLETE

- [x] Authentication (JWT + Cookie dual auth)
- [x] Player registration and login
- [x] Unit system (5 classes, 25 templates, abilities)
- [x] Team configuration (up to 5 units per team)
- [x] Battle system (queue, matchmaking, resolution)
- [x] Declarative strategy engine
- [x] Leaderboard (Elo rankings)
- [x] Custom API documentation framework

---

## Phase 2: Web UI + Monetization ✅ COMPLETE

- [x] Stripe integration (checkout, webhooks, customer portal)
- [x] Subscription management (create, cancel, update)
- [x] Account dashboard pages (profile, API keys, billing, settings)
- [x] Pricing page with tier comparison
- [x] Login/Register web pages
- [x] Cookie authentication for web UI
- [x] API key management service
- [x] Rate limiting middleware (tier-based: Free 60, Premium 120, Premium+ 300 req/min)

### Phase 2 — Tier Gating ✅ COMPLETE

- [x] SubscriptionTier enum (Free, Premium, PremiumPlus)
- [x] Player.CurrentTier and DailyBattlesUsed fields
- [x] RequiresTier attribute for controller actions
- [x] TierGatingActionFilter (IAsyncActionFilter)
- [x] Battle limit enforcement (Free: 10/day)
- [x] Team slot limit enforcement (Free: 3, Premium: 10)
- [x] Register TierGatingActionFilter in DI (global filter)

---

## Phase 3: Guilds + Social ✅ COMPLETE

### Guild Foundation ✅ COMPLETE

- [x] Guild domain model (Name, Tag, Description, Level, XP, MaxMembers)
- [x] GuildMembership model (PlayerId, GuildId, Role, ContributionPoints)
- [x] GuildRole enum (Member, Officer, Leader)
- [x] GuildResponse DTO
- [x] GuildInvite domain model (with status enum: Pending/Accepted/Declined/Expired)
- [x] GuildPermission enum (ViewInfo, InviteMembers, KickMembers, etc.)
- [x] IGuildService interface
- [x] GuildService implementation with permission matrix
- [x] Guild CRUD DTOs (CreateGuildRequest, GuildMemberDto, InvitePlayerRequest, etc.)
- [x] GuildController with endpoints:
  - [x] POST /api/v1/guild/create (Premium required via [RequiresTier])
  - [x] DELETE /api/v1/guild/{guildId}
  - [x] GET /api/v1/guild/{guildId}
  - [x] GET /api/v1/guild/{guildId}/members
  - [x] GET /api/v1/guild/mine
  - [x] POST /api/v1/guild/{guildId}/invite
  - [x] GET /api/v1/guild/invites
  - [x] POST /api/v1/guild/invites/{inviteId}/accept
  - [x] POST /api/v1/guild/invites/{inviteId}/decline
  - [x] POST /api/v1/guild/{guildId}/kick (Leader only)
  - [x] POST /api/v1/guild/{guildId}/promote (Leader only, supports leadership transfer)
  - [x] POST /api/v1/guild/leave
- [x] Permission matrix enforcement per role (Leader/Officer/Member)
- [x] GameDbContext: GuildInvite DbSet + relationship config
- [x] Register IGuildService in Program.cs

### Guild Treasury & Upgrades ✅ COMPLETE

- [x] Guild model: TreasuryBalance, GoldBonusPercent, MaxRaidAttempts fields
- [x] IGuildTreasuryService interface
- [x] GuildTreasuryService implementation
- [x] Treasury DTOs (TreasuryResponse, TreasurySpendRequest, GuildUpgradeOption, TreasuryDepositRequest)
- [x] Hardcoded guild upgrade definitions:
  - [x] max_members_30 (50,000g)
  - [x] max_members_50 (100,000g)
  - [x] gold_bonus_10 (30,000g)
  - [x] gold_bonus_20 (60,000g)
  - [x] raid_attempts_4 (40,000g)
  - [x] raid_attempts_5 (80,000g)
- [x] Endpoints:
  - [x] GET /api/v1/guild/{guildId}/treasury
  - [x] POST /api/v1/guild/{guildId}/treasury/spend (Leader only)
  - [x] POST /api/v1/guild/{guildId}/treasury/deposit (any member)
- [x] Integrate guild gold bonus into PlayerProgressionService
- [x] Register IGuildTreasuryService in Program.cs

### Guild Raid Boss System ✅ COMPLETE

- [x] GuildBoss domain model
- [x] GuildBossAttempt domain model
- [x] GuildBossResponse, BossAttemptRequest, BossAttemptResponse DTOs
- [x] IGuildBossService interface
- [x] GuildBossService: GetActiveGuildBoss, GetBossLeaderboard, SpawnBossForGuild
- [x] GuildBossController endpoints (current, attempt, leaderboard)
- [x] GuildBossSpawnJob (weekly Monday spawn)
- [x] Implement GuildBossService.AttemptBoss()
  - [x] Boss damage calculation (unit attack vs boss defense, 10 turns per unit)
  - [x] Daily attempt limit enforcement (respects guild MaxRaidAttempts upgrade)
  - [x] Boss defeat detection + reward distribution to all contributors
  - [x] Contribution points tracking
  - [x] Guild treasury deposit on boss defeat
  - [x] Achievement hook on killing blow

### Guild Chat ✅ COMPLETE

- [x] GuildChatMessage domain model (with system message support)
- [x] IGuildChatService interface
- [x] GuildChatService implementation
- [x] Chat DTOs (ChatMessageResponse, PostChatRequest)
- [x] Endpoints:
  - [x] GET /api/v1/guild/{guildId}/chat (cursor pagination via `before` param)
  - [x] POST /api/v1/guild/{guildId}/chat
- [x] GameDbContext: GuildChatMessage DbSet + index config

### Guild Strategy Library ✅ COMPLETE

- [x] GuildStrategy domain model
- [x] IGuildStrategyService interface
- [x] GuildStrategyService implementation
- [x] Strategy DTOs (PublishGuildStrategyRequest, UpdateGuildStrategyRequest, GuildStrategyResponse)
- [x] Endpoints:
  - [x] GET /api/v1/guild/{guildId}/strategies
  - [x] POST /api/v1/guild/{guildId}/strategies (Officer/Leader)
  - [x] PUT /api/v1/guild/{guildId}/strategies/{strategyId}
  - [x] DELETE /api/v1/guild/{guildId}/strategies/{strategyId} (Creator or Leader)
- [x] GameDbContext: GuildStrategy DbSet + index config

---

## Phase 4: Engagement Features ✅ COMPLETE

### Player Progression (XP, Leveling, Gold Economy) ✅ COMPLETE

- [x] Player model: ExperiencePoints, WinStreak, LastFirstBattleBonus fields
- [x] IPlayerProgressionService interface
- [x] PlayerProgressionService implementation
  - [x] Gold economy: Win +50, Loss +10, Streak +10/win, First battle +100/day
  - [x] XP system: Win +100, Loss +25
  - [x] Level-up formula: 500 * level * 1.5
  - [x] Level-up gold reward: +250
  - [x] Tier multipliers: Free 1.0x, Premium 1.5x, Premium+ 2.0x
  - [x] Guild gold bonus integration (GoldBonusPercent from guild upgrades)
- [x] BattleRewardsSummary DTO
- [x] BattleResultResponse extended with XP/streak/level fields
- [x] BattleService refactored to use PlayerProgressionService
- [x] Battle model: Team1ClassesJson, Team2ClassesJson for challenge checking
- [x] ChallengeService.ClaimReward uses progression service
- [x] PlayerController profile includes progression data
- [x] Registered in Program.cs

### Daily Challenge System ✅ COMPLETE

- [x] DailyChallenge domain model
- [x] IChallengeService interface
- [x] ChallengeService: GetActiveChallenges, GenerateDailyChallenges, ClaimReward
- [x] ChallengeService.CheckChallengeProgress framework
- [x] DailyChallengeGenerationJob (background)
- [x] WinStreakChallenge.CheckProgress (increments on win, resets on loss)
- [x] TeamCompositionChallenge.CheckProgress (validates all units match required class)

### Environmental Modifiers ✅ COMPLETE

- [x] EnvironmentalModifier domain model
- [x] IModifierService interface
- [x] ModifierService: GetCurrentModifier, GetUpcomingModifier, RotateModifier
- [x] IModifierEffect system (ArcaneDisruption, HeavyArmor)
- [x] WeeklyModifierRotationJob
- [x] Seed data (2 modifiers)

### Strategy Marketplace ✅ COMPLETE

- [x] Strategy domain model with decay system
- [x] StrategyRating domain model
- [x] IStrategyMarketplaceService interface
- [x] StrategyMarketplaceService: Upload, Browse, Download, Rate, ApplyDecay
- [x] StrategyDecayJob (background)

### Unit Mastery ✅ COMPLETE

- [x] UnitMastery domain model
- [x] IMasteryService interface
- [x] MasteryService: GetPlayerMastery, GetUnitMastery, IncrementMastery

### Battle Replays ✅ COMPLETE

- [x] BattleReplay domain model
- [x] IReplayService interface
- [x] ReplayService: CreateReplay, GetReplay, IncrementViewCount

### Achievement System ✅ COMPLETE

- [x] Achievement domain model
- [x] PlayerAchievement domain model
- [x] Seed data (8 achievements, 6 player titles)
- [x] IAchievementService interface
- [x] AchievementService implementation
  - [x] Event-based checking (battle_won, battle_lost, boss_killing_blow, unit_unlocked)
  - [x] Progress tracking per achievement
  - [x] Gold/points award on unlock
- [x] GET /api/v1/player/achievements endpoint (with secret achievement hiding)
- [x] Hook into battle completion flow (BattleService)
- [x] Hook into boss killing blow flow (GuildBossService)
- [x] Hook into unit unlock flow (PlayerController)

### Player Dashboard ✅ COMPLETE

- [x] Login streak tracking (Player.LoginStreak, LastLoginRewardDate)
- [x] Dashboard page with engagement hub
- [x] Daily login reward claim

---

## Phase 4.5: Integration & Polish ✅ COMPLETE

- [x] Tier-based rate limits in RateLimitingMiddleware (Free: 60, Premium: 120, Premium+: 300 req/min)
- [x] Add CurrentTier claim to JWT in AuthService
- [x] Player profile: guild info (name, tag, role, contribution), achievement count
- [x] GuildController API doc annotations (ApiCategoryMeta, ApiGameTip, ApiExample, ApiDifficulty, ApiPrerequisite)
- [x] GuildInviteExpiryJob (background job — daily cleanup of expired invites)
- [x] Material Design 3 CSS-first migration (all 29 pages)
- [x] Dark/light theme system

---

## Custom API Documentation ✅ COMPLETE

- [x] Custom C# attributes (ApiGameTip, ApiExample, ApiPrerequisite, ApiDifficulty, ApiCategoryMeta)
- [x] GameMetadataOperationFilter (x-game-* OpenAPI extensions)
- [x] TagDescriptionsDocumentFilter with icons/colors (22+ tags)
- [x] EnumSchemaFilter (string names instead of integers)
- [x] ApiDocs.cshtml.cs PageModel (reads OpenAPI spec via ISwaggerProvider)
- [x] ApiDocs.cshtml (hero, quick start, auth, TOC sidebar, endpoint groups, models)
- [x] Partial views: _EndpointGroup, _Endpoint, _CodeBlock, _SchemaTable
- [x] Gradient banner with stats bar
- [x] Active nav link styling
- [x] Dark mode support
- [x] All controllers annotated with custom attributes
- [x] Scalar package removed

---

## Admin Dashboard ✅ COMPLETE

See `docs/ADMIN-DASHBOARD-SPECIFICATION.md` for full spec.

### Infrastructure ✅ COMPLETE

- [x] AdminRole enum (None, Support, Analytics, SuperAdmin)
- [x] Player model: IsAdmin, AdminRole fields
- [x] Cookie auth: Admin claims (ClaimTypes.Role = "Admin", AdminRole claim)
- [x] Authorization policy: `options.AddPolicy("Admin", policy => policy.RequireRole("Admin"))`
- [x] AdminSeedData.cs (seeds admin from appsettings config)
- [x] Admin:SeedUsername/SeedEmail/SeedPassword in appsettings.Development.json

### Service Layer ✅ COMPLETE

- [x] IAdminAnalyticsService interface
- [x] AdminAnalyticsService implementation
  - [x] GetOverviewAsync (DAU/WAU/MAU, MRR/ARR, battles, signups, tier breakdown)
  - [x] GetPlayerAnalyticsAsync (search, filter, pagination, battle counts/win rates)
  - [x] GetPlayerDetailAsync (deep dive with recent battles, guild info, achievements)
  - [x] GetMetaDataAsync (unit class win rates from Team1/2ClassesJson, strategies, modifiers)
  - [x] GetGuildAnalyticsAsync (guild stats, top guilds, boss completion)
  - [x] GetTechnicalDataAsync (battle pipeline, DB entity counts)
  - [x] Admin actions: ToggleAdmin, AdjustCurrency, SetTier, ResetPassword
- [x] AdminViewModels.cs (14 view model classes)

### Admin Pages (Razor Pages with _AdminLayout) ✅ COMPLETE

- [x] _AdminLayout.cshtml (standalone sidebar layout with SVG icons, dark mode, active state)
- [x] Index.cshtml (Overview: 5 KPI cards, activity, player segments, engagement health)
- [x] Players.cshtml (Player list with search, tier filter, pagination)
- [x] PlayerDetail.cshtml (Deep dive + admin actions: adjust gold, set tier, toggle admin, reset password)
- [x] Meta.cshtml (Unit class balance with OP/UP/Watch/Balanced status, modifiers, top strategies)
- [x] Guilds.cshtml (Guild KPIs, top guilds table, boss completion rate)
- [x] Technical.cshtml (Battle pipeline, DB stats, background jobs)
- [x] Tools.cshtml (Grant admin by username, database reset, quick links)

---

## Notifications & Persistence System ✅ COMPLETE

### Domain Models ✅ COMPLETE

- [x] Notification (PlayerId, Type, Category, Title, Message, ActionUrl, IsRead, ExpiresAt)
- [x] PlayerActivity (PlayerId, ActivityDate, RequestCount, LastRequestAt — one row per player per day)
- [x] ApiKeyUsageLog (ApiKeyId, IpAddress, UserAgent, Endpoint, Timestamp)
- [x] AdminAuditLog (AdminPlayerId, Action, TargetPlayerId, DetailsJson, CreatedAt)
- [x] AdminAlert (Severity, Category, Message, ActionRequired, IsAcknowledged)
- [x] SubscriptionEvent (PlayerId, EventType, OldTier, NewTier, AmountUsd)

### Enums ✅ COMPLETE

- [x] NotificationType (26 values including SeasonReward, SeasonRankChange)
- [x] NotificationCategory (Battle, Guild, Progression, Marketplace, System, Security)
- [x] AlertSeverity (Info, Warning, Critical)

### Notification Service ✅ COMPLETE

- [x] INotificationService interface
- [x] NotificationService implementation (preference-aware, batch guild, system always-on)
- [x] NotificationPreferences class
- [x] 6 API endpoints (count, list, mark-read, mark-all, preferences get/put)
- [x] Bell icon with red badge in _Layout.cshtml
- [x] Account/Notifications.cshtml + Account/Settings.cshtml preference toggles

### Notification Hooks (7 services) ✅ COMPLETE

- [x] BattleService → BattleCompleted, WinStreakMilestone
- [x] GuildService → GuildInvited, GuildInviteResponse, GuildKicked, GuildPromoted
- [x] GuildBossService → GuildBossSpawned, GuildBossDefeated
- [x] AchievementService → AchievementUnlocked
- [x] GuildChatService → GuildChatMention
- [x] MasteryService → MasteryRankUp
- [x] PlayerProgressionService → LevelUp

### Admin Audit Logging ✅ COMPLETE

- [x] AdminAnalyticsService: AuditLogAsync helper
- [x] All 4 admin actions audited + security notifications sent

### Background Jobs ✅ COMPLETE

- [x] NotificationCleanupJob (every 6 hours)
- [x] AdminAlertJob (every hour)

---

## Phase 5: Strategic Roadmap Features ✅ COMPLETE

### 5.1 AI Practice Opponents ✅ COMPLETE

**Files created:**
- `Models/DTOs/AI/AiOpponentResponse.cs` — AiOpponentResponse, AiOpponentListResponse DTOs
- `Models/DTOs/AI/PracticeBattleRequest.cs` — Request DTO (TeamId + OpponentId)
- `Services/Interfaces/IAiOpponentService.cs` — Interface
- `Services/AiOpponentService.cs` — Full implementation

**Details:**
- [x] 9 AI presets across 3 difficulty tiers (3 per tier)
- [x] Novice: 3 units, rating 600-800, basic strategies
- [x] Intermediate: 4 units, rating 900-1100, coordinated abilities
- [x] Expert: 5 units, rating 1200-1500, optimized strategies
- [x] Practice battles resolve synchronously (no queue)
- [x] 50% reward multiplier (25g win / 5g loss, 50 XP win / 12 XP loss)
- [x] No rating change, no daily battle limit consumed
- [x] Battle record saved with Mode="practice", Player2Id=null
- [x] Endpoints:
  - [x] GET /api/v1/ai/opponents
  - [x] POST /api/v1/ai/practice
- [x] Registered in Program.cs, Tag added to TagDescriptionsDocumentFilter

### 5.2 Ranked Seasons ✅ COMPLETE

**Files created:**
- `Models/Enums/SeasonTier.cs` — Bronze, Silver, Gold, Platinum, Diamond, Legend
- `Models/Domain/Season.cs` — Season entity
- `Models/Domain/PlayerSeasonRank.cs` — Player season tracking
- `Models/DTOs/Season/SeasonResponses.cs` — All season DTOs
- `Services/Interfaces/ISeasonService.cs` — Interface
- `Services/SeasonService.cs` — Full implementation

**Details:**
- [x] 6 tiers: Bronze(0), Silver(1000), Gold(1200), Platinum(1400), Diamond(1600), Legend(1800)
- [x] 8-week season duration, auto-creates new seasons
- [x] End-of-season rewards: Bronze(100g/50xp) through Legend(5000g/1500xp + exclusive titles)
- [x] Tier promotion/demotion notifications
- [x] Integrated into BattleService (updates season rating after ranked battles)
- [x] DbContext: Season + PlayerSeasonRank with indexes
- [x] Endpoints:
  - [x] GET /api/v1/season/current
  - [x] GET /api/v1/season/leaderboard
  - [x] POST /api/v1/season/rewards/{seasonId}

### 5.3 Loot Drops & Variable Rewards ✅ COMPLETE

**Files created:**
- `Models/Enums/LootDropType.cs` — CurrencyPack, XpBoost, RareTitle, CriticalGold
- `Models/Domain/LootDrop.cs` — Loot drop entity
- `Models/DTOs/Loot/LootResponses.cs` — Response DTOs
- `Services/Interfaces/ILootService.cs` — Interface
- `Services/LootService.cs` — Full implementation

**Details:**
- [x] Base drop chance 15% per battle
- [x] Win streak bonus: +3.3% per streak (caps +10%)
- [x] 5% critical gold chance (3x base, independent roll)
- [x] Premium guaranteed drop every 5 battles
- [x] Drop distribution: 50% currency (50-500g), 30% XP (50-200), 15% rare currency (200-800g), 5% rare title
- [x] Integrated into BattleService (rolls loot for both players after battle)
- [x] Endpoints:
  - [x] GET /api/v1/loot/pending
  - [x] POST /api/v1/loot/claim

### 5.4 Referral System ✅ COMPLETE

**Files created:**
- `Models/Domain/Referral.cs` — Referral entity
- `Models/DTOs/Referral/ReferralResponses.cs` — All DTOs
- `Services/Interfaces/IReferralService.cs` — Interface
- `Services/ReferralService.cs` — Full implementation

**Details:**
- [x] 8-character referral codes from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`
- [x] Referrer reward: 500g per successful referral
- [x] Referred bonus: 300g welcome bonus
- [x] Auto-generates new code after each redemption
- [x] Referral leaderboard
- [x] Endpoints:
  - [x] GET /api/v1/referral/info
  - [x] POST /api/v1/referral/redeem/{code}
  - [x] GET /api/v1/referral/leaderboard

### 5.5 Currency Sink Expansion ✅ COMPLETE

**Files created:**
- `Models/DTOs/UnitCustomization/UnitCustomizationDtos.cs` — Request/response DTOs
- `Controllers/UnitCustomizationController.cs` — 3 endpoints

**Files modified:**
- `Models/Domain/Unit.cs` — Added CustomName, IsGolden, RerollCount

**Details:**
- [x] Unit rename: 200g, MaxLength 100
- [x] Stat reroll: 500g, randomizes one stat within class range
- [x] Golden upgrade: 2000g, +5% all stats, once per unit
- [x] Stat ranges defined per class (Warrior, Mage, Ranger, Healer, Tank)
- [x] Endpoints:
  - [x] POST /api/v1/units/rename
  - [x] POST /api/v1/units/reroll
  - [x] POST /api/v1/units/golden-upgrade

### 5.6 Rival System ✅ COMPLETE

**Files created:**
- `Models/Domain/RivalAssignment.cs` — Rival assignment entity
- `Models/DTOs/Rival/RivalResponses.cs` — DTOs
- `Services/Interfaces/IRivalService.cs` — Interface
- `Services/RivalService.cs` — Full implementation

**Details:**
- [x] Auto-assigns rival within 200 rating points (widens if needed)
- [x] 7-day duration, weekly rotation
- [x] +100g bonus per rival win
- [x] Tracks wins/losses/bonus gold against rival
- [x] Integrated into BattleService (CheckRivalBattleAsync after battles)
- [x] Notifications on rival defeat
- [x] Endpoints:
  - [x] GET /api/v1/rival/current

### 5.7 Battle Pass ✅ COMPLETE

**Files created:**
- `Models/Enums/BattlePassTrack.cs` — Free, Premium
- `Models/Domain/BattlePass.cs` — Pass definition (30 levels, tied to season)
- `Models/Domain/PlayerBattlePass.cs` — Player progress tracking
- `Models/DTOs/BattlePass/BattlePassResponses.cs` — All DTOs
- `Services/Interfaces/IBattlePassService.cs` — Interface
- `Services/BattlePassService.cs` — Full implementation

**Details:**
- [x] 30 levels, 1000 XP per level
- [x] Free track: currency and XP rewards at every level
- [x] Premium track: bigger rewards + exclusive titles (Premium Plus auto-included)
- [x] Milestone rewards every 5 levels, titles at level 10/20/30
- [x] Level 30 completion: "Season Champion" legendary title
- [x] Premium Plus gets 25% bonus XP
- [x] Auto-creates pass tied to active season
- [x] XP sources: battles (+100 win / +25 loss), challenges (+50-200), daily login (+25)
- [x] Integrated into BattleService (awards XP after battles)
- [x] DbContext: BattlePass + PlayerBattlePass
- [x] Endpoints:
  - [x] GET /api/v1/battlepass/progress
  - [x] POST /api/v1/battlepass/claim/{level}

### 5.8 Enhanced Daily Challenges ✅ COMPLETE

**Files created:**
- `Services/Challenges/BattleCountChallenge.cs` — Easy: Complete N battles
- `Services/Challenges/VarietySquadChallenge.cs` — Medium: Win with N different classes
- `Services/Challenges/UnderdogChallenge.cs` — Hard: Beat higher-rated opponents
- `Services/Challenges/FlawlessVictoryChallenge.cs` — Hard: Win with all units surviving

**Files modified:**
- `Models/Domain/DailyChallenge.cs` — Added Difficulty field
- `Models/DTOs/Challenge/ChallengeResponse.cs` — Added Difficulty + BattlePassXp fields
- `Services/ChallengeService.cs` — Full rewrite with difficulty tiers, BP integration, refresh
- `Services/Interfaces/IChallengeService.cs` — Added RefreshChallengesAsync
- `Controllers/Api/ChallengeController.cs` — Added refresh endpoint, updated DTO mapping

**Details:**
- [x] 3 difficulty tiers: easy, medium, hard
- [x] Daily challenge mix: 1 easy + 1 medium + 1 hard
- [x] 6 challenge generators total (2 existing + 4 new):
  - Easy: BattleCount (complete N battles)
  - Medium: TeamComposition, WinStreak, VarietySquad
  - Hard: Underdog, FlawlessVictory
- [x] Scaling rewards by difficulty:
  - Easy: 100-200g, 50-100 XP, 50 BP XP
  - Medium: 300-500g, 100-200 XP, 100 BP XP
  - Hard: 800-1000g, 250-300 XP, 200 BP XP
- [x] Challenge refresh for Premium subscribers
- [x] Battle pass XP on challenge claim
- [x] Endpoints:
  - [x] GET /api/v1/challenges/daily (enhanced with difficulty)
  - [x] POST /api/v1/challenges/claim (enhanced with BP XP)
  - [x] POST /api/v1/challenges/refresh (NEW — Premium only)

### 5.9 Guild Wars ✅ COMPLETE

**Files created:**
- `Models/Domain/GuildWar.cs` — GuildWar + GuildWarContribution entities
- `Models/DTOs/GuildWar/GuildWarResponses.cs` — DTOs
- `Services/Interfaces/IGuildWarService.cs` — Interface
- `Services/GuildWarService.cs` — Full implementation
- `Controllers/GuildWarController.cs` — 2 endpoints
- `BackgroundJobs/GuildWarMatchingJob.cs` — Weekly matching + finalization

**Details:**
- [x] Weekly guild vs guild matchups (7-day duration)
- [x] Matched by similar guild level, minimum 3 members to participate
- [x] 10 points per ranked battle win during war
- [x] Individual contribution tracking (points + wins per player)
- [x] Top 5 contributors shown in war status
- [x] Winner gets treasury bonus (500g base + 50g per guild level)
- [x] Draw splits the reward
- [x] Notifications on war start and end
- [x] Integrated into BattleService (records war contribution after ranked wins)
- [x] Background job: every 6 hours checks for expired wars + matches new ones
- [x] DbContext: GuildWar + GuildWarContribution with indexes
- [x] Endpoints:
  - [x] GET /api/v1/guildwar/status
  - [x] GET /api/v1/guildwar/history

### 5.10 Tournament System ✅ COMPLETE

**Files created:**
- `Models/Domain/Tournament.cs` — Tournament, TournamentEntry, TournamentMatch entities
- `Models/DTOs/Tournament/TournamentResponses.cs` — All DTOs
- `Services/Interfaces/ITournamentService.cs` — Interface
- `Services/TournamentService.cs` — Full implementation
- `Controllers/TournamentController.cs` — 3 endpoints
- `BackgroundJobs/TournamentProcessingJob.cs` — Auto-create + process matches

**Details:**
- [x] Weekly single-elimination tournament (16 players max)
- [x] Entry fee: 100g
- [x] Prize pool: 1st 5000g+title, 2nd 2500g, 3rd/4th 1000g
- [x] Seeded brackets by Elo rating
- [x] Auto-generates bracket with byes for non-power-of-2 participants
- [x] Match resolution based on rating probability
- [x] Automatic prize distribution + title award
- [x] Cancel + refund if <2 registrants
- [x] Background job: hourly check for tournament processing
- [x] DbContext: Tournament + TournamentEntry + TournamentMatch
- [x] Endpoints:
  - [x] GET /api/v1/tournament/current
  - [x] POST /api/v1/tournament/enter
  - [x] GET /api/v1/tournament/bracket/{tournamentId}

### 5.11 Cosmetic System & Gems ✅ COMPLETE

**Files created:**
- `Models/Domain/CosmeticItem.cs` — CosmeticItem + PlayerCosmetic entities
- `Models/DTOs/Cosmetic/CosmeticResponses.cs` — All DTOs
- `Services/Interfaces/ICosmeticService.cs` — Interface
- `Services/CosmeticService.cs` — Full implementation
- `Controllers/CosmeticController.cs` — 5 endpoints

**Files modified:**
- `Models/Domain/Player.cs` — Added Gems, GemsEarnedTotal, GemsSpentTotal, ActiveProfileBorderId, ActiveCardBackId

**Details:**
- [x] Premium currency: Gems (separate from gold)
- [x] Dual-currency shop: items purchasable with gems and/or gold
- [x] Legendary items gems-only (goldPrice = 0)
- [x] 4 cosmetic categories: unit_skin, profile_border, card_back, battle_effect
- [x] 4 rarity tiers: common, rare, epic, legendary
- [x] 16 seed items across all categories
- [x] Equip system: one cosmetic per category, auto-unequip previous
- [x] Profile integration: ActiveProfileBorderId, ActiveCardBackId on Player
- [x] DbContext: CosmeticItem + PlayerCosmetic with unique indexes
- [x] Endpoints:
  - [x] GET /api/v1/cosmetics/shop
  - [x] GET /api/v1/cosmetics/owned
  - [x] POST /api/v1/cosmetics/purchase/{cosmeticId}
  - [x] POST /api/v1/cosmetics/equip/{cosmeticId}
  - [x] GET /api/v1/cosmetics/balance

---

### 5.12 Premium Plus Differentiation ✅ COMPLETE

**Files created:**
- `Models/DTOs/Premium/PremiumPlusResponses.cs` — PremiumPerksResponse, GemStipendClaimResponse, BadgeResponse
- `Services/Interfaces/IPremiumPlusService.cs` — Interface
- `Services/PremiumPlusService.cs` — Full implementation
- `Controllers/Api/PremiumPlusController.cs` — 4 endpoints

**Files modified:**
- `Models/Domain/Player.cs` — Added Badge, LastGemStipendClaimedAt
- `Services/PlayerProgressionService.cs` — 1.5x XP multiplier for Premium Plus
- `Services/LootService.cs` — Enhanced loot: guaranteed drop every 3 battles, better rarity distribution (35/30/25/10 vs 50/30/15/5)
- `Services/CosmeticService.cs` — Tier gating, 4 PP-exclusive legendary cosmetics
- `Models/DTOs/Cosmetic/CosmeticResponses.cs` — Added RequiredTier, Locked fields
- `Models/Domain/CosmeticItem.cs` — Added RequiredTier field

**Details:**
- [x] Tiered perk system: Free/Premium/PremiumPlus with full perk breakdown
- [x] Gold multipliers: Free 1.0x, Premium 1.5x, PremiumPlus 2.0x
- [x] XP multiplier: PremiumPlus 1.5x (integrated into PlayerProgressionService)
- [x] Monthly gem stipend: 500 gems for PremiumPlus (30-day cooldown)
- [x] Badge system: 7 badges for PP ("newcomer", "premium", "supporter", "premium_plus", "creator", "elite", "vip")
- [x] Exclusive cosmetics: 4 legendary PP-only items (Platinum Frame, Aurora Pattern, Cosmic Eruption, Gilded Champion)
- [x] Enhanced loot: guaranteed drop every 3 battles (vs 5 for Premium), rarity boost
- [x] Cosmetic tier gating: shop shows locked status, purchase blocked for insufficient tier
- [x] Endpoints:
  - [x] GET /api/v1/premium/perks
  - [x] POST /api/v1/premium/claim-gems
  - [x] POST /api/v1/premium/badge
  - [x] GET /api/v1/premium/badges

### 5.13 Activity Feed & Social ✅ COMPLETE

**Files created:**
- `Models/Domain/ActivityFeedEntry.cs` — ActivityFeedEntry entity
- `Models/DTOs/Activity/ActivityResponses.cs` — ActivityFeedItem, ActivityFeedResponse, PlayerLifetimeStatsResponse
- `Services/Interfaces/IActivityFeedService.cs` — Interface
- `Services/ActivityFeedService.cs` — Full implementation
- `Controllers/Api/ActivityFeedController.cs` — 4 endpoints

**Files modified:**
- `Models/Domain/Player.cs` — Added TotalBattlesPlayed, TotalBattlesWon, TotalGoldEarned, TotalXpEarned, HighestRating, HighestWinStreak
- `Services/BattleService.cs` — Activity feed logging + battle stat recording hooks
- `Data/GameDbContext.cs` — ActivityFeedEntry DbSet + index config

**Details:**
- [x] Activity feed: log battle wins/losses, achievements, level-ups, and other game events
- [x] Lifetime stats tracking: total battles, wins, gold earned, XP earned, highest rating, highest win streak
- [x] Public profile: view other players' feeds and stats by username
- [x] Investment summary: human-readable string summarizing player's total game investment
- [x] Paginated feed with cursor-based pagination (page + pageSize)
- [x] Integrated into BattleService (records stats + logs activities after every battle)
- [x] Endpoints:
  - [x] GET /api/v1/activity/feed (own feed)
  - [x] GET /api/v1/activity/feed/{username} (public feed)
  - [x] GET /api/v1/activity/stats (own lifetime stats)
  - [x] GET /api/v1/activity/stats/{username} (public stats)

### 5.14 Notification System Enhancement ✅ COMPLETE

**Files modified:**
- `Models/Enums/NotificationType.cs` — Added 6 new types: RevengeOpportunity, RivalDefeatedYou, TournamentResult, GuildWarResult, RankUp, RankDown
- `Services/Interfaces/INotificationService.cs` — Added GetDigestAsync, SendRevengeAlertAsync, SendRankChangeAlertAsync, NotificationDigestResponse, DigestCategory, DigestItem
- `Services/NotificationService.cs` — Digest grouping by category, revenge alerts, rank change alerts (100-point thresholds)
- `Services/BattleService.cs` — Revenge alert + rank change alert hooks after battle
- `Controllers/PlayerController.cs` — Added digest endpoint

**Details:**
- [x] 6 new notification types: RevengeOpportunity, RivalDefeatedYou, TournamentResult, GuildWarResult, RankUp, RankDown
- [x] Revenge alert: notifies loser with actionable rematch opportunity
- [x] Rank change alerts: triggers on crossing 100-point rating thresholds (e.g., 1000→1100)
- [x] Notification digest: groups unread notifications by category with counts + top 10 highlights
- [x] Competitive notification preference toggle
- [x] Integrated into BattleService (revenge alert to loser, rank change alerts for both players)
- [x] Endpoint:
  - [x] GET /api/v1/player/notifications/digest

### 5.15 Educational Platform ✅ COMPLETE

**Files created:**
- `Models/Domain/Education.cs` — CurriculumModule + StudentEnrollment entities
- `Models/DTOs/Education/EducationResponses.cs` — All DTOs (module, lesson, enrollment, dashboard)
- `Services/Interfaces/IEducationService.cs` — Interface
- `Services/EducationService.cs` — Full implementation
- `Controllers/Api/EducationController.cs` — 9 endpoints

**Files modified:**
- `Data/GameDbContext.cs` — CurriculumModule + StudentEnrollment DbSets + indexes

**Details:**
- [x] CurriculumModule: title, description, difficulty, lessons (JSON), join code, published flag
- [x] StudentEnrollment: per-student per-module progress tracking
- [x] Any player can create curriculum modules (instructor role)
- [x] 8-character random join codes for private enrollment
- [x] Publish workflow: create → add lessons → publish for public access
- [x] Lesson completion tracking via JSON array of completed indices
- [x] Instructor dashboard: enrollment counts, completion rates, average progress per module
- [x] Endpoints:
  - [x] GET /api/v1/education/modules (browse published)
  - [x] GET /api/v1/education/modules/{moduleId} (detail + progress)
  - [x] POST /api/v1/education/modules (create module)
  - [x] POST /api/v1/education/modules/{moduleId}/publish
  - [x] POST /api/v1/education/enroll/{moduleId}
  - [x] POST /api/v1/education/enroll/code/{code}
  - [x] POST /api/v1/education/modules/{moduleId}/lessons/{lessonIndex}/complete
  - [x] GET /api/v1/education/my-progress
  - [x] GET /api/v1/education/instructor/dashboard

### 5.16 SDK & Client Libraries ✅ COMPLETE

**Files created:**
- `Models/DTOs/Sdk/SdkResponses.cs` — QuickStartResponse, EndpointCatalogResponse, GameStatusResponse
- `Services/Interfaces/ISdkService.cs` — Interface
- `Services/SdkService.cs` — Full implementation
- `Controllers/Api/SdkController.cs` — 3 endpoints (no auth required)

**Details:**
- [x] Quick-start guide: 7-step onboarding sequence with detailed instructions
- [x] Auth guide: JWT token flow explanation
- [x] Code snippets: curl, Python, JavaScript, C# examples for registration + login + battle
- [x] Endpoint catalog: 8 categories with endpoint details (method, path, description, auth required)
- [x] Game status: live metrics (total players, active season, active tournaments, recent battles)
- [x] All endpoints public (no auth required) — designed for developer onboarding
- [x] Endpoints:
  - [x] GET /api/v1/sdk/quickstart
  - [x] GET /api/v1/sdk/endpoints
  - [x] GET /api/v1/sdk/status

### 5.17 Discord Integration ✅ COMPLETE

**Files created:**
- `Models/Domain/DiscordLink.cs` — DiscordLink + DiscordWebhook entities
- `Models/DTOs/Discord/DiscordResponses.cs` — All DTOs (link, verify, webhook, profile)
- `Services/Interfaces/IDiscordService.cs` — Interface
- `Services/DiscordService.cs` — Full implementation
- `Controllers/Api/DiscordController.cs` — 8 endpoints

**Files modified:**
- `Data/GameDbContext.cs` — DiscordLink + DiscordWebhook DbSets + unique indexes

**Details:**
- [x] Account linking: link Discord user ID + username to game account
- [x] Verification: 6-digit code verification flow (link → verify → confirmed)
- [x] Webhook management: register up to 5 webhooks per player with event type filtering
- [x] Supported webhook events: battle_complete, level_up, achievement, guild_boss, season_end, loot_drop
- [x] Bot profile lookup: query player data by Discord user ID (for bot integration)
- [x] Unlink: remove Discord association
- [x] Endpoints:
  - [x] POST /api/v1/discord/link
  - [x] POST /api/v1/discord/verify
  - [x] GET /api/v1/discord/link
  - [x] DELETE /api/v1/discord/link
  - [x] POST /api/v1/discord/webhooks
  - [x] GET /api/v1/discord/webhooks
  - [x] DELETE /api/v1/discord/webhooks/{webhookId}
  - [x] GET /api/v1/discord/lookup/{discordUserId}

### 5.18 Content Creator Program ✅ COMPLETE

**Files created:**
- `Models/Domain/ContentCreator.cs` — ContentCreator entity
- `Models/DTOs/Creator/CreatorResponses.cs` — All DTOs (profile, stats, spotlight, apply request)
- `Services/Interfaces/IContentCreatorService.cs` — Interface
- `Services/ContentCreatorService.cs` — Full implementation
- `Controllers/Api/ContentCreatorController.cs` — 5 endpoints

**Files modified:**
- `Data/GameDbContext.cs` — ContentCreator DbSet + unique index on PlayerId

**Details:**
- [x] Creator application: submit name + bio, one application per player
- [x] Admin verification: verified creators get "creator" badge on Player profile
- [x] Creator stats: strategy downloads, average ratings, educational modules, students, gems earned
- [x] Monthly spotlight: auto-selects top 3 creators by downloads if none manually spotlighted
- [x] Verified creators list: public endpoint sorted by total downloads
- [x] Gem revenue share tracking (GemsEarned field)
- [x] Endpoints:
  - [x] POST /api/v1/creators/apply
  - [x] GET /api/v1/creators/me
  - [x] GET /api/v1/creators/stats
  - [x] GET /api/v1/creators/verified
  - [x] GET /api/v1/creators/spotlight

---

## Infrastructure & Background Jobs Summary

### Background Services (10 total):
1. `BackgroundBattleProcessor` — Processes queued battles (every 5s)
2. `WeeklyModifierRotationJob` — Rotates environmental modifiers
3. `DailyChallengeGenerationJob` — Generates daily challenges
4. `StrategyDecayJob` — Decays strategy marketplace ratings
5. `GuildBossSpawnJob` — Weekly boss spawns
6. `GuildInviteExpiryJob` — Cleans expired guild invites
7. `NotificationCleanupJob` — Cleans expired notifications (every 6h)
8. `AdminAlertJob` — System health alerts (every 1h)
9. `GuildWarMatchingJob` — Guild war matching + finalization (every 6h)
10. `TournamentProcessingJob` — Tournament creation + match processing (every 1h)

### Database Entities (50+ tables):
Core: Player, Unit, Ability, Team, Battle
Guilds: Guild, GuildMembership, GuildBoss, GuildBossAttempt, GuildInvite, GuildChatMessage, GuildStrategy
Engagement: DailyChallenge, EnvironmentalModifier, Strategy, StrategyRating, UnitMastery, Achievement, PlayerAchievement, BattleReplay, PlayerTitle
Monetization: Subscription, ApiKey, LootDrop, CosmeticItem, PlayerCosmetic
Phase 5 (5.1-5.11): Season, PlayerSeasonRank, Referral, RivalAssignment, BattlePass, PlayerBattlePass, GuildWar, GuildWarContribution, Tournament, TournamentEntry, TournamentMatch
Phase 5 (5.12-5.18): ActivityFeedEntry, CurriculumModule, StudentEnrollment, DiscordLink, DiscordWebhook, ContentCreator
System: Notification, PlayerActivity, ApiKeyUsageLog, AdminAuditLog, AdminAlert, SubscriptionEvent

### BattleService Integration Chain (13 dependencies):
After each battle resolves, the following hooks execute (in order):
1. Rating calculation (Elo)
2. Progression rewards (gold, XP, level-ups) — includes PP 1.5x XP multiplier
3. Season rating update (ranked only)
4. Achievement checks
5. Loot drops (both players) — enhanced for Premium Plus
6. Rival check (bonus gold for rival wins)
7. Battle pass XP (100 win / 25 loss)
8. Guild war contribution (ranked wins)
9. Notifications (both players)
10. Revenge alert (to loser)
11. Rank change alerts (both players, on 100-point threshold crossings)
12. Activity feed logging (battle stats + activity entries for both players)

---

## Remaining Work (Phase 4 Gaps & Polish)

### API Key Usage Tracking (Not Yet Wired)

- [ ] Wire ApiKeyUsageLog into API key authentication flow
- [ ] Admin dashboard: show API key usage stats per player
- [ ] Alert: new IP detected for API key → ApiKeyNewIp notification

### Admin Dashboard Enhancements

- [ ] Integrate AdminAuditLog viewer page
- [ ] Integrate AdminAlerts with acknowledge button
- [ ] Integrate PlayerActivity data into overview DAU/WAU/MAU
- [ ] Integrate SubscriptionEvents into player detail view
- [ ] Add notification count/stats to admin technical page

### Additional Notification Sources ✅ COMPLETE

- [x] Modifier rotation → NewModifierActive notification (all players notified)
- [x] Daily challenge generation → DailyChallengesAvailable notification
- [x] Strategy rated → StrategyRated notification to creator
- [x] Strategy download milestone → StrategyDownloadMilestone (10/50/100/250/500/1000)
- [x] Guild treasury upgrade → GuildTreasuryUpgrade notification (all guild members)
- [x] Guild strategy published → GuildStrategyPublished notification (all guild members)
- [x] Rating milestone → RatingMilestone (crossing 500/1000/1500/2000/2500/3000 thresholds)

---

## Phase 6: Education Mode Enhancements ✅ COMPLETE

> Gaps identified by mapping the [Wisconsin CS Standards curriculum](https://learnedgeek.com/Blog/Post/rest-api-lesson-plan-wisconsin-standards) to existing features.
> See `docs/EDUCATION_STANDARDS_ALIGNMENT.md` for full standards mapping.

### 6.1 Class-Scoped Leaderboard ✅ COMPLETE

- [x] `GET /api/v1/education/modules/{moduleId}/leaderboard` — enrolled students ranked by rating/wins
- [x] Query enrolled students via `StudentEnrollment`, join with `Player` stats, sort by rating desc
- [x] Response: `[{ rank, username, rating, wins, losses, winRate, lessonsCompleted }]`
- [x] Only accessible to enrolled students and module instructor

### 6.2 Batch Practice Endpoint ✅ COMPLETE

- [x] `POST /api/v1/ai/batch-practice` — run N practice battles server-side
- [x] Request: `{ teamId, opponentId?, count (max 200) }`
- [x] Response: `{ totalBattles, wins, losses, winRate, avgTurns, opponentName }`
- [x] Reuses `AiOpponentService` strategy engine in a loop, aggregate results
- [x] Does not award gold/XP (simulation only, no economy impact)

### 6.3 Class-Scoped Tournament ✅ COMPLETE

- [x] `POST /api/v1/education/modules/{moduleId}/tournament` — instructor creates class tournament
- [x] Only enrolled students can register (enrollment check in `TournamentService.EnterTournamentAsync`)
- [x] Extend `Tournament` model with optional `ModuleId` column + EF migration
- [x] Class tournament bracket visible at `GET /api/v1/tournament/bracket/{tournamentId}`
- [x] Configurable entry fee (default 0 for classroom use)

### 6.4 Endpoint-Linked Challenge Assignments ✅ COMPLETE

- [x] Extended lesson schema with optional `verificationEndpoint` and `verificationMethod` fields
- [x] `CreateLessonRequest` and `LessonDto` both include verification fields
- [x] Instructors can set per-lesson verification endpoints when creating modules
- [x] Verification fields surfaced in `GetModuleDetailAsync` response

### 6.5 Classroom Isolation (Deferred)

- [ ] Decision deferred — need instructor feedback on whether global pool is acceptable

### 6.6 Student Unenroll ✅ COMPLETE

- [x] `DELETE /api/v1/education/enroll/{moduleId}` — student unenrolls from module
- [x] Decrements module enrolled count
- [x] Removes student enrollment record

---

## Phase 7: Launch Preparation ✅ PARTIAL

- [x] Performance: Response caching on public/read-heavy endpoints (leaderboard 30s, SDK 1h, AI opponents 1h, game status 60s)
- [x] Security headers: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy
- [x] Error handling: GlobalExceptionMiddleware + JSON error responses for API routes
- [x] Documentation polish: IMPLEMENTATION_TASK_LIST.md, MANUAL_TEST.md fully updated
- [ ] Load testing (10K+ concurrent users) — requires production environment
- [ ] Error monitoring (Sentry or similar) — infrastructure decision
- [ ] Backup/recovery procedures — infrastructure decision
- [x] Marketing materials: HN post, Education page, About page, Landing page
- [x] Support channels: Contact form, Discord links in nav

---

## Pre-Launch Features ✅ COMPLETE

### Password Reset ✅ COMPLETE

- [x] Player model: PasswordResetToken, PasswordResetExpiresAt fields
- [x] IAuthService: RequestPasswordResetAsync, ResetPasswordAsync
- [x] AuthService: URL-safe base64 token, 1-hour expiry, no email leak
- [x] IEmailTemplateService: PasswordResetEmail template
- [x] IEmailService: SendPasswordResetEmailAsync
- [x] ForgotPassword page (email input, always shows "check inbox")
- [x] ResetPassword page (token from URL, new password + confirm)
- [x] Login page: "Forgot password?" link, success message handling

### Terms of Service ✅ COMPLETE

- [x] Terms page (static, Privacy.cshtml pattern)
- [x] Footer: Terms of Service link in Legal section
- [x] Register page: references both Terms and Privacy Policy

### Account Deletion (GDPR) ✅ COMPLETE

- [x] Player model: IsDeleted, DeletedAt fields
- [x] IAuthService: DeleteAccountAsync (password verification)
- [x] AuthService: soft delete, PII anonymization, confirmation email
- [x] IEmailTemplateService: AccountDeletionEmail template
- [x] IEmailService: SendAccountDeletionEmailAsync
- [x] Settings page: Danger Zone card with password confirm + JS dialog
- [x] Login: rejects deleted accounts

### Cookie Consent Banner ✅ COMPLETE

- [x] _Layout.cshtml: fixed-bottom banner with "Got it" button
- [x] Sets cookie_consent cookie (365-day, SameSite=Lax)
- [x] Only shown when cookie not set
- [x] Links to Privacy Policy

### Email Verification ✅ COMPLETE

- [x] Player model: EmailConfirmed, EmailConfirmationToken fields
- [x] IAuthService: SendVerificationEmailAsync, VerifyEmailAsync
- [x] AuthService: token generation, verification, auto-send on registration
- [x] IEmailTemplateService: VerificationEmail template
- [x] IEmailService: SendVerificationEmailAsync
- [x] VerifyEmail page (success/error display)
- [x] Dashboard: verification banner with "Resend Link" button

### Public Leaderboard ✅ COMPLETE

- [x] Leaderboard page (no auth required)
- [x] Top 50 non-bot non-deleted players by rating
- [x] RatingTierHelper tier badges
- [x] Top 3 gold/silver/bronze icons
- [x] Navbar: "Leaderboard" link (public)

### Favicon ✅ COMPLETE

- [x] SVG favicon (primary blue circle + white crossed swords)
- [x] _Layout.cshtml: `<link rel="icon" type="image/svg+xml">`

### About Page ✅ COMPLETE

- [x] About page (static, Privacy.cshtml pattern)
- [x] Content: mission, how it works, fair play, open to all, CTAs
- [x] Footer: About link in Resources section

### Advertising Landing Page ✅ COMPLETE

- [x] _LandingLayout.cshtml (minimal: logo, no nav/footer)
- [x] Landing page with social proof (real player count, battle count)
- [x] UTM param passthrough to Register CTA
- [x] noindex/nofollow meta tag
- [x] Selling points grid + hero section

### Documentation ✅ COMPLETE

- [x] MANUAL_TEST.md: test cases for all pre-launch features (sections 22-29)
- [x] LAUNCH-CHECKLIST.md: production launch checklist
- [x] IMPLEMENTATION_TASK_LIST.md: pre-launch features section

---

## Build Status

- **Build**: 0 errors, 0 warnings
- **Tests**: 619 passing (607 unit/integration + 12 Playwright), 0 failing, 0 skipped
- **API Endpoints**: 100+ across 28 tagged controllers
- **Background Jobs**: 10 hosted services
- **OpenAPI Tags**: 28 (Auth, Player, Team, Battle, Leaderboard, Strategy Marketplace, Guild, Guild Boss, Challenges, Mastery, Modifiers, Replays, AI Practice, Ranked Seasons, Loot, Referral, Unit Customization, Rival, Battle Pass, Guild Wars, Tournament, Cosmetics, Premium Plus, Activity Feed, Education, SDK, Discord, Creators)
- **Wisconsin CS Standards**: 15/15 covered (see `docs/EDUCATION_STANDARDS_ALIGNMENT.md`)

---

*Last updated: February 2026*
