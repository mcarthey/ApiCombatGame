using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using ApiCombatGame.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class GuildServiceTests
{
    private readonly Mock<INotificationService> _notificationsMock;
    private readonly NullLogger<GuildService> _logger;

    public GuildServiceTests()
    {
        _notificationsMock = new Mock<INotificationService>();
        _logger = NullLogger<GuildService>.Instance;
    }

    private GuildService CreateService(GameDbContext context)
    {
        return new GuildService(context, _notificationsMock.Object, _logger);
    }

    // ---------------------------------------------------------------
    // CreateGuildAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task CreateGuildAsync_PremiumPlayer_CreatesGuildAndMembership()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var service = CreateService(context);

        var guild = await service.CreateGuildAsync(player.Id, "Alpha Squad", "ALPHA", "First guild");

        Assert.NotNull(guild);
        Assert.Equal("Alpha Squad", guild.Name);
        Assert.Equal("ALPHA", guild.Tag);
        Assert.Equal("First guild", guild.Description);
        Assert.Equal(player.Id, guild.LeaderId);
        Assert.Equal(1, guild.Level);
        Assert.Equal(20, guild.MaxMembers);

        var membership = context.GuildMemberships.FirstOrDefault(m => m.PlayerId == player.Id);
        Assert.NotNull(membership);
        Assert.Equal(GuildRole.Leader, membership.Role);
        Assert.Equal(guild.Id, membership.GuildId);
    }

    [Fact]
    public async Task CreateGuildAsync_FreeTierPlayer_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "freebie", SubscriptionTier.Free);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateGuildAsync(player.Id, "Cheap Guild", "FREE", "No money"));

        Assert.Contains("Premium", ex.Message);
    }

    [Fact]
    public async Task CreateGuildAsync_PlayerAlreadyInGuild_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        TestDbContextFactory.CreateGuildWithLeader(context, player, "Existing Guild", "EXST");
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateGuildAsync(player.Id, "New Guild", "NEW", "Second guild attempt"));

        Assert.Contains("already in a guild", ex.Message);
    }

    [Fact]
    public async Task CreateGuildAsync_DuplicateName_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var leader1 = TestDbContextFactory.CreatePlayer(context, "leader1", SubscriptionTier.Premium);
        TestDbContextFactory.CreateGuildWithLeader(context, leader1, "Unique Name", "UNQ1");

        var leader2 = TestDbContextFactory.CreatePlayer(context, "leader2", SubscriptionTier.Premium);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateGuildAsync(leader2.Id, "Unique Name", "UNQ2", "Dup name"));

        Assert.Contains("name already exists", ex.Message);
    }

    [Fact]
    public async Task CreateGuildAsync_StoresTagUppercase()
    {
        using var context = TestDbContextFactory.Create();
        var player = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var service = CreateService(context);

        var guild = await service.CreateGuildAsync(player.Id, "Case Guild", "lower", "Lowercase tag");

        Assert.Equal("LOWER", guild.Tag);
    }

    // ---------------------------------------------------------------
    // DeleteGuildAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeleteGuildAsync_Leader_DeletesGuild()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        var service = CreateService(context);

        await service.DeleteGuildAsync(guild.Id, leader.Id);

        Assert.Null(await context.Guilds.FindAsync(guild.Id));
    }

    [Fact]
    public async Task DeleteGuildAsync_NonLeader_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var member = TestDbContextFactory.CreatePlayer(context, "member", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteGuildAsync(guild.Id, member.Id));

        Assert.Contains("leader", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------
    // InvitePlayerAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task InvitePlayerAsync_LeaderInvites_CreatesInviteAndNotifies()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var target = TestDbContextFactory.CreatePlayer(context, "target", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        var service = CreateService(context);

        var invite = await service.InvitePlayerAsync(guild.Id, leader.Id, "target");

        Assert.NotNull(invite);
        Assert.Equal(guild.Id, invite.GuildId);
        Assert.Equal(target.Id, invite.InvitedPlayerId);
        Assert.Equal(leader.Id, invite.InvitedByPlayerId);
        Assert.Equal(GuildInviteStatus.Pending, invite.Status);
        Assert.True(invite.ExpiresAt > DateTime.UtcNow.AddDays(6));

        _notificationsMock.Verify(
            n => n.SendAsync(target.Id, NotificationType.GuildInvited, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task InvitePlayerAsync_RegularMember_ThrowsNoPermission()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var member = TestDbContextFactory.CreatePlayer(context, "member", SubscriptionTier.Free);
        var target = TestDbContextFactory.CreatePlayer(context, "target", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member, GuildRole.Member);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InvitePlayerAsync(guild.Id, member.Id, "target"));

        Assert.Contains("permission", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvitePlayerAsync_TargetNotFound_ThrowsKeyNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.InvitePlayerAsync(guild.Id, leader.Id, "nonexistent"));

        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public async Task InvitePlayerAsync_TargetAlreadyInGuild_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var leader1 = TestDbContextFactory.CreatePlayer(context, "leader1", SubscriptionTier.Premium);
        var leader2 = TestDbContextFactory.CreatePlayer(context, "leader2", SubscriptionTier.Premium);
        var (guild1, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader1, "Guild One", "ONE");
        TestDbContextFactory.CreateGuildWithLeader(context, leader2, "Guild Two", "TWO");
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.InvitePlayerAsync(guild1.Id, leader1.Id, "leader2"));

        Assert.Contains("already in a guild", ex.Message);
    }

    // ---------------------------------------------------------------
    // AcceptInviteAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task AcceptInviteAsync_ValidInvite_CreatesMembershipAndAccepts()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var target = TestDbContextFactory.CreatePlayer(context, "target", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var invite = new GuildInvite
        {
            Id = Guid.NewGuid(),
            GuildId = guild.Id,
            InvitedPlayerId = target.Id,
            InvitedByPlayerId = leader.Id,
            Status = GuildInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.GuildInvites.Add(invite);
        context.SaveChanges();

        var service = CreateService(context);

        var result = await service.AcceptInviteAsync(invite.Id, target.Id);

        Assert.Equal(GuildInviteStatus.Accepted, result.Status);

        var membership = context.GuildMemberships.FirstOrDefault(m => m.PlayerId == target.Id);
        Assert.NotNull(membership);
        Assert.Equal(GuildRole.Member, membership.Role);
        Assert.Equal(guild.Id, membership.GuildId);
    }

    [Fact]
    public async Task AcceptInviteAsync_DeclinesOtherPendingInvites()
    {
        using var context = TestDbContextFactory.Create();
        var leader1 = TestDbContextFactory.CreatePlayer(context, "leader1", SubscriptionTier.Premium);
        var leader2 = TestDbContextFactory.CreatePlayer(context, "leader2", SubscriptionTier.Premium);
        var target = TestDbContextFactory.CreatePlayer(context, "target", SubscriptionTier.Free);

        var (guild1, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader1, "Guild One", "ONE");
        var (guild2, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader2, "Guild Two", "TWO");

        var invite1 = new GuildInvite
        {
            Id = Guid.NewGuid(),
            GuildId = guild1.Id,
            InvitedPlayerId = target.Id,
            InvitedByPlayerId = leader1.Id,
            Status = GuildInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        var invite2 = new GuildInvite
        {
            Id = Guid.NewGuid(),
            GuildId = guild2.Id,
            InvitedPlayerId = target.Id,
            InvitedByPlayerId = leader2.Id,
            Status = GuildInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.GuildInvites.AddRange(invite1, invite2);
        context.SaveChanges();

        var service = CreateService(context);

        await service.AcceptInviteAsync(invite1.Id, target.Id);

        var declinedInvite = await context.GuildInvites.FindAsync(invite2.Id);
        Assert.NotNull(declinedInvite);
        Assert.Equal(GuildInviteStatus.Declined, declinedInvite.Status);
    }

    // ---------------------------------------------------------------
    // DeclineInviteAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeclineInviteAsync_PendingInvite_MarksAsDeclined()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var target = TestDbContextFactory.CreatePlayer(context, "target", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);

        var invite = new GuildInvite
        {
            Id = Guid.NewGuid(),
            GuildId = guild.Id,
            InvitedPlayerId = target.Id,
            InvitedByPlayerId = leader.Id,
            Status = GuildInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        context.GuildInvites.Add(invite);
        context.SaveChanges();

        var service = CreateService(context);

        var result = await service.DeclineInviteAsync(invite.Id, target.Id);

        Assert.Equal(GuildInviteStatus.Declined, result.Status);
    }

    // ---------------------------------------------------------------
    // KickMemberAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task KickMemberAsync_LeaderKicksMember_RemovesMembershipAndNotifies()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var member = TestDbContextFactory.CreatePlayer(context, "member", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member);
        var service = CreateService(context);

        await service.KickMemberAsync(guild.Id, leader.Id, member.Id);

        var membership = context.GuildMemberships.FirstOrDefault(m => m.PlayerId == member.Id);
        Assert.Null(membership);

        _notificationsMock.Verify(
            n => n.SendAsync(member.Id, NotificationType.GuildKicked, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task KickMemberAsync_RegularMember_ThrowsNoPermission()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var member1 = TestDbContextFactory.CreatePlayer(context, "member1", SubscriptionTier.Free);
        var member2 = TestDbContextFactory.CreatePlayer(context, "member2", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member1, GuildRole.Member);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member2, GuildRole.Member);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.KickMemberAsync(guild.Id, member1.Id, member2.Id));

        Assert.Contains("permission", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task KickMemberAsync_EqualRank_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var officer1 = TestDbContextFactory.CreatePlayer(context, "officer1", SubscriptionTier.Free);
        var officer2 = TestDbContextFactory.CreatePlayer(context, "officer2", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, officer1, GuildRole.Officer);
        TestDbContextFactory.AddGuildMember(context, guild.Id, officer2, GuildRole.Officer);
        var service = CreateService(context);

        // Officers don't have KickMembers permission, so this throws permission error first.
        // Test that even if we had an Officer trying to kick another Officer it fails.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.KickMemberAsync(guild.Id, officer1.Id, officer2.Id));

        Assert.Contains("permission", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------
    // PromoteMemberAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task PromoteMemberAsync_ToOfficer_UpdatesRole()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var member = TestDbContextFactory.CreatePlayer(context, "member", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member, GuildRole.Member);
        var service = CreateService(context);

        await service.PromoteMemberAsync(guild.Id, leader.Id, member.Id, GuildRole.Officer);

        var membership = context.GuildMemberships.First(m => m.PlayerId == member.Id);
        Assert.Equal(GuildRole.Officer, membership.Role);
    }

    [Fact]
    public async Task PromoteMemberAsync_ToLeader_TransfersLeadership()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var officer = TestDbContextFactory.CreatePlayer(context, "officer", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, officer, GuildRole.Officer);
        var service = CreateService(context);

        await service.PromoteMemberAsync(guild.Id, leader.Id, officer.Id, GuildRole.Leader);

        var updatedGuild = await context.Guilds.FindAsync(guild.Id);
        Assert.NotNull(updatedGuild);
        Assert.Equal(officer.Id, updatedGuild.LeaderId);

        var newLeaderMembership = context.GuildMemberships.First(m => m.PlayerId == officer.Id);
        Assert.Equal(GuildRole.Leader, newLeaderMembership.Role);

        var oldLeaderMembership = context.GuildMemberships.First(m => m.PlayerId == leader.Id);
        Assert.Equal(GuildRole.Officer, oldLeaderMembership.Role);
    }

    [Fact]
    public async Task PromoteMemberAsync_NonLeader_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var officer = TestDbContextFactory.CreatePlayer(context, "officer", SubscriptionTier.Free);
        var member = TestDbContextFactory.CreatePlayer(context, "member", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, officer, GuildRole.Officer);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member, GuildRole.Member);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PromoteMemberAsync(guild.Id, officer.Id, member.Id, GuildRole.Officer));

        Assert.Contains("leader", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------
    // LeaveGuildAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task LeaveGuildAsync_Member_RemovesMembership()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var member = TestDbContextFactory.CreatePlayer(context, "member", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, member);
        var service = CreateService(context);

        await service.LeaveGuildAsync(member.Id);

        var membership = context.GuildMemberships.FirstOrDefault(m => m.PlayerId == member.Id);
        Assert.Null(membership);
    }

    [Fact]
    public async Task LeaveGuildAsync_Leader_ThrowsInvalidOperation()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        TestDbContextFactory.CreateGuildWithLeader(context, leader);
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LeaveGuildAsync(leader.Id));

        Assert.Contains("leader", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------
    // HasPermission - matrix validation
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(GuildRole.Leader, GuildPermission.ViewInfo, true)]
    [InlineData(GuildRole.Leader, GuildPermission.ViewMembers, true)]
    [InlineData(GuildRole.Leader, GuildPermission.InviteMembers, true)]
    [InlineData(GuildRole.Leader, GuildPermission.KickMembers, true)]
    [InlineData(GuildRole.Leader, GuildPermission.PromoteMembers, true)]
    [InlineData(GuildRole.Leader, GuildPermission.SpendTreasury, true)]
    [InlineData(GuildRole.Leader, GuildPermission.DeleteGuild, true)]
    [InlineData(GuildRole.Leader, GuildPermission.PublishStrategy, true)]
    [InlineData(GuildRole.Leader, GuildPermission.DeleteAnyStrategy, true)]
    [InlineData(GuildRole.Leader, GuildPermission.PostChat, true)]
    [InlineData(GuildRole.Leader, GuildPermission.AttackBoss, true)]
    [InlineData(GuildRole.Officer, GuildPermission.ViewInfo, true)]
    [InlineData(GuildRole.Officer, GuildPermission.ViewMembers, true)]
    [InlineData(GuildRole.Officer, GuildPermission.InviteMembers, true)]
    [InlineData(GuildRole.Officer, GuildPermission.KickMembers, false)]
    [InlineData(GuildRole.Officer, GuildPermission.PromoteMembers, false)]
    [InlineData(GuildRole.Officer, GuildPermission.SpendTreasury, false)]
    [InlineData(GuildRole.Officer, GuildPermission.DeleteGuild, false)]
    [InlineData(GuildRole.Officer, GuildPermission.PublishStrategy, true)]
    [InlineData(GuildRole.Officer, GuildPermission.DeleteAnyStrategy, false)]
    [InlineData(GuildRole.Officer, GuildPermission.PostChat, true)]
    [InlineData(GuildRole.Officer, GuildPermission.AttackBoss, true)]
    [InlineData(GuildRole.Member, GuildPermission.ViewInfo, true)]
    [InlineData(GuildRole.Member, GuildPermission.ViewMembers, true)]
    [InlineData(GuildRole.Member, GuildPermission.InviteMembers, false)]
    [InlineData(GuildRole.Member, GuildPermission.KickMembers, false)]
    [InlineData(GuildRole.Member, GuildPermission.PromoteMembers, false)]
    [InlineData(GuildRole.Member, GuildPermission.SpendTreasury, false)]
    [InlineData(GuildRole.Member, GuildPermission.DeleteGuild, false)]
    [InlineData(GuildRole.Member, GuildPermission.PublishStrategy, false)]
    [InlineData(GuildRole.Member, GuildPermission.DeleteAnyStrategy, false)]
    [InlineData(GuildRole.Member, GuildPermission.PostChat, true)]
    [InlineData(GuildRole.Member, GuildPermission.AttackBoss, true)]
    public void HasPermission_ReturnsExpectedResult(GuildRole role, GuildPermission permission, bool expected)
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var result = service.HasPermission(role, permission);

        Assert.Equal(expected, result);
    }

    // ---------------------------------------------------------------
    // InvitePlayerAsync - Officer can invite (has InviteMembers)
    // ---------------------------------------------------------------

    [Fact]
    public async Task InvitePlayerAsync_OfficerInvites_Succeeds()
    {
        using var context = TestDbContextFactory.Create();
        var leader = TestDbContextFactory.CreatePlayer(context, "leader", SubscriptionTier.Premium);
        var officer = TestDbContextFactory.CreatePlayer(context, "officer", SubscriptionTier.Free);
        var target = TestDbContextFactory.CreatePlayer(context, "target", SubscriptionTier.Free);
        var (guild, _) = TestDbContextFactory.CreateGuildWithLeader(context, leader);
        TestDbContextFactory.AddGuildMember(context, guild.Id, officer, GuildRole.Officer);
        var service = CreateService(context);

        var invite = await service.InvitePlayerAsync(guild.Id, officer.Id, "target");

        Assert.NotNull(invite);
        Assert.Equal(GuildInviteStatus.Pending, invite.Status);
        Assert.Equal(officer.Id, invite.InvitedByPlayerId);
    }
}
