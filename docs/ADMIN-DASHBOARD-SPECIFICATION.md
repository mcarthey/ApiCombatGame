# API Combat Game - Admin Dashboard & Analytics Specification

**Version:** 1.0  
**Date:** February 11, 2026  
**Author:** Mark (Learned Geek Consulting)  
**Purpose:** Comprehensive admin tooling for game monitoring, balance, and growth

---

## Table of Contents

1. [Why Admin Analytics Matter](#why-admin-analytics-matter)
2. [Dashboard Architecture](#dashboard-architecture)
3. [Core Dashboards](#core-dashboards)
4. [Analytics Endpoints](#analytics-endpoints)
5. [Database Views & Queries](#database-views--queries)
6. [Alerts & Monitoring](#alerts--monitoring)
7. [Export & Reporting](#export--reporting)
8. [Implementation Roadmap](#implementation-roadmap)

---

## Why Admin Analytics Matter

### The Problem Without Analytics

**You're flying blind:**
- ❌ Don't know which units are overpowered
- ❌ Can't see player churn patterns
- ❌ Miss when meta becomes stale
- ❌ Don't understand conversion funnel
- ❌ Can't measure feature impact
- ❌ No early warning for problems

**Result:** You make changes based on guesses, not data

### The Power With Analytics

**You have vision:**
- ✅ See meta health in real-time
- ✅ Detect balance issues before players complain
- ✅ Track engagement metrics daily
- ✅ Understand what drives conversions
- ✅ Measure feature adoption
- ✅ Get alerts when something breaks

**Result:** Data-driven decisions, faster iteration, healthier game

---

## Dashboard Architecture

### Access Control

```
Admin Role System:
├── Super Admin (You)
│   ├── Access: ALL dashboards
│   ├── Actions: All admin operations
│   └── Data: Full historical access
│
├── Analytics Viewer (Future: Investors/Partners)
│   ├── Access: Read-only dashboards
│   ├── Actions: Export reports only
│   └── Data: Aggregated metrics only
│
└── Support Staff (Future: If you hire)
    ├── Access: Player support dashboard
    ├── Actions: View player details, issue refunds
    └── Data: Individual player data only
```

### Authentication

```csharp
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // Only accessible with admin role
}

// In Database:
public class Player
{
    public bool IsAdmin { get; set; } = false;
    public AdminRole AdminRole { get; set; } = AdminRole.None;
}

public enum AdminRole
{
    None,
    Support,
    Analytics,
    SuperAdmin
}
```

### Navigation Structure

```
/admin
├── /overview              (Landing page, KPIs)
├── /players               (Player analytics)
├── /engagement            (Retention, DAU/MAU)
├── /meta                  (Balance, unit usage)
├── /revenue               (Monetization metrics)
├── /guilds                (Guild analytics)
├── /content               (Season planning)
├── /technical             (API health, errors)
├── /players/{id}          (Individual player deep dive)
└── /tools                 (Admin actions)
```

---

## Core Dashboards

### 1. Overview Dashboard (`/admin/overview`)

**Purpose:** At-a-glance health check

**KPIs (Top Row - Big Numbers):**
```
┌────────────────────────────────────────────────────────────┐
│  DAU          WAU          MAU          MRR         Churn   │
│  1,234        4,567        12,345       $6,400      3.2%    │
│  ↑ +12%       ↑ +8%        ↑ +15%       ↑ +22%      ↓ -1.1% │
└────────────────────────────────────────────────────────────┘
```

**Trends (Charts):**
```
User Growth (Last 30 Days)
┌────────────────────────────────────────┐
│ 15K ┤                           ╭───   │
│     │                    ╭──────╯      │
│ 10K ┤             ╭──────╯             │
│     │      ╭──────╯                    │
│  5K ┤──────╯                           │
│     └────────────────────────────────  │
│      Jan 12      Jan 26      Feb 9     │
└────────────────────────────────────────┘

Revenue (Last 30 Days)
┌────────────────────────────────────────┐
│ $8K ┤                           ╭───   │
│     │                    ╭──────╯      │
│ $6K ┤             ╭──────╯             │
│     │      ╭──────╯                    │
│ $4K ┤──────╯                           │
│     └────────────────────────────────  │
│      Jan 12      Jan 26      Feb 9     │
└────────────────────────────────────────┘
```

**Health Indicators:**
```
System Health                        Status
├── API Response Time                ✅ 124ms (target: <200ms)
├── Error Rate                       ✅ 0.12% (target: <1%)
├── Database Performance             ⚠️  342ms (target: <300ms)
├── Meta Diversity                   ✅ 0.73 (target: >0.70)
├── 7-Day Retention                  ✅ 42% (target: >40%)
└── Premium Conversion               ✅ 16.2% (target: >15%)
```

**Recent Alerts:**
```
Today, 2:34 PM    ⚠️  Fire Mage win rate spiked to 68%
Yesterday, 4:12 PM ✅ 100 new signups in last hour (launch spike)
2 days ago        ⚠️  Premium churn increased to 4.1%
```

---

### 2. Player Analytics (`/admin/players`)

**Purpose:** Understand player behavior and segments

**Player Segments:**
```
┌──────────────────────────────────────────────────────────┐
│ Total Players: 12,345                                    │
├──────────────────────────────────────────────────────────┤
│ Free Tier:           9,876 (80%)                         │
│ Premium:             1,976 (16%)                         │
│ Premium+:              493 (4%)                          │
├──────────────────────────────────────────────────────────┤
│ Active (7 days):     7,407 (60%)                         │
│ Churned (30+ days):  2,469 (20%)                         │
│ New (< 7 days):      1,234 (10%)                         │
└──────────────────────────────────────────────────────────┘
```

**Cohort Analysis:**
```
Registration Cohort Retention

Cohort      Day 1   Day 7   Day 14  Day 30
Jan Week 1   100%    45%     38%     28%
Jan Week 2   100%    48%     41%     32%
Jan Week 3   100%    52%     44%     35%  ← Improving!
Jan Week 4   100%    51%     43%     N/A
Feb Week 1   100%    54%     N/A     N/A  ← Best yet!
```

**Player Lifetime Value (LTV):**
```
By Acquisition Source:

Organic Search:        $12.34 LTV, 18% conversion
Product Hunt:          $18.45 LTV, 22% conversion
Reddit:                $8.23 LTV, 12% conversion
University Referral:   $25.67 LTV, 28% conversion ← Best!
Direct:                $15.34 LTV, 19% conversion
```

**Top Players (Leaderboard):**
```
Rank  Username           Rating  Battles  Win Rate  Tier      Last Active
1     OptimizeThis       2,456   1,234    68%       Premium+  2 hours ago
2     CodeNinja          2,398   987      65%       Premium   5 mins ago
3     AlgoMaster         2,301   1,567    62%       Premium+  Today, 10:23
4     BugSlayer          2,234   876      64%       Premium   Yesterday
5     StackOverflow42    2,189   1,045    61%       Free      3 hours ago
...
```

**Engagement Distribution:**
```
Battles Per Week (All Players)

  40% ┤█
      │█
  30% ┤█
      │█ █
  20% ┤█ █
      │█ █ █
  10% ┤█ █ █ █
      │█ █ █ █ █ █
   0% ┼─────────────────
      0 10 20 30 40 50+ battles/week
      
Most common: 15-20 battles/week (32% of players)
```

**Player Search:**
```
┌────────────────────────────────────────────┐
│ Search: [Username, Email, Player ID]       │
│                                    [Search]│
└────────────────────────────────────────────┘

Recent Searches:
├── AwesomeDev42 (view details)
├── premium cancellations today (23 players)
└── new signups last hour (45 players)
```

---

### 3. Engagement Dashboard (`/admin/engagement`)

**Purpose:** Monitor stickiness and retention

**DAU/WAU/MAU Trends:**
```
┌────────────────────────────────────────────────────┐
│ Daily Active Users (Last 30 Days)                  │
│                                                     │
│ 2K ┤                                 ╭─────────    │
│    │                          ╭──────╯             │
│ 1.5K┤                  ╭───────╯                   │
│    │           ╭───────╯                           │
│ 1K ┤───────────╯                                   │
│    └────────────────────────────────────────────   │
│     Jan 12           Jan 26            Feb 9       │
│                                                     │
│ DAU/MAU Ratio: 0.32 (Stickiness: Good)            │
└────────────────────────────────────────────────────┘
```

**Session Depth (API Calls Per Session):**
```
Average API Calls Per User Per Day

    │
100 ┤              Premium+
 80 ┤         Premium
 60 ┤    Free
 40 ┤
 20 ┤
  0 ┼─────────────────────────────
     Week 1  Week 2  Week 3  Week 4

Premium+ users: 3.2x more API calls than Free
```

**Feature Adoption:**
```
Feature                      Adoption Rate  Avg Usage (Weekly)
Guild Membership             62%            N/A
Guild Raid Participation     38%            2.3 attempts
Strategy Marketplace         24%            1.4 downloads
Battle Simulation (Premium)  71%            45 simulations
Scripting Engine (Premium+)  82%            12 script runs
```

**Retention Curves:**
```
7-Day Retention by Cohort

    │
100%┤█
    │ █
 75%┤  █
    │   █
 50%┤    █
    │     █
 25%┤       █
    │         █───────────  ← Stabilizes ~25%
  0%┼─────────────────────
     D0  D1  D3  D7  D14  D30

Target: 40% at D7 ✅ (Currently: 42%)
Target: 25% at D30 ✅ (Currently: 28%)
```

**Churn Analysis:**
```
Why Players Leave (Exit Surveys + Behavior):

Reason                           % of Churned
├── "Ran out of things to do"    32%  ← Need more content!
├── "Too competitive"            18%
├── "Not enough time"            15%
├── "Meta became stale"          12%  ← Weekly modifiers help
├── "Price too high"             8%
├── "Technical issues"           5%
└── "Other/Unknown"              10%

Action Items:
- Add more progression systems (achievements, mastery)
- Consider casual mode queue
- Improve new player onboarding
```

---

### 4. Meta & Balance Dashboard (`/admin/meta`)

**Purpose:** Detect balance issues early

**Unit Win Rates:**
```
Unit Usage & Performance (Last 7 Days)

Unit Name         Usage    Win Rate  Expected  Δ      Status
Fire Mage         18.4%    68.2%     50%       +18.2% 🔴 OP!
Bronze Warrior    23.1%    52.1%     50%       +2.1%  ✅ Good
Ice Ranger        14.2%    48.3%     50%       -1.7%  ✅ Good
Divine Healer     19.8%    54.7%     50%       +4.7%  ⚠️  Watch
Shadow Assassin   8.7%     39.2%     50%       -10.8% 🔴 UP!
Tank Knight       15.8%    51.3%     50%       +1.3%  ✅ Good

Legend:
✅ Balanced (45-55% win rate)
⚠️  Watch (55-60% or 40-45%)
🔴 Action Needed (>60% or <40%)
```

**Meta Diversity Score:**
```
Strategy Diversity: 0.73  ✅ Healthy

┌────────────────────────────────────────┐
│ Top 10 Strategies Account For:        │
│ 47% of all battles                     │
│                                        │
│ Target: <70%  ✅ Current: 47%         │
│                                        │
│ ████████████████████░░░░░░░░░░░  47%  │
└────────────────────────────────────────┘

Interpretation:
- High diversity = healthy meta
- Players experimenting with many strategies
- No single dominant strategy
```

**Team Composition Analysis:**
```
Most Common Team Comps (Last 1000 Battles):

Composition                          Usage   Win Rate
3 Warriors + 2 Healers               8.2%    56.3%
2 Mages + 2 Rangers + 1 Healer       7.1%    62.1%  ← Strong!
4 Rangers + 1 Tank                   6.4%    48.7%
3 Mages + 2 Tanks                    5.9%    59.4%  ← Strong!
5 Warriors (all-in)                  4.2%    41.2%  ← Weak

Action: Mage + Tank combo may need slight nerf
```

**Environmental Modifier Impact:**
```
Current Modifier: "Arcane Disruption"
- Mage abilities cost 2x mana
- Physical attacks +20% damage

Impact Analysis:
┌────────────────────────────────────────┐
│ Mage Usage:  18.4% → 12.1%  ✅ Working!│
│ Warrior Usage: 23.1% → 28.7% ✅ Good   │
│ Meta Shift: Successful                 │
└────────────────────────────────────────┘

Next Week: "Heavy Armor"
- All units +50% defense
- Healer effectiveness 2x

Expected Impact: Longer battles, healer meta
```

**Emerging Strategies:**
```
New Strategies Gaining Traction (Last 7 Days):

Strategy Name         Creator      Uses   Win Rate  Growth
"Burst Ranger Meta"   OptimizeThis 89     64%       +42%/day 🔥
"Tank Wall v2"        CodeNinja    67     58%       +28%/day
"Speed Blitz"         AlgoMaster   45     61%       +35%/day 🔥

Monitor: "Burst Ranger Meta" - Could become OP
```

**Balance Change History:**
```
Recent Changes & Impact

Date       Change                     Impact
Feb 8      Fire Mage damage -10%      Win rate: 72% → 68% (still high)
Feb 1      Shadow Assassin speed +15% Win rate: 35% → 39% (still low)
Jan 25     Healer mana cost -20%      Win rate: 48% → 55% (overcorrection?)

Next Balance Patch (Planned: Feb 15):
- Fire Mage damage -5% (additional)
- Shadow Assassin damage +10%
- Healer mana cost +10% (revert partial)
```

---

### 5. Revenue Dashboard (`/admin/revenue`)

**Purpose:** Track monetization and optimize conversion

**Revenue Overview:**
```
┌────────────────────────────────────────────────────────┐
│ MRR: $6,400  (↑ +22% vs last month)                   │
│ ARR: $76,800                                           │
│ ARPU: $0.52                                            │
│ ARPPU: $6.48 (Paying users only)                      │
└────────────────────────────────────────────────────────┘
```

**Revenue Breakdown:**
```
By Tier (This Month)

Premium:      1,976 users × $5  = $9,880  (62%)
Premium+:       493 users × $12 = $5,916  (38%)
──────────────────────────────────────────────
Total MRR:                        $15,796

Note: Showing Feb actual, not normalized monthly
```

**Conversion Funnel:**
```
Free → Premium Conversion

Total Free Users:     9,876
├── Viewed Pricing:   3,951  (40%)
├── Started Checkout:   988  (10% of free, 25% of viewers)
├── Completed Payment:  658  (16.6% of viewers)
└── Active After 30d:   592  (90% retention) ✅

Dropoff Analysis:
- 60% never view pricing → Need better CTAs
- 75% of pricing viewers don't checkout → Price resistance?
- 33% abandon checkout → Friction in payment flow?
- 10% cancel within 30 days → Unmet expectations
```

**Cohort LTV:**
```
Lifetime Value by Cohort

Cohort         Users  Avg LTV  Premium %  Churn Rate
Jan Week 1     234    $18.23   18%        4.2%
Jan Week 2     456    $19.45   19%        3.8%
Jan Week 3     523    $21.34   22%        3.1%  ← Best!
Jan Week 4     489    $20.12   21%        3.5%
Feb Week 1     612    $22.56*  24%*       N/A   ← Trending up!

* Projected based on first 7 days
```

**Churn Analysis:**
```
Premium Churn Rate: 3.2%/month  ✅ Target: <5%

Churn Reasons (Exit Survey):
├── "Not using enough"           42%
├── "Too expensive"              23%
├── "Found alternative"          12%
├── "Technical issues"           8%
├── "Lost interest in game"      7%
└── "Other"                      8%

Retention Strategies:
- Email: "You haven't battled in 5 days" → 18% re-engage
- Offer: Pause subscription (3 months) → 23% accept, 67% return
- Downgrade: Premium+ → Premium → 31% accept vs cancel
```

**Revenue by Feature:**
```
What Drives Conversions?

Feature Used               Conversion Rate
Guild Creation             68%  🔥 Highest!
Simulation Endpoint        54%
Battle > 50 times          47%
Strategy Marketplace       42%
Joined Guild               38%
Basic Gameplay Only        8%   ← Baseline

Insight: Guild features drive conversion!
```

**Pricing Experiments:**
```
A/B Test: Premium Pricing

Variant A: $5/month  (Current)
- Conversion: 16.2%
- MRR/User: $0.81

Variant B: $7/month  (Test)
- Conversion: 11.4%  (-30%)
- MRR/User: $0.80   (-1.2%)

Result: Stay with $5 pricing ✅

Next Test: Premium+ $10 vs $12 vs $15
```

---

### 6. Guild Analytics (`/admin/guilds`)

**Purpose:** Monitor social features and collaboration

**Guild Overview:**
```
Total Guilds: 234

Active Guilds (raid in last 7 days): 187 (80%)
Average Members per Guild: 14.2
Average Raids Completed: 3.2/week
```

**Top Guilds:**
```
Rank  Guild Name           Members  Raids/Week  Avg Rating  Premium %
1     The Optimizers       48/50    8.3         1,856       94%  🔥
2     Code Warriors        42/50    7.1         1,734       88%
3     Algorithm Masters    38/50    6.8         1,689       82%
4     Bug Slayers          35/50    5.4         1,567       76%
5     Stack Overflow       29/30    4.9         1,501       71%

Insight: Top guilds have 70%+ premium rate!
```

**Guild Engagement:**
```
Guild Activity Distribution

Members    Guilds   Avg Raids/Week  Premium %
1-5        34       1.2             23%
6-10       67       2.4             42%
11-20      89       4.1             67%
21-30      32       5.8             81%
31-50      12       7.2             89%  ← Most engaged!

Insight: Larger guilds = higher engagement + conversion
```

**Raid Boss Completion Rates:**
```
Boss           Spawned  Defeated  Completion %  Avg Time
Fire Dragon    187      134       72%           4.2 days
Ice Giant      187      156       83%           3.8 days  ← Too easy?
Shadow Demon   187      89        48%           6.1 days  ← Too hard?
Thunder Lord   187      145       78%           4.5 days

Action: Shadow Demon may need HP reduction
```

**Strategy Sharing:**
```
Most Downloaded Guild Strategies

Strategy Name          Guild              Downloads  Rating
"Tank Meta v4"         The Optimizers     234        4.8/5
"All-in Burst"         Code Warriors      189        4.6/5
"Defensive Wall"       Algorithm Masters  156        4.7/5

Insight: Top strategies from top guilds
```

**Guild Revenue Impact:**
```
Players in Guilds vs Solo

Metric                  In Guild    Solo      Δ
Premium Conversion      42%         8%        +34% 🔥
Avg LTV                 $28.34      $6.12     +$22
Churn Rate              2.1%        6.8%      -4.7%
Battles/Week            23.4        12.1      +11.3

Insight: Guilds drive retention AND revenue!
```

---

### 7. Content Planning Dashboard (`/admin/content`)

**Purpose:** Plan seasons, modifiers, and new content

**Current Season Status:**
```
Season 1: "The Awakening"
Start: Feb 1, 2026
End: Feb 28, 2026
Progress: 38% complete (11/29 days)

Leaderboard Top 10:
1. OptimizeThis - 2,456 pts
2. CodeNinja - 2,398 pts
3. AlgoMaster - 2,301 pts
...

Participation: 8,234 players (67% of active)
```

**Weekly Modifier Schedule:**
```
Week  Start Date  Modifier            Status      Impact
1     Feb 1       Normal              Complete    Baseline
2     Feb 8       Arcane Disruption   Active      Meta shifted ✅
3     Feb 15      Heavy Armor         Scheduled   Preview live
4     Feb 22      Speed Demon         Scheduled   -

Next Season (March):
- Theme: "The Reckoning"
- New Units: 5 (3 common, 2 rare)
- New Boss: "Void Lord"
- New Modifier: "Chaos Storm"
```

**Content Pipeline:**
```
In Development:
├── Unit: "Frost Mage" (80% complete)
├── Boss: "Void Lord" (60% complete)
├── Modifier: "Chaos Storm" (40% complete)
├── Achievement: "Perfectionist" (90% complete)
└── Map: "Arena of Champions" (20% complete)

Ready to Ship (Next Week):
├── Unit: "Lightning Ranger"
├── Achievement: "Master Tactician"
└── Title: "Season 1 Champion"
```

**Player Feedback Summary:**
```
Top Requested Features (from surveys + Discord):

Feature                       Votes  Difficulty  Priority
Custom game modes             234    High        Medium
More unit variety             189    Medium      High  🎯
Guild vs Guild tournaments    167    High        High  🎯
Replay analysis tools         134    Medium      Medium
More cosmetics               112    Low         Low
Mobile app                    98     Very High   Low

Next Quarter Focus:
1. More units (5-10 new)
2. Guild tournaments
3. Replay tools
```

**Seasonal Goals & Tracking:**
```
Season 1 Goals:

Goal                          Target    Current   Status
Registered Users              10,000    12,345    ✅ +23%
DAU                          1,000     1,234     ✅ +23%
Premium Conversions          1,000     1,976     ✅ +98%
MRR                          $5,000    $6,400    ✅ +28%
7-Day Retention              40%       42%       ✅
Guild Participation          50%       62%       ✅ +24%

All goals exceeded! 🎉
```

---

### 8. Technical Dashboard (`/admin/technical`)

**Purpose:** Monitor system health and API performance

**API Performance:**
```
┌────────────────────────────────────────────────┐
│ Avg Response Time: 124ms  ✅ Target: <200ms   │
│ 99th Percentile: 456ms    ⚠️  Target: <500ms  │
│ Error Rate: 0.12%         ✅ Target: <1%      │
│ Uptime: 99.87%            ✅ Target: >99.5%   │
└────────────────────────────────────────────────┘
```

**API Endpoint Performance:**
```
Slowest Endpoints (Last 24 Hours)

Endpoint                     Avg    p95    p99    Calls
POST /battle/queue           234ms  456ms  789ms  12,345
GET  /player/roster          189ms  345ms  612ms  23,456
GET  /guild/raid/current     312ms  567ms  892ms  3,456  ← Slow!
POST /guild/raid/attack      267ms  489ms  734ms  4,567
GET  /leaderboard            145ms  289ms  445ms  8,901

Action: Optimize /guild/raid/current query
```

**Database Performance:**
```
Query Performance

Slowest Queries:
1. Guild raid leaderboard: 456ms avg  ← Add index!
2. Player battle history: 234ms avg
3. Global leaderboard: 189ms avg

Database Size: 2.3 GB
Growth Rate: +45 MB/day

Estimated capacity: 6 months at current rate
```

**Error Tracking:**
```
Top Errors (Last 24 Hours)

Error                            Count   % of Total
"Battle team invalid"            234     42%  ← User error, not bug
"Rate limit exceeded"            123     22%  ← Expected
"Database timeout"               45      8%   ⚠️  Investigate!
"Unit not found"                 34      6%
"Insufficient currency"          28      5%
Other                           96      17%
──────────────────────────────────────────────
Total Errors:                    560     0.12%

Action: Database timeouts need investigation
```

**Infrastructure Metrics:**
```
Railway App Performance

CPU Usage:        34%   ✅ Target: <70%
Memory Usage:     1.2GB ✅ Target: <2GB
Disk Usage:       18%   ✅ Target: <80%
Network Out:      234MB/day

Cost: $47/month (well under budget)
```

**Background Jobs:**
```
Job Status (Last 24 Hours)

Job                          Runs   Success  Failures  Avg Duration
Weekly Modifier Rotation     1      1        0         2.3s
Daily Challenge Generation   1      1        0         45.2s
Strategy Decay Update        1      1        0         12.8s
Guild Boss Spawn             0      -        -         -  (weekly)

All jobs healthy ✅
```

---

## Analytics Endpoints

### Admin API Endpoints

```csharp
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin")]
public class AdminAnalyticsController : ControllerBase
{
    // Overview Dashboard
    [HttpGet("overview")]
    public async Task<ActionResult<OverviewMetrics>> GetOverview()
    {
        return Ok(new OverviewMetrics
        {
            Dau = await GetDAU(),
            Wau = await GetWAU(),
            Mau = await GetMAU(),
            Mrr = await GetMRR(),
            ChurnRate = await GetChurnRate(),
            RecentAlerts = await GetRecentAlerts()
        });
    }
    
    // Player Analytics
    [HttpGet("players")]
    public async Task<ActionResult<PlayerAnalytics>> GetPlayerAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string segment = "all")
    {
        // Return player metrics filtered by date and segment
    }
    
    [HttpGet("players/{playerId}")]
    public async Task<ActionResult<PlayerDetail>> GetPlayerDetail(Guid playerId)
    {
        // Deep dive on specific player
        return Ok(new PlayerDetail
        {
            Player = await _context.Players.FindAsync(playerId),
            BattleHistory = await GetBattleHistory(playerId),
            RevenueHistory = await GetRevenueHistory(playerId),
            EngagementMetrics = await GetEngagementMetrics(playerId)
        });
    }
    
    // Engagement Metrics
    [HttpGet("engagement/retention")]
    public async Task<ActionResult<RetentionCurve>> GetRetentionCurve(
        [FromQuery] DateTime cohortStart,
        [FromQuery] int days = 30)
    {
        // Return retention curve for cohort
    }
    
    [HttpGet("engagement/feature-adoption")]
    public async Task<ActionResult<List<FeatureAdoption>>> GetFeatureAdoption()
    {
        return Ok(await _analyticsService.GetFeatureAdoption());
    }
    
    // Meta & Balance
    [HttpGet("meta/unit-stats")]
    public async Task<ActionResult<List<UnitStats>>> GetUnitStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        return Ok(await _analyticsService.GetUnitStats(startDate, endDate));
    }
    
    [HttpGet("meta/diversity")]
    public async Task<ActionResult<MetaDiversity>> GetMetaDiversity()
    {
        return Ok(await _analyticsService.CalculateMetaDiversity());
    }
    
    [HttpGet("meta/emerging-strategies")]
    public async Task<ActionResult<List<EmergingStrategy>>> GetEmergingStrategies()
    {
        // Strategies gaining traction fast
    }
    
    // Revenue Analytics
    [HttpGet("revenue/overview")]
    public async Task<ActionResult<RevenueOverview>> GetRevenueOverview()
    {
        return Ok(new RevenueOverview
        {
            Mrr = await GetMRR(),
            Arr = await GetARR(),
            Arpu = await GetARPU(),
            Arppu = await GetARPPU(),
            ConversionRate = await GetConversionRate(),
            ChurnRate = await GetChurnRate()
        });
    }
    
    [HttpGet("revenue/funnel")]
    public async Task<ActionResult<ConversionFunnel>> GetConversionFunnel()
    {
        // Free → Premium conversion funnel
    }
    
    [HttpGet("revenue/cohorts")]
    public async Task<ActionResult<List<CohortLTV>>> GetCohortLTV()
    {
        // LTV by registration cohort
    }
    
    // Guild Analytics
    [HttpGet("guilds/overview")]
    public async Task<ActionResult<GuildOverview>> GetGuildOverview()
    {
        return Ok(new GuildOverview
        {
            TotalGuilds = await _context.Guilds.CountAsync(),
            ActiveGuilds = await GetActiveGuildCount(),
            AvgMembersPerGuild = await GetAvgMembersPerGuild(),
            TopGuilds = await GetTopGuilds()
        });
    }
    
    [HttpGet("guilds/{guildId}/analytics")]
    public async Task<ActionResult<GuildAnalytics>> GetGuildAnalytics(Guid guildId)
    {
        // Deep dive on specific guild
    }
    
    // Content Planning
    [HttpGet("content/season-status")]
    public async Task<ActionResult<SeasonStatus>> GetSeasonStatus()
    {
        // Current season progress and stats
    }
    
    [HttpGet("content/feedback-summary")]
    public async Task<ActionResult<FeedbackSummary>> GetFeedbackSummary()
    {
        // Aggregated player feedback from surveys
    }
    
    // Technical Monitoring
    [HttpGet("technical/performance")]
    public async Task<ActionResult<TechnicalMetrics>> GetTechnicalMetrics()
    {
        return Ok(new TechnicalMetrics
        {
            AvgResponseTime = await GetAvgResponseTime(),
            ErrorRate = await GetErrorRate(),
            Uptime = await GetUptime(),
            DatabasePerformance = await GetDatabaseMetrics()
        });
    }
    
    [HttpGet("technical/errors")]
    public async Task<ActionResult<List<ErrorSummary>>> GetRecentErrors(
        [FromQuery] int hours = 24)
    {
        // Recent errors grouped by type
    }
    
    // Export
    [HttpGet("export/{reportType}")]
    public async Task<IActionResult> ExportReport(
        string reportType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string format = "csv")
    {
        var data = await _exportService.GenerateReport(reportType, startDate, endDate);
        
        if (format == "csv")
            return File(data, "text/csv", $"{reportType}_{DateTime.UtcNow:yyyyMMdd}.csv");
        else if (format == "json")
            return File(data, "application/json", $"{reportType}_{DateTime.UtcNow:yyyyMMdd}.json");
        
        return BadRequest("Unsupported format");
    }
}
```

---

## Database Views & Queries

### Pre-computed Analytics Views

```sql
-- Daily Metrics Snapshot (Materialized View)
CREATE VIEW analytics_daily_snapshot AS
SELECT 
    DATE(created_at) as date,
    COUNT(DISTINCT player_id) as dau,
    COUNT(DISTINCT CASE WHEN tier != 'Free' THEN player_id END) as paying_dau,
    COUNT(*) as total_battles,
    AVG(CASE WHEN winner_id IS NOT NULL THEN 1 ELSE 0 END) as completion_rate
FROM battles
WHERE created_at >= CURRENT_DATE - INTERVAL '90 days'
GROUP BY DATE(created_at);

-- Unit Performance Stats
CREATE VIEW analytics_unit_performance AS
SELECT 
    u.id as unit_id,
    u.name as unit_name,
    u.class as unit_class,
    COUNT(DISTINCT b.id) as battles_used,
    SUM(CASE WHEN b.winner_id = u.player_id THEN 1 ELSE 0 END)::float / 
        COUNT(*) as win_rate,
    COUNT(*) * 100.0 / SUM(COUNT(*)) OVER () as usage_rate
FROM units u
JOIN battle_teams bt ON u.id = ANY(bt.unit_ids)
JOIN battles b ON bt.battle_id = b.id
WHERE b.completed_at >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY u.id, u.name, u.class;

-- Revenue by Cohort
CREATE VIEW analytics_cohort_revenue AS
SELECT 
    DATE_TRUNC('week', p.created_at) as cohort_week,
    COUNT(DISTINCT p.id) as cohort_size,
    COUNT(DISTINCT CASE WHEN s.tier != 'Free' THEN p.id END) as paid_users,
    SUM(CASE WHEN s.status = 'Active' THEN s.amount_usd ELSE 0 END) as mrr,
    AVG(CASE WHEN s.tier != 'Free' THEN s.amount_usd ELSE 0 END) as avg_ltv
FROM players p
LEFT JOIN subscriptions s ON p.id = s.player_id
GROUP BY DATE_TRUNC('week', p.created_at);

-- Guild Activity Metrics
CREATE VIEW analytics_guild_activity AS
SELECT 
    g.id as guild_id,
    g.name as guild_name,
    COUNT(DISTINCT gm.player_id) as member_count,
    COUNT(DISTINCT gba.id) as raid_attempts_this_week,
    COUNT(DISTINCT gb.id) FILTER (WHERE gb.is_defeated) as bosses_defeated,
    AVG(p.rating) as avg_member_rating,
    COUNT(DISTINCT CASE WHEN s.tier != 'Free' THEN gm.player_id END)::float /
        COUNT(DISTINCT gm.player_id) as premium_rate
FROM guilds g
LEFT JOIN guild_memberships gm ON g.id = gm.guild_id
LEFT JOIN players p ON gm.player_id = p.id
LEFT JOIN subscriptions s ON p.id = s.player_id AND s.status = 'Active'
LEFT JOIN guild_bosses gb ON g.id = gb.guild_id
LEFT JOIN guild_boss_attempts gba ON gb.id = gba.guild_boss_id 
    AND gba.attempted_at >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY g.id, g.name;
```

---

## Alerts & Monitoring

### Automated Alerts

```csharp
public class AlertService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAlerts();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
    
    private async Task CheckAlerts()
    {
        // Check 1: Meta Balance
        var unitStats = await _analytics.GetUnitStats();
        foreach (var unit in unitStats)
        {
            if (unit.WinRate > 0.60 || unit.WinRate < 0.40)
            {
                await SendAlert(new Alert
                {
                    Severity = AlertSeverity.Warning,
                    Category = "Balance",
                    Message = $"{unit.Name} win rate: {unit.WinRate:P1} (expected: 50%)",
                    ActionRequired = "Consider balance adjustment"
                });
            }
        }
        
        // Check 2: Error Rate Spike
        var errorRate = await _analytics.GetErrorRate(TimeSpan.FromMinutes(5));
        if (errorRate > 0.05) // 5%
        {
            await SendAlert(new Alert
            {
                Severity = AlertSeverity.Critical,
                Category = "Technical",
                Message = $"Error rate spiked to {errorRate:P1}",
                ActionRequired = "Investigate immediately"
            });
        }
        
        // Check 3: Churn Spike
        var churnRate = await _analytics.GetChurnRate(TimeSpan.FromDays(7));
        if (churnRate > 0.05) // 5% weekly
        {
            await SendAlert(new Alert
            {
                Severity = AlertSeverity.Warning,
                Category = "Revenue",
                Message = $"Premium churn increased to {churnRate:P1}",
                ActionRequired = "Review recent changes"
            });
        }
        
        // Check 4: Signup Spike (Good alert!)
        var signups = await _analytics.GetSignups(TimeSpan.FromHours(1));
        if (signups > 100)
        {
            await SendAlert(new Alert
            {
                Severity = AlertSeverity.Info,
                Category = "Growth",
                Message = $"{signups} new signups in last hour!",
                ActionRequired = "Monitor for source"
            });
        }
        
        // Check 5: Database Performance
        var dbPerf = await _analytics.GetAvgQueryTime();
        if (dbPerf > 300) // 300ms
        {
            await SendAlert(new Alert
            {
                Severity = AlertSeverity.Warning,
                Category = "Technical",
                Message = $"Database queries averaging {dbPerf}ms",
                ActionRequired = "Check slow queries"
            });
        }
    }
    
    private async Task SendAlert(Alert alert)
    {
        // Store in database
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();
        
        // Send email if critical
        if (alert.Severity == AlertSeverity.Critical)
        {
            await _emailService.SendEmail(
                to: "mark@learnedgeek.com",
                subject: $"[CRITICAL] {alert.Category}: {alert.Message}",
                body: alert.ActionRequired
            );
        }
        
        // Post to Discord webhook
        await _discordService.PostAlert(alert);
    }
}

public class Alert
{
    public Guid Id { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Category { get; set; }
    public string Message { get; set; }
    public string ActionRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Acknowledged { get; set; }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}
```

---

## Export & Reporting

### Scheduled Reports

```csharp
public class ReportingService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            
            // Daily Report (8 AM UTC)
            if (now.Hour == 8 && now.Minute == 0)
            {
                await SendDailyReport();
            }
            
            // Weekly Report (Monday 9 AM UTC)
            if (now.DayOfWeek == DayOfWeek.Monday && now.Hour == 9 && now.Minute == 0)
            {
                await SendWeeklyReport();
            }
            
            // Monthly Report (1st of month, 10 AM UTC)
            if (now.Day == 1 && now.Hour == 10 && now.Minute == 0)
            {
                await SendMonthlyReport();
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
    
    private async Task SendDailyReport()
    {
        var report = await _analytics.GenerateDailyReport();
        
        var email = $@"
        Daily Report - {DateTime.UtcNow:yyyy-MM-dd}
        
        KEY METRICS:
        - DAU: {report.Dau:N0} ({report.DauChange:+0;-0}% vs yesterday)
        - Battles: {report.Battles:N0}
        - New Signups: {report.NewSignups:N0}
        - Premium Conversions: {report.PremiumConversions}
        - MRR: ${report.Mrr:N2} ({report.MrrChange:+0.0;-0.0}%)
        
        ALERTS:
        {string.Join("
", report.Alerts)}
        
        TOP PERFORMERS:
        {string.Join("
", report.TopPlayers.Take(5))}
        
        View full dashboard: https://your-app.railway.app/admin
        ";
        
        await _emailService.SendEmail("mark@learnedgeek.com", "Daily Report", email);
    }
    
    private async Task SendWeeklyReport()
    {
        // More detailed weekly summary
    }
    
    private async Task SendMonthlyReport()
    {
        // Comprehensive monthly analysis
    }
}
```

### Manual Export Options

```csharp
[HttpGet("export/players")]
public async Task<IActionResult> ExportPlayers(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate)
{
    var players = await _context.Players
        .Where(p => !startDate.HasValue || p.CreatedAt >= startDate)
        .Where(p => !endDate.HasValue || p.CreatedAt <= endDate)
        .Include(p => p.Subscription)
        .ToListAsync();
    
    var csv = new StringBuilder();
    csv.AppendLine("PlayerId,Username,Email,CreatedAt,Tier,Rating,TotalBattles,WinRate");
    
    foreach (var player in players)
    {
        csv.AppendLine($"{player.Id},{player.Username},{player.Email}," +
                      $"{player.CreatedAt:yyyy-MM-dd},{player.CurrentTier}," +
                      $"{player.Rating},{player.TotalBattles},{player.WinRate:P1}");
    }
    
    return File(Encoding.UTF8.GetBytes(csv.ToString()), 
                "text/csv", 
                $"players_{DateTime.UtcNow:yyyyMMdd}.csv");
}
```

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1)
- [ ] Admin authentication/authorization
- [ ] Overview dashboard (KPIs only)
- [ ] Basic player analytics
- [ ] Database views for common queries
- [ ] Manual refresh (no real-time yet)

### Phase 2: Core Analytics (Week 2)
- [ ] Engagement dashboard (retention curves)
- [ ] Meta/balance dashboard (unit stats)
- [ ] Revenue dashboard (MRR, conversion funnel)
- [ ] Guild analytics
- [ ] Export functionality (CSV)

### Phase 3: Monitoring (Week 3)
- [ ] Technical dashboard (API performance)
- [ ] Automated alerts
- [ ] Error tracking integration
- [ ] Daily email reports

### Phase 4: Content Planning (Week 4)
- [ ] Season status tracking
- [ ] Content pipeline view
- [ ] Player feedback aggregation
- [ ] A/B testing framework

### Phase 5: Polish (Week 5)
- [ ] Real-time data (auto-refresh)
- [ ] Chart improvements (interactive)
- [ ] Mobile-responsive design
- [ ] Weekly/monthly email reports

---

## Success Criteria

**Admin dashboard is successful when:**

✅ You can answer these questions in < 30 seconds:
- "Is the game healthy right now?"
- "Which unit needs a nerf?"
- "Why did 5 premium users churn yesterday?"
- "Is the current meta diverse?"
- "How's today's revenue vs last week?"

✅ You catch problems before players complain:
- Balance issues detected automatically
- Technical issues trigger alerts
- Churn spikes identified early
- Revenue drops investigated immediately

✅ You make data-driven decisions:
- "We should add more guild features" (drives conversion)
- "Fire Mage needs -10% damage" (68% win rate)
- "Keep $5 pricing" (A/B test showed $7 decreased revenue)
- "Shadow Demon boss too hard" (48% completion rate)

---

## Final Thoughts

The admin dashboard is **not optional**. It's the **control panel** for your game.

Without it:
- ❌ You're guessing
- ❌ Problems go unnoticed
- ❌ Can't prove ROI
- ❌ Slow iteration

With it:
- ✅ You have visibility
- ✅ Early problem detection
- ✅ Data-driven decisions
- ✅ Fast iteration

**Build it early. Use it daily. Let it guide you.**

---

*Document Version: 1.0*  
*Last Updated: February 11, 2026*  
*Prepared by: Mark @ Learned Geek Consulting*
