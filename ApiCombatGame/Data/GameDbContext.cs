using ApiCombatGame.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    // Phase 1: Core
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Battle> Battles => Set<Battle>();
    public DbSet<Ability> Abilities => Set<Ability>();

    // Phase 2: Subscriptions & API Keys
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    // Phase 3: Engagement & Anti-Meta
    public DbSet<EnvironmentalModifier> EnvironmentalModifiers => Set<EnvironmentalModifier>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMembership> GuildMemberships => Set<GuildMembership>();
    public DbSet<GuildBoss> GuildBosses => Set<GuildBoss>();
    public DbSet<GuildBossAttempt> GuildBossAttempts => Set<GuildBossAttempt>();
    public DbSet<DailyChallenge> DailyChallenges => Set<DailyChallenge>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<StrategyRating> StrategyRatings => Set<StrategyRating>();
    public DbSet<UnitMastery> UnitMasteries => Set<UnitMastery>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<PlayerAchievement> PlayerAchievements => Set<PlayerAchievement>();
    public DbSet<BattleReplay> BattleReplays => Set<BattleReplay>();
    public DbSet<PlayerTitle> PlayerTitles => Set<PlayerTitle>();
    public DbSet<GuildInvite> GuildInvites => Set<GuildInvite>();
    public DbSet<GuildChatMessage> GuildChatMessages => Set<GuildChatMessage>();
    public DbSet<GuildStrategy> GuildStrategies => Set<GuildStrategy>();

    // Phase 5: Ranked Seasons
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<PlayerSeasonRank> PlayerSeasonRanks => Set<PlayerSeasonRank>();

    // Phase 5: Loot Drops
    public DbSet<LootDrop> LootDrops => Set<LootDrop>();

    // Phase 5: Referrals
    public DbSet<Referral> Referrals => Set<Referral>();

    // Phase 5: Rivals
    public DbSet<RivalAssignment> RivalAssignments => Set<RivalAssignment>();

    // Phase 5: Battle Pass
    public DbSet<BattlePass> BattlePasses => Set<BattlePass>();
    public DbSet<PlayerBattlePass> PlayerBattlePasses => Set<PlayerBattlePass>();

    // Phase 5: Guild Wars
    public DbSet<GuildWar> GuildWars => Set<GuildWar>();
    public DbSet<GuildWarContribution> GuildWarContributions => Set<GuildWarContribution>();

    // Phase 5: Cosmetics
    public DbSet<CosmeticItem> CosmeticItems => Set<CosmeticItem>();
    public DbSet<PlayerCosmetic> PlayerCosmetics => Set<PlayerCosmetic>();

    // Phase 5: Tournaments
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentEntry> TournamentEntries => Set<TournamentEntry>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();

    // Phase 5: Activity Feed
    public DbSet<ActivityFeedEntry> ActivityFeedEntries => Set<ActivityFeedEntry>();

    // Phase 5: Education
    public DbSet<CurriculumModule> CurriculumModules => Set<CurriculumModule>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();

    // Phase 5: Discord Integration
    public DbSet<DiscordLink> DiscordLinks => Set<DiscordLink>();
    public DbSet<DiscordWebhook> DiscordWebhooks => Set<DiscordWebhook>();

    // Phase 5: Content Creators
    public DbSet<ContentCreator> ContentCreators => Set<ContentCreator>();

    // Easter Egg Hunt
    public DbSet<EasterEgg> EasterEggs => Set<EasterEgg>();
    public DbSet<EggClaim> EggClaims => Set<EggClaim>();

    // Phase 4: Notifications & Logging
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PlayerActivity> PlayerActivities => Set<PlayerActivity>();
    public DbSet<ApiKeyUsageLog> ApiKeyUsageLogs => Set<ApiKeyUsageLog>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<AdminAlert> AdminAlerts => Set<AdminAlert>();
    public DbSet<SubscriptionEvent> SubscriptionEvents => Set<SubscriptionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Player ──
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasIndex(p => p.Username).IsUnique();
            entity.HasIndex(p => p.Email).IsUnique();

            entity.HasMany(p => p.Roster)
                .WithOne(u => u.Player)
                .HasForeignKey(u => u.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Teams)
                .WithOne(t => t.Player)
                .HasForeignKey(t => t.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Subscription)
                .WithOne(s => s.Player)
                .HasForeignKey<Subscription>(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.ApiKeys)
                .WithOne(k => k.Player)
                .HasForeignKey(k => k.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Phase 3 relationships
            entity.HasOne(p => p.ActiveTitle)
                .WithMany()
                .HasForeignKey(p => p.ActiveTitleId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(p => p.GuildMembership)
                .WithOne(m => m.Player)
                .HasForeignKey<GuildMembership>(m => m.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.DailyChallenges)
                .WithOne(c => c.Player)
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.CreatedStrategies)
                .WithOne(s => s.Creator)
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.UnitMasteries)
                .WithOne(m => m.Player)
                .HasForeignKey(m => m.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Achievements)
                .WithOne(a => a.Player)
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.BossAttempts)
                .WithOne(a => a.Player)
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Notifications)
                .WithOne(n => n.Player)
                .HasForeignKey(n => n.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Unit ──
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasMany(u => u.Abilities)
                .WithOne(a => a.Unit)
                .HasForeignKey(a => a.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Battle ──
        modelBuilder.Entity<Battle>(entity =>
        {
            entity.HasOne(b => b.Player1)
                .WithMany()
                .HasForeignKey(b => b.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Player2)
                .WithMany()
                .HasForeignKey(b => b.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Subscription (Phase 2) ──
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasIndex(s => s.StripeCustomerId);
            entity.HasIndex(s => s.StripeSubscriptionId);
            entity.Property(s => s.AmountUsd).HasColumnType("decimal(10,2)");
        });

        // ── ApiKey (Phase 2) ──
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(k => k.KeyHash);
            entity.HasIndex(k => new { k.PlayerId, k.IsActive });
        });

        // ── Guild ──
        modelBuilder.Entity<Guild>(entity =>
        {
            entity.HasIndex(g => g.Name).IsUnique();
            entity.HasIndex(g => g.Tag).IsUnique();

            entity.HasOne(g => g.Leader)
                .WithMany()
                .HasForeignKey(g => g.LeaderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(g => g.Members)
                .WithOne(m => m.Guild)
                .HasForeignKey(m => m.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.Bosses)
                .WithOne(b => b.Guild)
                .HasForeignKey(b => b.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.ChatMessages)
                .WithOne(m => m.Guild)
                .HasForeignKey(m => m.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(g => g.Strategies)
                .WithOne(s => s.Guild)
                .HasForeignKey(s => s.GuildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── GuildChatMessage ──
        modelBuilder.Entity<GuildChatMessage>(entity =>
        {
            entity.HasIndex(m => new { m.GuildId, m.CreatedAt });

            entity.HasOne(m => m.Player)
                .WithMany()
                .HasForeignKey(m => m.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── GuildStrategy ──
        modelBuilder.Entity<GuildStrategy>(entity =>
        {
            entity.HasIndex(s => new { s.GuildId, s.CreatedAt });

            entity.HasOne(s => s.Creator)
                .WithMany()
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── GuildMembership ──
        modelBuilder.Entity<GuildMembership>(entity =>
        {
            entity.HasIndex(m => new { m.GuildId, m.PlayerId }).IsUnique();
        });

        // ── GuildInvite ──
        modelBuilder.Entity<GuildInvite>(entity =>
        {
            entity.HasIndex(i => new { i.GuildId, i.InvitedPlayerId, i.Status });

            entity.HasOne(i => i.Guild)
                .WithMany()
                .HasForeignKey(i => i.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.InvitedPlayer)
                .WithMany()
                .HasForeignKey(i => i.InvitedPlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.InvitedBy)
                .WithMany()
                .HasForeignKey(i => i.InvitedByPlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── GuildBoss ──
        modelBuilder.Entity<GuildBoss>(entity =>
        {
            entity.HasMany(b => b.Attempts)
                .WithOne(a => a.GuildBoss)
                .HasForeignKey(a => a.GuildBossId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DailyChallenge ──
        modelBuilder.Entity<DailyChallenge>(entity =>
        {
            entity.HasIndex(c => new { c.PlayerId, c.ExpiresAt });
        });

        // ── Strategy ──
        modelBuilder.Entity<Strategy>(entity =>
        {
            entity.HasIndex(s => s.IsPublic);
            entity.HasIndex(s => s.DownloadCount);

            entity.HasMany(s => s.Ratings)
                .WithOne(r => r.Strategy)
                .HasForeignKey(r => r.StrategyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── StrategyRating ──
        modelBuilder.Entity<StrategyRating>(entity =>
        {
            entity.HasIndex(r => new { r.StrategyId, r.PlayerId }).IsUnique();
        });

        // ── UnitMastery ──
        modelBuilder.Entity<UnitMastery>(entity =>
        {
            entity.HasIndex(m => new { m.PlayerId, m.UnitId }).IsUnique();
        });

        // ── Achievement ──
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasIndex(a => a.Category);

            entity.HasMany(a => a.PlayerAchievements)
                .WithOne(pa => pa.Achievement)
                .HasForeignKey(pa => pa.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlayerAchievement ──
        modelBuilder.Entity<PlayerAchievement>(entity =>
        {
            entity.HasIndex(pa => new { pa.PlayerId, pa.AchievementId }).IsUnique();
        });

        // ── BattleReplay ──
        modelBuilder.Entity<BattleReplay>(entity =>
        {
            entity.HasIndex(r => r.ShareUrl).IsUnique();
            entity.HasIndex(r => r.BattleId).IsUnique();

            entity.HasOne(r => r.Battle)
                .WithMany()
                .HasForeignKey(r => r.BattleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── EnvironmentalModifier ──
        modelBuilder.Entity<EnvironmentalModifier>(entity =>
        {
            entity.HasIndex(m => new { m.IsActive, m.StartDate, m.EndDate });
        });

        // ── PlayerTitle ──
        modelBuilder.Entity<PlayerTitle>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });

        // ── Notification ──
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => new { n.PlayerId, n.IsRead, n.CreatedAt });
            entity.HasIndex(n => n.ExpiresAt);
        });

        // ── PlayerActivity ──
        modelBuilder.Entity<PlayerActivity>(entity =>
        {
            entity.HasIndex(a => new { a.PlayerId, a.ActivityDate }).IsUnique();
            entity.HasIndex(a => a.ActivityDate);

            entity.HasOne(a => a.Player)
                .WithMany()
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ApiKeyUsageLog ──
        modelBuilder.Entity<ApiKeyUsageLog>(entity =>
        {
            entity.HasIndex(l => new { l.ApiKeyId, l.Timestamp });
            entity.HasIndex(l => new { l.ApiKeyId, l.IpAddress });

            entity.HasOne(l => l.ApiKey)
                .WithMany()
                .HasForeignKey(l => l.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AdminAuditLog ──
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.HasIndex(l => l.CreatedAt);
            entity.HasIndex(l => l.AdminPlayerId);

            entity.HasOne(l => l.AdminPlayer)
                .WithMany()
                .HasForeignKey(l => l.AdminPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.TargetPlayer)
                .WithMany()
                .HasForeignKey(l => l.TargetPlayerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── AdminAlert ──
        modelBuilder.Entity<AdminAlert>(entity =>
        {
            entity.HasIndex(a => new { a.IsAcknowledged, a.CreatedAt });
            entity.HasIndex(a => a.Severity);
        });

        // ── SubscriptionEvent ──
        modelBuilder.Entity<SubscriptionEvent>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.CreatedAt });
            entity.Property(e => e.AmountUsd).HasColumnType("decimal(10,2)");

            entity.HasOne(e => e.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Season ──
        modelBuilder.Entity<Season>(entity =>
        {
            entity.HasIndex(s => s.IsActive);
            entity.HasIndex(s => s.SeasonNumber).IsUnique();

            entity.HasMany(s => s.PlayerRanks)
                .WithOne(r => r.Season)
                .HasForeignKey(r => r.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlayerSeasonRank ──
        modelBuilder.Entity<PlayerSeasonRank>(entity =>
        {
            entity.HasIndex(r => new { r.PlayerId, r.SeasonId }).IsUnique();
            entity.HasIndex(r => new { r.SeasonId, r.SeasonRating });

            entity.HasOne(r => r.Player)
                .WithMany()
                .HasForeignKey(r => r.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── LootDrop ──
        modelBuilder.Entity<LootDrop>(entity =>
        {
            entity.HasIndex(d => new { d.PlayerId, d.Claimed });
            entity.HasIndex(d => d.BattleId);

            entity.HasOne(d => d.Player)
                .WithMany()
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Referral ──
        modelBuilder.Entity<Referral>(entity =>
        {
            entity.HasIndex(r => r.Code).IsUnique();
            entity.HasIndex(r => r.ReferrerId);

            entity.HasOne(r => r.Referrer)
                .WithMany()
                .HasForeignKey(r => r.ReferrerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ReferredPlayer)
                .WithMany()
                .HasForeignKey(r => r.ReferredPlayerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── RivalAssignment ──
        modelBuilder.Entity<RivalAssignment>(entity =>
        {
            entity.HasIndex(r => new { r.PlayerId, r.ExpiresAt });

            entity.HasOne(r => r.Player)
                .WithMany()
                .HasForeignKey(r => r.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Rival)
                .WithMany()
                .HasForeignKey(r => r.RivalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BattlePass ──
        modelBuilder.Entity<BattlePass>(entity =>
        {
            entity.HasIndex(bp => bp.IsActive);

            entity.HasOne(bp => bp.Season)
                .WithMany()
                .HasForeignKey(bp => bp.SeasonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(bp => bp.PlayerProgress)
                .WithOne(pp => pp.BattlePass)
                .HasForeignKey(pp => pp.BattlePassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlayerBattlePass ──
        modelBuilder.Entity<PlayerBattlePass>(entity =>
        {
            entity.HasIndex(pp => new { pp.PlayerId, pp.BattlePassId }).IsUnique();

            entity.HasOne(pp => pp.Player)
                .WithMany()
                .HasForeignKey(pp => pp.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── GuildWar ──
        modelBuilder.Entity<GuildWar>(entity =>
        {
            entity.HasIndex(w => w.Status);

            entity.HasOne(w => w.Guild1)
                .WithMany()
                .HasForeignKey(w => w.Guild1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(w => w.Guild2)
                .WithMany()
                .HasForeignKey(w => w.Guild2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(w => w.Contributions)
                .WithOne(c => c.GuildWar)
                .HasForeignKey(c => c.GuildWarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── GuildWarContribution ──
        modelBuilder.Entity<GuildWarContribution>(entity =>
        {
            entity.HasIndex(c => new { c.GuildWarId, c.PlayerId }).IsUnique();

            entity.HasOne(c => c.Player)
                .WithMany()
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Tournament ──
        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasIndex(t => t.Status);

            entity.HasMany(t => t.Entries)
                .WithOne(e => e.Tournament)
                .HasForeignKey(e => e.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.Matches)
                .WithOne(m => m.Tournament)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TournamentEntry ──
        modelBuilder.Entity<TournamentEntry>(entity =>
        {
            entity.HasIndex(e => new { e.TournamentId, e.PlayerId }).IsUnique();

            entity.HasOne(e => e.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TournamentMatch ──
        modelBuilder.Entity<TournamentMatch>(entity =>
        {
            entity.HasIndex(m => new { m.TournamentId, m.Round, m.MatchNumber });
        });

        // ── ActivityFeedEntry ──
        modelBuilder.Entity<ActivityFeedEntry>(entity =>
        {
            entity.HasIndex(a => new { a.PlayerId, a.CreatedAt });
            entity.HasIndex(a => a.ActivityType);

            entity.HasOne(a => a.Player)
                .WithMany()
                .HasForeignKey(a => a.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CosmeticItem ──
        modelBuilder.Entity<CosmeticItem>(entity =>
        {
            entity.HasIndex(c => c.Category);
            entity.HasIndex(c => c.IsAvailable);
        });

        // ── PlayerCosmetic ──
        modelBuilder.Entity<PlayerCosmetic>(entity =>
        {
            entity.HasIndex(pc => new { pc.PlayerId, pc.CosmeticItemId }).IsUnique();

            entity.HasOne(pc => pc.Player)
                .WithMany()
                .HasForeignKey(pc => pc.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pc => pc.CosmeticItem)
                .WithMany()
                .HasForeignKey(pc => pc.CosmeticItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CurriculumModule ──
        modelBuilder.Entity<CurriculumModule>(entity =>
        {
            entity.HasIndex(m => m.JoinCode).IsUnique();
            entity.HasIndex(m => m.IsPublished);

            entity.HasOne(m => m.Instructor)
                .WithMany()
                .HasForeignKey(m => m.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── StudentEnrollment ──
        modelBuilder.Entity<StudentEnrollment>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.ModuleId }).IsUnique();

            entity.HasOne(e => e.Player)
                .WithMany()
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Module)
                .WithMany()
                .HasForeignKey(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DiscordLink ──
        modelBuilder.Entity<DiscordLink>(entity =>
        {
            entity.HasIndex(d => d.PlayerId).IsUnique();
            entity.HasIndex(d => d.DiscordUserId);

            entity.HasOne(d => d.Player)
                .WithMany()
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── DiscordWebhook ──
        modelBuilder.Entity<DiscordWebhook>(entity =>
        {
            entity.HasIndex(w => w.PlayerId);

            entity.HasOne(w => w.Player)
                .WithMany()
                .HasForeignKey(w => w.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ContentCreator ──
        modelBuilder.Entity<ContentCreator>(entity =>
        {
            entity.HasIndex(c => c.PlayerId).IsUnique();
            entity.HasIndex(c => c.IsVerified);

            entity.HasOne(c => c.Player)
                .WithMany()
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── EasterEgg ──
        modelBuilder.Entity<EasterEgg>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => new { e.PagePath, e.IsActive });
            entity.HasIndex(e => new { e.WeekStart, e.WeekEnd });
        });

        // ── EggClaim ──
        modelBuilder.Entity<EggClaim>(entity =>
        {
            entity.HasIndex(c => new { c.PlayerId, c.EasterEggId }).IsUnique();

            entity.HasOne(c => c.Player)
                .WithMany()
                .HasForeignKey(c => c.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.EasterEgg)
                .WithMany(e => e.Claims)
                .HasForeignKey(c => c.EasterEggId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
