# Lesson Plan: Introduction to REST APIs

**Duration:** 2 weeks (10 class periods, 50 minutes each)
**Level:** Beginner (no prior API or programming experience required)
**Platform:** [API Combat](https://apicombat.com) — "The API is the game"
**Format:** Workshop-style — ideal for summer programs, bootcamp modules, or first-week orientation

---

## Course Overview

A fast-paced introduction to REST APIs for students with zero API experience. Students go from "what's an API?" to fighting battles, reading documentation, and writing their first API client script — all in 10 sessions. Every concept is taught through a live, working API where the stakes are wins, losses, and leaderboard rankings.

---

## Wisconsin CS Standards Alignment

| Standard | Code | How This Curriculum Addresses It |
|----------|------|----------------------------------|
| Describe internet protocols | NI2.b.3.h | HTTP methods, headers, status codes, request/response cycle |
| Encryption & authentication | NI2.d.5.h | HTTPS, JWT Bearer tokens |
| Use API documentation | AP3.c.5.h | OpenAPI spec, API docs page, endpoint catalog |
| Use online resources | AP3.c.6.h | Self-describing API with HATEOAS links |
| Code reuse via APIs | AP2.a.16.h | Building on existing API endpoints |
| Debug systematically | AP6.a.4.h | HTTP error codes, reading error responses |

---

## Week 1: Understanding APIs (5 periods)

### Day 1: What Is an API?

**Discussion (20 min):**
- API = Application Programming Interface — a way for programs to talk to each other
- You use APIs every day: weather apps, social media, payment systems
- REST = a style of API that uses HTTP (the same protocol as web browsing)
- Key concepts: client (you), server (API), request, response

**Demo (15 min):**
Instructor demonstrates a simple API call live:
```bash
curl https://apicombat.com/api/v1/leaderboard?limit=5
```
Walk through: What did we send? What came back? What's JSON?

**Hands-on (15 min):**
Students make their first API call (no account needed — leaderboard is public):
```bash
curl https://apicombat.com/api/v1/leaderboard?limit=3
```
Identify in the response: player names, ratings, win counts.

**Deliverable:** Written answers — What is an API? What did the leaderboard endpoint return?

### Day 2: HTTP Methods & Your First Account

**Lesson (15 min):**
- GET = read data (like viewing a web page)
- POST = send data (like submitting a form)
- PUT = update data
- DELETE = remove data
- Every request has: method, URL, headers, body (optional)
- Every response has: status code, headers, body

**Hands-on (35 min):**
1. Register an account (your first POST request!):
   ```bash
   curl -X POST https://apicombat.com/api/v1/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "username": "your_name",
       "email": "you@school.edu",
       "password": "SecurePass123!"
     }'
   ```
2. Examine the response — find your player ID and token
3. Log in (another POST):
   ```bash
   curl -X POST https://apicombat.com/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "you@school.edu", "password": "SecurePass123!"}'
   ```

**Discussion:** Why does register need `-H "Content-Type: application/json"`? What would happen without it?

**Deliverable:** Screenshot of successful registration response

### Day 3: Headers & Authentication

**Lesson (15 min):**
- Headers = metadata about the request (like an envelope around a letter)
- `Content-Type`: tells the server what format the body is in
- `Authorization`: proves who you are
- JWT (JSON Web Token): a signed token the server trusts
- `Bearer` scheme: "I'm carrying this token as proof of identity"

**Hands-on (35 min):**
1. View your profile (authenticated GET):
   ```bash
   curl https://apicombat.com/api/v1/player/profile \
     -H "Authorization: Bearer YOUR_TOKEN"
   ```
2. View your roster (the units you can use in battle):
   ```bash
   curl https://apicombat.com/api/v1/player/roster \
     -H "Authorization: Bearer YOUR_TOKEN"
   ```
3. Try without the Authorization header — what error do you get?
4. Try with a wrong token — what error do you get?

**Deliverable:** Table showing request (with/without token) and response (status code + body)

### Day 4: Status Codes & Error Handling

**Lesson (10 min):**
- 200 = OK (success)
- 201 = Created (something new was made)
- 400 = Bad Request (you sent something wrong)
- 401 = Unauthorized (who are you?)
- 403 = Forbidden (you can't do that)
- 404 = Not Found (that doesn't exist)
- 429 = Too Many Requests (slow down)
- 500 = Server Error (something broke on their end)

**Hands-on (40 min):**

Trigger each status code and document what you get:

| What You Did | Status Code | What the API Told You |
|-------------|-------------|----------------------|
| GET /api/v1/player/profile (with token) | | |
| GET /api/v1/player/profile (no token) | | |
| POST /api/v1/auth/login (wrong password) | | |
| GET /api/v1/player/nonexistent-endpoint | | |
| POST /api/v1/auth/register (missing fields) | | |
| GET /api/v1/player/profile (garbage token) | | |

**Discussion:** Why are status codes important? How would a program use them?

**Deliverable:** Completed status code exploration table

### Day 5: Reading API Documentation

**Hands-on (40 min):**
1. Visit the API docs: https://apicombat.com/api-docs/v1
2. Find three endpoints you haven't used yet
3. For each endpoint, answer:
   - What HTTP method does it use?
   - What URL path?
   - Does it need authentication?
   - What parameters does it accept?
   - What does it return?
4. Try calling one of the three endpoints

**Discussion (10 min):** Why do APIs have documentation? What makes good API docs vs bad ones?

**Deliverable:** Documentation worksheet for 3 endpoints + proof of calling one

---

## Week 2: Building & Battling (5 periods)

### Day 6: Your First Battle

**Hands-on (45 min):**
1. List AI opponents:
   ```bash
   curl https://apicombat.com/api/v1/ai/opponents \
     -H "Authorization: Bearer $TOKEN"
   ```
2. Pick a novice opponent and fight:
   ```bash
   curl -X POST https://apicombat.com/api/v1/ai/practice \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"teamId": "YOUR_TEAM_ID", "opponentId": "novice-1"}'
   ```
3. Read the battle results — did you win or lose?
4. Fight all 3 novice opponents

**Discussion (5 min):** What data comes back from a battle? How could you use it?

**Deliverable:** Battle results from 3 novice fights

### Day 7: Enrollment & Progress Tracking

**Hands-on (35 min):**
1. Enroll in the class module using the join code your instructor provides:
   ```bash
   curl -X POST https://apicombat.com/api/v1/education/enroll/code/YOUR_JOIN_CODE \
     -H "Authorization: Bearer $TOKEN"
   ```
2. Check your progress:
   ```bash
   curl https://apicombat.com/api/v1/education/my-progress \
     -H "Authorization: Bearer $TOKEN"
   ```
3. View the module details:
   ```bash
   curl https://apicombat.com/api/v1/education/modules/{moduleId} \
     -H "Authorization: Bearer $TOKEN"
   ```
4. Complete lessons as you go:
   ```bash
   curl -X POST https://apicombat.com/api/v1/education/modules/{moduleId}/lessons/0/complete \
     -H "Authorization: Bearer $TOKEN"
   ```

**Discussion (15 min):** Walk through the progress response — what do currentLesson, lessonsCompleted, and progressPercent mean?

**Deliverable:** Screenshot of enrollment response and progress

### Day 8: Writing Your First API Client

**Lesson (10 min):**
So far we've used curl (one request at a time). Real applications make many API calls in sequence. Let's write a script.

**Hands-on (40 min):**

Write a script in your preferred language (Python shown) that:

```python
import requests

BASE = "https://apicombat.com/api/v1"

# Step 1: Log in
resp = requests.post(f"{BASE}/auth/login", json={
    "email": "you@school.edu",
    "password": "SecurePass123!"
})
token = resp.json()["token"]
headers = {"Authorization": f"Bearer {token}"}

# Step 2: View profile
profile = requests.get(f"{BASE}/player/profile", headers=headers)
print(f"Username: {profile.json()['username']}")
print(f"Rating: {profile.json()['rating']}")

# Step 3: Fight an AI opponent
battle = requests.post(f"{BASE}/ai/practice", headers=headers, json={
    "teamId": "YOUR_TEAM_ID",
    "opponentId": "novice-1"
})
result = battle.json()
print(f"Battle result: {'Win!' if result['isWin'] else 'Loss'}")
```

Students adapt this template, fill in their credentials, and run it.

**Deliverable:** Working script that logs in, checks profile, and fights one battle

### Day 9: Putting It All Together

**Hands-on (50 min):**

Build a "complete journey" script that performs every API operation you've learned:

1. Log in
2. View your profile
3. View your roster
4. List AI opponents
5. Fight 3 AI battles (different opponents)
6. Print a summary: wins, losses, final rating

**Bonus challenges (for students who finish early):**
- Check the class leaderboard after your battles
- View your battle history
- Handle errors gracefully (check status codes before reading JSON)

**Deliverable:** Working "journey" script + printed output

### Day 10: Class Tournament & Wrap-Up

**Tournament (30 min):**
Instructor creates a class tournament:
```bash
curl -X POST https://apicombat.com/api/v1/education/modules/{moduleId}/tournament \
  -H "Authorization: Bearer $INSTRUCTOR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"entryFee": 0, "maxParticipants": 32}'
```
Students register and compete.

**Reflection (20 min):**
Written reflection:
1. What is a REST API in your own words?
2. What was the hardest concept? What clicked once you tried it?
3. Name 3 real-world applications that probably use REST APIs
4. What would you build if you could use any API?

**Final deliverable:** Completed reflection + all scripts from the course

---

## Materials & Resources

- **API Documentation:** https://apicombat.com/api-docs/v1
- **OpenAPI Spec:** https://apicombat.com/openapi/v1.json
- **Education Mode Guide:** See EDUCATION_MODE.md for instructor setup
- **Pre-built module:** Join code `BASICS01` for the API Basics 101 module (6 lessons)
- **Tools needed:** curl (comes with macOS/Linux; Windows: Git Bash or WSL), plus Python/Node.js for scripting days

## Assessment Summary

| Component | Weight | Description |
|-----------|--------|-------------|
| Daily deliverables | 40% | Completed worksheets, screenshots, tables from each session |
| Scripts | 35% | Working API client (Day 8) + journey script (Day 9) |
| Final reflection | 15% | Thoughtful written reflection on what was learned |
| Participation | 10% | Engagement with class discussions and pair activities |

## Curriculum Module Setup

```bash
curl -X POST https://apicombat.com/api/v1/education/modules \
  -H "Authorization: Bearer $INSTRUCTOR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Introduction to REST APIs",
    "description": "Go from zero to API in 2 weeks. Learn HTTP methods, JSON, authentication, and build your first API client.",
    "difficulty": "beginner",
    "lessons": [
      {
        "title": "Your First API Call",
        "objective": "Make a GET request to a public endpoint and read the JSON response.",
        "endpoint": "GET /api/v1/leaderboard",
        "hint": "No authentication needed. Try adding ?limit=3 to the URL."
      },
      {
        "title": "Register an Account",
        "objective": "Send a POST request with a JSON body to create your player account.",
        "endpoint": "POST /api/v1/auth/register",
        "hint": "Set Content-Type to application/json. Send username, email, and password."
      },
      {
        "title": "Authenticate and View Profile",
        "objective": "Log in to get a JWT token, then use it to access your profile.",
        "endpoint": "GET /api/v1/player/profile",
        "hint": "Log in first (POST /api/v1/auth/login), then use the token: Authorization: Bearer <token>"
      },
      {
        "title": "Explore Error Codes",
        "objective": "Trigger 401, 400, and 404 errors and document what the API returns.",
        "endpoint": "GET /api/v1/player/profile",
        "hint": "Try without a token (401), with a bad token (401), and a nonexistent path (404)."
      },
      {
        "title": "Fight Your First Battle",
        "objective": "Use the AI practice endpoint to fight a novice opponent.",
        "endpoint": "POST /api/v1/ai/practice",
        "hint": "List opponents with GET /api/v1/ai/opponents. Send teamId and opponentId."
      },
      {
        "title": "Check Your Progress",
        "objective": "Enroll in this module and check your progress via the API.",
        "endpoint": "GET /api/v1/education/my-progress",
        "hint": "You should already be enrolled. This shows your lesson completion across all modules."
      }
    ]
  }'
```

---

## Instructor Notes

### Before the Course
1. Create the education module (use the curl command above or the web UI)
2. Share the join code with students
3. Verify students have access to: a terminal with curl, a text editor, Python or Node.js
4. Test the API yourself — make sure you can register, battle, and see the leaderboard

### Common Student Issues
- **"curl is not recognized"** — Windows students need Git Bash, WSL, or PowerShell 7+
- **"401 Unauthorized"** — Token expired or missing. Have them log in again.
- **"400 Bad Request"** — Usually a JSON formatting error. Check for missing quotes or commas.
- **Token storage** — Students will lose their tokens. Show them how to save to a variable: `TOKEN=$(curl ... | jq -r '.token')`

### Adapting for Shorter Programs
- **1-week version:** Days 1-6 (skip scripting, focus on curl + concepts)
- **3-day workshop:** Days 1, 2, 6 (register, understand HTTP, fight a battle)
- **1-day demo:** Day 1 + Day 6 first half (concepts + one battle)
