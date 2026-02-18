# Manual Testing Checklist

This document provides a step-by-step manual testing checklist for the API Combat Game.
Use a tool like **curl**, **Postman**, or the built-in Swagger UI at `/swagger` to execute requests.

> **Base URL:** `http://localhost:5000` (or `https://localhost:5001`)
> **Auth:** Most endpoints require a `Bearer` token in the `Authorization` header.

---

## Prerequisites

1. Start the application: `dotnet run --project ApiCombatGame`
2. Verify the health check: `GET /health` returns `200 OK`
3. Open Swagger UI at `/swagger` for interactive testing

---

## 1. Authentication

### 1.1 Registration
- [ ] `POST /api/v1/auth/register` with valid username/email/password returns `201 Created`
- [ ] Response contains `token`, `playerId`, and `username`
- [ ] Duplicate username returns `409 Conflict`
- [ ] Duplicate email returns `409 Conflict`
- [ ] Missing required fields returns `400 Bad Request`

### 1.2 Login
- [ ] `POST /api/v1/auth/login` with valid credentials returns `200 OK` with token
- [ ] Invalid password returns `401 Unauthorized`
- [ ] Non-existent username returns `401 Unauthorized`

### 1.3 Token Refresh
- [ ] `POST /api/v1/auth/refresh` with valid token returns new token
- [ ] Expired/invalid token returns `401 Unauthorized`

### 1.4 Protected Endpoints
- [ ] Any protected endpoint without `Authorization` header returns `401 Unauthorized`
- [ ] Valid `Bearer <token>` grants access

---

## 2. Player Profile & Roster

### 2.1 Profile
- [ ] `GET /api/v1/player/profile` returns player data with all fields:
  - `id`, `username`, `email`, `level`, `experiencePoints`, `xpToNextLevel`
  - `currency` (default 1000), `rating` (default 1000), `winStreak` (default 0)
  - `tier` (default "Free"), `achievementPoints`, `achievementsUnlocked`
  - `guild` (null for new player), `rosterCount`, `teamCount`, `createdAt`, `lastLoginAt`

### 2.2 Roster
- [ ] `GET /api/v1/player/roster` returns starter units (3 units for new player)
- [ ] Each unit has `id`, `name`, `class`, `attack`, `defense`, `speed`, `hp`, `abilities`
- [ ] `GET /api/v1/player/roster/available` returns unit shop listing
- [ ] `POST /api/v1/player/roster/unlock` with valid template ID and sufficient currency unlocks a unit
- [ ] Unlocking with insufficient currency returns `400 Bad Request`
- [ ] Unlocking an already-owned unit returns `400 Bad Request`

---

## 3. Team Management

### 3.1 Create Team
- [ ] `POST /api/v1/team/configure` with valid unit IDs returns `201 Created`
  ```json
  {
    "name": "My Team",
    "unitIds": ["<unit-id-1>", "<unit-id-2>", "<unit-id-3>"],
    "strategy": {
      "formation": "aggressive",
      "targetPriority": ["lowest_hp"]
    }
  }
  ```
- [ ] Response contains `id`, `name`, `units`, `strategy`
- [ ] Creating with >5 units returns `400 Bad Request`
- [ ] Creating with unowned unit IDs returns `400 Bad Request`
- [ ] Free tier: max 3 team slots enforced

### 3.2 Read / List
- [ ] `GET /api/v1/team/{teamId}` returns the team
- [ ] `GET /api/v1/team/list` returns all player teams
- [ ] Accessing another player's team returns `404 Not Found`

### 3.3 Update
- [ ] `PUT /api/v1/team/{teamId}` with new name/units updates the team
- [ ] Returns `200 OK` with updated team data

### 3.4 Delete
- [ ] `DELETE /api/v1/team/{teamId}` returns `204 No Content`
- [ ] Subsequent GET returns `404 Not Found`

---

## 4. Battle System

### 4.1 Queue
- [ ] `POST /api/v1/battle/queue` with valid team returns `201 Created`
  ```json
  {
    "teamId": "<team-id>",
    "mode": "ranked"
  }
  ```
- [ ] Response contains `battleId` and `status: "queued"`
- [ ] Invalid/non-existent team returns `400 Bad Request`
- [ ] Free tier: 10 battles/day limit enforced (11th returns `403 Forbidden`)

### 4.2 Status & Results
- [ ] `GET /api/v1/battle/status/{battleId}` returns battle status
- [ ] Non-existent battle returns `404 Not Found`
- [ ] `GET /api/v1/battle/results/{battleId}` returns detailed results for completed battles
- [ ] Results include `winner`, `battleLog`, `rewards` (gold, XP, streaks)

### 4.3 History
- [ ] `GET /api/v1/battle/history` returns recent battles
- [ ] Supports `limit` and `offset` query parameters

### 4.4 Reward Verification
- [ ] After a **win**: player currency increases by ~50g base (+ tier multiplier + streak bonus)
- [ ] After a **loss**: player currency increases by ~10g base
- [ ] Win streak increments on consecutive wins, resets on loss
- [ ] First battle of the day grants +100g bonus
- [ ] XP awarded and level-up occurs when XP threshold reached (level-up grants +250g)

---

## 5. Leaderboard

- [ ] `GET /api/v1/leaderboard` returns ranked player list (sorted by rating desc)
- [ ] Each entry has `id`, `username`, `rating`, `level`, `wins`, `losses`
- [ ] `GET /api/v1/leaderboard/player/{playerId}` returns specific player ranking

---

## 6. Daily Challenges

### 6.1 View Challenges
- [ ] `GET /api/v1/challenges/daily` returns active daily challenges
- [ ] Each challenge has `id`, `type`, `description`, `targetValue`, `progress`, `rewardCurrency`, `rewardXp`
- [ ] Challenges refresh daily

### 6.2 Challenge Progress
- [ ] Playing battles increments relevant challenge progress (e.g., win streak challenges)
- [ ] Progress does not exceed `targetValue`

### 6.3 Claim Reward
- [ ] `POST /api/v1/challenges/claim` with completed challenge ID returns reward
  ```json
  { "challengeId": "<challenge-id>" }
  ```
- [ ] Claiming incomplete challenge returns `400 Bad Request`
- [ ] Claiming already-claimed challenge returns `400 Bad Request`
- [ ] Currency and XP are credited to player (verify via profile)

---

## 7. Guild System (Premium Tier Required)

> **Setup:** Register a Premium-tier player or manually update tier in DB for testing.

### 7.1 Create Guild
- [ ] `POST /api/v1/guild/create` as Premium player returns `201 Created`
  ```json
  {
    "name": "Test Guild",
    "tag": "TST",
    "description": "A test guild"
  }
  ```
- [ ] Free tier player gets `403 Forbidden`
- [ ] Player already in a guild gets `400 Bad Request`
- [ ] Duplicate guild name returns `409 Conflict`
- [ ] Tag is uppercased automatically

### 7.2 Guild Info
- [ ] `GET /api/v1/guild/{guildId}` returns guild details
- [ ] `GET /api/v1/guild/mine` returns current player's guild
- [ ] `GET /api/v1/guild/{guildId}/members` lists all members with roles

### 7.3 Invitations
- [ ] Leader/Officer: `POST /api/v1/guild/{guildId}/invite` with `{ "playerId": "<id>" }` creates invite
- [ ] Regular member cannot invite (returns `403`)
- [ ] Target player already in a guild: returns `400 Bad Request`
- [ ] `GET /api/v1/guild/invites` lists pending invites for authenticated player
- [ ] `POST /api/v1/guild/invites/{inviteId}/accept` joins the guild
- [ ] `POST /api/v1/guild/invites/{inviteId}/decline` declines the invite
- [ ] Accepting one invite auto-declines all others

### 7.4 Member Management
- [ ] Leader: `POST /api/v1/guild/{guildId}/kick` with `{ "playerId": "<id>" }` removes member
- [ ] Officer cannot kick equal/higher rank
- [ ] Leader: `POST /api/v1/guild/{guildId}/promote` with `{ "playerId": "<id>", "newRole": "Officer" }`
- [ ] Promoting to Leader transfers leadership (old leader becomes Officer)
- [ ] `POST /api/v1/guild/leave` removes self from guild
- [ ] Leader cannot leave (must transfer leadership or delete guild)

### 7.5 Delete Guild
- [ ] Leader: `DELETE /api/v1/guild/{guildId}` deletes guild and all memberships
- [ ] Non-leader returns `403 Forbidden`

---

## 8. Guild Treasury & Upgrades

### 8.1 Treasury
- [ ] `GET /api/v1/guild/{guildId}/treasury` returns balance and available upgrades
- [ ] `POST /api/v1/guild/{guildId}/treasury/deposit` with `{ "amount": 100 }` transfers personal gold
- [ ] Insufficient personal currency returns `400 Bad Request`
- [ ] Zero or negative amount returns `400 Bad Request`
- [ ] Deposit updates contribution points

### 8.2 Upgrades
- [ ] Leader: `POST /api/v1/guild/{guildId}/treasury/spend` purchases an upgrade
  ```json
  { "upgradeId": "max_members_30" }
  ```
- [ ] Non-leader cannot purchase (returns `403`)
- [ ] Insufficient treasury balance returns `400 Bad Request`
- [ ] Available upgrades: `max_members_30` (50,000g), `max_members_50` (100,000g), `gold_bonus_10` (30,000g), `gold_bonus_20` (60,000g), `raid_attempts_4` (40,000g), `raid_attempts_5` (80,000g)

---

## 9. Guild Chat

- [ ] `POST /api/v1/guild/{guildId}/chat` with `{ "message": "Hello!" }` posts a message
- [ ] Non-member cannot post (returns `403`)
- [ ] Empty message returns `400 Bad Request`
- [ ] Messages over 500 characters are rejected
- [ ] `GET /api/v1/guild/{guildId}/chat?limit=50` returns recent messages (newest first)
- [ ] Supports `before={messageId}` cursor for pagination
- [ ] `@username` mentions generate notifications for mentioned player
- [ ] System messages appear for guild events (join, leave, kick, boss damage)

---

## 10. Guild Strategy Library

- [ ] Officer/Leader: `POST /api/v1/guild/{guildId}/strategies` publishes a strategy
  ```json
  {
    "name": "Rush Strat",
    "description": "Aggressive opening",
    "strategyJson": "{\"formation\":\"aggressive\",\"targetPriority\":[\"lowest_hp\"]}"
  }
  ```
- [ ] `GET /api/v1/guild/{guildId}/strategies` lists all guild strategies
- [ ] Creator or Leader: `PUT /api/v1/guild/{guildId}/strategies/{strategyId}` updates strategy
- [ ] Leader only: `DELETE /api/v1/guild/{guildId}/strategies/{strategyId}` deletes strategy
- [ ] Regular member cannot publish (returns `403`)

---

## 11. Guild Boss Battles

- [ ] `GET /api/v1/guild/boss/current` returns the current boss encounter
- [ ] Response contains `id`, `name`, `currentHp`, `maxHp`, `defense`, `rewardCurrency`
- [ ] `POST /api/v1/guild/boss/attempt` with `{ "teamId": "<id>" }` attacks the boss
- [ ] Damage is calculated and boss HP reduced
- [ ] Daily attempt limit enforced (default 3, upgradeable)
- [ ] Killing blow: boss marked defeated, reward deposited to guild treasury, XP/gold to contributors
- [ ] `GET /api/v1/guild/boss/leaderboard` shows damage rankings for guild members
- [ ] Non-guild-member returns `403`

---

## 12. Achievements

- [ ] `GET /api/v1/player/achievements` returns all achievements with progress
- [ ] Each has `name`, `description`, `progress`, `targetValue`, `isUnlocked`, `achievementPoints`
- [ ] Winning battles increments `battle_won` achievements
- [ ] Joining a guild triggers `guild_joined` achievements
- [ ] Unlocking achievements awards gold and achievement points
- [ ] Achievement points accumulate on player profile

---

## 13. Mastery System

- [ ] `GET /api/v1/mastery/units` returns mastery for all owned units
- [ ] `GET /api/v1/mastery/unit/{unitId}` returns mastery for specific unit
- [ ] Using a unit in battles increases mastery XP
- [ ] Mastery level-up provides stat bonuses to the unit

---

## 14. Environmental Modifiers

- [ ] `GET /api/v1/modifiers/current` returns this week's active modifier (no auth required)
- [ ] Response contains `name`, `description`, `effects`, `startDate`, `endDate`
- [ ] `GET /api/v1/modifiers/upcoming` previews next week's modifier (no auth required)
- [ ] Active modifier affects battle calculations (e.g., "Warrior's Blessing" boosts Warrior damage)

---

## 15. Battle Replays

- [ ] `POST /api/v1/replays/create` with `{ "battleId": "<id>" }` creates a shareable replay
- [ ] Returns `shareUrl` for the replay
- [ ] `GET /api/v1/replays/{shareUrl}` retrieves replay data (no auth required)
- [ ] Replay contains full turn-by-turn battle log

---

## 16. Strategy Marketplace

- [ ] `GET /api/v1/strategies/browse` lists marketplace strategies (no auth, supports pagination)
- [ ] `POST /api/v1/strategies/upload` publishes a strategy
  ```json
  {
    "name": "My Strategy",
    "description": "A great strategy",
    "price": 100,
    "strategyConfig": { "formation": "defensive", "targetPriority": ["highest_attack"] }
  }
  ```
- [ ] `POST /api/v1/strategies/{strategyId}/download` purchases a strategy (deducts currency)
- [ ] `POST /api/v1/strategies/{strategyId}/rate` rates a strategy (1-5)
  ```json
  { "rating": 5 }
  ```

---

## 17. Notifications

### 17.1 Count & List
- [ ] `GET /api/v1/player/notifications/count` returns `{ "unreadCount": N }`
- [ ] `GET /api/v1/player/notifications?page=1&unreadOnly=false` returns paginated notifications
- [ ] New player has 0 unread notifications

### 17.2 Mark Read
- [ ] `POST /api/v1/player/notifications/{notificationId}/read` marks one notification read
- [ ] `POST /api/v1/player/notifications/read-all` marks all as read

### 17.3 Preferences
- [ ] `GET /api/v1/player/notifications/preferences` returns default preferences (all true)
- [ ] `PUT /api/v1/player/notifications/preferences` updates preferences
  ```json
  { "battle": true, "guild": false, "progression": true, "marketplace": true }
  ```
- [ ] Disabled categories suppress those notification types
- [ ] Security/System notifications always delivered regardless of preferences

---

## 18. Rate Limiting

- [ ] Rate limit headers present on all API responses:
  - `X-RateLimit-Limit` (Free: 60, Premium: 120, Premium+: 300)
  - `X-RateLimit-Remaining`
  - `X-RateLimit-Reset`
- [ ] Exceeding limit returns `429 Too Many Requests` with `Retry-After` header

---

## 19. Tier Gating

- [ ] Free tier: max 10 battles/day, max 3 team slots, cannot create guild
- [ ] Premium tier: unlimited battles, 10 team slots, can create guild
- [ ] Premium+ tier: all Premium benefits + higher rate limit

---

## 20. Player Dashboard (`/Dashboard`)

> The player dashboard is the main post-login landing page. It showcases player progress and drives engagement.

### 20.1 Login Streak
- [ ] First visit: streak = 1, "+25g" bonus displayed
- [ ] Second visit same day: no duplicate reward, streak unchanged
- [ ] Next consecutive day: streak increments, correct reward (Day 2: 25g, Day 3: 50g, etc.)
- [ ] Missed a day: streak resets to 1
- [ ] Day 7 reward: 200g, next day resets to Day 1
- [ ] Streak calendar shows 7 circles with claimed/unclaimed states

### 20.2 Player Status Cards
- [ ] Level with XP progress bar displays correctly
- [ ] Currency shows current gold balance
- [ ] Rating shows current Elo rating
- [ ] Achievement points total shown

### 20.3 Achievement Showcase
- [ ] Recent unlocks displayed (up to 3)
- [ ] "Next Up" shows closest achievement to completion with progress bar
- [ ] Link to API docs for achievements endpoint works
- [ ] Empty state shown for new player with no progress

### 20.4 Hero Roster
- [ ] All owned units displayed with class badges and mastery levels
- [ ] Highest mastery unit has gold highlight/crown
- [ ] "X of Y heroes" count shown
- [ ] Affordable unlock prompt shown when player can afford new units
- [ ] API doc link for unlock endpoint works

### 20.5 Daily Challenges
- [ ] Active challenges displayed with progress bars
- [ ] Time remaining countdown shown
- [ ] Completed challenges have green checkmark + claim API link
- [ ] Empty state: "New challenges are on the way!"

### 20.6 Guild Hub
- [ ] If in guild: guild name, tag, role badge, contribution points shown
- [ ] If guild has active boss: HP bar and attack API link shown
- [ ] If NOT in guild: promo card with join/create messaging
- [ ] Free tier sees "Upgrade to Premium" CTA

### 20.7 Battle Status
- [ ] Battles today count shown (with limit for Free tier)
- [ ] Win streak with fire icons displayed
- [ ] First battle bonus reminder shown if haven't battled today
- [ ] "Queue Battle" CTA links to API docs
- [ ] Free tier sees upgrade prompt

### 20.8 Suggested Actions
- [ ] Context-aware action cards appear based on player state
- [ ] "Fight your first battle" shown if no battles today
- [ ] "Go Premium" shown for Free tier players
- [ ] API doc links in each action card work correctly

### 20.9 Navigation
- [ ] Top nav shows "Dashboard" and "Account" as separate links
- [ ] Login redirects to `/Dashboard`
- [ ] Account sidebar shows "Account Overview" (not "Dashboard")
- [ ] Dark mode toggle works on all dashboard sections

---

## 21. Web UI (Razor Pages)

### 21.1 Public Pages
- [ ] `GET /` renders homepage
- [ ] `GET /Privacy` renders privacy page
- [ ] `GET /Auth/Login` renders login form
- [ ] `GET /Auth/Register` renders registration form

### 21.2 Account Pages (requires authentication)
- [ ] `GET /Account` renders account overview
- [ ] `GET /Account/Settings` renders settings form
- [ ] `GET /Account/Subscription` renders subscription management
- [ ] `GET /Account/Billing` renders billing information
- [ ] `GET /Account/ApiKeys` renders API key management

### 21.3 API Documentation
- [ ] `GET /swagger` renders Swagger UI with all endpoints
- [ ] All endpoints include game-specific annotations (tips, examples, difficulty)

---

## 22. Password Reset

### 22.1 Forgot Password
- [ ] `GET /Auth/ForgotPassword` renders email input form
- [ ] Submit valid email shows "check your inbox" success message (no email leak)
- [ ] Submit invalid/nonexistent email shows same success message (no email leak)
- [ ] Reset email received with valid link (check spam folder)

### 22.2 Reset Password
- [ ] Click reset link in email -> `GET /Auth/ResetPassword?token=...` renders password form
- [ ] Submit matching passwords (8+ chars) -> redirects to Login with success message
- [ ] Submit mismatched passwords -> shows error
- [ ] Use expired token (>1 hour) -> shows "expired" error with link to request new one
- [ ] Reuse already-used token -> shows "invalid" error
- [ ] Login with new password succeeds

---

## 23. Account Deletion (GDPR)

- [ ] `GET /Account/Settings` shows "Danger Zone" section at bottom
- [ ] Click "Delete My Account" without password -> shows validation error
- [ ] Enter wrong password -> shows "Incorrect password" error
- [ ] Enter correct password + confirm dialog -> redirects to Login with "deleted" message
- [ ] Try to login with deleted account credentials -> "Invalid email or password"
- [ ] Deletion confirmation email received
- [ ] Deleted player data is anonymized in database (`deleted-*` username/email)

---

## 24. Cookie Consent

- [ ] First visit (incognito): cookie consent banner visible at bottom
- [ ] Click "Got it" -> banner disappears, `cookie_consent` cookie set
- [ ] Subsequent page loads: banner not shown
- [ ] Privacy Policy link in banner works

---

## 25. Email Verification

- [ ] New registration triggers verification email
- [ ] Dashboard shows "email not verified" banner with "Resend Link" button
- [ ] Click verification link in email -> `GET /Auth/VerifyEmail?token=...` shows success
- [ ] Dashboard no longer shows verification banner after verifying
- [ ] "Resend Link" button sends new verification email
- [ ] Invalid/expired token shows error page with dashboard link

---

## 26. Public Leaderboard

- [ ] `GET /Leaderboard` accessible without authentication
- [ ] Shows top 50 non-bot players by rating
- [ ] Top 3 have gold/silver/bronze icons
- [ ] Rating tier badges display correctly
- [ ] Win rate color-coded (green >55%, red <45%)
- [ ] "Leaderboard" nav link visible to all visitors
- [ ] Empty state shown when no players exist

---

## 27. Static Pages

- [ ] `GET /Terms` renders Terms of Service page
- [ ] `GET /About` renders About page with CTAs
- [ ] Footer shows "Terms of Service" link
- [ ] Footer shows "About" link
- [ ] Register page references both Terms and Privacy Policy

---

## 28. Landing Page (Advertising)

- [ ] `GET /Landing` renders with minimal layout (logo only, no nav/footer)
- [ ] `noindex, nofollow` meta tag present
- [ ] Real player count and battle count displayed
- [ ] UTM params preserved in Register CTA link (`?utm_source=...`)
- [ ] CTA buttons link to registration

---

## 29. Favicon

- [ ] Favicon visible in browser tab (blue circle with crossed swords)
- [ ] `<link rel="icon" type="image/svg+xml">` present in page source

---

## Quick Smoke Test Sequence

A minimal end-to-end flow to verify the system works:

1. `GET /health` -> `200 OK`
2. `POST /api/v1/auth/register` -> `201 Created` (save token)
3. `GET /api/v1/player/profile` -> `200 OK` (verify defaults)
4. `GET /api/v1/player/roster` -> `200 OK` (note unit IDs)
5. `POST /api/v1/team/configure` -> `201 Created` (save team ID)
6. `POST /api/v1/battle/queue` -> `201 Created` (save battle ID)
7. `GET /api/v1/battle/status/{battleId}` -> `200 OK`
8. `GET /api/v1/challenges/daily` -> `200 OK`
9. `GET /api/v1/leaderboard` -> `200 OK`
10. `GET /api/v1/player/notifications/count` -> `200 OK` (0 unread)
11. Navigate to `/Dashboard` -> renders player dashboard with all sections
