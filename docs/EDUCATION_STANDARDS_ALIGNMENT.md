# Education Standards Alignment — Wisconsin CS Standards

Maps the [5-Week REST API Curriculum](https://learnedgeek.com/Blog/Post/rest-api-lesson-plan-wisconsin-standards) to API Combat features.

---

## Standards Coverage Summary

| Standard | Code | Covered By | Status |
|----------|------|-----------|--------|
| Design algorithmic solutions | AP1.a.8.h | Battle bot scripting, strategy A/B testing | READY |
| Develop test cases | AP1.a.14.h | Strategy win-rate experiments, tournament prep | READY |
| Decompose problems | AP2.a.13.h | Multi-endpoint bot architecture | READY |
| Code reuse via APIs | AP2.a.16.h | Full REST API surface (100+ endpoints) | READY |
| Use API documentation | AP3.c.5.h | OpenAPI spec, /api-docs/v1, SDK quickstart | READY |
| Use online resources | AP3.c.6.h | OpenAPI spec is self-describing | READY |
| Deconstruct complex problems | AP4.a.6.h | Multi-step battle flow decomposition | READY |
| Design in teams | AP5.a.6.h | Guild system, guild strategy library | READY |
| Version control & collaboration | AP5.a.9.h | External (GitHub) — not platform responsibility | N/A |
| Debug systematically | AP6.a.4.h | HTTP status codes, error responses, rate limiting | READY |
| Describe protocols | NI2.b.3.h | HTTP methods, headers, status codes, JSON | READY |
| Encryption & authentication | NI2.d.5.h | JWT Bearer tokens, HTTPS | READY |
| Storage & retrieval | DA2.a.4.h | Battle history, replay data, analytics | READY |
| Computational data collection | DA3.a.6.h | Bot scripting → battle data → analysis | READY |
| Ethical collaboration | IC2.c.5.h | Strategy marketplace, guild participation | READY |

**All 15 Wisconsin standards are supported by existing features.**

---

## Week-by-Week Feature Mapping

### Week 1: API Fundamentals — READY

| Curriculum Activity | API Combat Feature | Endpoint(s) |
|--------------------|--------------------|-------------|
| Register via API | Auth registration | `POST /api/v1/auth/register` |
| Authenticate with Bearer token | JWT auth | `POST /api/v1/auth/login` |
| Parse JSON responses | All responses are JSON | Any endpoint |
| Complete first battle | AI Practice (instant, no queue) | `POST /api/v1/ai/practice` |
| Read API documentation | Custom docs + OpenAPI spec | `/api-docs/v1`, `/openapi/v1.json` |
| Onboarding guide | SDK quickstart with code snippets | `GET /api/v1/sdk/quickstart` |
| Endpoint catalog | SDK endpoint listing | `GET /api/v1/sdk/endpoints` |

**No gaps.** Students can register, auth, battle an AI, and read docs on Day 1.

### Week 2: Build a Bot — READY

| Curriculum Activity | API Combat Feature | Endpoint(s) |
|--------------------|--------------------|-------------|
| Script HTTP requests | Standard REST API | All endpoints |
| Auto-queue 10 battles | AI practice (instant) or ranked queue | `POST /api/v1/ai/practice` |
| Handle 400/401/403/429/500 | Error responses + rate limiting | All endpoints |
| Retry logic / exponential backoff | 429 + `Retry-After` header | Rate-limited endpoints |
| Structured logging | JSON responses, battle results | `GET /api/v1/battle/results/{id}` |

**No gaps.** AI practice mode is ideal for Week 2 — instant battles, no opponent needed, JSON results.

### Week 3: Strategy & Optimization — READY

| Curriculum Activity | API Combat Feature | Endpoint(s) |
|--------------------|--------------------|-------------|
| Study strategy JSON schema | OpenAPI spec describes strategy format | `/openapi/v1.json` |
| Create 3 formations | Strategy upload | `POST /api/v1/strategies/upload` |
| Team configuration | Team with strategy | `POST /api/v1/team/configure` |
| A/B test strategies (20+ battles each) | AI practice per strategy config | `POST /api/v1/ai/practice` |
| Calculate win rates | Battle history with results | `GET /api/v1/battle/history` |
| Rate-limiting headers | Present on all API responses | Any API call |

**No gaps.** Students create 3 team configs with different strategies, run 20+ AI practice battles each, compare results.

### Week 4: Advanced Features & Collaboration — PARTIAL

| Curriculum Activity | API Combat Feature | Status |
|--------------------|--------------------|--------|
| Batch operations (100+ battles) | No batch endpoint | **GAP** (see below) |
| Statistical analysis | Battle history + analytics | READY |
| Guild collaboration | Guild system + strategy library | READY |
| Team GitHub repos | External — not platform | N/A |
| Peer review | Guild strategy library | READY |

**Gap: Batch simulation endpoint.** The curriculum calls for running 100+ simulated battles for data analysis. Current workarounds:
- Students can loop 100+ AI practice battles via their bot scripts (each resolves instantly)
- This actually reinforces Week 2 skills and is arguably better pedagogy
- A dedicated batch endpoint would reduce friction but isn't strictly required

### Week 5: Tournament & Presentation — PARTIAL

| Curriculum Activity | API Combat Feature | Status |
|--------------------|--------------------|--------|
| Final bot optimization | AI practice + ranked | READY |
| Class tournament | Weekly tournament system | **GAP** (see below) |
| Live leaderboard | Leaderboard API | READY (polling) |
| Team presentations | External | N/A |

**Gap: Class-scoped tournaments.** The existing tournament system is global (all players). For classroom use:
- An instructor cannot create a tournament limited to enrolled students
- Workaround: time a global tournament to class schedule, or use the leaderboard filtered by class

---

## Education Mode Feature Audit

The blog post promises these Education Mode features. Here's what exists:

| Promised Feature | Implementation | Status |
|-----------------|----------------|--------|
| Private class instance | No multi-tenancy / classroom isolation | **GAP** |
| Student progress tracking dashboard | `EducationService` with enrollment, lesson completion, instructor dashboard | READY |
| Custom challenge assignments tied to endpoints | `CurriculumModule` with lessons — but lessons are instructional, not challenge-based | **PARTIAL** |
| Leaderboards | Global leaderboard exists | READY (not class-scoped) |
| Tournament infrastructure | Weekly tournament exists | READY (not class-scoped) |
| Guild Wars functionality | Full guild war system | READY |

### Existing Education Endpoints (5.15)

| Endpoint | Purpose |
|----------|---------|
| `GET /api/v1/education/modules` | Browse published curriculum modules |
| `GET /api/v1/education/modules/{id}` | Module detail + student progress |
| `POST /api/v1/education/modules` | Create module (instructor) |
| `POST /api/v1/education/modules/{id}/publish` | Publish module |
| `POST /api/v1/education/enroll/{id}` | Enroll by module ID |
| `POST /api/v1/education/enroll/code/{code}` | Enroll by join code |
| `POST /api/v1/education/modules/{id}/lessons/{idx}/complete` | Mark lesson complete |
| `GET /api/v1/education/my-progress` | Student progress across all modules |
| `GET /api/v1/education/instructor/dashboard` | Instructor analytics |

---

## Implementation Gaps — Prioritized

### Priority 1: Class-Scoped Leaderboard (Low Effort, High Value)

**Why:** Every week of the curriculum uses leaderboards. Class-scoped leaderboards let instructors see just their students.

**Proposed:** Add `GET /api/v1/education/modules/{moduleId}/leaderboard` that returns enrolled students ranked by rating/wins.

**Effort:** Small — query enrolled students, join with player stats, sort.

### Priority 2: Batch Practice Endpoint (Medium Effort, High Value)

**Why:** Week 4 requires 100+ simulated battles for statistical analysis. Students CAN loop their bots, but a batch endpoint reduces friction.

**Proposed:** Add `POST /api/v1/ai/batch-practice` that accepts a team ID + count (max 200), runs N practice battles server-side, returns aggregate results (wins, losses, avg turns, damage stats).

**Effort:** Medium — reuse existing `AiOpponentService` in a loop, return summary.

### Priority 3: Class-Scoped Tournament (Medium Effort, High Value)

**Why:** Week 5 culminates in a class tournament. Currently tournaments are global.

**Proposed:** Add `POST /api/v1/education/modules/{moduleId}/tournament` (instructor only) that creates a tournament limited to enrolled students.

**Effort:** Medium — extend `TournamentService` with optional `moduleId` filter on registration.

### Priority 4: Endpoint-Linked Challenge Assignments (Medium Effort, Medium Value)

**Why:** Education Mode promises "custom challenge assignments tied to endpoints." Current lessons are instructional (read/complete), not challenge-based (verify via API call).

**Proposed:** Extend `CurriculumModule` lessons with optional `verificationEndpoint` + `verificationCriteria` fields. When a student hits the right endpoint with correct parameters, the lesson auto-completes.

**Effort:** Medium — add verification logic to lesson completion, or add a new challenge type to the education module.

### Priority 5: Classroom Isolation (High Effort, Future)

**Why:** "Private class instance" means students only battle each other, not the global player pool.

**Proposed options:**
- **Option A (simple):** Tag enrolled students with a `classroomId`; matchmaking prefers same-classroom opponents
- **Option B (complex):** Full multi-tenant isolation per classroom

**Effort:** High — touches matchmaking, leaderboards, tournaments. Defer to post-launch.

### Not Required (Advanced Extensions)

These are mentioned as optional stretch goals in the curriculum, not core requirements:

| Feature | Curriculum Role | Status |
|---------|----------------|--------|
| WebSockets / SignalR | Advanced: live dashboard updates | NOT BUILT — future |
| Game event webhooks | Advanced: real-time notifications | PARTIAL (Discord webhooks exist) |
| Adaptive counter-strategies | Advanced student project | SUPPORTED (replay data + battle history enables this) |

---

## Deliverable Validation

Can students produce every curriculum deliverable with current features?

| Week | Deliverable | Can Produce? | How |
|------|------------|-------------|-----|
| 1 | Annotated log of completed battle | YES | Register → login → GET roster → configure team → POST ai/practice → GET results. All JSON. |
| 2 | Bot script (10 battles + error handling + log file) | YES | Python script hitting AI practice endpoint in a loop. Rate limit headers available for backoff logic. |
| 3 | 3 strategy configs + battle data + written analysis | YES | Upload 3 strategies, configure 3 teams, run 20+ AI practice battles each, GET history for data. |
| 4 | Simulation report (100+ battles) + team GitHub repo | YES | Loop AI practice 100+ times via bot script. Guild strategy library for sharing. GitHub is external. |
| 5 | Tournament-ready bot + presentation + reflection | YES | Use existing tournament system. Bot from Week 2-4 is tournament-ready. |

**All 5 deliverables are achievable with current features.**

---

## Assessment Rubric Support

| Rubric Category (Weight) | API Combat Support |
|--------------------------|-------------------|
| API Fundamentals & HTTP (20%) | Full HTTP method support, Bearer auth, OpenAPI spec, HATEOAS links |
| Programming & Automation (25%) | AI practice for bot testing, rate limiting for backoff logic, JSON for parsing |
| Data Analysis & Optimization (20%) | Battle history, replay data, player analytics, strategy win rates |
| Collaboration & Version Control (15%) | Guild system, guild strategies, marketplace. Git is external. |
| Communication & Presentation (10%) | External — not platform responsibility |
| Error Handling & Debugging (10%) | Proper HTTP status codes (400, 401, 403, 404, 429, 500), `Retry-After`, `X-RateLimit-*` headers |

---

## Summary

**The game meets all 15 Wisconsin CS standards and supports all 5 weekly deliverables today.**

Gaps are enhancements to the Education Mode experience, not blockers:

| Gap | Blocking? | Workaround |
|-----|-----------|-----------|
| Batch practice endpoint | No | Students loop AI practice via bot scripts (reinforces Week 2 skills) |
| Class-scoped leaderboard | No | Use global leaderboard; instructor dashboard shows enrolled student progress |
| Class-scoped tournament | No | Use global weekly tournament timed to class schedule |
| Endpoint-linked challenges | No | Instructor verifies via lesson completion + student demo |
| Classroom isolation | No | Students battle global pool (adds realism); AI practice for controlled experiments |

*Last updated: February 2026*
