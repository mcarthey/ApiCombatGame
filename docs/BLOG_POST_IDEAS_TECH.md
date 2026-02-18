# Technical Blog Post Ideas

Deep-dive technical posts about the architecture, patterns, and decisions behind API Combat.
Target audience: developers who build things — the same people who'd play the game.
Publish on: learnedgeek.com (cross-link to apicombat.com where relevant)

---

## 1. Building a Turn-Based Battle Engine in 400 Lines of C#

**Hook:** "No game engine. No Unity. Just a `DeclarativeStrategyEngine` class that resolves entire battles from JSON input."

**Content:**
- The `StrategyConfig` JSON schema — formations, target priorities, conditional ability triggers
- `ResolveBattle()` walkthrough: initiative order by Speed stat, tiebreaker with seeded RNG
- `DetermineAction()` priority chain: Ultimate → ClassAbility → BasicAttack, cooldown tracking
- Condition evaluation: `ally_hp_below_50`, `enemy_count_gte_2`, healer auto-heal fallback
- Damage pipeline: formation bonus (+15% aggressive, -15% defensive incoming) → crit roll (10%, 1.5x) → class advantage triangle (Warrior > Ranger > Mage > Warrior, ±20%) → ability-specific modifiers
- Why deterministic seeding matters — same seed = same replay every time
- Draw resolution: total surviving HP sum, both wiped = draw

**CTA:** "Try writing a strategy JSON and queue a battle at apicombat.com"

---

## 2. One Battle, Ten Service Calls: Fan-Out Without a Message Bus

**Hook:** "When a battle completes, we update Elo, award XP, roll loot, check achievements, update season ranks, process guild wars, advance the battle pass, check rivals, and send notifications. No RabbitMQ. No Kafka. Just try/catch."

**Content:**
- The `UpdateRatingsAndRewards()` cascade in `BattleService`
- Why each service call is wrapped in its own try/catch — a broken loot roll shouldn't kill the match result
- The ordering question: Elo first (it's the source of truth), then progression, then notifications last
- Fire-and-forget email/notification patterns — `_ = Task.Run(...)` for non-critical side effects
- Trade-offs: this works at our scale (hundreds of battles/day), here's where you'd introduce a queue
- Comparison: event-driven vs. orchestrated — we chose orchestrated because debugging is easier when you can set a breakpoint

**Takeaway:** "You don't need infrastructure complexity until you need infrastructure complexity."

---

## 3. 10 Background Services on Shared Hosting (No Hangfire, No Quartz)

**Hook:** "ASP.NET Core's `BackgroundService` base class is all you need for a surprisingly robust job system."

**Content:**
- Tour of all 10 hosted services: battle processor (5s), weekly modifier rotation, daily challenges, strategy decay, guild boss spawns, invite expiry, guild war matching, tournament processing, notification cleanup, admin alerts
- The pattern: `while (!stoppingToken.IsCancellationRequested) { await Task.Delay(...); DoWork(); }`
- Smart scheduling: `WeeklyModifierRotationJob` calculates exact delay to next Monday 00:00 UTC instead of polling
- Scoped service resolution: `IServiceProvider.CreateScope()` inside the loop — why you can't inject `DbContext` directly into a singleton
- The `AdminAlertJob` pattern: monitors queue health (>10 stuck battles >30 min = warning), growth milestones (100 signups/day), expired bosses — deduplicates by checking for unacknowledged alerts in the same category
- Running on SmarterASP.NET shared hosting — the app pool recycle problem and how `IHostApplicationLifetime` handles graceful shutdown
- What we'd change: the 5-second battle processor poll is fine now, but a `Channel<T>` producer-consumer would be cleaner

**Takeaway:** "Hosted services + `Task.Delay` is the right amount of complexity for most side projects."

---

## 4. Dual Authentication in One ASP.NET Core App: JWT for API, Cookies for Web

**Hook:** "The same app serves Razor Pages with cookie auth AND a REST API with JWT. Here's the policy scheme that routes between them."

**Content:**
- The problem: game is played via API (JWT), but the admin dashboard and player settings are Razor Pages (cookies)
- `AddPolicyScheme("JWT_OR_COOKIE")` with `ForwardDefaultSelector` — check for `Bearer` header first, then fall back based on path prefix `/api`
- Cookie config: SameSite=Lax, HttpOnly, 8-hour sliding expiration, 30-day max
- JWT config: `ClockSkew = TimeSpan.Zero` — why you don't want the default 5-minute grace period in a game
- The gotcha: browser JavaScript calling `/api/*` endpoints sends cookies, not JWT — needs explicit `[Authorize(AuthenticationSchemes = "Cookies,Bearer")]`
- Embedding `CurrentTier` as a JWT claim so the rate limiter reads it without a DB call
- `PlayerId` as a custom claim — parsing it in controllers via `User.FindFirst("PlayerId")`

**Takeaway:** "Policy schemes are the cleanest way to serve two auth strategies from one app."

---

## 5. Hand-Rolling Rate Limiting by Subscription Tier (and Why We Skipped the Built-In Middleware)

**Hook:** "ASP.NET 8 shipped `AddRateLimiter()`. We wrote our own anyway. Here's why."

**Content:**
- The requirement: Free (60/min), Premium (120/min), Premium+ (300/min) — tier read from JWT claim, no DB lookup
- `ConcurrentDictionary<string, ClientRateInfo>` with bucket key `"{IP}:{Tier}"` — upgrading resets your bucket
- Thread-safe window reset: `lock` on the `ClientRateInfo` object, not the dictionary
- Response headers on every request: `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
- 429 response body: JSON with `error`, `limit`, `tier`, `retryAfterSeconds` — because developers deserve useful error messages
- Stochastic cleanup: 1% chance per request removes entries older than 5 minutes — no background timer needed
- The `public static bool Enabled` escape hatch for integration tests
- Why we didn't use ASP.NET 8's built-in: it doesn't natively key by JWT claims, and the `PartitionedRateLimiter` API is more complexity than our use case needs

**Takeaway:** "Middleware is just a function. Sometimes the simplest implementation is a dictionary and a lock."

---

## 6. HATEOAS-Lite: Making a REST API Actually Discoverable

**Hook:** "Our API responses include `_links` that tell you what to do next. Not because the spec says so — because it makes the game playable without reading docs."

**Content:**
- The `ApiLink` model: `Href`, `Method`, `Title` (nullable, suppressed when null via `JsonIgnoreCondition`)
- The `Links` static factory: `Links.Get(href, title)`, `Links.Post(...)`, etc.
- Real example: battle result response includes `self`, `replay`, `queue_again`, and conditionally `winner` (only if there's a winner)
- The "follow the chain" philosophy: profile → roster → teams → battle queue, documented in the API docs
- Why `Dictionary<string, ApiLink>` instead of an array — named relations are greppable and self-documenting
- What we skip from full HATEOAS: no media type negotiation, no templated URIs — just enough to be useful
- The developer experience payoff: new players can explore the API by following links without ever opening the docs

**Takeaway:** "HATEOAS doesn't have to be an academic exercise. Even a simple `_links` object makes your API dramatically more usable."

---

## 7. Custom API Docs: Why We Ditched Swagger UI for Razor Pages

**Hook:** "Swagger UI is great for CRUD APIs. For a game with 100+ endpoints, difficulty ratings, and in-game tips, we needed something custom."

**Content:**
- The problem with generic docs renderers: no concept of "beginner endpoint" vs. "advanced", no game tips, no quick-start flow
- Custom OpenAPI extensions: `x-game-tips`, `x-game-examples`, `x-game-prerequisites`, `x-game-difficulty`, `x-icon`, `x-color`, `x-order`
- `GameMetadataOperationFilter : IOperationFilter` — reads custom attributes from controller methods via reflection, injects into spec
- Controller decoration: `[ApiDifficulty("intermediate")]`, `[ApiGameTip("Check active modifiers before queuing")]`, `[ApiCategoryMeta("swords", "#ef4444", Order = 4)]`
- The renderer: `ApiDocsModel` walks the `OpenApiDocument`, builds `TagGroup` / `EndpointInfo` view models, renders via `_Endpoint.cshtml` partial
- `<details>` for expand/collapse (zero JS), color-coded HTTP method badges, lock icons for auth-required endpoints
- Sticky TOC sidebar with scroll tracking, live search/filter, "Expand All" / "Collapse All"
- The "6 API calls to first battle" quick-start card grid — onboarding built into the docs
- Stats bar: endpoint count, tag group count, schema count — all computed from the live spec

**Takeaway:** "Your OpenAPI spec is a data source, not just a Swagger UI config file. Treat it that way."

---

## 8. Strategy Marketplace: Building an Economy Around Player-Created JSON

**Hook:** "Players write battle strategies in JSON. Then they sell them to each other for in-game currency. Here's how we built a marketplace with decay mechanics."

**Content:**
- The strategy sharing model: publish a `StrategyConfig` with a price (or free), other players can browse/buy/download
- Sort modes: popular (downloads), rating (star average), recent, winrate (`WinCount / (WinCount + LossCount)`)
- The decay problem: if a dominant strategy stays dominant forever, the meta stagnates
- `StrategyDecayJob` runs daily at 2 AM: `EffectivenessMultiplier = Max(0.5, 1.0 - (ageInWeeks * 0.05))`
- 5% per week, floored at 50% — old strategies still work, they just lose their edge
- Currency flow: buyer pays, creator earns — emergent economy without a real-money system
- The social engineering angle: decay incentivizes continuous strategy innovation, which creates marketplace content, which attracts buyers

**Takeaway:** "Game economies need entropy. Without decay, optimization kills creativity."

---

## 9. Environmental Modifiers: Rotating the Meta Weekly Without Patching

**Hook:** "Every Monday at midnight UTC, the rules of combat change. No deploy required."

**Content:**
- The `IModifierEffect` interface: `ModifyUnitStats(Unit)` and `ApplyToBattle(BattleContext)`
- `BaseModifierEffect` provides no-op defaults — adding a modifier = one class + one dictionary entry
- Examples: `ArcaneDisruption` (Mages -30% attack, Warriors/Rangers +20%), `HeavyArmor` (all +50% defense, healing 2x)
- `BattleContext` carries `HealingMultiplier` and a `CustomData` dictionary for engine flags
- `WeeklyModifierRotationJob`: calculates exact `Task.Delay` to next Monday 00:00 UTC, deactivates expired, activates next queued
- The design constraint: modifiers must be expressible as stat multipliers or battle context flags — no new game logic per modifier
- Why weekly instead of daily: gives players time to adapt their strategies, creates a meaningful "this week's meta" conversation

**Takeaway:** "If your game balance lives in the database instead of the code, you can change it without a deploy."

---

## 10. Elo Rating + Seasonal Tiers: Two Parallel Rating Tracks

**Hook:** "Your global rating is forever. Your seasonal rating resets every 8 weeks. Here's why we run both."

**Content:**
- Standard Elo: K=32, 400-point scale, floor at 100 — nothing fancy, intentionally
- Seasonal layer: `PlayerSeasonRank` table, separate `SeasonRating`, tier thresholds (Bronze 0 → Legend 1800)
- Season auto-creation: 8-week duration, rotating names ("Dawn of Battle", "Rising Storm", "Iron Conquest")
- Peak tracking: `PeakRating` and `PeakTier` — end-of-season rewards based on peak, not final
- Tier change notifications via `INotificationService` — celebration on promotion, gentle nudge on demotion
- End-of-season rewards: Gold + XP by tier, exclusive titles ("Gold Gladiator" → "Legendary Conqueror")
- Matchmaking Elo range: starts at ±200 (Premium) / ±300 (Free), expands +50 every 5s, force-match after 20-30s
- Bot fallback: if no human match found after 10-15s, create a bot match — players shouldn't stare at a queue
- The API (Arena Power Index) tier names: Rubber Duck, Copy Pasta, Code Monkey, Bug Hunter, 10x Dev, Wizard, I Use Arch btw

**Takeaway:** "Global rating for bragging rights, seasonal rating for fresh competition. The two serve different psychological needs."

---

## 11. Tier Gating with Custom Action Filters (Not Just `[Authorize]`)

**Hook:** "`[Authorize]` checks if you're logged in. `[RequiresTier(Premium)]` checks if you're paying."

**Content:**
- `RequiresTierAttribute` — a simple marker attribute with `MinimumTier` property
- `TierGatingActionFilter : IAsyncActionFilter` — reads attribute from `EndpointMetadata`, resolves `PlayerId` from JWT, single `FindAsync()` on Players table
- The 403 response: JSON with `requiredTier`, `currentTier`, and `upgradeUrl` — not just "forbidden", but "here's what to do about it"
- Why an action filter instead of a policy: policies are binary (yes/no), filters can return structured responses with upgrade paths
- Composition: `[Authorize]` + `[RequiresTier(Premium)]` stack naturally — auth runs first, tier check second
- No feature flags needed — the tier check IS the feature flag, and it reads from the player's current subscription

**Takeaway:** "Custom action filters are the right abstraction for business rules that go beyond 'is this user authenticated?'"

---

## 12. Daily Challenge Generation: Procedural Content from Typed Generators

**Hook:** "Every day at midnight, every active player gets 3 new challenges. No designer hand-placed them — they're generated from typed C# classes."

**Content:**
- `BaseChallengeGenerator` pattern: `Generate(Player player)` returns a `DailyChallenge`, `CheckProgress(DailyChallenge, Battle)` updates it
- Generator types: `BattleCountChallenge`, `FlawlessVictoryChallenge`, `TeamCompositionChallenge`, `UnderdogChallenge`, `VarietySquadChallenge`, `WinStreakChallenge`
- Requirements stored as JSON (`RequirementsJson`), progress as integer — generic schema, typed logic
- Reward formula: `100 + (required * 20)` gold, `50 + (required * 10)` XP — scales with difficulty
- `DailyChallengeGenerationJob`: queries players active in last 7 days, generates 3 challenges each
- Why typed generators instead of a data-driven rule engine: each challenge type has unique win-condition logic that doesn't reduce cleanly to config
- The active-player filter: don't generate challenges for dormant accounts — saves DB writes and avoids "you missed 47 challenges" on return

**Takeaway:** "Procedural content generation doesn't need machine learning. Typed generators with randomized parameters create infinite variety from finite code."

---

## 13. Deploying ASP.NET Core to Shared Hosting: The Constraints That Shaped the Architecture

**Hook:** "Our entire game runs on a $5/month SmarterASP.NET plan. Here's every constraint we hit and how we worked around it."

**Content:**
- MSDeploy via publish profile: `EnableMSDeployAppOffline=true`, 10 retries at 15-second intervals
- The app pool recycle dance: must recycle the dedicated pool before publish to avoid `ERROR_FILE_IN_USE` on locked DLLs
- Shadow copy (`handlerSettings`) causes 502.5 on SmarterASP.NET — disabled, just accept the downtime
- OutOfProcess hosting: Kestrel behind IIS, not InProcess — because shared hosting IIS modules interfere
- 10 background services running in the same process — no separate worker service, no Azure Functions
- EF Core `MigrateAsync()` on startup — no CI/CD pipeline to run migrations separately
- LocalDB for development, MSSQL for production — connection strings in environment-specific appsettings
- The `dotnet-ef` version pinning gotcha: v10.x causes `System.Runtime` mismatch with net8.0 target, must use v8.0.11

**Takeaway:** "Constraints breed creativity. Shared hosting forced us into a simpler architecture than we'd have built on Azure — and it's better for it."

---

## 14. Branded HTML Emails That Actually Look Good in Gmail

**Hook:** "Gmail strips your `<style>` tags and overrides your `<a>` colors. Here's how we build branded HTML emails that survive every client."

**Content:**
- The template architecture: `IEmailTemplateService.Render(title, bodyHtml, preheader)` wraps content in a branded shell
- Raw string literals (`$"""..."""` in C# 11) for email HTML — no Razor views, no template engine, just interpolated strings
- The Gmail color hack: `style="color:#ffffff !important"` on every CTA button `<a>` tag — Gmail overrides normal `color` but respects `!important`
- Table-based layout (yes, still): `<table>` for structure, inline styles for everything — it's 2026 and email clients still can't do flexbox
- Dark mode support: `@media (prefers-color-scheme: dark)` in a `<style>` block that Gmail will strip — so the light theme must work standalone
- `HtmlEncoder.Default.Encode()` on all user-provided content (usernames, etc.) — XSS in email is real
- SMTP via MailKit: `StartTls` on port 587, FromAddress must match the authenticated SMTP account

**Takeaway:** "Email HTML is the cockroach of web development. It refuses to modernize, and you have to respect that."

---

## 15. Essential-Only Cookies: A GDPR Implementation That's Actually Simple

**Hook:** "We use exactly two cookies. Here's our entire cookie inventory and why we track consent in localStorage, not a cookie."

**Content:**
- The full inventory: `.AspNetCore.Cookies` (auth, 8h sliding / 30d max), `.AspNetCore.Antiforgery.*` (CSRF, session-scoped)
- localStorage keys: `theme` (dark/light preference), `cookie_consent` (acknowledgment)
- Why localStorage for consent: storing a "consent to cookies" value in a cookie is a circular dependency — the blog post that inspired this approach
- The banner implementation: fixed-bottom div, inline JS, no cookie library, no CMP platform
- "Got it" sets localStorage and hides the banner — one click, done
- The Privacy Policy: specific cookie table with name, purpose, duration, and type for every cookie
- reCAPTCHA v3 disclosure: it sets cookies too, disclosed in Third-Party Services section
- What we DON'T have: no analytics, no tracking pixels, no ad scripts, no third-party cookies (except reCAPTCHA)
- The competitive advantage of simplicity: our cookie banner is 15 lines of JS, not a 200KB CMP SDK

**Takeaway:** "The easiest way to comply with GDPR cookie rules is to not use cookies you'd need consent for."
**Cross-link:** learnedgeek.com/Blog/Post/gdpr-cookie-consent-essential-only

---

## 16. Soft Delete + PII Anonymization: Account Deletion That Actually Deletes

**Hook:** "GDPR says users can request erasure. Most apps flip `IsDeleted` and call it a day. We anonymize the PII too."

**Content:**
- The soft delete fields: `IsDeleted`, `DeletedAt` — standard pattern
- The anonymization step: username → `deleted-{guid:N}`, email → `deleted-{guid}@removed`, PasswordHash → cleared, all tokens → cleared
- Why not hard delete: foreign key relationships (battle history, guild membership, leaderboard snapshots) would cascade-delete game history
- The LoginAsync guard: `IsDeleted` check returns the same "invalid credentials" error — no information leak
- Confirmation flow: password verification → soft delete + anonymize → sign out → confirmation email (sent before anonymization clears the email)
- The email timing problem: must send the confirmation email BEFORE clearing the email address — order matters
- Leaderboard/metrics exclusion: `!IsDeleted` filter on all public queries and admin analytics

**Takeaway:** "Soft delete preserves relational integrity. PII anonymization preserves user privacy. You need both."

---

## Writing Guidelines

- **Tone:** Technical but conversational. "Here's what we built and why" not "best practices for enterprise."
- **Code examples:** Real snippets from the codebase, not sanitized pseudocode. Developers respect authenticity.
- **Length:** 1,500-2,500 words. Long enough to be useful, short enough to finish in one sitting.
- **Structure:** Hook → Problem → Solution → Code → Trade-offs → Takeaway
- **Cross-promotion:** Every post links to apicombat.com and the relevant API docs section. The blog IS the marketing.
- **Honesty about trade-offs:** "This works at our scale, here's where it would break" builds more credibility than "this is the right way."
