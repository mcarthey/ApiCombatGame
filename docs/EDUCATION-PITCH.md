# Education Pitch — Campus Presentation Guide

**Audience:** Continuing education directors, workforce development directors, department chairs, CS program leads
**Pitch:** A REST API curriculum tool where students learn by playing — not by reading

---

## The One-Liner

"API Combat is a game where students write code to battle each other's bots via REST API. Same technical concepts as any API course, but now they're debugging at midnight because they want to win."

---

## Pricing

### Standard Institutional License

| Component | Cost |
|-----------|------|
| **Platform fee** | $500/semester |
| **Per student** | $20/student |

**What students get:** Premium access (unlimited battles, 10 team slots, priority matchmaking) for the semester. Drops to Free tier when the semester ends.

**What the institution gets:** Instructor tools (curriculum builder, class leaderboard, class tournaments, instructor dashboard, student analytics), onboarding support, join-code enrollment.

**Students never pay.** The institution pays. Students never see a payment screen.

### Example Pricing

| Scenario | Total |
|----------|-------|
| 15-student continuing ed cohort | $500 + $300 = **$800** |
| 25-student class | $500 + $500 = **$1,000** |
| 60-student bootcamp (3 cohorts) | $500 + $1,200 = **$1,700** |

### Discounts

- **Pilot pricing:** First cohort at reduced rate (negotiate per campus)
- **Annual/multi-semester:** Discounted for commitment
- **Dedicated curriculum integration:** Discounted when embedded in a standing program

### Billing

- Invoice at start of semester, payment due on receipt
- Manual setup: we grant instructor access and enrolled students get Premium

---

## What's Built (Not Vaporware)

Everything below is live at [apicombat.com](https://apicombat.com) today.

### Education Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /api/v1/education/modules` | Browse published curriculum modules |
| `GET /api/v1/education/modules/{id}` | Module detail + student progress |
| `POST /api/v1/education/modules` | Create module (instructor only) |
| `POST /api/v1/education/modules/{id}/publish` | Publish module |
| `POST /api/v1/education/enroll/{id}` | Enroll by module ID |
| `POST /api/v1/education/enroll/code/{code}` | Enroll by join code |
| `POST /api/v1/education/modules/{id}/lessons/{idx}/complete` | Mark lesson complete |
| `GET /api/v1/education/my-progress` | Student progress across all modules |
| `GET /api/v1/education/modules/{id}/leaderboard` | Class leaderboard |
| `POST /api/v1/education/modules/{id}/tournament` | Create class tournament |
| `DELETE /api/v1/education/enroll/{id}` | Unenroll |
| `GET /api/v1/education/instructor/dashboard` | Instructor analytics |

### Pre-Seeded Module

- **"API Basics 101"** — 6 lessons: register, login, view profile, check roster, queue battle, check results
- **Join code:** `BASICS01`
- Published and ready for students on any new deployment

### Platform Stats

- 100+ API endpoints across 36 controllers
- 5 unit classes with rock-paper-scissors advantages
- AI practice mode (instant battles, no queue, no rating impact)
- Ranked seasons, tournaments, guilds, strategy marketplace
- Full OpenAPI spec + custom interactive API docs

---

## Curriculum Resources

### Published

- **5-Week REST API Lesson Plan:** [learnedgeek.com/Blog/Post/rest-api-lesson-plan-wisconsin-standards](https://learnedgeek.com/Blog/Post/rest-api-lesson-plan-wisconsin-standards)
- **Wisconsin CS Standards Alignment:** See `docs/EDUCATION_STANDARDS_ALIGNMENT.md` — all 15 standards mapped
- **API Documentation:** [apicombat.com/api-docs/v1](https://apicombat.com/api-docs/v1)

### Week-by-Week Summary

| Week | Focus | Key Endpoints |
|------|-------|---------------|
| 1 | API Fundamentals | Register, login, profile, first AI battle |
| 2 | Build a Bot | Script HTTP requests, error handling, retry logic |
| 3 | Strategy & Optimization | Team config, strategy upload, A/B test via AI practice |
| 4 | Advanced Features & Collaboration | Batch testing, guild system, data analysis |
| 5 | Tournament & Presentation | Class tournament, final bot, presentations |

### Student Deliverables (All Achievable Today)

1. Annotated log of completed battle (Week 1)
2. Bot script — 10 battles + error handling + log file (Week 2)
3. 3 strategy configs + battle data + written analysis (Week 3)
4. Simulation report (100+ battles) + team GitHub repo (Week 4)
5. Tournament-ready bot + presentation + reflection (Week 5)

---

## Demo Script (15-Minute Campus Visit)

### 1. Zero-Auth Hook (2 min)

Open a terminal and run:

```bash
curl https://apicombat.com/api/v1/leaderboard?limit=5
```

"This is live data. Real players, real rankings. Your students can see this without even creating an account."

### 2. Register → Battle in 60 Seconds (3 min)

```bash
# Register
curl -X POST https://apicombat.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"demo_student","email":"demo@example.com","password":"Pass123!"}'

# Queue a battle (Starter Team used automatically)
curl -X POST https://apicombat.com/api/v1/battle/queue \
  -H "Authorization: Bearer TOKEN_FROM_ABOVE" \
  -H "Content-Type: application/json" \
  -d '{"mode":"casual"}'
```

"Three API calls. They're playing. No SDK to install, no IDE to configure. curl and a terminal."

### 3. Show the API Docs (2 min)

Open [apicombat.com/api-docs/v1](https://apicombat.com/api-docs/v1):
- "Try It Now" section — public endpoints they can paste into a terminal
- "Copy with my token" buttons — paste JWT once, all commands auto-fill
- "Stuck? Try This" troubleshooting — the 3 most common errors with fixes

### 4. Education Mode (3 min)

Show the curriculum module:

```bash
curl https://apicombat.com/api/v1/education/modules \
  -H "Authorization: Bearer TOKEN"
```

"API Basics 101 — 6 lessons that walk students from registration to their first battle. Students enroll with a join code. You see their progress on your instructor dashboard."

### 5. The Lesson Plan (2 min)

Open the blog post: [5-Week REST API Lesson Plan](https://learnedgeek.com/Blog/Post/rest-api-lesson-plan-wisconsin-standards)

"This is mapped to Wisconsin CS Standards. Ready to hand to your department chair. All 15 standards, all 5 weekly deliverables, assessment rubric included."

### 6. Close (3 min)

"Education Mode is $500 for the semester plus $20 per student. Students get unlimited access. I handle the setup — you give me a class list, I turn it on."

"I piloted this with students at Waukesha County Technical College. Happy to set up a pilot cohort for your next semester."

Leave behind:
- Lesson plan URL
- API docs URL
- Your contact info
- Pricing one-pager (this doc)

---

## Objection Handling

**"We already teach APIs with Postman/Swagger tutorials."**
> "So did I. Students check out the moment you say 'HTTP verbs.' This gives them a reason to care — they're debugging at midnight because they want to beat their classmates."

**"$20/student feels steep for a tool."**
> "It replaces lab software, not a textbook. And students get Premium access — unlimited battles, full feature set. Compare that to $30/month/seat for HackerRank or Codecademy."

**"Can it work with Python / JavaScript / any language?"**
> "It's a REST API. Any language that can make HTTP requests works. Python, JavaScript, C#, Java, Go, even bash scripts. That's the point — students pick their own tools."

**"What about students who fall behind?"**
> "AI practice mode lets them battle bots instantly — no queue, no opponent needed, no rating impact. They can catch up at their own pace."

**"We need it to align with our standards."**
> "It's mapped to all 15 Wisconsin CS Standards. I have the alignment document ready." (Hand them the standards alignment or pull it up.)

**"How do I know students are actually doing the work?"**
> "The instructor dashboard shows enrollment, lesson completion rates, and a class leaderboard. You can also create class-only tournaments for Week 5."

---

## Target Institutions (Wisconsin Focus)

### Technical Colleges (WTCS)

| Institution | Location | Programs |
|------------|----------|----------|
| Waukesha County Technical College | Pewaukee | IT, Software Dev (home base) |
| Milwaukee Area Technical College | Milwaukee | IT, Cybersecurity, Web Dev |
| Madison Area Technical College | Madison | IT, Software Dev |
| Fox Valley Technical College | Appleton | IT, Software Dev |
| Gateway Technical College | Kenosha/Racine | IT, Web Dev |
| Chippewa Valley Technical College | Eau Claire | IT |
| Northeast Wisconsin Technical College | Green Bay | IT |
| Western Technical College | La Crosse | IT |
| Northcentral Technical College | Wausau | IT |
| Nicolet Area Technical College | Rhinelander | IT |

### Universities

| Institution | Location | Programs |
|------------|----------|----------|
| UW-Milwaukee | Milwaukee | CS, IT |
| UW-Madison | Madison | CS |
| UW-Whitewater | Whitewater | CS, IT |
| UW-Parkside | Kenosha | CS |
| Marquette University | Milwaukee | CS, Data Science |
| MSOE | Milwaukee | Software Engineering |
| Carroll University | Waukesha | CS (local!) |
| Concordia University | Mequon | CS |

### Bootcamps / Workforce

| Institution | Location | Notes |
|------------|----------|-------|
| Dev Bootcamp (various) | Milwaukee/Madison | Short-cohort, high engagement |
| Workforce development boards | Statewide | Grant-funded programs |

---

## Follow-Up Sequence

**After campus visit or cold email:**

1. **Day 1:** Send email with lesson plan link + API docs link
2. **Day 7:** Follow up — "Did you get a chance to look at the lesson plan?"
3. **Day 14:** Offer a pilot — "I'd love to set up a free trial cohort for your next class"
4. **Day 30:** If no response, move to next semester's outreach list

**After a pilot:**

1. Collect feedback from instructor and students
2. Ask for a testimonial / case study
3. Propose full-semester license for next term

---

## Contact

Mark McArthey
Learned Geek Consulting | [learnedgeek.com](https://learnedgeek.com)
Instructor, Waukesha County Technical College
support@apicombat.com

---

*Last updated: February 2026*
