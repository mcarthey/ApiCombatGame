# Lesson Plan: API Security & Authentication

**Duration:** 3 weeks (15 class periods, 50 minutes each)
**Level:** Intermediate (assumes basic HTTP/REST knowledge)
**Platform:** [API Combat](https://apicombat.com) — "The API is the game"
**Prerequisites:** Students should be able to make HTTP requests (curl, Postman, or a scripting language)

---

## Course Overview

Students explore authentication, authorization, and API security through hands-on gameplay. They'll register accounts, manage JWT tokens, understand rate limiting, generate API keys, and analyze how security mechanisms protect real systems — all by interacting with a live API that fights back.

---

## Wisconsin CS Standards Alignment

| Standard | Code | How This Curriculum Addresses It |
|----------|------|----------------------------------|
| Describe internet protocols | NI2.b.3.h | HTTP methods, headers, status codes, HTTPS |
| Encryption & authentication | NI2.d.5.h | JWT tokens, Bearer auth, password hashing, API keys |
| Debug systematically | AP6.a.4.h | HTTP 401/403/429 error analysis, rate limit headers |
| Use API documentation | AP3.c.5.h | OpenAPI spec, interactive API docs |
| Code reuse via APIs | AP2.a.16.h | Building authenticated API clients |
| Design algorithmic solutions | AP1.a.8.h | Token refresh logic, backoff algorithms |

---

## Week 1: Identity & Authentication (5 periods)

### Learning Objectives
- Explain the difference between authentication and authorization
- Register an account and receive a JWT token
- Use Bearer tokens in HTTP requests
- Understand token expiration and refresh

### Day 1: What Is Authentication?

**Discussion (15 min):** How does a website know who you are? Passwords, sessions, tokens — how do APIs handle it differently from browsers?

**Hands-on (30 min):**
1. Register an account via the API:
   ```bash
   curl -X POST https://apicombat.com/api/v1/auth/register \
     -H "Content-Type: application/json" \
     -d '{"username": "your_name", "email": "you@school.edu", "password": "YourPass123!"}'
   ```
2. Examine the response — identify the JWT token
3. Decode the JWT at jwt.io — what's in the payload? What's the expiry?

**Deliverable:** Screenshot of decoded JWT with annotations identifying each claim

### Day 2: Using Your Token

**Hands-on (40 min):**
1. Log in to get a fresh token:
   ```bash
   curl -X POST https://apicombat.com/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "you@school.edu", "password": "YourPass123!"}'
   ```
2. Use the token to access your profile:
   ```bash
   curl https://apicombat.com/api/v1/player/profile \
     -H "Authorization: Bearer YOUR_TOKEN_HERE"
   ```
3. Try without the token — what happens? (Expected: 401 Unauthorized)
4. Try with a tampered token — what happens?

**Discussion (10 min):** Why does the server reject tampered tokens? (Signature verification)

**Deliverable:** Log of successful and failed requests with HTTP status codes annotated

### Day 3: Token Lifecycle

**Hands-on (35 min):**
1. Check your token's expiration time
2. Use the refresh endpoint before expiry:
   ```bash
   curl -X POST https://apicombat.com/api/v1/auth/refresh \
     -H "Authorization: Bearer YOUR_TOKEN_HERE"
   ```
3. Write a script (Python, JavaScript, or language of choice) that:
   - Logs in
   - Stores the token
   - Makes 3 authenticated API calls
   - Handles token expiration gracefully

**Discussion (15 min):** What happens if someone steals your token? How long should tokens live? Tradeoffs between convenience and security.

**Deliverable:** Working script with token management

### Day 4: Authentication Failures Lab

**Hands-on (40 min):**

Deliberately trigger every auth-related error and document what you get:

| Scenario | Expected Status | Actual Status | Response Body |
|----------|----------------|---------------|---------------|
| No Authorization header | 401 | | |
| Expired token | 401 | | |
| Malformed token | 401 | | |
| Wrong password on login | 401 | | |
| Nonexistent email on login | 401 | | |
| Valid token, wrong endpoint | varies | | |

**Discussion (10 min):** Why do login failures say "Invalid credentials" instead of "Wrong password" or "Email not found"? (Information disclosure prevention)

**Deliverable:** Completed error matrix with analysis

### Day 5: Week 1 Assessment

**Written component (20 min):**
- Explain how JWT authentication works in 3-5 sentences
- What are the three parts of a JWT? What does each contain?
- Why is HTTPS important when sending Bearer tokens?

**Practical component (30 min):**
- Write a script that registers a new account, logs in, views the profile, and views the roster — all in sequence
- Script must handle errors (print the status code and message if any step fails)

---

## Week 2: Authorization & Rate Limiting (5 periods)

### Learning Objectives
- Distinguish authentication from authorization (403 vs 401)
- Read and respect rate limit headers
- Implement exponential backoff
- Understand API keys as an alternative auth mechanism

### Day 6: Authorization — Who Can Do What?

**Hands-on (35 min):**
1. As a regular player, try to access admin endpoints:
   ```bash
   curl https://apicombat.com/api/v1/admin/dashboard \
     -H "Authorization: Bearer YOUR_TOKEN_HERE"
   ```
   Expected: 403 Forbidden
2. Try accessing another player's data vs your own
3. Try creating an education module (requires educator status):
   ```bash
   curl -X POST https://apicombat.com/api/v1/education/modules \
     -H "Authorization: Bearer YOUR_TOKEN_HERE" \
     -H "Content-Type: application/json" \
     -d '{"title": "Test", "description": "Test", "difficulty": "beginner", "lessons": [{"title": "L1", "objective": "O1", "endpoint": "GET /test"}]}'
   ```
   Expected: 403 (unless you have educator status)

**Discussion (15 min):** 401 vs 403 — when do you get each? Why does the distinction matter?

**Deliverable:** Table comparing 401 and 403 responses with real examples from the API

### Day 7: Rate Limiting — The Speed Bump

**Hands-on (40 min):**
1. Make several rapid API calls and observe the rate limit headers:
   ```
   X-RateLimit-Limit: 100
   X-RateLimit-Remaining: 97
   X-RateLimit-Reset: 1709164800
   ```
2. Write a script that makes requests in a tight loop until it hits a 429
3. Read the `Retry-After` header — how long must you wait?

**Discussion (10 min):** Why do APIs rate-limit? What would happen without it? (DDoS, resource exhaustion, fair usage)

**Deliverable:** Script output showing rate limit headers decreasing, then the 429 response

### Day 8: Exponential Backoff

**Hands-on (40 min):**

Write a "respectful API client" that:
1. Makes a request
2. If it gets 429, waits `Retry-After` seconds (or 2^attempt seconds if no header)
3. Retries up to 3 times
4. Logs each attempt with timestamp

Test it by running rapid-fire AI practice battles:
```bash
# Run this in a loop to trigger rate limiting
curl -X POST https://apicombat.com/api/v1/ai/practice \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"teamId": "YOUR_TEAM_ID", "opponentId": "novice-1"}'
```

**Discussion (10 min):** What's "exponential" about exponential backoff? Why not just wait a fixed time?

**Deliverable:** Working backoff script with logged retry attempts

### Day 9: API Keys

**Hands-on (35 min):**
1. Generate an API key from your account settings (web UI)
2. Use the API key instead of JWT:
   ```bash
   curl https://apicombat.com/api/v1/player/profile \
     -H "X-Api-Key: acg_your_key_here"
   ```
3. Compare: When would you use an API key vs a JWT token?

**Discussion (15 min):**
- API keys: long-lived, no expiry management, good for scripts/bots
- JWT: short-lived, carries user claims, good for interactive sessions
- When should you rotate API keys? How do you revoke a compromised key?

**Deliverable:** Written comparison of JWT vs API key authentication (pros, cons, use cases)

### Day 10: Week 2 Assessment

**Practical lab (50 min):**

Build a "security-aware bot" that:
1. Logs in and stores the token
2. Checks rate limit headers on every response
3. Implements exponential backoff for 429s
4. Fights 5 AI practice battles
5. Logs every request with: timestamp, endpoint, status code, rate limit remaining
6. Handles 401 by re-authenticating automatically

**Grading rubric:**
- Authentication works (20%)
- Rate limit headers read correctly (20%)
- Backoff logic implemented (25%)
- Error handling for 401/403/429 (25%)
- Code quality and logging (10%)

---

## Week 3: Threat Modeling & Defense (5 periods)

### Learning Objectives
- Identify common API security threats
- Analyze how the API defends against attacks
- Document security observations in a threat model
- Present findings to the class

### Day 11: Threat Modeling Introduction

**Discussion (20 min):** What could go wrong with an API? Brainstorm attack categories:
- Credential stuffing, brute force
- Token theft
- Parameter tampering
- Injection attacks
- Rate limit abuse

**Hands-on (30 min):**
Test each category against the API Combat API (safely, with your own account):
1. Try SQL injection in login fields — what happens?
2. Try very long strings in registration fields — what happens?
3. Try negative values or huge numbers in API requests
4. Document every response: status code, error message, behavior

**Deliverable:** Initial threat model table with attack category, test, and API response

### Day 12: HTTPS & Transport Security

**Hands-on (30 min):**
1. Use `curl -v` to see the TLS handshake:
   ```bash
   curl -v https://apicombat.com/api/v1/player/profile \
     -H "Authorization: Bearer $TOKEN" 2>&1 | head -30
   ```
2. Identify: TLS version, cipher suite, certificate issuer
3. Try HTTP (non-HTTPS) — does the API redirect or reject?

**Discussion (20 min):**
- Why can't someone on the same WiFi read your JWT if you use HTTPS?
- What is a man-in-the-middle attack?
- Certificate pinning — what is it and when is it used?

**Deliverable:** Annotated TLS handshake output with each step explained

### Day 13: Security Review Lab

**Group activity (50 min):**

In pairs, review the API Combat OpenAPI spec at `https://apicombat.com/openapi/v1.json` and answer:
1. Which endpoints require authentication? Which don't?
2. Are there any endpoints that should require auth but don't?
3. What data is returned in error messages? Is any of it sensitive?
4. How does the API prevent one player from modifying another's data?
5. What validation does the API perform on input? (Try boundary values)

**Deliverable:** Security review document with findings and recommendations

### Day 14: Threat Model Presentations

**Presentations (50 min):**

Each pair presents their security findings (5-7 minutes each):
- Attack surface identified
- Defenses observed
- Potential improvements suggested
- Most interesting finding

### Day 15: Final Assessment

**Written exam (25 min):**
1. Explain the difference between authentication and authorization with examples
2. Describe three API security mechanisms you observed and how they work
3. What is rate limiting and why is it important?
4. A user reports their account was compromised. What steps should the API provider take?

**Practical exam (25 min):**
Build a script that demonstrates secure API usage:
- Proper token management (store securely, refresh before expiry)
- Rate limit awareness (read headers, backoff on 429)
- Error handling (401 re-auth, 403 graceful failure, 5xx retry)
- Clean logging of all security-relevant events

---

## Materials & Resources

- **API Documentation:** https://apicombat.com/api-docs/v1
- **OpenAPI Spec:** https://apicombat.com/openapi/v1.json
- **Education Mode Guide:** See EDUCATION_MODE.md for enrollment, progress tracking, and class tournaments
- **Join Code:** Instructor creates a custom module — share the join code with students
- **AI Practice:** Free, instant battles for safe experimentation: `POST /api/v1/ai/practice`

## Assessment Summary

| Component | Weight | Description |
|-----------|--------|-------------|
| Weekly scripts | 30% | Authentication client (W1), security-aware bot (W2) |
| Error matrices & logs | 20% | Documented exploration of auth failures, rate limits |
| Security review | 25% | Threat model + peer review of API security |
| Final exam | 15% | Written + practical security knowledge |
| Participation | 10% | In-class discussion, pair work, presentations |

## Curriculum Module Setup

Create this as an API Combat education module for progress tracking:

```bash
curl -X POST https://apicombat.com/api/v1/education/modules \
  -H "Authorization: Bearer $INSTRUCTOR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "API Security & Authentication",
    "description": "Explore JWT tokens, rate limiting, API keys, and threat modeling through live API gameplay.",
    "difficulty": "intermediate",
    "lessons": [
      {
        "title": "Register & Get Your First Token",
        "objective": "Create an account and receive a JWT token.",
        "endpoint": "POST /api/v1/auth/register",
        "hint": "Send username, email, and password as JSON."
      },
      {
        "title": "Authenticate & Access Your Profile",
        "objective": "Log in with credentials and use the Bearer token to access a protected endpoint.",
        "endpoint": "GET /api/v1/player/profile",
        "hint": "Pass the token in the Authorization header: Bearer <token>"
      },
      {
        "title": "Refresh Your Token",
        "objective": "Use the refresh endpoint to get a new token before the old one expires.",
        "endpoint": "POST /api/v1/auth/refresh",
        "hint": "Send your current valid token in the Authorization header."
      },
      {
        "title": "Trigger & Handle Rate Limiting",
        "objective": "Make rapid requests until you receive a 429 response and read the Retry-After header.",
        "endpoint": "GET /api/v1/player/roster",
        "hint": "Watch the X-RateLimit-Remaining header decrease. When it hits 0, you get 429."
      },
      {
        "title": "Fight Your First AI Battle",
        "objective": "Use your authenticated client to fight an AI opponent.",
        "endpoint": "POST /api/v1/ai/practice",
        "hint": "You need a teamId — check your roster first to see your units."
      },
      {
        "title": "Analyze Battle Security",
        "objective": "Review the OpenAPI spec and identify which endpoints require authentication.",
        "endpoint": "GET /api/v1/sdk/endpoints",
        "hint": "Look for endpoints that return 401 without a token vs those that work anonymously."
      }
    ]
  }'
```
