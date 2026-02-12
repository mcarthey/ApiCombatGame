# API Combat Game - Implementation Task List

**Tracking document for all phases from ENGAGEMENT-MONETIZATION-STRATEGY.md**

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

---

## Phase 4.5: Integration & Polish ✅ COMPLETE

- [x] Tier-based rate limits in RateLimitingMiddleware (Free: 60, Premium: 120, Premium+: 300 req/min)
- [x] Add CurrentTier claim to JWT in AuthService
- [x] Player profile: guild info (name, tag, role, contribution), achievement count
- [x] GuildController API doc annotations (ApiCategoryMeta, ApiGameTip, ApiExample, ApiDifficulty, ApiPrerequisite)
- [x] GuildInviteExpiryJob (background job — daily cleanup of expired invites)

---

## Custom API Documentation ✅ COMPLETE

- [x] Custom C# attributes (ApiGameTip, ApiExample, ApiPrerequisite, ApiDifficulty, ApiCategoryMeta)
- [x] GameMetadataOperationFilter (x-game-* OpenAPI extensions)
- [x] TagDescriptionsDocumentFilter with icons/colors
- [x] EnumSchemaFilter (string names instead of integers)
- [x] ApiDocs.cshtml.cs PageModel (reads OpenAPI spec via ISwaggerProvider)
- [x] ApiDocs.cshtml (hero, quick start, auth, TOC sidebar, endpoint groups, models)
- [x] Partial views: _EndpointGroup, _Endpoint, _CodeBlock, _SchemaTable
- [x] Gradient banner with stats bar
- [x] Active nav link styling
- [x] Dark mode support
- [x] All 11 controllers annotated with custom attributes
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

- [x] NotificationType (24 values: BattleCompleted, WinStreakMilestone, RatingMilestone, GuildInvited, GuildInviteResponse, GuildKicked, GuildPromoted, GuildBossSpawned, GuildBossDefeated, GuildChatMention, GuildStrategyPublished, GuildTreasuryUpgrade, LevelUp, AchievementUnlocked, DailyChallengesAvailable, MasteryRankUp, StrategyRated, StrategyDownloadMilestone, NewModifierActive, AdminAnnouncement, ApiKeyNewIp, PasswordChanged, TierChanged, AdminActionOnAccount)
- [x] NotificationCategory (Battle, Guild, Progression, Marketplace, System, Security)
- [x] AlertSeverity (Info, Warning, Critical)

### GameDbContext ✅ COMPLETE

- [x] 6 new DbSets (Notifications, PlayerActivities, ApiKeyUsageLogs, AdminAuditLogs, AdminAlerts, SubscriptionEvents)
- [x] Entity configurations with indexes, relationships, cascade rules
- [x] Player → Notifications relationship

### Notification Service ✅ COMPLETE

- [x] INotificationService interface (Send, SendToGuild, GetUnreadCount, GetNotifications, MarkRead, MarkAllRead, DeleteExpired, Preferences)
- [x] NotificationService implementation
  - [x] Preference-aware sending (checks player preferences before creating)
  - [x] Batch guild notifications (loads all member preferences in single query)
  - [x] System/Security categories always on (cannot opt out)
  - [x] Expiry: 30-day auto-expire, 7-day read cleanup
- [x] NotificationPreferences class (Battle, Guild, Progression, Marketplace toggles)
- [x] Player.NotificationPreferencesJson field (JSON column for preferences)

### Notification Hooks (7 services) ✅ COMPLETE

- [x] BattleService → BattleCompleted (won/lost), WinStreakMilestone (every 5 wins)
- [x] GuildService → GuildInvited, GuildInviteResponse (new member joined), GuildKicked, GuildPromoted
- [x] GuildBossService → GuildBossSpawned, GuildBossDefeated (killing blow)
- [x] AchievementService → AchievementUnlocked
- [x] GuildChatService → GuildChatMention (@username detection via regex)
- [x] MasteryService → MasteryRankUp
- [x] PlayerProgressionService → LevelUp

### Admin Audit Logging ✅ COMPLETE

- [x] AdminAnalyticsService: AuditLogAsync helper persists every admin action
- [x] Admin actions now require adminPlayerId parameter
- [x] All 4 admin actions audited (ToggleAdmin, AdjustCurrency, SetTier, ResetPassword)
- [x] Security notifications sent to affected players (AdminActionOnAccount, TierChanged, PasswordChanged)
- [x] SubscriptionEvent records created on tier changes

### Player Activity Tracking ✅ COMPLETE

- [x] PlayerActivityMiddleware (auto-tracks daily request counts per authenticated user)
- [x] Unique index on (PlayerId, ActivityDate) for accurate DAU/WAU/MAU
- [x] Registered in pipeline after UseAuthorization

### API Endpoints ✅ COMPLETE

- [x] GET /api/v1/player/notifications/count (unread count)
- [x] GET /api/v1/player/notifications (paginated, unreadOnly filter)
- [x] POST /api/v1/player/notifications/{id}/read (mark single read)
- [x] POST /api/v1/player/notifications/read-all (mark all read)
- [x] GET /api/v1/player/notifications/preferences (get preferences)
- [x] PUT /api/v1/player/notifications/preferences (update preferences)

### Web UI ✅ COMPLETE

- [x] Bell icon with red badge in _Layout.cshtml (polls /api/v1/player/notifications/count every 60s)
- [x] Account/Notifications.cshtml (full page: category badges, read/unread filter, pagination, mark-all-read)
- [x] Account/Settings.cshtml updated with notification preferences section (toggle checkboxes per category, System/Security always on)

### Background Jobs ✅ COMPLETE

- [x] NotificationCleanupJob (every 6 hours: deletes expired + old read notifications)
- [x] AdminAlertJob (every hour: checks battle queue health, growth milestones, boss expirations)

---

## Phase 5: Premium+ Features ⏳ DEFERRED

- [ ] Lua scripting engine
- [ ] Sandboxed execution environment
- [ ] Script validation and storage
- [ ] WebSocket server
- [ ] Real-time battle updates
- [ ] Guild notifications via WebSocket
- [ ] Batch operations API (queue 100 battles)
- [ ] Higher rate limits for Premium+ (5x standard)
- [ ] Advanced analytics API
- [ ] Historical trend data
- [ ] Discord webhook integration

---

## Phase 6: Launch Preparation ⏳ DEFERRED

- [ ] Performance optimization
- [ ] Security audit
- [ ] Load testing (10K+ concurrent users)
- [ ] Error monitoring (Sentry or similar)
- [ ] Backup/recovery procedures
- [ ] Documentation polish
- [ ] Marketing materials
- [ ] Support channels (Discord, email)

---

## Remaining Work (Phase 4 Gaps & Polish)

### API Key Usage Tracking (Not Yet Wired)

- [ ] Wire ApiKeyUsageLog into API key authentication flow (log IP, user agent, endpoint per request)
- [ ] Admin dashboard: show API key usage stats per player
- [ ] Alert: new IP detected for API key → send ApiKeyNewIp notification

### Admin Dashboard Enhancements

- [ ] Integrate AdminAuditLog into admin dashboard (audit log viewer page)
- [ ] Integrate AdminAlerts into admin dashboard (alert feed with acknowledge button)
- [ ] Integrate PlayerActivity data into overview DAU/WAU/MAU (replace LastLoginAt with actual activity)
- [ ] Integrate SubscriptionEvents into player detail view (subscription history)
- [ ] Add notification count/stats to admin technical page

### Additional Notification Sources (Not Yet Wired)

- [ ] Modifier rotation → NewModifierActive notification to all players (in WeeklyModifierRotationJob)
- [ ] Daily challenge generation → DailyChallengesAvailable notification (in DailyChallengeGenerationJob)
- [ ] Strategy rated → StrategyRated notification to strategy creator (in StrategyMarketplaceService)
- [ ] Strategy download milestone → StrategyDownloadMilestone (in StrategyMarketplaceService)
- [ ] Guild treasury upgrade → GuildTreasuryUpgrade notification to guild (in GuildTreasuryService)
- [ ] Guild strategy published → GuildStrategyPublished notification (in GuildStrategyService)
- [ ] Rating milestone → RatingMilestone (e.g., reaching 1500, 2000 rating) (in BattleService)

### Engagement Features Not Yet Built

- [ ] Seasonal ladder resets (monthly soft reset: rating * 0.8 + 200)
- [ ] Interactive battle replay viewer UI (in API docs — visual turn-by-turn replay)
- [ ] Dashboard visualizations (Chart.js: win rate over time, unit usage pie chart, gold earned)
- [ ] Tiered endpoint visibility in docs (show/hide endpoints based on user tier)
- [ ] Battle simulation endpoint (Premium: run N hypothetical battles)
- [ ] Strategy versioning (Premium: save 10 versions of a strategy)

### Cosmetics System (Not Yet Built)

- [ ] Unit skins model + shop
- [ ] Battle themes model + shop
- [ ] Profile customization (borders, backgrounds, badges)
- [ ] Guild banner customization (treasury purchase)
- [ ] Premium-exclusive cosmetic titles

---

*Last updated: February 2026*
