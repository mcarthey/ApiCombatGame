# API Combat Game - Engagement & Monetization Strategy

**Version:** 1.0  
**Date:** February 11, 2026  
**Author:** Mark (Learned Geek Consulting)  
**Purpose:** Comprehensive strategy for player engagement, monetization, and social features

---

## Table of Contents

1. [Core Design Philosophy](#core-design-philosophy)
2. [Monetization Model](#monetization-model)
3. [Guild System Architecture](#guild-system-architecture)
4. [Engagement Loops](#engagement-loops)
5. [Custom API Documentation](#custom-api-documentation)
6. [Admin Analytics](#admin-analytics)
7. [Loot & Progression](#loot--progression)
8. [Implementation Roadmap](#implementation-roadmap)
9. [Success Metrics](#success-metrics)

---

## Core Design Philosophy

### The Anti-Mobile-Game Manifesto

**What This Game Is NOT:**
- ❌ FOMO-driven (no limited-time events forcing daily login)
- ❌ Pay-to-win (premium features = tools, not power)
- ❌ Grind-heavy (no artificial time gates)
- ❌ Exploit-focused (no loot boxes, no gambling mechanics)
- ❌ Over-monetized (no 5 different currency types)
- ❌ Session-locked (no "must be online at 7pm for raid")

**What This Game IS:**
- ✅ Respect for player time (async, play-your-way)
- ✅ Developer-focused (tools as premium value)
- ✅ Social-first (guilds are the game)
- ✅ API-native (collaboration through code)
- ✅ Clean monetization (one price, clear value)
- ✅ Community-driven (player strategies, not publisher content drops)

### Core Principles

**1. API Simplification**
> "We're not making people jump all over the place. Simple APIs, clear purposes."

**2. Engagement Through Collaboration**
> "Social features keep people coming back, not artificial timers."

**3. Monetization Without Exploitation**
> "Premium tier = better tools for serious players. Not FOMO."

**4. Async-First Design**
> "Guilds work together on their schedule, not ours."

**5. Developer-Centric Value**
> "Premium features are things developers actually want: scripting, simulations, analytics."

---

## Monetization Model

### Three-Tier System

```
┌─────────────────────────────────────────────────────────────┐
│                          FREE TIER                          │
│                      "Try the game"                         │
├─────────────────────────────────────────────────────────────┤
│ Battles:        10 per day                                  │
│ Team Slots:     3 configurations                            │
│ Units:          Basic 20 units                              │
│ Social:         Solo play only (no guilds)                  │
│ API Access:     Standard endpoints                          │
│ Leaderboard:    View public rankings                        │
│ Docs:           Full API documentation                      │
│                                                              │
│ COST: $0/month                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                       PREMIUM TIER                          │
│                   "Serious optimizer"                       │
├─────────────────────────────────────────────────────────────┤
│ Battles:        Unlimited                                   │
│ Team Slots:     10 configurations                           │
│ Units:          All 50+ units unlocked                      │
│ Social:         Create/join guilds                          │
│                 Guild leadership tools                      │
│                 Shared strategy library                     │
│ API Access:     Simulation endpoint (10K battles/day)       │
│                 Strategy versioning (save 10 versions)      │
│                 Discord webhooks                            │
│                 Advanced stats API                          │
│ Progression:    1.5x gold earn rate                         │
│                 Exclusive cosmetic titles                   │
│ Support:        Priority support channel                    │
│                                                              │
│ COST: $5/month                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                      PREMIUM+ TIER                          │
│                    "Power user / API master"                │
├─────────────────────────────────────────────────────────────┤
│ Everything in Premium PLUS:                                 │
│                                                              │
│ Scripting:      Lua scripting engine                        │
│                 Write custom battle AI logic                │
│                 Automate strategy optimization              │
│ Real-time:      WebSocket connections                       │
│                 Live battle updates                         │
│                 Real-time guild notifications               │
│ Operations:     Batch operations (queue 100 battles)        │
│                 Higher API rate limits (5x standard)        │
│ Analytics:      Advanced analytics API                      │
│                 Historical trend data                       │
│                 Meta-game insights dashboard                │
│ Coaching:       1 monthly 1-on-1 strategy session (opt-in)  │
│ Cosmetics:      Exclusive Premium+ unit skins               │
│                 Custom battle themes/effects                │
│                                                              │
│ COST: $10-15/month                                          │
│                                                              │
│ JUSTIFICATION: Real server costs                            │
│ - Script execution environment (CPU)                        │
│ - WebSocket persistent connections (memory)                │
│ - Batch processing overhead (infrastructure)               │
└─────────────────────────────────────────────────────────────┘
```

### Why This Model Works

**For Free Players:**
- Can genuinely play and enjoy the game
- No paywall blocking core mechanics
- Can compete (skill matters more than premium)
- Clear path to understand premium value

**For Premium Players ($5/mo):**
- Unlocks guild features (social = retention)
- Simulation endpoint saves hours of manual testing
- Developer mindset: "This tool is worth $5"
- Not pay-to-win, pay-for-convenience

**For Premium+ Players ($10-15/mo):**
- Serious optimization nerds
- Scripting = "I can automate my entire strategy"
- WebSockets = "Real-time optimization loop"
- Justified cost = actual server resources consumed

### Revenue Projections

**Conservative (Year 1):**
```
Users:           5,000 registered
Free:            4,000 (80%)
Premium:         800 (16%)
Premium+:        200 (4%)

Monthly Revenue:
Premium:         800 × $5 = $4,000
Premium+:        200 × $12 = $2,400
Total MRR:       $6,400
Annual:          $76,800
```

**Optimistic (Year 1):**
```
Users:           20,000 registered
Free:            15,000 (75%)
Premium:         4,000 (20%)
Premium+:        1,000 (5%)

Monthly Revenue:
Premium:         4,000 × $5 = $20,000
Premium+:        1,000 × $12 = $12,000
Total MRR:       $32,000
Annual:          $384,000
```

**Realistic Target:** $10K MRR by month 6

---

## Guild System Architecture

### Role-Based API Access

**Three Guild Roles:**

```
┌─────────────────────────────────────────────────────────────┐
│                        GUILD LEADER                         │
├─────────────────────────────────────────────────────────────┤
│ Can access ALL guild endpoints:                             │
│                                                              │
│ POST   /api/v1/guild/create                                 │
│ DELETE /api/v1/guild/{id}                                   │
│ POST   /api/v1/guild/{id}/invite                            │
│ POST   /api/v1/guild/{id}/kick                              │
│ PUT    /api/v1/guild/{id}/promote (change roles)            │
│ POST   /api/v1/guild/{id}/raid/queue                        │
│ POST   /api/v1/guild/{id}/upgrade                           │
│ PUT    /api/v1/guild/{id}/settings                          │
│ GET    /api/v1/guild/{id}/treasury                          │
│ POST   /api/v1/guild/{id}/treasury/spend                    │
│ POST   /api/v1/guild/{id}/strategy/publish                  │
│ DELETE /api/v1/guild/{id}/strategy/{stratId}                │
│                                                              │
│ Limit: 1 per guild                                          │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                        GUILD OFFICER                        │
├─────────────────────────────────────────────────────────────┤
│ Can access:                                                 │
│                                                              │
│ POST   /api/v1/guild/{id}/invite (limited: 5/day)          │
│ GET    /api/v1/guild/{id}/members                           │
│ PUT    /api/v1/guild/{id}/strategy/{stratId}                │
│ POST   /api/v1/guild/{id}/strategy/publish                  │
│ POST   /api/v1/guild/{id}/raid/attack                       │
│ GET    /api/v1/guild/{id}/raid/leaderboard                  │
│ GET    /api/v1/guild/{id}/treasury (read-only)              │
│ POST   /api/v1/guild/{id}/chat                              │
│ GET    /api/v1/guild/{id}/chat                              │
│                                                              │
│ Limit: 5 per guild (configurable via upgrade)              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                        GUILD MEMBER                         │
├─────────────────────────────────────────────────────────────┤
│ Can access:                                                 │
│                                                              │
│ GET    /api/v1/guild/{id}/info                              │
│ GET    /api/v1/guild/{id}/members                           │
│ GET    /api/v1/guild/{id}/raid/current                      │
│ POST   /api/v1/guild/{id}/raid/attack                       │
│ GET    /api/v1/guild/{id}/raid/leaderboard                  │
│ GET    /api/v1/guild/{id}/strategies                        │
│ GET    /api/v1/guild/{id}/treasury (read-only)              │
│ POST   /api/v1/guild/{id}/chat                              │
│ GET    /api/v1/guild/{id}/chat                              │
│ POST   /api/v1/guild/leave                                  │
│                                                              │
│ Limit: 20-50 per guild (depends on upgrades)               │
└─────────────────────────────────────────────────────────────┘
```

### Guild Endpoints (Detailed)

#### Core Guild Management

```http
POST /api/v1/guild/create
Authorization: Bearer {premium_token}
Content-Type: application/json

{
  "name": "The Optimizers",
  "tag": "OPT",
  "description": "Min-maxing since 2026",
  "isPublic": true,
  "maxMembers": 20
}

Response 201 Created:
{
  "guildId": "guild_abc123",
  "name": "The Optimizers",
  "tag": "OPT",
  "leaderId": "player_xyz789",
  "createdAt": "2026-02-11T14:30:00Z",
  "inviteCode": "OPT-X7Y2Z"
}
```

```http
POST /api/v1/guild/{guildId}/invite
Authorization: Bearer {leader_or_officer_token}
Content-Type: application/json

{
  "playerUsername": "AwesomeDev42"
}

Response 200 OK:
{
  "inviteId": "invite_123",
  "playerUsername": "AwesomeDev42",
  "status": "pending",
  "expiresAt": "2026-02-18T14:30:00Z"
}
```

```http
GET /api/v1/guild/{guildId}/members
Authorization: Bearer {member_token}

Response 200 OK:
{
  "guildId": "guild_abc123",
  "totalMembers": 18,
  "maxMembers": 20,
  "members": [
    {
      "playerId": "player_xyz789",
      "username": "LeaderDude",
      "role": "leader",
      "joinedAt": "2026-01-15T10:00:00Z",
      "contributionPoints": 12450,
      "lastActive": "2026-02-11T13:45:00Z"
    },
    {
      "playerId": "player_abc456",
      "username": "OfficerGal",
      "role": "officer",
      "joinedAt": "2026-01-16T11:30:00Z",
      "contributionPoints": 8900,
      "lastActive": "2026-02-11T09:20:00Z"
    },
    // ... more members
  ]
}
```

#### Guild Raid Boss System

```http
GET /api/v1/guild/{guildId}/raid/current
Authorization: Bearer {member_token}

Response 200 OK:
{
  "raidId": "raid_week_7",
  "boss": {
    "name": "The Fire Dragon",
    "description": "Ancient wyrm resistant to magic",
    "maxHp": 100000,
    "currentHp": 67340,
    "attack": 450,
    "defense": 380,
    "modifiers": [
      "Fire attacks deal +20% damage",
      "Healing reduced by 50%",
      "Physical attacks normal effectiveness"
    ]
  },
  "status": "active",
  "spawnedAt": "2026-02-10T00:00:00Z",
  "expiresAt": "2026-02-17T00:00:00Z",
  "timeRemaining": "5 days, 9 hours",
  "guildProgress": {
    "totalDamageDealt": 32660,
    "percentComplete": 32.66,
    "membersContributed": 12,
    "totalMembers": 18
  },
  "rewards": {
    "gold": 5000,
    "guildGold": 10000,
    "experience": 2500
  }
}
```

```http
POST /api/v1/guild/{guildId}/raid/attack
Authorization: Bearer {member_token}
Content-Type: application/json

{
  "teamId": "team_abc123"
}

Response 200 OK:
{
  "attackId": "attack_xyz789",
  "damageDealt": 2340,
  "isCritical": true,
  "boss": {
    "currentHp": 64000,
    "percentRemaining": 64.0
  },
  "rewards": {
    "gold": 234,
    "contributionPoints": 100
  },
  "battleLog": [
    {
      "turn": 1,
      "actor": "Warrior_A",
      "action": "attack",
      "target": "Fire Dragon",
      "damage": 340,
      "effects": ["class_advantage", "critical_hit"]
    },
    // ... battle log
  ],
  "attemptsRemaining": 2,
  "nextAttemptAvailable": "2026-02-11T20:30:00Z"
}
```

```http
GET /api/v1/guild/{guildId}/raid/leaderboard
Authorization: Bearer {member_token}

Response 200 OK:
{
  "raidId": "raid_week_7",
  "leaderboard": [
    {
      "rank": 1,
      "playerId": "player_def789",
      "username": "TopDPS",
      "totalDamage": 8900,
      "attacksUsed": 3,
      "bestSingleAttack": 3200,
      "contributionPoints": 450
    },
    {
      "rank": 2,
      "playerId": "player_ghi012",
      "username": "BurstKing",
      "totalDamage": 7340,
      "attacksUsed": 3,
      "bestSingleAttack": 2900,
      "contributionPoints": 370
    },
    // ... top 10
  ]
}
```

#### Shared Strategy Library

```http
GET /api/v1/guild/{guildId}/strategies
Authorization: Bearer {member_token}

Response 200 OK:
{
  "guildId": "guild_abc123",
  "strategies": [
    {
      "strategyId": "strat_123",
      "name": "Tank Meta v4",
      "description": "Updated for fire dragon resistance",
      "creatorId": "player_xyz789",
      "creatorUsername": "LeaderDude",
      "createdAt": "2026-02-09T14:00:00Z",
      "updatedAt": "2026-02-11T10:30:00Z",
      "usageCount": 234,
      "winRate": 0.62,
      "averageRating": 4.3,
      "tags": ["raid", "tank", "defensive"]
    },
    {
      "strategyId": "strat_456",
      "name": "All-in Burst",
      "description": "High risk, high reward for speedruns",
      "creatorId": "player_abc456",
      "creatorUsername": "OfficerGal",
      "createdAt": "2026-02-08T18:00:00Z",
      "updatedAt": "2026-02-10T12:00:00Z",
      "usageCount": 89,
      "winRate": 0.58,
      "averageRating": 3.9,
      "tags": ["raid", "burst", "aggressive"]
    }
  ]
}
```

```http
POST /api/v1/guild/{guildId}/strategy/publish
Authorization: Bearer {officer_or_leader_token}
Content-Type: application/json

{
  "name": "Counter Fire Dragon v2",
  "description": "Focus on physical damage dealers",
  "strategyJson": "{...strategy config...}",
  "tags": ["raid", "physical", "current-boss"]
}

Response 201 Created:
{
  "strategyId": "strat_789",
  "name": "Counter Fire Dragon v2",
  "shareUrl": "/guild/guild_abc123/strategy/strat_789",
  "createdAt": "2026-02-11T14:45:00Z"
}
```

```http
PUT /api/v1/guild/{guildId}/strategy/{strategyId}
Authorization: Bearer {creator_or_leader_token}
Content-Type: application/json

{
  "description": "Updated: Use Rangers instead of Warriors for higher DPS",
  "strategyJson": "{...updated config...}"
}

Response 200 OK:
{
  "strategyId": "strat_789",
  "updatedAt": "2026-02-11T15:00:00Z",
  "version": 2
}
```

#### Guild Treasury

```http
GET /api/v1/guild/{guildId}/treasury
Authorization: Bearer {member_token}

Response 200 OK:
{
  "guildId": "guild_abc123",
  "gold": 45600,
  "contributionThisWeek": 8900,
  "topContributors": [
    {
      "playerId": "player_xyz789",
      "username": "LeaderDude",
      "contributed": 3400
    },
    {
      "playerId": "player_abc456",
      "username": "OfficerGal",
      "contributed": 2100
    }
  ],
  "availableUpgrades": [
    {
      "upgradeId": "max_members_30",
      "name": "Expand Guild (30 members)",
      "cost": 50000,
      "currentLevel": 20,
      "nextLevel": 30
    },
    {
      "upgradeId": "gold_bonus_10",
      "name": "Guild Gold Bonus +10%",
      "cost": 30000,
      "currentBonus": 0,
      "nextBonus": 10
    },
    {
      "upgradeId": "raid_attempts_4",
      "name": "Extra Raid Attempt (4 per day)",
      "cost": 40000,
      "currentAttempts": 3,
      "nextAttempts": 4
    }
  ]
}
```

```http
POST /api/v1/guild/{guildId}/treasury/spend
Authorization: Bearer {leader_token}
Content-Type: application/json

{
  "upgradeId": "gold_bonus_10"
}

Response 200 OK:
{
  "upgradeId": "gold_bonus_10",
  "purchased": true,
  "goldRemaining": 15600,
  "goldSpent": 30000,
  "effectActive": true,
  "appliedAt": "2026-02-11T15:15:00Z"
}
```

#### Guild Chat/Communication

```http
GET /api/v1/guild/{guildId}/chat
Authorization: Bearer {member_token}
Query: ?limit=50&before=messageId_123

Response 200 OK:
{
  "guildId": "guild_abc123",
  "messages": [
    {
      "messageId": "msg_xyz789",
      "playerId": "player_abc456",
      "username": "OfficerGal",
      "message": "New strategy posted: Counter Fire Dragon v2. Check it out!",
      "timestamp": "2026-02-11T14:46:00Z",
      "type": "chat"
    },
    {
      "messageId": "msg_abc123",
      "playerId": "system",
      "username": "System",
      "message": "TopDPS dealt 3200 damage to Fire Dragon (new record!)",
      "timestamp": "2026-02-11T14:30:00Z",
      "type": "system"
    },
    {
      "messageId": "msg_def456",
      "playerId": "player_xyz789",
      "username": "LeaderDude",
      "message": "Great job everyone! We're at 68% boss HP. Keep pushing!",
      "timestamp": "2026-02-11T14:15:00Z",
      "type": "chat"
    }
  ],
  "hasMore": true,
  "nextCursor": "msg_ghi789"
}
```

```http
POST /api/v1/guild/{guildId}/chat
Authorization: Bearer {member_token}
Content-Type: application/json

{
  "message": "Just did 2.1K damage using the new strategy. Works great!"
}

Response 201 Created:
{
  "messageId": "msg_jkl012",
  "timestamp": "2026-02-11T15:20:00Z",
  "message": "Just did 2.1K damage using the new strategy. Works great!"
}
```

### Guild Permissions Matrix

| Endpoint | Leader | Officer | Member |
|----------|--------|---------|--------|
| Create guild | ✅ | ❌ | ❌ |
| Delete guild | ✅ | ❌ | ❌ |
| Invite members | ✅ | ✅ (5/day) | ❌ |
| Kick members | ✅ | ❌ | ❌ |
| Promote/demote | ✅ | ❌ | ❌ |
| Edit settings | ✅ | ❌ | ❌ |
| View members | ✅ | ✅ | ✅ |
| View treasury | ✅ (full) | ✅ (read) | ✅ (read) |
| Spend treasury | ✅ | ❌ | ❌ |
| Queue raid | ✅ | ❌ | ❌ |
| Attack raid boss | ✅ | ✅ | ✅ |
| View leaderboard | ✅ | ✅ | ✅ |
| Publish strategy | ✅ | ✅ | ❌ |
| Edit own strategy | ✅ | ✅ | ✅ |
| Delete any strategy | ✅ | ❌ | ❌ |
| View strategies | ✅ | ✅ | ✅ |
| Post in chat | ✅ | ✅ | ✅ |
| Leave guild | ❌ | ✅ | ✅ |

### Guild Monetization

**Premium Requirement:**
- Only Premium or Premium+ players can create guilds
- Free players can join guilds (but not create)
- Guild features designed to convert free → premium

**Why this works:**
- Guild leaders want tools (simulation, scripting)
- Free players see guild value → convert to premium
- Social pressure: "Contribute to raid boss!"

**Guild Upgrades (Treasury spending):**
```
┌────────────────────────────────────────────────┐
│ Guild Upgrade Shop                             │
├────────────────────────────────────────────────┤
│ Max Members:                                   │
│   20 → 30 members: 50,000 gold                │
│   30 → 50 members: 100,000 gold               │
│                                                │
│ Gold Bonus:                                    │
│   +10% all members: 30,000 gold               │
│   +20% all members: 60,000 gold               │
│                                                │
│ Raid Attempts:                                 │
│   3 → 4 per day: 40,000 gold                  │
│   4 → 5 per day: 80,000 gold                  │
│                                                │
│ Cosmetics:                                     │
│   Custom guild banner: 20,000 gold            │
│   Guild title prefix: 15,000 gold             │
│                                                │
│ Utility:                                       │
│   Private Discord webhook: 10,000 gold        │
│   Advanced guild analytics: 25,000 gold       │
└────────────────────────────────────────────────┘
```

---

## Engagement Loops

### Daily Engagement Loop

**Goal:** Give players a reason to check in daily (without forcing it)

```
Player wakes up
    ↓
Checks Discord: "Guild raid boss at 68% HP!"
    ↓
Opens API docs or custom client
    ↓
GET /api/v1/guild/raid/current
    ↓
Sees boss modifiers changed overnight
    ↓
Configures team to counter new modifiers
    ↓
POST /api/v1/guild/raid/attack
    ↓
Sees damage dealt: 2.3K (personal best!)
    ↓
GET /api/v1/guild/chat
    ↓
Teammates celebrating, strategizing
    ↓
Feels accomplished, part of something
    ↓
Closes tab, goes about day
```

**Time commitment:** 5-10 minutes  
**Engagement driver:** Social contribution, not artificial timer  
**Result:** Player feels good, not obligated

### Weekly Engagement Loop

**Goal:** Fresh content, prevent stagnation

```
Monday 00:00 UTC
    ↓
New raid boss spawns for all guilds
    ↓
Boss has unique modifiers (randomized)
    ↓
Guilds strategize in Discord
    ↓
Players experiment with counters
    ↓
Shared strategy library updates
    ↓
Meta shifts weekly (no "solved" state)
    ↓
Sunday night: Final push to defeat boss
    ↓
Monday: Rewards distributed, new boss
```

**Why this works:**
- Weekly reset = fresh challenge
- Different modifiers = different strategies
- Guild collaboration = social stickiness
- No FOMO (you can miss a week)

### Monthly Engagement Loop

**Goal:** Seasonal progression, long-term goals

```
Season 1 (Feb 1 - Feb 28)
    ↓
Ranked ladder reset
    ↓
Players climb rankings
    ↓
Guild leaderboards compete
    ↓
Special seasonal achievements
    ↓
End of season rewards:
  - Top 100: Exclusive title
  - Top 10 guilds: Cosmetic banner
  - Participation: Gold bonus
    ↓
March 1: Season 2 begins
    ↓
Soft reset (rating × 0.8)
    ↓
Fresh climb, new meta
```

**Why this works:**
- Seasonal urgency without daily pressure
- Rewards skill, not just time investment
- Fresh start prevents stagnation
- Cosmetic rewards (no power creep)

---

## Custom API Documentation

### The Vision

**Problem with Standard Tools:**
- Swagger: Too technical, not user-friendly
- Redoc: Limited customization
- Scalar: Ugly, feels generic

**Solution: Custom docs with full control**

### Architecture

```
┌─────────────────────────────────────────────────┐
│               Custom Annotations                │
│   (Controller attributes you control)           │
└─────────────────────┬───────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│            OpenAPI Spec Generator               │
│   (Reads annotations, generates spec)           │
└─────────────────────┬───────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│              Template Engine                    │
│   (Razor, custom HTML, your design)             │
└─────────────────────┬───────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│          Beautiful Custom Docs Page             │
│   (Exactly the UX you want)                     │
└─────────────────────────────────────────────────┘
```

### Example Custom Annotations

```csharp
[ApiDocumentation(
    Title = "Queue a Battle",
    Description = "Submit your team to the matchmaking queue",
    Category = "Combat",
    Tier = ApiTier.Free,
    ShowInDocs = true,
    ExampleRequest = @"{
        ""teamId"": ""team_abc123"",
        ""mode"": ""ranked""
    }",
    ExampleResponse = @"{
        ""battleId"": ""battle_xyz789"",
        ""status"": ""queued"",
        ""estimatedWait"": ""30 seconds""
    }",
    Notes = "Battles resolve in 30-60 seconds. Check results with GET /battle/results/{id}"
)]
[HttpPost("battle/queue")]
public async Task<ActionResult<BattleQueueResponse>> QueueBattle(
    [FromBody] BattleQueueRequest request)
{
    // ...
}
```

### Key Features to Implement

#### 1. Interactive Battle Replay Viewer

**Integration into docs:**

```
GET /api/v1/battle/results/{battleId}

Response:
{
  "battleId": "battle_xyz789",
  "winner": "playerA",
  "turns": [...battle log...]
}

[Interactive Replay]
┌─────────────────────────────────────────┐
│ ▶ Watch Battle Replay                   │
│                                          │
│ ┌──────────────────────────────────┐   │
│ │ Turn 3/24        [◀ ▶ ⏸]  Speed:1x│   │
│ ├──────────────────────────────────┤   │
│ │                                   │   │
│ │ PlayerA Team          PlayerB Team│   │
│ │ ┌─────┐┌─────┐       ┌─────┐     │   │
│ │ │ WAR ││ MAG │       │ TNK │     │   │
│ │ │ 80% ││ 90% │       │ 60% │     │   │
│ │ └─────┘└─────┘       └─────┘     │   │
│ │                                   │   │
│ │ [Warrior attacks Tank]            │   │
│ │ "Critical hit! 45 damage"         │   │
│ │                                   │   │
│ └──────────────────────────────────┘   │
│                                          │
│ Turn Log:                                │
│ 1. Mage casts fireball → 30 dmg        │
│ 2. Tank defends → -50% incoming        │
│ 3. Warrior attacks → 45 dmg (CRIT!)    │
│ ...                                      │
└─────────────────────────────────────────┘
```

**Why this is killer:**
- New players learn by watching
- Players analyze strategies visually
- Shareable (social marketing)
- Lives in the docs (always accessible)

#### 2. Simple Dashboard Visualizations

**Example: Win Rate Over Time**

```
GET /api/v1/player/stats

Response:
{
  "winRateHistory": [
    {"date": "2026-02-01", "winRate": 0.45},
    {"date": "2026-02-02", "winRate": 0.48},
    {"date": "2026-02-03", "winRate": 0.52},
    ...
  ]
}

[Simple Graph]
┌─────────────────────────────────────────┐
│ Your Win Rate (Last 30 Days)            │
│                                          │
│ 60% ┤                           ╭───    │
│     │                      ╭────╯       │
│ 50% ┤              ╭──────╯            │
│     │      ╭───────╯                   │
│ 40% ┤──────╯                           │
│     └───────────────────────────────   │
│      Feb 1        Feb 15      Feb 28   │
└─────────────────────────────────────────┘
```

**Other simple visualizations:**
- Unit usage pie chart
- Gold earned over time
- Battle frequency heatmap
- Guild contribution bar chart

**Implementation:** Use Chart.js or similar (lightweight, API-friendly)

#### 3. Tiered Endpoint Visibility

**Control what shows in docs based on user tier:**

```csharp
[ApiTier(Free)]     // Shows for everyone
[ApiTier(Premium)]  // Shows only for premium users
[ApiTier(Premium+)] // Shows only for premium+ users
[ApiTier(Hidden)]   // Never shows in public docs
```

**Example:**

```
Free User sees:
├── Authentication
├── Player Profile
├── Battle Queue
└── Leaderboard

Premium User sees:
├── Everything above
├── Simulation Endpoint
├── Strategy Versioning
├── Guild Management
└── Advanced Stats

Premium+ User sees:
├── Everything above
├── Scripting Engine
├── WebSocket Endpoints
├── Batch Operations
└── Analytics API
```

**Why this works:**
- Docs are personalized
- Creates "upgrade to see more" curiosity
- Premium users feel valued

### Custom Docs UX Flow

```
User lands on /api-docs
    ↓
Sees beautiful landing page (not generic OpenAPI)
    ↓
Categories: Authentication, Combat, Guilds, Admin
    ↓
Clicks "Combat" → expands to show endpoints
    ↓
Clicks "Queue Battle"
    ↓
Sees:
  - Description
  - Request example (syntax highlighted)
  - Response example
  - "Try It" button (inline playground)
  - Live replay viewer (if battle endpoint)
  - Notes/tips section
    ↓
Clicks "Try It"
    ↓
Fills in parameters
    ↓
Executes request
    ↓
Sees response (formatted, syntax highlighted)
    ↓
If battle: Can watch replay immediately
    ↓
Feels: "This is professional. I trust this."
```

---

## Admin Analytics

### The Challenge

**Traditional session metrics don't work for APIs:**
- No "session time" (just API calls)
- No "page views" (just endpoint hits)
- No "bounce rate" (continuous usage)

**Need API-native metrics**

### Admin Analytics Endpoints

```http
GET /api/v1/admin/analytics/overview
Authorization: Bearer {admin_token}

Response 200 OK:
{
  "period": "last_30_days",
  "users": {
    "total": 5420,
    "new": 890,
    "active": 3240,
    "dailyActive": 1430,
    "weeklyActive": 2890
  },
  "battles": {
    "total": 124560,
    "avgPerUser": 38.5,
    "avgPerDay": 4152
  },
  "apiCalls": {
    "total": 8934200,
    "avgPerUser": 2758,
    "avgPerDay": 297806,
    "topEndpoints": [
      {
        "endpoint": "GET /battle/results/{id}",
        "calls": 1234000,
        "percent": 13.8
      },
      {
        "endpoint": "POST /battle/queue",
        "calls": 987000,
        "percent": 11.1
      },
      {
        "endpoint": "GET /player/roster",
        "calls": 654000,
        "percent": 7.3
      }
    ]
  },
  "engagement": {
    "retention7Day": 0.42,
    "retention30Day": 0.28,
    "avgSessionsPerWeek": 8.3
  },
  "monetization": {
    "totalRevenue": 32100,
    "mrr": 6400,
    "premiumUsers": 800,
    "premiumPlusUsers": 200,
    "conversionRate": 0.184,
    "churnRate": 0.034
  }
}
```

```http
GET /api/v1/admin/analytics/meta
Authorization: Bearer {admin_token}

Response 200 OK:
{
  "period": "last_7_days",
  "unitUsage": [
    {
      "unitId": "warrior_basic",
      "name": "Bronze Warrior",
      "uses": 45600,
      "winRate": 0.54,
      "trend": "stable"
    },
    {
      "unitId": "mage_fire",
      "name": "Fire Mage",
      "uses": 38900,
      "winRate": 0.62,
      "trend": "rising"
    },
    {
      "unitId": "healer_divine",
      "name": "Divine Healer",
      "uses": 34200,
      "winRate": 0.58,
      "trend": "falling"
    }
  ],
  "strategyDiversity": {
    "uniqueStrategies": 234,
    "avgStrategiesPerPlayer": 3.2,
    "topStrategyUsage": 0.18,
    "diversityScore": 0.73,
    "health": "good"
  },
  "emergingStrategies": [
    {
      "strategyHash": "hash_abc123",
      "name": "Burst Ranger Meta",
      "uses": 890,
      "winRate": 0.64,
      "growthRate": 0.42,
      "status": "emerging_threat"
    }
  ],
  "balanceIssues": [
    {
      "type": "unit_overperforming",
      "unitId": "mage_fire",
      "winRate": 0.62,
      "expectedWinRate": 0.50,
      "severity": "medium"
    }
  ]
}
```

```http
GET /api/v1/admin/analytics/player/{playerId}
Authorization: Bearer {admin_token}

Response 200 OK:
{
  "playerId": "player_xyz789",
  "username": "AwesomeDev42",
  "tier": "premium",
  "registered": "2026-01-15T10:30:00Z",
  "lastActive": "2026-02-11T14:23:00Z",
  "stats": {
    "totalBattles": 247,
    "winRate": 0.58,
    "currentStreak": 5,
    "rating": 1432,
    "rank": 234
  },
  "engagement": {
    "daysActive": 27,
    "avgBattlesPerDay": 9.1,
    "apiCallsThisWeek": 1342,
    "lastSessionLength": "12 minutes"
  },
  "strategies": {
    "total": 12,
    "mostUsed": {
      "name": "Tank Meta v3",
      "uses": 89,
      "winRate": 0.61
    }
  },
  "guild": {
    "guildId": "guild_abc123",
    "name": "The Optimizers",
    "role": "officer",
    "contributionPoints": 8900
  },
  "monetization": {
    "tier": "premium",
    "subscriptionStart": "2026-01-20T00:00:00Z",
    "lifetimeValue": 65.00,
    "churnRisk": "low"
  }
}
```

### Key Metrics to Track

**Engagement Metrics:**
```
DAU (Daily Active Users)
- Users with at least 1 API call in last 24 hours

WAU (Weekly Active Users)
- Users with at least 1 API call in last 7 days

MAU (Monthly Active Users)
- Users with at least 1 API call in last 30 days

DAU/MAU Ratio (Stickiness)
- 0.3+ is good for async game
- 0.4+ is excellent

Retention:
- Day 7: % of users who return after 7 days
- Day 30: % of users who return after 30 days

Avg Battles Per User
- Free users: 8-10/day expected
- Premium: 15-20/day expected

API Calls Per User
- Indicator of engagement depth
- High calls = building custom clients (good)
```

**Meta Health Metrics:**
```
Strategy Diversity Score
- 0 = everyone uses same strategy (bad)
- 1 = all unique strategies (good)
- 0.7+ = healthy meta

Unit Usage Balance
- No single unit > 30% usage
- All units > 5% usage
- Otherwise: balance issues

Win Rate Distribution
- All units 45-55% win rate = balanced
- Outliers indicate overpowered/underpowered

Emerging Strategies
- New strategies gaining >20% adoption/week
- Early warning for meta shifts
```

**Monetization Metrics:**
```
MRR (Monthly Recurring Revenue)
- Sum of all active subscriptions

Conversion Rate
- % of free users → premium

Churn Rate
- % of premium users who cancel

LTV (Lifetime Value)
- Avg revenue per user over lifetime
- Target: LTV > 3x CAC

Premium Attach Rate
- % of active users who are premium
- Target: 15-20%
```

### Admin Dashboard Features

**Real-time monitoring:**
- Current DAU/WAU
- Battles in last hour
- API error rate
- Server health metrics

**Trend analysis:**
- User growth over time
- Revenue growth
- Engagement trends
- Meta shifts

**Alerts:**
- Spike in error rate
- Sudden drop in DAU
- Balance issues detected
- Churn rate > 5%
- Server load > 80%

**Export options:**
- CSV download for all metrics
- API access for BI tools
- Automated weekly reports

---

## Loot & Progression

### Currency System

**Single Currency: Gold**

**Why single currency?**
- Simple to understand
- No confusion about "which currency for what?"
- Can't accidentally introduce pay-to-win
- Clean API responses

**How to Earn Gold:**
```
Win a battle:              +50 gold (base)
Lose a battle:             +10 gold (participation)
Win streak bonus:          +10 gold per consecutive win
First battle of day:       +100 gold (daily bonus)
Complete daily challenge:  +500 gold
Guild raid contribution:   +100-500 gold (based on damage)
Achievement unlock:        +100-5000 gold (one-time)
Level up:                  +250 gold
Rank up (ladder):          +500 gold
```

**Premium Multiplier:**
- Free: 1.0x gold
- Premium: 1.5x gold
- Premium+: 2.0x gold

**Example earnings:**
```
Free player (10 battles/day):
Base: 10 × 50 = 500 gold
Daily bonus: 100 gold
Total: 600 gold/day

Premium player (unlimited):
Base: 20 × 50 = 1000 gold
Premium bonus: 1000 × 0.5 = 500 gold
Daily bonus: 100 gold
Total: 1,600 gold/day
```

### What Gold Buys

**Unit Unlocks:**
```
Common units:     500 gold each
Uncommon units:   1,000 gold each
Rare units:       2,500 gold each
Epic units:       5,000 gold each
Legendary units:  10,000 gold each
```

**Cosmetics:**
```
Unit Skins:
├── Recolors:         500 gold
├── Themed:           1,000 gold
├── Animated:         2,500 gold
└── Legendary:        5,000 gold

Titles:
├── Common:           Free (earned)
├── Uncommon:         1,000 gold
├── Rare:             2,500 gold
└── Epic:             5,000 gold

Battle Themes:
├── Simple:           1,500 gold
├── Themed:           3,000 gold
└── Premium:          5,000 gold

Profile Customization:
├── Borders:          500 gold
├── Backgrounds:      1,000 gold
└── Badges:           1,500 gold
```

**Guild Contributions:**
- Donate to guild treasury (optional)
- No direct benefit, shows commitment
- Guild can spend on upgrades

### Progression Systems

**1. Player Level (Experience)**

```
Experience sources:
- Win a battle: +100 XP
- Lose a battle: +25 XP
- Complete challenge: +500 XP
- Guild raid: +250 XP
- Achievement: +100-1000 XP

Level progression:
Level 1 → 2: 500 XP
Level 2 → 3: 750 XP
Level 3 → 4: 1,000 XP
...
Formula: baseXP × (level × 1.5)

Level rewards:
Every level: +250 gold
Every 5 levels: +1 team slot
Every 10 levels: Unlock cosmetic title
Level 50: Unlock "Master" title
Level 100: Unlock legendary cosmetic
```

**2. Ranked Ladder (Rating)**

```
ELO-style system:
- Win: +15-30 rating (based on opponent)
- Loss: -10-20 rating

Ranks:
Bronze:     0-999
Silver:     1000-1499
Gold:       1500-1999
Platinum:   2000-2499
Diamond:    2500+

Rank rewards (seasonal):
- Bronze: 500 gold
- Silver: 1,000 gold + title
- Gold: 2,500 gold + title + skin
- Platinum: 5,000 gold + title + skin + badge
- Diamond: 10,000 gold + exclusive cosmetics

Soft reset each season:
New rating = (old rating × 0.8) + 200
```

**3. Unit Mastery**

```
Per-unit progression:
- Use unit in battle: +10 mastery XP
- Win with unit: +25 mastery XP
- MVP performance: +50 mastery XP

Mastery levels (per unit):
Level 1: Unlocked
Level 3: +5% HP
Level 5: Unlock alternate ability
Level 7: +5% Attack
Level 10: Unlock "Mastered" cosmetic

Visual indicator:
★☆☆☆☆ - Level 1-2
★★☆☆☆ - Level 3-4
★★★☆☆ - Level 5-6
★★★★☆ - Level 7-9
★★★★★ - Level 10 (Mastered)
```

**4. Achievements**

```
Categories:

Combat:
├── "First Blood" - Win your first battle
├── "Undefeated" - Win 10 battles in a row
├── "Centurion" - Win 100 battles
└── "Gladiator" - Win 1000 battles

Strategy:
├── "Experimenter" - Try 10 different strategies
├── "Optimizer" - Win with the same strategy 50 times
├── "Theorycrafterr" - Create strategy used by 100 players
└── "Meta Breaker" - Win with bottom-tier units

Social:
├── "Team Player" - Join a guild
├── "Leader" - Create a guild
├── "Raid Champion" - Deal 100K damage to raid bosses
└── "Helpful" - Share 10 strategies

Collection:
├── "Collector" - Unlock 25 units
├── "Master Collector" - Unlock all units
├── "Fashionista" - Own 10 cosmetics
└── "Completionist" - 100% achievements

Secret:
├── ??? (discover through gameplay)
├── ??? (hint: use only healers)
└── ??? (hint: defeat raid boss solo)
```

### Progression Feel

**Week 1:**
- Player levels up 5-10 times (feels fast)
- Unlocks 3-5 new units
- Earns 5,000-10,000 gold
- Reaches Silver rank
- Feels: "I'm making progress!"

**Month 1:**
- Player level 25-30
- Unlocked 15-20 units
- Earned 50,000+ gold
- Reached Gold rank
- Multiple mastered units
- Joined guild
- Feels: "I'm invested in this"

**Month 6:**
- Player level 60-80
- All units unlocked
- 200,000+ gold earned
- Platinum+ rank
- Multiple legendary cosmetics
- Guild officer or leader
- Feels: "I'm a veteran"

**Goal:** Always something to work toward, never "finished"

---

## Implementation Roadmap

### Phase 1: Core API (Complete) ✅

**Status:** Done
- Battle system working
- Authentication
- Basic teams/roster
- Leaderboard
- Custom API docs framework

**Next:** Phase 2

---

### Phase 2: Web UI + Monetization (Current Priority)

**Timeline:** 1-2 weeks

**Tasks:**
- [ ] Add Stripe integration
- [ ] Subscription management pages
- [ ] Pricing tiers (Free, Premium, Premium+)
- [ ] Account dashboard
- [ ] Payment methods
- [ ] Billing history

**Deliverables:**
- Users can subscribe to Premium/Premium+
- Stripe webhooks handle subscriptions
- Tier-based API access enforced
- Revenue tracking starts

**Blockers:** None (ready to implement)

---

### Phase 3: Guilds + Social (Next)

**Timeline:** 2-3 weeks

**Tasks:**

**Week 1: Guild Foundation**
- [ ] Guild database models
- [ ] Role-based permissions system
- [ ] Guild API endpoints (CRUD)
- [ ] Guild membership management
- [ ] Treasury system

**Week 2: Guild Features**
- [ ] Raid boss system
- [ ] Shared strategy library
- [ ] Guild chat/messaging
- [ ] Contribution tracking
- [ ] Leaderboards (guild-level)

**Week 3: Polish + Test**
- [ ] Guild upgrades (treasury spending)
- [ ] Discord webhooks
- [ ] Notifications system
- [ ] Admin tools for guild management
- [ ] Load testing (1000 concurrent guild members)

**Deliverables:**
- Functional guild system
- Raid bosses spawn weekly
- Strategy sharing works
- Players can collaborate async

**Blockers:** Need Phase 2 complete (premium tier gates guild creation)

---

### Phase 4: Engagement Features (After Guilds)

**Timeline:** 2-3 weeks

**Tasks:**

**Week 1: Progression**
- [ ] Unit mastery system
- [ ] Achievement system
- [ ] Title system
- [ ] Seasonal ladder
- [ ] Gold/currency system

**Week 2: Content Variety**
- [ ] Environmental modifiers (weekly rotation)
- [ ] Daily challenges (personalized)
- [ ] Strategy marketplace
- [ ] Replay system
- [ ] Battle simulation endpoint

**Week 3: Polish**
- [ ] Custom API docs improvements
- [ ] Interactive replay viewer
- [ ] Dashboard visualizations
- [ ] Strategy decay system
- [ ] Admin analytics dashboard

**Deliverables:**
- Players have clear progression paths
- Weekly content changes (modifiers)
- Meta stays fresh
- Engagement loops complete

**Blockers:** Guilds should be stable first

---

### Phase 5: Premium+ Features (Polish)

**Timeline:** 2-4 weeks

**Tasks:**

**Week 1-2: Scripting Engine**
- [ ] Lua runtime integration
- [ ] Sandboxed execution environment
- [ ] Script validation
- [ ] Script storage/versioning
- [ ] Script debugging tools
- [ ] Rate limiting for scripts

**Week 3: WebSockets**
- [ ] WebSocket server
- [ ] Real-time battle updates
- [ ] Guild notifications
- [ ] Live leaderboard updates
- [ ] Connection management

**Week 4: Advanced Features**
- [ ] Batch operations API
- [ ] Advanced analytics
- [ ] Higher rate limits
- [ ] Custom dashboards
- [ ] Export tools

**Deliverables:**
- Premium+ tier fully functional
- Scripting engine stable
- WebSockets working
- Clear value for $10-15/month

**Blockers:** All previous phases stable

---

### Phase 6: Launch Preparation

**Timeline:** 1-2 weeks

**Tasks:**
- [ ] Performance optimization
- [ ] Security audit
- [ ] Load testing (10K+ concurrent users)
- [ ] Error monitoring (Sentry or similar)
- [ ] Backup/recovery procedures
- [ ] Documentation polish
- [ ] Marketing materials ready
- [ ] Support channels set up (Discord, email)

**Deliverables:**
- Production-ready infrastructure
- Can handle launch traffic
- Support systems in place
- Marketing ready to go

---

### Launch Timeline

**Soft Launch (Month 3):**
- Friends & family
- Beta testers
- Fix critical bugs
- Gather feedback
- 50-100 users

**Public Launch (Month 4):**
- Product Hunt
- Hacker News
- Reddit
- Dev.to articles
- 1,000-5,000 users

**Growth (Month 5-6):**
- University partnerships
- Content marketing
- Community tournaments
- 5,000-10,000 users

**Target (Month 12):**
- 20,000+ registered users
- $10K MRR
- Profitable
- Self-sustaining

---

## Success Metrics

### Short-Term (Month 1-3)

**User Growth:**
- ✅ 1,000 registered users
- ✅ 200 DAU
- ✅ 40% 7-day retention

**Engagement:**
- ✅ 50% of users join guilds
- ✅ 20 battles/user/week average
- ✅ 10 custom clients built by community

**Monetization:**
- ✅ 100 premium subscribers
- ✅ $500 MRR
- ✅ 10% conversion rate

**Community:**
- ✅ 200 Discord members
- ✅ 10 strategy marketplace submissions
- ✅ 5 community-run tournaments

---

### Medium-Term (Month 6)

**User Growth:**
- ✅ 10,000 registered users
- ✅ 1,500 DAU
- ✅ 35% 30-day retention

**Engagement:**
- ✅ 70% of users in guilds
- ✅ 100 active guilds
- ✅ 50+ raid bosses defeated/week
- ✅ Meta diversity score > 0.70

**Monetization:**
- ✅ 1,500 premium subscribers
- ✅ $10,000 MRR
- ✅ 15% conversion rate
- ✅ <5% monthly churn

**Community:**
- ✅ 1,000 Discord members
- ✅ 100+ strategies in marketplace
- ✅ 10 universities using for teaching
- ✅ Featured in 1 major tech publication

---

### Long-Term (Year 1)

**User Growth:**
- ✅ 25,000+ registered users
- ✅ 3,000+ DAU
- ✅ 30% 30-day retention

**Engagement:**
- ✅ 200+ active guilds
- ✅ 500+ raid bosses defeated
- ✅ Healthy meta (no dominant strategy)
- ✅ 500+ community strategies

**Monetization:**
- ✅ 4,000+ premium subscribers
- ✅ $30,000+ MRR ($360K annual)
- ✅ Break-even or profitable
- ✅ Multiple revenue streams

**Community:**
- ✅ 2,000+ Discord members
- ✅ 20+ universities partnered
- ✅ Regular community tournaments
- ✅ Active developer ecosystem

**Business:**
- ✅ Can hire part-time help if needed
- ✅ Sustainable as side project
- ✅ Portfolio piece for consulting
- ✅ Proven API-first product model

---

## Appendix: Key Takeaways

### Design Philosophy
- **Respect player time** - Async-first, no forced login
- **Developer-centric value** - Premium = better tools, not power
- **Social-first engagement** - Guilds are the retention hook
- **Clean monetization** - One price, clear value, no FOMO

### What Makes This Different
- API-native (not web-first)
- Collaboration over competition
- Scripting as endgame (not just consumption)
- Educational use cases built-in
- Non-exploitative monetization

### Critical Success Factors
1. **Guild system must work well** - This is the retention engine
2. **Weekly content rotation** - Prevents stale meta
3. **Premium value must be clear** - Tools developers actually want
4. **Docs must be beautiful** - First impression matters
5. **Community must feel valued** - Players create content, not you

### Risks to Mitigate
- **Meta stagnation** → Weekly modifiers, strategy decay
- **Guild inactivity** → Async design, no forced timing
- **Churn** → Multiple progression systems, social hooks
- **Pay-to-win perception** → Cosmetics only, tools not power
- **Complexity creep** → Keep API simple, add features carefully

### Next Steps
1. Complete Phase 2 (Web UI + Stripe)
2. Implement Phase 3 (Guilds)
3. Test with small group (50-100 users)
4. Iterate based on feedback
5. Public launch (Product Hunt, HN)

---

**This is the roadmap. Let's build it.** 🚀

---

*Document Version: 1.0*  
*Last Updated: February 11, 2026*  
*Prepared by: Mark @ Learned Geek Consulting*
