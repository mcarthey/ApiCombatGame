# API Combat Game — Strategic Roadmap & Monetization Plan
## Learned Geek LLC | Passive Income Product Strategy

---

## Executive Summary

API Combat Game is a unique product in an underserved niche: **gamified API education**. Players learn to build HTTP clients, handle authentication, and think programmatically — while competing in a combat strategy game where the API *is* the controller. This positions it perfectly at the intersection of **developer education**, **competitive gaming**, and **SaaS monetization**.

The core insight: developers will pay for something that's both fun and makes them better at their job. Every coding bootcamp graduate, every junior dev, every API integration engineer is a potential customer.

---

## Current State Assessment

### What's Built (Solid Foundation)
- **25 template units** across 5 classes (Warrior, Mage, Ranger, Healer, Tank) with 200-600g unlock costs
- **71 API endpoints** across 12 controller groups with full OpenAPI spec
- **3 subscription tiers**: Free (10 battles/day), Premium (unlimited), Premium Plus (early access)
- **Stripe integration** with webhook handling for subscription billing
- **Elo matchmaking** with 1000 starting rating
- **7 engagement systems**: login streaks, daily challenges, achievements, mastery, win streaks, guild bosses, weekly modifiers
- **Strategy marketplace** for community-created battle configs
- **Guild system** with chat, roles, invites, shared strategies, boss raids
- **Battle replay** system with shareable URLs
- **Admin dashboard** with player management, meta analytics, and tools
- **306 passing tests** with strong integration coverage

### What's Missing (Opportunities)
- No real-time battle visualization
- No social/competitive features beyond leaderboards
- No seasonal/event content cadence
- No community features (forums, Discord integration)
- No tutorial/onboarding flow for new players
- No cosmetic/vanity monetization layer
- No referral or viral growth mechanics
- No mobile experience
- No AI opponent for practice/learning
- Limited Premium Plus differentiation

---

## The Dopamine Architecture

The most addictive games layer **five psychological reward loops** at different time scales. Here's how to engineer each one:

### Loop 1: Immediate Feedback (seconds)
> *"I did something, I see a result"*

- **Battle animations/visualization** — Even text-based, show turn-by-turn combat unfolding with damage numbers, crits, ability names
- **Sound design** — Victory fanfares, level-up chimes, coin sounds (even subtle browser audio)
- **Micro-celebrations** — Confetti on achievements, screen shake on crits, gold counter incrementing visibly
- **API response enrichment** — Battle results should include narrative flavor text: *"Your Archmage's Lightning Storm decimated the enemy formation!"*

### Loop 2: Session Goals (minutes)
> *"One more battle to finish this challenge"*

- **Daily challenge system** (EXISTS — enhance with variety)
- **"Just one more" hooks**: Show next reward threshold after each battle
- **Quick Match vs Ranked** — Quick match has no daily limit, lower rewards; Ranked is where tiers matter
- **Battle Pass progression** — Visual progress bar always showing next reward

### Loop 3: Daily Rituals (hours)
> *"I need to log in today or I lose my streak"*

- **Login streak** (EXISTS — 7-day cycle, 25g-200g)
- **Daily first-battle bonus** (EXISTS — 100g)
- **Daily challenges** (EXISTS — enhance with 3 difficulty tiers)
- **Guild daily contribution** — Donate to guild treasury for shared rewards
- **"Revenge" queue** — Get notified when someone who beat you is online; rematch for bonus rewards

### Loop 4: Weekly/Seasonal Arc (days-weeks)
> *"This season I'm pushing for Diamond"*

- **Ranked seasons** (4-8 weeks) with tier resets and exclusive rewards
- **Weekly modifiers** (EXISTS — make them more impactful)
- **Weekend events** — Double XP, special boss raids, limited-time units
- **Battle Pass** — Free track + Premium track, 30 levels per season
- **Tournaments** — Weekly automated brackets with prize pools

### Loop 5: Long-term Investment (weeks-months)
> *"I've built something here, I can't leave"*

- **Unit mastery** (EXISTS — make it more visible and rewarding)
- **Achievement hunting** (EXISTS — add rare/legendary tiers)
- **Guild progression** — Guild levels, unlockable perks, guild wars
- **Collection completion** — "Gotta catch 'em all" for units, titles, cosmetics
- **Elo prestige** — Permanent badge for reaching certain ratings

---

## Monetization Strategy

### Tier 1: Subscription Revenue (Recurring)

| Feature | Free | Premium ($7.99/mo) | Premium Plus ($14.99/mo) |
|---|---|---|---|
| Daily battles | 10 | Unlimited | Unlimited |
| Team slots | 3 | 10 | 15 |
| Matchmaking | Standard | Priority | Priority + Ranked |
| Gold multiplier | 1.0x | 1.5x | 2.0x |
| XP multiplier | 1.0x | 1.25x | 1.5x |
| Guild creation | No | Yes | Yes |
| Strategy marketplace | Browse only | Buy + Sell | Buy + Sell + Featured |
| Battle Pass | Free track | Free + Premium track | Free + Premium + Bonus track |
| Replay storage | 5 replays | 50 replays | Unlimited |
| API rate limit | 60/min | 300/min | 600/min |
| Custom API keys | 1 | 5 | 10 |
| Early access units | No | No | Yes (1 week early) |
| Exclusive cosmetics | No | Monthly badge | Monthly badge + title |

**Target**: 3-5% free-to-premium conversion at $7.99/mo = $800-1,300/mo per 1,000 MAU

### Tier 2: Battle Pass (Seasonal Revenue)

A **30-level seasonal Battle Pass** ($4.99 per season, ~8 weeks):
- **Free track**: Basic currency, XP boosters, 1 common unit skin
- **Premium track** (included with Premium Plus, or $4.99 standalone): Exclusive units, rare titles, cosmetic card backs, profile borders, animated avatars
- **Completion bonus**: Legendary title + unique unit variant

Players who buy the Battle Pass play 4.5x more frequently (industry average). It creates urgency: *"Season ends in 12 days and I'm only level 22!"*

### Tier 3: Cosmetic Store (Impulse Revenue)

Non-gameplay-affecting items purchasable with **Gems** (premium currency):
- **Unit skins** — Visual variants (Frost Archmage, Shadow Knight)
- **Profile customization** — Borders, backgrounds, animated avatars
- **Battle effects** — Custom victory screens, damage number styles
- **Titles** — Display titles below username ("The Unstoppable", "API Whisperer")
- **Emotes** — Post-battle emotes visible in replays

**Gem pricing**: $0.99 = 100 gems, $4.99 = 550 gems, $9.99 = 1200 gems
Small amounts earnable for free through achievements and events.

### Tier 4: Educational Licensing (B2B Revenue)

This is the **Learned Geek LLC** differentiator — sell to coding bootcamps and corporate training:
- **Classroom Edition** ($49/mo per 30 seats): Instructor dashboard, student progress tracking, curriculum-aligned challenges
- **Enterprise API Training** ($199/mo per 100 seats): Custom API scenarios, team exercises, completion certificates
- **Self-paced Course Integration**: Embed game challenges in LMS platforms via LTI

**This is the sleeper revenue stream.** A single bootcamp contract = 10-50x a consumer subscription.

### Revenue Projection (Conservative, Year 1)

| Source | Month 6 | Month 12 |
|---|---|---|
| Subscriptions (500 → 2,000 MAU) | $400/mo | $1,600/mo |
| Battle Pass (quarterly) | $200/mo | $800/mo |
| Cosmetic Store | $100/mo | $500/mo |
| Educational Licensing | $0/mo | $500/mo |
| **Total** | **$700/mo** | **$3,400/mo** |

---

## Implementation Roadmap

### Phase 1: Core Polish & Addiction Loops (Weeks 1-4)
*Goal: Make the existing game irresistible to play daily*

1. **Interactive Onboarding Tutorial**
   - Guided first-battle flow with an AI opponent (no matchmaking needed)
   - Step-by-step: register → get token → browse units → unlock → build team → fight
   - Reward: exclusive "First Steps" title + bonus 500g

2. **Battle Visualization Page**
   - `/battle/{id}/watch` — Animated turn-by-turn replay with damage numbers
   - Auto-scroll combat log with unit portraits, HP bars, ability icons
   - Even a simple text-based "typewriter" effect creates engagement

3. **Ranked Seasons System**
   - Add `Season` model: Bronze → Silver → Gold → Platinum → Diamond → Legend
   - Rating thresholds: 800/1000/1200/1400/1600/1800+
   - Season duration: 8 weeks, soft reset (rating compressed toward 1000)
   - End-of-season rewards: exclusive title + currency + cosmetic per tier reached

4. **Enhanced Daily Challenges**
   - 3 tiers: Easy (1 battle), Medium (3 battles + condition), Hard (5 battles + restriction)
   - Scaling rewards: 50g/100g/250g + XP
   - "Challenge refresh" for Premium: reroll one challenge per day

5. **Notification System Enhancement**
   - Push-style notifications: "Your guild boss is at 10% HP!", "Someone beat your leaderboard rank!"
   - Email digest (weekly): streak status, rank changes, guild activity
   - "Revenge available" notifications when a player who beat you is active

### Phase 2: Social & Competitive (Weeks 5-8)
*Goal: Make players bring their friends*

6. **Referral System**
   - Unique referral code per player
   - Referrer gets 500g + 1 day Premium trial per signup
   - Referred player gets bonus 300g starting currency
   - "Referral leaderboard" with monthly prizes

7. **Guild Wars**
   - Guild vs Guild weekly matchups (automatic based on guild rating)
   - Each member's best battle counts toward guild score
   - Winning guild gets treasury bonus + exclusive guild badge
   - Creates organic coordination and guild chat activity

8. **Live Leaderboard & Spectating**
   - Top 100 leaderboard with sparkline rating graphs
   - "Watch" button on top players — see their recent battles
   - "Battle of the Day" — auto-featured exciting close match

9. **Discord Integration**
   - **Account Linking (OAuth2)**: "Link Discord" button on Settings page → Discord OAuth2 flow → store `DiscordId` on Player model → maps Discord user to API Combat player
   - **Automatic Tier Role Sync**: Discord bot with "Manage Roles" permission. When a player's rating crosses a tier threshold after a battle, bot calls Discord API to swap their role (Rubber Duck → Copy Pasta → ... → I Use Arch btw)
   - **Bot Commands**: `/rank`, `/challenge @player`, `/guild-status`, `/leaderboard`
   - **Battle result notifications** posted to guild Discord channel via webhook
   - **Discord role sync** with both rating tier and subscription tier
   - Server: https://discord.gg (invite link TBD) — channels, roles, and welcome embed already configured

10. **Tournament System**
    - Weekly auto-brackets: 16/32/64 player single elimination
    - Entry: free (1 per week) or Premium (unlimited)
    - Prize pool: currency + exclusive tournament winner title
    - Admin can create special event tournaments

### Phase 3: Monetization & Content (Weeks 9-12)
*Goal: Turn engagement into revenue*

11. **Battle Pass Implementation**
    - 30-level track with free + premium paths
    - XP earned from battles, challenges, and daily activities
    - Premium track rewards: exclusive unit variants, titles, profile cosmetics
    - "Catch-up" mechanic: bonus XP for late starters

12. **Cosmetic System**
    - `Gems` premium currency with Stripe purchase flow
    - Unit skins (visual-only name/icon variants)
    - Profile borders, backgrounds, animated avatars
    - Battle effects and victory screens
    - Seasonal limited-time cosmetics (FOMO driver)

13. **6th Unit Class: Assassin**
    - High attack, high speed, low HP — glass cannon
    - Unique mechanic: "Stealth" (50% dodge for first 2 turns)
    - 5 new units (200-600g), shakes up meta completely
    - Premium Plus gets 1-week early access

14. **Premium Plus Differentiation**
    - 2.0x gold multiplier (up from 1.5x)
    - Exclusive monthly unit variant
    - Premium Battle Pass track included
    - "Creator" badge on marketplace strategies
    - Priority guild boss damage slot

15. **Currency Sink Expansion**
    - Unit stat reroll: 500g to randomize one stat within class range
    - Name customization: 200g to rename a unit
    - "Golden" unit upgrade: 2000g for a permanent +5% stat boost (once per unit)
    - Guild treasury funding for shared perks

### Phase 4: Scale & Ecosystem (Weeks 13-20)
*Goal: Build network effects and B2B revenue*

16. **Mobile-Responsive PWA**
    - Responsive redesign of all pages for mobile
    - Add to homescreen (PWA manifest)
    - Push notifications via service worker
    - Touch-optimized battle viewer

17. **SDK & Client Libraries**
    - Official `npm` package: `@learnedgeek/api-combat-sdk`
    - Official Python package: `pip install api-combat`
    - Starter templates in JS, Python, C#, Go
    - "Build Your Bot" tutorial series driving organic SEO

18. **Educational Platform**
    - Instructor dashboard: create custom challenges, track student progress
    - Curriculum modules: "API Authentication 101", "RESTful Design Patterns"
    - LTI integration for Canvas/Blackboard
    - Completion certificates (PDF generation)

19. **AI Practice Opponents**
    - Difficulty levels: Novice, Intermediate, Expert
    - AI uses different strategies per difficulty
    - No rating impact, reduced rewards (50%)
    - Perfect for learning and testing strategies without pressure

20. **Content Creator Program**
    - Revenue share on marketplace strategies (70/30 split)
    - "Verified Creator" badge for top strategy makers
    - Monthly spotlight of best community strategies
    - YouTube/Twitch integration for replay sharing

---

## Engagement Mechanics Deep Dive

### The "Variable Ratio Reinforcement" Framework

The most addictive mechanic in gaming is **unpredictable rewards**. Implement these:

1. **Loot Drops After Battles**
   - Random chance (15%) of a bonus item after any battle
   - Items: currency pack (50-500g), XP boost (2x for 1hr), rare title, cosmetic shard
   - Higher chance on win streaks (15% → 25% at 3+ streak)
   - Premium gets guaranteed drop every 5th battle

2. **Critical Gold Events**
   - 5% chance any battle rewards 3x gold
   - Screen goes gold, special animation, "JACKPOT!" text
   - Players chase the dopamine of the unexpected big win

3. **Mystery Box (Weekly)**
   - Free: 1 mystery box per week
   - Premium: 3 per week
   - Contains random: currency, XP boost, cosmetic shard, rare unit unlock discount
   - "Guaranteed epic every 10 boxes" pity system

### The Social Pressure Architecture

1. **Activity Feed** — "PlayerX just reached Diamond!", "GuildY defeated the Dragon Boss!"
2. **Rival System** — Auto-assign a rival at similar rating; bonus rewards for beating them
3. **Guild Pressure** — "Your guild needs 3 more boss attempts today to defeat the boss before it expires!"
4. **Loss Aversion** — "Your Diamond rank decays in 3 days without a battle!"
5. **Sunk Cost** — Show total playtime, battles fought, gold earned on profile (makes leaving feel wasteful)

### The "Streaker" System

Layer multiple streaks to create daily obligation:

| Streak | Reward Per Day | Bonus at Milestone |
|---|---|---|
| Login Streak (EXISTS) | 25-200g over 7 days | Day 7: mystery box |
| Battle Streak (NEW) | +10% gold per consecutive day battled | Day 14: rare title |
| Win Streak (EXISTS) | +5% gold per consecutive win | 5 wins: loot drop |
| Challenge Streak (NEW) | Bonus XP for completing daily challenge N days in a row | Day 7: challenge refresh token |
| Guild Activity Streak (NEW) | Guild XP for daily contribution | Day 30: exclusive guild banner |

Breaking ANY streak feels painful. Maintaining all of them becomes a daily ritual.

---

## Technical Architecture Changes

### New Database Models Needed
```
Season              — id, name, startDate, endDate, isActive
PlayerSeasonRank    — playerId, seasonId, tier, peakRating, gamesPlayed
BattlePass          — id, seasonId, levels[], premiumLevels[]
BattlePassProgress  — playerId, battlePassId, currentLevel, xpEarned
Cosmetic            — id, name, type(skin/border/title/effect), rarity, gemCost
PlayerCosmetic      — playerId, cosmeticId, equippedSlot
GemTransaction      — playerId, amount, type(purchase/earn/spend), stripeId
Tournament          — id, name, bracketSize, startDate, entries[]
TournamentMatch     — tournamentId, round, player1Id, player2Id, winnerId
Referral            — referrerId, referredPlayerId, rewardClaimed
RivalAssignment     — playerId, rivalId, seasonId, winsAgainst, lossesAgainst
```

### New API Endpoints Needed
```
POST   /api/v1/battle/quick-match          — Unranked, no daily limit
GET    /api/v1/season/current               — Current season info + player rank
GET    /api/v1/season/leaderboard           — Season-specific rankings
GET    /api/v1/battlepass/progress           — Current battle pass level
POST   /api/v1/battlepass/claim/{level}      — Claim battle pass reward
GET    /api/v1/cosmetics/store               — Available cosmetics
POST   /api/v1/cosmetics/purchase            — Buy with gems
POST   /api/v1/cosmetics/equip               — Equip a cosmetic
GET    /api/v1/player/rival                  — Current rival info
POST   /api/v1/tournament/enter              — Enter weekly tournament
GET    /api/v1/tournament/bracket             — View tournament bracket
POST   /api/v1/referral/generate              — Generate referral code
POST   /api/v1/gems/purchase                  — Stripe checkout for gems
GET    /api/v1/ai/opponents                   — List AI opponents
POST   /api/v1/battle/practice                — Fight AI opponent
```

### Infrastructure Priorities
1. **Move to PostgreSQL** — In-memory/SQLite won't scale; Postgres is free on Railway/Render
2. **Add Redis** — Session cache, rate limiting, real-time leaderboard
3. **Background job processor** — Hangfire for season resets, tournament processing, email digests
4. **CDN for static assets** — Cloudflare (free tier) for global performance
5. **Monitoring** — Application Insights or Sentry for error tracking

---

## Marketing & Growth Strategy

### Launch Channels (Cost: $0-50/mo)
1. **Hacker News** — "Show HN: I built a competitive game where the API is the controller" (this *will* hit front page)
2. **Reddit** — r/programming, r/webdev, r/learnprogramming, r/gamedev
3. **Dev.to / Hashnode** — "How I Built a Game You Play with curl Commands"
4. **Twitter/X** — Developer community, coding influencers
5. **YouTube** — "I Built an API Game" video, tutorial series
6. **Product Hunt** — Launch day coordinated with HN post

### Organic SEO Content
- "Learn REST APIs by Playing a Game"
- "API Authentication Tutorial — The Fun Way"
- "Best API Practice Projects for Junior Developers"
- "How Elo Rating Works — Build It Yourself"

### Community Building
- **Discord server** with channels: #general, #strategies, #guild-recruitment, #bug-reports, #feature-requests
- **Weekly dev blog** — patch notes, meta analysis, featured strategies
- **Monthly tournament** live-streamed on YouTube/Twitch
- **"API Warrior of the Month"** spotlight with interview

### Viral Mechanics Built Into the Product
- Battle replay sharing (shareable URL, open graph preview)
- "I just reached Diamond in API Combat!" share buttons
- Guild recruitment posts with auto-generated guild cards
- Referral system with tangible in-game rewards

---

## Success Metrics

| Metric | Month 3 | Month 6 | Month 12 |
|---|---|---|---|
| Registered players | 500 | 2,000 | 8,000 |
| Monthly Active Users | 200 | 800 | 3,000 |
| Daily Active Users | 50 | 200 | 800 |
| DAU/MAU ratio | 25% | 25% | 27% |
| Paid subscribers | 10 | 50 | 200 |
| Monthly revenue | $100 | $700 | $3,400 |
| Avg session length | 8 min | 12 min | 15 min |
| D7 retention | 20% | 30% | 40% |
| Guild participation | 10% | 25% | 40% |

---

## Priority Stack Rank (What to Build Next)

If I had to pick the **top 5 highest-impact items** to build right now:

1. **Interactive Onboarding + AI Opponent** — Without this, new players bounce. The #1 retention killer is confusion at signup.
2. **Ranked Seasons** — Creates the long-term "why" for playing. Everything else feeds into climbing the ladder.
3. **Battle Visualization Page** — Battles are invisible right now. Seeing your units fight is the core fantasy.
4. **Battle Pass** — Proven monetization that also drives daily engagement. Low dev cost, high revenue impact.
5. **Hacker News Launch Post** — Free distribution to your exact target audience. Time it with onboarding polish.

Everything else (guilds wars, cosmetics, mobile, B2B) layers on top of these five pillars.

---

*Document prepared for Learned Geek LLC — API Combat Game strategic planning*
*Last updated: February 2026*
