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

### Week 4: Advanced Features & Collaboration — READY

| Curriculum Activity | API Combat Feature | Status |
|--------------------|--------------------|--------|
| Batch operations (100+ battles) | Batch practice endpoint | READY |
| Statistical analysis | Battle history + analytics | READY |
| Guild collaboration | Guild system + strategy library | READY |
| Team GitHub repos | External — not platform | N/A |
| Peer review | Guild strategy library | READY |

**No gaps.** `POST /api/v1/ai/batch-practice` runs up to 200 simulated battles server-side and returns aggregate stats (wins, losses, avg turns, damage). Ideal for Week 4 data analysis.

### Week 5: Tournament & Presentation — READY

| Curriculum Activity | API Combat Feature | Status |
|--------------------|--------------------|--------|
| Final bot optimization | AI practice + ranked | READY |
| Class tournament | Class-scoped tournament | READY |
| Live leaderboard | Class leaderboard API | READY |
| Team presentations | External | N/A |

**No gaps.** Instructors create class-only tournaments via `POST /api/v1/education/modules/{id}/tournament`. Class leaderboard at `GET /api/v1/education/modules/{id}/leaderboard` shows enrolled students ranked by rating/wins.

---

## Education Mode Feature Audit

The blog post promises these Education Mode features. Here's what exists:

| Promised Feature | Implementation | Status |
|-----------------|----------------|--------|
| Private class instance | No multi-tenancy / classroom isolation | **FUTURE** (not required — class tournaments + leaderboards provide scoping) |
| Student progress tracking dashboard | `EducationService` with enrollment, lesson completion, instructor dashboard | READY |
| Custom challenge assignments tied to endpoints | Lessons with `verificationEndpoint` — auto-complete when student hits the right endpoint | READY |
| Leaderboards | Class-scoped leaderboard per module | READY |
| Tournament infrastructure | Class-scoped tournaments (instructor creates for enrolled students) | READY |
| Guild Wars functionality | Full guild war system | READY |
| Batch practice | `POST /api/v1/ai/batch-practice` — up to 200 battles with aggregate stats | READY |

### Education Endpoints (12 total)

| Endpoint | Purpose |
|----------|---------|
| `GET /api/v1/education/modules` | Browse published curriculum modules |
| `GET /api/v1/education/modules/{id}` | Module detail + student progress |
| `POST /api/v1/education/modules` | Create module (instructor) |
| `POST /api/v1/education/modules/{id}/publish` | Publish module |
| `POST /api/v1/education/enroll/{id}` | Enroll by module ID |
| `POST /api/v1/education/enroll/code/{code}` | Enroll by join code |
| `DELETE /api/v1/education/enroll/{id}` | Unenroll from module |
| `POST /api/v1/education/modules/{id}/lessons/{idx}/complete` | Mark lesson complete |
| `GET /api/v1/education/my-progress` | Student progress across all modules |
| `GET /api/v1/education/modules/{id}/leaderboard` | Class leaderboard (enrolled students) |
| `POST /api/v1/education/modules/{id}/tournament` | Create class tournament (instructor) |
| `GET /api/v1/education/instructor/dashboard` | Instructor analytics |

---

## Remaining Enhancements (Nice-to-Have)

All curriculum-blocking gaps have been resolved. The following are enhancements for future development:

### Classroom Isolation (High Effort, Future)

**Why:** "Private class instance" means students only battle each other, not the global player pool.

**Proposed options:**
- **Option A (simple):** Tag enrolled students with a `classroomId`; matchmaking prefers same-classroom opponents
- **Option B (complex):** Full multi-tenant isolation per classroom

**Effort:** High — touches matchmaking, leaderboards, tournaments. Defer until demand justifies it.

**Current workaround:** Class-scoped tournaments and leaderboards already provide isolation where it matters most. Students battling the global pool adds realism. AI practice mode provides fully controlled experiments.

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

**The game meets all 15 Wisconsin CS standards and supports all 5 weekly deliverables today. All curriculum weeks are fully READY.**

| Feature | Status |
|---------|--------|
| Batch practice endpoint (`POST /api/v1/ai/batch-practice`) | READY |
| Class-scoped leaderboard (`GET /api/v1/education/modules/{id}/leaderboard`) | READY |
| Class-scoped tournament (`POST /api/v1/education/modules/{id}/tournament`) | READY |
| Endpoint-linked lesson verification (`verificationEndpoint` on lessons) | READY |
| Instructor dashboard with student analytics | READY |
| Pre-seeded "API Basics 101" module (join code: `BASICS01`) | READY |
| Classroom isolation (private matchmaking pool) | FUTURE — not required, class tournaments provide scoping |

*Last updated: February 2026*
