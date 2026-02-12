using ApiCombatGame.Models.Domain;
using Xunit;

namespace ApiCombatGame.Tests.UnitTests;

public class LoginStreakTests
{
    private static readonly int[] StreakRewards = [25, 25, 50, 50, 75, 75, 200];

    /// <summary>
    /// Replicates the ProcessLoginStreak logic from DashboardPageModel so we can
    /// unit-test the algorithm in isolation without needing the full PageModel
    /// dependencies (DbContext, IConfiguration, IPlayerProgressionService, ILogger).
    ///
    /// The <paramref name="today"/> parameter replaces DateTime.UtcNow.Date so
    /// tests can exercise date-boundary behaviour deterministically.
    /// </summary>
    private static (bool Updated, bool TodayClaimed, int RewardAmount) ProcessLoginStreak(
        Player player, DateTime today)
    {
        // Already claimed today
        if (player.LastLoginRewardDate.HasValue && player.LastLoginRewardDate.Value.Date == today)
            return (false, true, StreakRewards[Math.Min(player.LoginStreak - 1, 6)]);

        bool isConsecutive = player.LastLoginRewardDate.HasValue
            && player.LastLoginRewardDate.Value.Date == today.AddDays(-1);

        if (isConsecutive && player.LoginStreak < 7)
        {
            player.LoginStreak++;
        }
        else
        {
            // Missed a day, first visit, or completed day 7 cycle
            player.LoginStreak = 1;
        }

        var rewardIndex = Math.Min(player.LoginStreak - 1, 6);
        var reward = StreakRewards[rewardIndex];

        player.Currency += reward;
        player.LastLoginRewardDate = today;
        player.LastLoginRewardClaimedAt = today; // Simplified; production code uses DateTime.UtcNow

        return (true, true, reward);
    }

    /// <summary>Helper to create a Player with optional login-streak state.</summary>
    private static Player CreatePlayer(
        int loginStreak = 0,
        DateTime? lastLoginRewardDate = null,
        int currency = 0)
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            Username = "testplayer",
            Email = "testplayer@test.com",
            PasswordHash = "hashed",
            LoginStreak = loginStreak,
            LastLoginRewardDate = lastLoginRewardDate,
            LastLoginRewardClaimedAt = null,
            Currency = currency
        };
    }

    // ---------------------------------------------------------------
    // 1. First-ever login (null LastLoginRewardDate)
    // ---------------------------------------------------------------

    [Fact]
    public void FirstEverLogin_SetsStreakToOne_AndAwards25Gold()
    {
        var player = CreatePlayer();
        var today = new DateTime(2025, 6, 15);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.True(todayClaimed);
        Assert.Equal(25, reward);
        Assert.Equal(1, player.LoginStreak);
        Assert.Equal(25, player.Currency);
        Assert.Equal(today, player.LastLoginRewardDate);
        Assert.NotNull(player.LastLoginRewardClaimedAt);
    }

    // ---------------------------------------------------------------
    // 2. Consecutive day login (yesterday) increments streak
    // ---------------------------------------------------------------

    [Fact]
    public void ConsecutiveDay_FromStreak1_IncrementsTo2_AndAwards25Gold()
    {
        var today = new DateTime(2025, 6, 16);
        var player = CreatePlayer(
            loginStreak: 1,
            lastLoginRewardDate: today.AddDays(-1),
            currency: 100);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.True(todayClaimed);
        Assert.Equal(25, reward);              // StreakRewards[1] = 25
        Assert.Equal(2, player.LoginStreak);
        Assert.Equal(125, player.Currency);    // 100 + 25
    }

    // ---------------------------------------------------------------
    // 3. Day 3 consecutive -> 50g reward
    // ---------------------------------------------------------------

    [Fact]
    public void Day3Consecutive_Awards50Gold()
    {
        var today = new DateTime(2025, 6, 17);
        var player = CreatePlayer(
            loginStreak: 2,
            lastLoginRewardDate: today.AddDays(-1),
            currency: 200);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.Equal(50, reward);              // StreakRewards[2] = 50
        Assert.Equal(3, player.LoginStreak);
        Assert.Equal(250, player.Currency);    // 200 + 50
    }

    // ---------------------------------------------------------------
    // 4. Day 7 consecutive -> 200g reward
    // ---------------------------------------------------------------

    [Fact]
    public void Day7Consecutive_Awards200Gold()
    {
        var today = new DateTime(2025, 6, 21);
        var player = CreatePlayer(
            loginStreak: 6,
            lastLoginRewardDate: today.AddDays(-1),
            currency: 500);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.Equal(200, reward);             // StreakRewards[6] = 200
        Assert.Equal(7, player.LoginStreak);
        Assert.Equal(700, player.Currency);    // 500 + 200
    }

    // ---------------------------------------------------------------
    // 5. Day after completing 7-day cycle -> resets to 1 (25g)
    // ---------------------------------------------------------------

    [Fact]
    public void DayAfterFullCycle_ResetsToStreak1_AndAwards25Gold()
    {
        var today = new DateTime(2025, 6, 22);
        var player = CreatePlayer(
            loginStreak: 7,
            lastLoginRewardDate: today.AddDays(-1),
            currency: 1000);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.Equal(25, reward);              // Reset: StreakRewards[0] = 25
        Assert.Equal(1, player.LoginStreak);   // Streak resets
        Assert.Equal(1025, player.Currency);   // 1000 + 25
    }

    // ---------------------------------------------------------------
    // 6. Missed a day (last login 2 days ago) -> resets to 1
    // ---------------------------------------------------------------

    [Fact]
    public void MissedDay_ResetsStreakTo1_AndAwards25Gold()
    {
        var today = new DateTime(2025, 6, 20);
        var player = CreatePlayer(
            loginStreak: 5,
            lastLoginRewardDate: today.AddDays(-2), // skipped yesterday
            currency: 300);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.Equal(25, reward);              // Reset: StreakRewards[0] = 25
        Assert.Equal(1, player.LoginStreak);
        Assert.Equal(325, player.Currency);    // 300 + 25
    }

    // ---------------------------------------------------------------
    // 7. Same-day second visit -> no duplicate reward (idempotent)
    // ---------------------------------------------------------------

    [Fact]
    public void SameDay_SecondVisit_NoDuplicateReward()
    {
        var today = new DateTime(2025, 6, 15);
        var player = CreatePlayer(
            loginStreak: 3,
            lastLoginRewardDate: today, // already claimed today
            currency: 500);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.False(updated);                 // No DB update needed
        Assert.True(todayClaimed);             // UI still shows as claimed
        Assert.Equal(50, reward);              // StreakRewards[2] = 50 (day 3 display value)
        Assert.Equal(3, player.LoginStreak);   // Unchanged
        Assert.Equal(500, player.Currency);    // Unchanged -- no double reward
    }

    // ---------------------------------------------------------------
    // 8. Currency correctly accumulated across a full 7-day cycle
    // ---------------------------------------------------------------

    [Fact]
    public void FullSevenDayCycle_AccumulatesCorrectTotalCurrency()
    {
        var startDate = new DateTime(2025, 7, 1);
        var player = CreatePlayer(currency: 0);

        int totalAwarded = 0;
        for (int day = 0; day < 7; day++)
        {
            var today = startDate.AddDays(day);
            var (updated, _, reward) = ProcessLoginStreak(player, today);

            Assert.True(updated);
            Assert.Equal(StreakRewards[day], reward);
            totalAwarded += reward;
        }

        // 25 + 25 + 50 + 50 + 75 + 75 + 200 = 500
        Assert.Equal(500, totalAwarded);
        Assert.Equal(500, player.Currency);
        Assert.Equal(7, player.LoginStreak);
    }

    // ---------------------------------------------------------------
    // 9. LastLoginRewardDate and LastLoginRewardClaimedAt updated
    // ---------------------------------------------------------------

    [Fact]
    public void NewLogin_UpdatesLastLoginRewardDate_AndClaimedAt()
    {
        var today = new DateTime(2025, 8, 10);
        var player = CreatePlayer(
            loginStreak: 1,
            lastLoginRewardDate: today.AddDays(-1));

        ProcessLoginStreak(player, today);

        Assert.Equal(today, player.LastLoginRewardDate);
        Assert.NotNull(player.LastLoginRewardClaimedAt);
        Assert.Equal(today, player.LastLoginRewardClaimedAt!.Value);
    }

    // ---------------------------------------------------------------
    // 10. Same-day visit does NOT overwrite timestamps
    // ---------------------------------------------------------------

    [Fact]
    public void SameDayVisit_DoesNotOverwriteTimestamps()
    {
        var today = new DateTime(2025, 8, 10);
        var originalClaimedAt = today.AddHours(-6);
        var player = CreatePlayer(
            loginStreak: 2,
            lastLoginRewardDate: today,
            currency: 100);
        player.LastLoginRewardClaimedAt = originalClaimedAt;

        ProcessLoginStreak(player, today);

        // Timestamps should remain unchanged
        Assert.Equal(today, player.LastLoginRewardDate);
        Assert.Equal(originalClaimedAt, player.LastLoginRewardClaimedAt);
    }

    // ---------------------------------------------------------------
    // 11. Missed multiple days still resets to 1
    // ---------------------------------------------------------------

    [Fact]
    public void MissedMultipleDays_StillResetsToStreak1()
    {
        var today = new DateTime(2025, 9, 1);
        var player = CreatePlayer(
            loginStreak: 7,
            lastLoginRewardDate: today.AddDays(-30), // a month gap
            currency: 0);

        var (updated, _, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.Equal(1, player.LoginStreak);
        Assert.Equal(25, reward);
        Assert.Equal(25, player.Currency);
    }

    // ---------------------------------------------------------------
    // 12. Day 4 consecutive -> 50g reward (StreakRewards[3])
    // ---------------------------------------------------------------

    [Fact]
    public void Day4Consecutive_Awards50Gold()
    {
        var today = new DateTime(2025, 6, 18);
        var player = CreatePlayer(
            loginStreak: 3,
            lastLoginRewardDate: today.AddDays(-1),
            currency: 0);

        var (_, _, reward) = ProcessLoginStreak(player, today);

        Assert.Equal(50, reward);              // StreakRewards[3] = 50
        Assert.Equal(4, player.LoginStreak);
    }

    // ---------------------------------------------------------------
    // 13. Day 5 consecutive -> 75g reward (StreakRewards[4])
    // ---------------------------------------------------------------

    [Fact]
    public void Day5Consecutive_Awards75Gold()
    {
        var today = new DateTime(2025, 6, 19);
        var player = CreatePlayer(
            loginStreak: 4,
            lastLoginRewardDate: today.AddDays(-1),
            currency: 0);

        var (_, _, reward) = ProcessLoginStreak(player, today);

        Assert.Equal(75, reward);              // StreakRewards[4] = 75
        Assert.Equal(5, player.LoginStreak);
    }

    // ---------------------------------------------------------------
    // 14. Day 6 consecutive -> 75g reward (StreakRewards[5])
    // ---------------------------------------------------------------

    [Fact]
    public void Day6Consecutive_Awards75Gold()
    {
        var today = new DateTime(2025, 6, 20);
        var player = CreatePlayer(
            loginStreak: 5,
            lastLoginRewardDate: today.AddDays(-1),
            currency: 0);

        var (_, _, reward) = ProcessLoginStreak(player, today);

        Assert.Equal(75, reward);              // StreakRewards[5] = 75
        Assert.Equal(6, player.LoginStreak);
    }

    // ---------------------------------------------------------------
    // 15. Two full consecutive cycles accumulate correctly
    // ---------------------------------------------------------------

    [Fact]
    public void TwoFullCycles_AccumulatesCorrectly()
    {
        var startDate = new DateTime(2025, 7, 1);
        var player = CreatePlayer(currency: 0);

        // First 7-day cycle
        for (int day = 0; day < 7; day++)
        {
            ProcessLoginStreak(player, startDate.AddDays(day));
        }

        Assert.Equal(7, player.LoginStreak);
        Assert.Equal(500, player.Currency);    // 25+25+50+50+75+75+200

        // Day 8 resets the cycle
        ProcessLoginStreak(player, startDate.AddDays(7));

        Assert.Equal(1, player.LoginStreak);
        Assert.Equal(525, player.Currency);    // 500 + 25

        // Continue second cycle through day 14
        for (int day = 8; day < 14; day++)
        {
            ProcessLoginStreak(player, startDate.AddDays(day));
        }

        Assert.Equal(7, player.LoginStreak);
        Assert.Equal(1000, player.Currency);   // 525 + 25+50+50+75+75+200 = 1000
    }

    // ---------------------------------------------------------------
    // 16. Same-day visit returns correct reward amount for display
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(1, 25)]
    [InlineData(2, 25)]
    [InlineData(3, 50)]
    [InlineData(4, 50)]
    [InlineData(5, 75)]
    [InlineData(6, 75)]
    [InlineData(7, 200)]
    public void SameDayVisit_ReturnsCorrectRewardForDisplay(int streak, int expectedReward)
    {
        var today = new DateTime(2025, 6, 15);
        var player = CreatePlayer(
            loginStreak: streak,
            lastLoginRewardDate: today);

        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.False(updated);
        Assert.True(todayClaimed);
        Assert.Equal(expectedReward, reward);
    }

    // ---------------------------------------------------------------
    // 17. Streak does not exceed 7 even with consecutive logins
    // ---------------------------------------------------------------

    [Fact]
    public void StreakCapsAt7_ThenResets()
    {
        var startDate = new DateTime(2025, 7, 1);
        var player = CreatePlayer(currency: 0);

        // Play through 8 consecutive days
        for (int day = 0; day < 8; day++)
        {
            ProcessLoginStreak(player, startDate.AddDays(day));
        }

        // On day 8 the streak must have reset to 1 (not become 8)
        Assert.Equal(1, player.LoginStreak);
    }

    // ---------------------------------------------------------------
    // 18. Claiming after exactly midnight boundary is considered
    //     a new day and awards a new reward
    // ---------------------------------------------------------------

    [Fact]
    public void MidnightBoundary_NewDayIsConsecutive()
    {
        var yesterday = new DateTime(2025, 6, 15);
        var today = new DateTime(2025, 6, 16); // exactly the next calendar day

        var player = CreatePlayer(
            loginStreak: 1,
            lastLoginRewardDate: yesterday,
            currency: 100);

        var (updated, _, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.Equal(2, player.LoginStreak);   // Consecutive
        Assert.Equal(25, reward);
        Assert.Equal(125, player.Currency);
    }

    // ---------------------------------------------------------------
    // 19. Break in streak mid-cycle then rebuild
    // ---------------------------------------------------------------

    [Fact]
    public void BreakMidCycle_ThenRebuild_StartsFromDay1()
    {
        var player = CreatePlayer(currency: 0);

        // Build streak to day 4
        var day1 = new DateTime(2025, 7, 1);
        ProcessLoginStreak(player, day1);
        ProcessLoginStreak(player, day1.AddDays(1));
        ProcessLoginStreak(player, day1.AddDays(2));
        ProcessLoginStreak(player, day1.AddDays(3));

        Assert.Equal(4, player.LoginStreak);
        Assert.Equal(150, player.Currency);    // 25+25+50+50

        // Skip a day (day 5 is missed), login on day 6
        ProcessLoginStreak(player, day1.AddDays(5));

        Assert.Equal(1, player.LoginStreak);   // Reset
        Assert.Equal(175, player.Currency);    // 150 + 25

        // Continue consecutive from the reset
        ProcessLoginStreak(player, day1.AddDays(6));

        Assert.Equal(2, player.LoginStreak);
        Assert.Equal(200, player.Currency);    // 175 + 25
    }

    // ---------------------------------------------------------------
    // 20. Player with zero login streak and null dates is handled
    //     identically to first-ever login
    // ---------------------------------------------------------------

    [Fact]
    public void ZeroStreak_NullDates_TreatedAsFirstLogin()
    {
        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = "brand_new",
            Email = "new@test.com",
            PasswordHash = "hashed",
            LoginStreak = 0,
            LastLoginRewardDate = null,
            LastLoginRewardClaimedAt = null,
            Currency = 0
        };

        var today = new DateTime(2025, 1, 1);
        var (updated, todayClaimed, reward) = ProcessLoginStreak(player, today);

        Assert.True(updated);
        Assert.True(todayClaimed);
        Assert.Equal(1, player.LoginStreak);
        Assert.Equal(25, reward);
        Assert.Equal(25, player.Currency);
        Assert.Equal(today, player.LastLoginRewardDate);
    }
}
