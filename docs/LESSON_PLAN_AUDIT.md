# Blog Content Audit — Educator-Readiness Review

**Audit date:** March 2026
**Purpose:** Ensure all public blog posts are accurate, free of unverified claims, and appropriate to share with educators and institutions considering the platform.

---

## Table of Contents

1. [Post 1: 5-Week REST API Curriculum](#post-1-5-week-rest-api-curriculum)
2. [Post 2: Teaching REST APIs Through Gaming](#post-2-teaching-rest-apis-through-gaming)
3. [Post 3: Introducing API Combat](#post-3-introducing-api-combat)
4. [Post 4: Why I Built a Game With No GUI](#post-4-why-i-built-a-game-with-no-gui)
5. [Cross-Post Issues](#cross-post-issues)
6. [Features Needing Implementation](#features-needing-implementation)
7. [Recommended Action Plan](#recommended-action-plan)

---

## Post 1: 5-Week REST API Curriculum

**URL:** https://learnedgeek.com/Blog/Post/rest-api-lesson-plan-wisconsin-standards
**Audience risk:** HIGH — this is the primary document educators would use to evaluate and adopt the platform

### Wrong Endpoint Paths

The blog uses `/v1/` prefix throughout. All actual endpoints use `/api/v1/`.

| Blog Says | Actual Endpoint | Notes |
|-----------|----------------|-------|
| `POST /v1/strategy/create` | `POST /api/v1/strategies/upload` | Publishes to marketplace; no "create" verb |
| `PUT /v1/strategy/update` | Does not exist | Strategies are immutable once uploaded |
| `GET /v1/player/roster` | `GET /api/v1/player/roster` | Path prefix wrong |

**Fix:** Search-replace `/v1/` → `/api/v1/` throughout, and replace strategy create/update references with `POST /api/v1/strategies/upload`.

### Unimplemented Features Promised

| Claim | Reality | Severity |
|-------|---------|----------|
| "Private class instance where students battle each other, not public players" | Not implemented. Matchmaking is global. Class tournaments + leaderboards provide scoping. | HIGH — educators may expect isolated environments |
| "Custom challenge builder creating assignments tied to specific endpoints with automated success criteria" | Partially covered — lessons can have a `verificationEndpoint` for auto-completion, but there's no UI "challenge builder" | MEDIUM — overstates the feature |
| "Automated milestone notifications" | General notification system exists but nothing education-specific triggers on lesson milestones | LOW |
| "Curriculum extensions (WebSocket modules, OAuth integration labs)" | Neither WebSocket nor OAuth exists in the platform | HIGH — promises nonexistent features |
| "Semester-long pacing guides expanding the 5-week unit into a full 15-week course" | No 15-week guide exists | MEDIUM |

### What's Accurate

- Week-by-week structure (1-5) is pedagogically sound and all activities are achievable
- Wisconsin standards mapping — all 15 standards are genuinely supported
- Assessment rubric — categories, weights, and descriptions are reasonable
- Core tools: curl, Python `requests`, JSON parsing — all work as described
- Batch practice, guild collaboration, class tournaments, leaderboards — all implemented
- "Free for accredited institutions" — this is a business decision, not a technical claim

### Recommended Changes

1. Fix all endpoint paths (`/v1/` → `/api/v1/`)
2. Replace strategy create/update with actual upload endpoint
3. Reword "Private class instance" to "class-scoped tournaments and leaderboards"
4. Remove or mark WebSocket/OAuth as "advanced student project (build externally)"
5. Remove "custom challenge builder" language; replace with "instructor-defined curriculum modules with endpoint-linked lessons"
6. Remove 15-week pacing guide claim or create the guide

---

## Post 2: Teaching REST APIs Through Gaming

**URL:** https://learnedgeek.com/Blog/Post/teaching-rest-apis-through-gaming
**Audience risk:** CRITICAL — contains fabricated pilot data and the most sensationalized claims

### Fabricated Pilot Results

The post claims specific engagement metrics from "three bootcamps, 60 total students":

| Claim | Problem |
|-------|---------|
| "87% completion rate (vs. 54% traditional)" | No pilot has occurred. These numbers are fabricated. |
| "Average 45 API calls per student (vs. 12 for CRUD)" | No pilot has occurred. |
| "23 students built unrequired features" | No pilot has occurred. |
| "92% explained HTTP methods without notes (vs. 67% before)" | No pilot has occurred. |
| "78% implemented error handling unprompted (vs. 31% before)" | No pilot has occurred. |
| "100% understood JSON structure (vs. 89% before)" | No pilot has occurred. |

**Severity: CRITICAL.** An educator or institution performing due diligence would ask for the pilot study. This data does not exist and claiming it does is dishonest. This must be removed entirely before sharing with any prospect.

### Fabricated Student Quotes

> "I finally get why APIs matter. I wasn't building for a grade. I was building to win."
> "I learned more debugging my bot than I did in 3 weeks of lectures."
> "Can we do this for databases too?"

**These are fabricated quotes attributed to nonexistent students.** Remove entirely.

### Wrong Endpoint Paths

Same `/v1/` prefix issue as Post 1, plus:

| Blog Says | Actual Endpoint | Notes |
|-----------|----------------|-------|
| `GET /v1/player/roster` | `GET /api/v1/player/roster` | Wrong prefix |
| `POST /v1/strategy/create` | `POST /api/v1/strategies/upload` | Wrong path and verb |
| `PUT /v1/strategy/update` | Does not exist | Strategies are immutable |
| `DELETE /v1/unit/retire` | Does not exist | No unit retire/delete endpoint |

### Wrong Rate Limit Values

| Blog Says | Actual (in code) |
|-----------|-----------------|
| Free: 10 requests/min | Free: 60 requests/min |
| Premium: 50 requests/min | Premium: 120 requests/min |
| Premium+: 250 requests/min | Premium+: 300 requests/min |

**Fix:** Update to actual values from `RateLimitingMiddleware.cs`.

### Week 5 Claims WebSocket Content

Blog outlines Week 5 as: "WebSocket connections, event-driven programming, real-time dashboards." No WebSocket/SignalR implementation exists. The actual Week 5 in the lesson plan document is about tournaments and presentations, which is accurate.

### Unimplemented Features Listed

| Claim | Reality |
|-------|---------|
| "Private isolated instances" | Not implemented |
| "Custom challenge creation" | Partially — `verificationEndpoint` on lessons, but no "challenge builder" |

### What's Accurate

- Core pedagogical argument (game-based learning > CRUD tutorials) is sound
- HTTP methods, JWT auth, JSON parsing, error handling — all genuine learning outcomes
- Guild wars, class leaderboards — implemented
- General competitive framing — reasonable

### Recommended Changes

1. **Remove all pilot statistics and student quotes** — replace with honest framing: "designed for classroom use" or "built based on educator feedback"
2. If/when a real pilot occurs, add genuine data with methodology description
3. Fix all endpoint paths
4. Fix rate limit values
5. Remove WebSocket Week 5 outline
6. Remove DELETE /v1/unit/retire reference

---

## Post 3: Introducing API Combat

**URL:** https://learnedgeek.com/Blog/Post/introducing-api-combat
**Audience risk:** MEDIUM — general announcement, but contains factual errors educators might notice

### Wrong Unit Class Count and Names

Blog lists 6 unit classes:
- Tanks, Damage Dealers, Healers, Support, Specialists, Hybrids

**Actual (from `UnitClass.cs`):** 5 classes:
- Warrior, Mage, Ranger, Healer, Tank

No "Damage Dealers," "Support," "Specialists," or "Hybrids" classes exist.

### Wrong Unit Counts and Tier Restrictions

| Claim | Reality |
|-------|---------|
| "Free tier unlocks 20 units" | No tier-based unit limits. All 25 template units are available to all players (purchased with in-game gold). |
| "Premium unlocks 50+" | Only 25 template units exist total. No tier restriction on unlocking. |

### Pricing Tier Feature Claims

| Claim | Reality | Accurate? |
|-------|---------|-----------|
| Free: 10 battles/day | Confirmed in `BattleController.cs` | YES |
| Free: Full API access | Confirmed | YES |
| Premium: Unlimited battles | Confirmed | YES |
| Premium: Guild access | Guild creation requires Premium (joining does not) | PARTIALLY — overstates restriction |
| Premium: Simulation endpoint | Batch practice is available to all tiers, not Premium-only | NO |
| Premium: Strategy versioning | Does not exist | NO |
| Premium: Discord webhooks | Discord webhooks exist and appear available to all players | PARTIALLY |
| Premium+: Lua scripting engine | Does not exist anywhere in codebase | NO |
| Premium+: WebSocket connections | Does not exist | NO |
| Premium+: Batch operations (100 battles) | Batch practice exists for all tiers, limit is 200 not 100 | NO (wrong tier + wrong limit) |
| Premium+: 5x API rate limits | Premium+ is 300/min vs Free 60/min (5x ratio is correct) | YES |
| Premium+: Advanced analytics | Player analytics exist but not tier-restricted | PARTIALLY |

### "Premium features are tools for optimization, not power upgrades. Free players can absolutely compete."

This statement is actually true and should stay — it accurately describes the design philosophy.

### What's Accurate

- Core concept (API-only game, no GUI) — accurate
- Rating tiers (Rubber Duck to I Use Arch btw) — accurate
- Game modes (ranked, casual, tournaments, education mode) — accurate
- Register/build teams/configure strategies/queue battles flow — accurate
- Target audiences (developers, educators, teams) — reasonable

### Recommended Changes

1. Fix unit classes: 5 types (Warrior, Mage, Ranger, Healer, Tank), not 6
2. Remove unit count claims (20/50+) — replace with "25 unique units across 5 classes"
3. Remove Lua scripting, WebSocket, and strategy versioning from Premium/Premium+ descriptions
4. Fix batch practice: available to all tiers, limit is 200
5. Clarify guild access: creation is Premium, but joining and participating in wars is open
6. Remove or mark nonexistent features as "planned"

---

## Post 4: Why I Built a Game With No GUI

**URL:** https://learnedgeek.com/Blog/Post/why-i-built-a-game-with-no-gui
**Audience risk:** LOW — this is a developer/philosophy post, less likely to be scrutinized by educators

### Claims

| Claim | Reality | Accurate? |
|-------|---------|-----------|
| "Over 100 documented API endpoints" | Likely accurate — 100+ endpoints across all controllers | YES |
| "All interaction occurs through REST API calls" | Web UI exists (Razor Pages), but all game mechanics are API-accessible | MOSTLY — should clarify web UI exists for convenience |
| "Python starter client completes game loop in ~200 lines" | No starter client exists in the repo; this is a reasonable estimate for a hypothetical client | UNVERIFIABLE |
| "Players can create Discord bots, Grafana dashboards, ML models" | Technically possible — API is open. Discord webhooks exist. | YES (aspirational but truthful) |
| "Eliminated ~80% of typical UI development time" | Subjective developer claim — reasonable given no frontend framework | REASONABLE |

### What's Accurate

This is the most accurate of the four posts. It describes the philosophy and architecture without making specific feature claims that can be verified against code. The core argument is genuine.

### Recommended Changes

1. Minor: Clarify that a web UI exists for account management and docs, even though gameplay is API-driven
2. If referencing the "200-line Python client," consider actually creating and publishing one as a resource

---

## Cross-Post Issues

These problems appear across multiple posts and need consistent fixes:

### 1. Endpoint Path Prefix

**Every post** uses `/v1/` instead of `/api/v1/`. This is wrong everywhere.

### 2. Unit Class Count

Posts 1 and 3 reference 6 unit classes. The actual count is **5**: Warrior, Mage, Ranger, Healer, Tank.

### 3. Nonexistent Endpoints Referenced

| Endpoint | Referenced In | Status |
|----------|--------------|--------|
| `POST /v1/strategy/create` | Posts 1, 2 | Does not exist — use `POST /api/v1/strategies/upload` |
| `PUT /v1/strategy/update` | Posts 1, 2 | Does not exist — strategies are immutable |
| `DELETE /v1/unit/retire` | Post 2 | Does not exist — no unit deletion |
| `POST /v1/strategy/{id}/test` | Post 1 | Does not exist — use `POST /api/v1/ai/practice` |

### 4. WebSocket/Lua/OAuth Claims

Referenced in Posts 1, 2, and 3. None of these exist:
- No WebSocket or SignalR implementation
- No Lua scripting engine
- No OAuth endpoints (auth is JWT-only)

### 5. "Private Class Instance" Promise

Referenced in Posts 1 and 2. Not implemented. Current workaround: class-scoped tournaments and leaderboards provide isolation where it matters most.

---

## Features Needing Implementation

If these features are important to the education pitch, they would need to be built. Otherwise, remove all references.

| Feature | Referenced In | Effort | Recommendation |
|---------|--------------|--------|----------------|
| Private class matchmaking | Posts 1, 2 | HIGH | Remove claim — class tournaments already provide scoping |
| WebSocket / SignalR | Posts 1, 2, 3 | MEDIUM | Remove claim — not needed for curriculum |
| Lua scripting engine | Post 3 | HIGH | Remove claim — not needed for any lesson plan |
| OAuth endpoints | Post 1 | MEDIUM | Remove claim — JWT is sufficient for teaching auth |
| Strategy versioning | Post 3 | LOW | Remove claim — minor feature |
| Unit retire endpoint | Post 2 | LOW | Could implement, but not needed for curriculum |
| 15-week pacing guide | Post 1 | LOW (docs only) | Create if wanted, or remove promise |
| Python starter client | Post 4 | LOW | Create and publish — good marketing asset |

---

## Recommended Action Plan

### Must Fix Before Sharing With Educators (Critical)

1. **Remove all fabricated pilot data from Post 2** — the "87% completion rate," "60 students across 3 bootcamps," and all comparative statistics. These are verifiable lies that would destroy credibility with any institution that asks for details.

2. **Remove fabricated student quotes from Post 2** — same reason. Replace with honest framing about the platform's design goals.

3. **Fix endpoint paths across all posts** — `/v1/` → `/api/v1/`

4. **Remove nonexistent features** (Lua, WebSockets, OAuth, strategy versioning, private class instances) from feature lists, or clearly mark them as "planned/roadmap"

5. **Fix unit class count** — 5 classes, not 6

### Should Fix (Accuracy)

6. **Fix rate limit values** in Post 2 — actual values are more generous than claimed (60/120/300 vs 10/50/250)

7. **Fix unit counts** in Post 3 — 25 total units available to all tiers, no tier-based restrictions

8. **Fix batch practice claims** in Post 3 — available to all tiers (not Premium+ exclusive), limit is 200 (not 100)

9. **Clarify guild access** — creation requires Premium, but joining/participating does not

### Nice to Have

10. **Create the Python starter client** referenced in Post 4 — good onboarding resource

11. **Create a 15-week pacing guide** or remove the promise from Post 1

12. **Add honest educator testimonials** once a real pilot occurs — replace the fabricated ones
