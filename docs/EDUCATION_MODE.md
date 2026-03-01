# Education Mode — Complete Guide

API Combat's Education Mode turns REST API concepts into competitive gameplay. Instructors create curriculum modules with guided lessons tied to real API endpoints. Students learn by doing — every lesson is an API call.

This guide walks through every education feature from both the **instructor** and **student** perspective.

---

## Table of Contents

1. [Getting Educator Access](#1-getting-educator-access)
2. [Creating a Curriculum Module](#2-creating-a-curriculum-module)
3. [Publishing Your Module](#3-publishing-your-module)
4. [Enrolling Students](#4-enrolling-students)
5. [Student Workflow](#5-student-workflow)
6. [Completing Lessons](#6-completing-lessons)
7. [Tracking Progress](#7-tracking-progress)
8. [Instructor Dashboard](#8-instructor-dashboard)
9. [Class Leaderboard](#9-class-leaderboard)
10. [Class Tournaments](#10-class-tournaments)
11. [AI Practice & Batch Practice](#11-ai-practice--batch-practice)
12. [Unenrolling](#12-unenrolling)
13. [Pre-Built Module: API Basics 101](#13-pre-built-module-api-basics-101)
14. [Endpoint Reference](#14-endpoint-reference)

---

## 1. Getting Educator Access

Educator status is required to create modules, publish them, view the instructor dashboard, and create class tournaments. There are two ways to get it:

**Option A: Verified .edu email** — Register with a `.edu` email address. Educator status is granted automatically.

**Option B: Admin-granted** — Contact support@apicombat.com or ask a platform admin to enable educator status on your account. Admins use:

```
POST /api/v1/admin/players/{playerId}/toggle-educator
```

Once you have educator access, all instructor endpoints become available.

---

## 2. Creating a Curriculum Module

A module is a collection of ordered lessons, each tied to a specific API endpoint. Students work through lessons sequentially, learning API concepts by calling real endpoints.

```bash
curl -X POST https://apicombat.com/api/v1/education/modules \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "REST Fundamentals",
    "description": "Learn HTTP methods, authentication, and JSON payloads through gameplay.",
    "difficulty": "beginner",
    "lessons": [
      {
        "title": "Register an Account",
        "objective": "Create an account using POST and receive a JWT token.",
        "endpoint": "POST /api/v1/auth/register",
        "hint": "Send username, email, and password as JSON in the request body."
      },
      {
        "title": "Log In",
        "objective": "Authenticate with your credentials and receive a fresh token.",
        "endpoint": "POST /api/v1/auth/login",
        "hint": "Use the email and password from registration."
      },
      {
        "title": "View Your Profile",
        "objective": "Make an authenticated GET request using your JWT token.",
        "endpoint": "GET /api/v1/player/profile",
        "hint": "Pass the token in the Authorization header: Bearer <token>"
      }
    ]
  }'
```

**Response** (201 Created):
```json
{
  "id": "a1b2c3d4-...",
  "instructorUsername": "prof_smith",
  "title": "REST Fundamentals",
  "description": "Learn HTTP methods, authentication, and JSON payloads through gameplay.",
  "difficulty": "beginner",
  "lessonCount": 3,
  "enrolledCount": 0,
  "isPublished": false,
  "joinCode": "REST2026",
  "createdAt": "2026-02-28T10:00:00Z"
}
```

**Key fields:**
- `difficulty` — Must be `beginner`, `intermediate`, or `advanced`
- `joinCode` — Auto-generated. Share this with students for direct enrollment.
- `isPublished` — Starts `false`. Students can't see it until you publish.
- Each lesson's `endpoint` — The API endpoint students will call to complete the lesson
- `hint` — Optional guidance shown to students
- `verificationEndpoint` / `verificationMethod` — Optional. If set, calling this endpoint auto-completes the lesson.

**Validation limits:**
- Title: 1–100 characters (required)
- Description: up to 500 characters
- At least 1 lesson required
- Lesson titles: 1–100 characters
- Lesson objectives: 1–500 characters
- Lesson endpoints: 1–200 characters

---

## 3. Publishing Your Module

Modules start unpublished (draft). Publish when you're ready for students to discover and enroll.

```bash
curl -X POST https://apicombat.com/api/v1/education/modules/{moduleId}/publish \
  -H "Authorization: Bearer $TOKEN"
```

**Response:**
```json
{
  "message": "Module published and visible to all players."
}
```

Once published, the module appears in the public module list and students can enroll. Publishing is one-way — there's no unpublish.

---

## 4. Enrolling Students

Students can enroll in two ways:

### Option A: By Module ID (student browses the catalog)

Students browse published modules:
```bash
curl https://apicombat.com/api/v1/education/modules \
  -H "Authorization: Bearer $TOKEN"
```

Then enroll by ID:
```bash
curl -X POST https://apicombat.com/api/v1/education/enroll/{moduleId} \
  -H "Authorization: Bearer $TOKEN"
```

### Option B: By Join Code (instructor shares a code)

This is the recommended classroom flow. Share your module's join code (e.g., `BASICS01`) and students enroll directly:

```bash
curl -X POST https://apicombat.com/api/v1/education/enroll/code/BASICS01 \
  -H "Authorization: Bearer $TOKEN"
```

**Response** (both methods):
```json
{
  "currentLesson": 0,
  "lessonsCompleted": 0,
  "totalLessons": 6,
  "progressPercent": 0,
  "isCompleted": false
}
```

Join codes work for both published and unpublished modules — useful for private beta testing with a small group before publishing to the wider platform.

---

## 5. Student Workflow

After enrolling, students follow this loop:

1. **View the module** to see all lessons and their current progress:
   ```bash
   curl https://apicombat.com/api/v1/education/modules/{moduleId} \
     -H "Authorization: Bearer $TOKEN"
   ```

2. **Read the current lesson** — each lesson shows:
   - `title` — What this lesson teaches
   - `objective` — What the student needs to accomplish
   - `endpoint` — The API endpoint to call
   - `hint` — Optional guidance

3. **Call the endpoint** described in the lesson (this is the actual learning)

4. **Mark the lesson complete** (see next section)

5. **Repeat** until all lessons are done

---

## 6. Completing Lessons

After completing the task described in a lesson, students mark it done:

```bash
curl -X POST https://apicombat.com/api/v1/education/modules/{moduleId}/lessons/{lessonIndex}/complete \
  -H "Authorization: Bearer $TOKEN"
```

- `lessonIndex` is 0-based (first lesson = 0, second = 1, etc.)
- Students can complete lessons in any order
- Each lesson can only be completed once

**Response:**
```json
{
  "currentLesson": 2,
  "lessonsCompleted": 2,
  "totalLessons": 6,
  "progressPercent": 33.33,
  "isCompleted": false
}
```

When all lessons are completed, `isCompleted` flips to `true` and `progressPercent` hits 100.

**Auto-completion:** If a lesson has a `verificationEndpoint` configured, the student can complete the lesson by successfully calling that endpoint — no manual `/complete` call needed.

---

## 7. Tracking Progress

### Student: Check your own progress

See all your enrolled modules and progress:

```bash
curl https://apicombat.com/api/v1/education/my-progress \
  -H "Authorization: Bearer $TOKEN"
```

**Response:**
```json
[
  {
    "currentLesson": 4,
    "lessonsCompleted": 4,
    "totalLessons": 6,
    "progressPercent": 66.67,
    "isCompleted": false
  }
]
```

### Student: Check progress in a specific module

View the module detail endpoint — your progress is included in the `myProgress` field:

```bash
curl https://apicombat.com/api/v1/education/modules/{moduleId} \
  -H "Authorization: Bearer $TOKEN"
```

---

## 8. Instructor Dashboard

View aggregate analytics across all modules you've created:

```bash
curl https://apicombat.com/api/v1/education/instructor/dashboard \
  -H "Authorization: Bearer $TOKEN"
```

**Response:**
```json
{
  "totalModules": 3,
  "publishedModules": 2,
  "totalStudents": 47,
  "studentsCompleted": 12,
  "modules": [
    {
      "id": "a1b2c3d4-...",
      "title": "REST Fundamentals",
      "enrolledCount": 28,
      "completedCount": 8,
      "averageProgress": 71.5
    },
    {
      "id": "e5f6a7b8-...",
      "title": "Advanced Strategies",
      "enrolledCount": 19,
      "completedCount": 4,
      "averageProgress": 45.2
    }
  ]
}
```

**Use this to:**
- See which modules have the highest completion rates
- Identify modules where students are getting stuck (low `averageProgress`)
- Track total student engagement across your curriculum

---

## 9. Class Leaderboard

Each module has its own leaderboard showing only enrolled students — not the global leaderboard. This keeps the competition within your classroom.

```bash
curl https://apicombat.com/api/v1/education/modules/{moduleId}/leaderboard \
  -H "Authorization: Bearer $TOKEN"
```

**Who can view:** Enrolled students and the module instructor.

**Response:**
```json
[
  {
    "rank": 1,
    "username": "alice_dev",
    "rating": 1250,
    "wins": 15,
    "losses": 3,
    "winRate": 83.3,
    "lessonsCompleted": 6
  },
  {
    "rank": 2,
    "username": "bob_codes",
    "rating": 1180,
    "wins": 12,
    "losses": 5,
    "winRate": 70.6,
    "lessonsCompleted": 5
  }
]
```

Students are ranked by rating, with wins, losses, win rate, and lesson progress shown. This drives friendly competition and shows students who are both learning (lessons) and applying (battles).

---

## 10. Class Tournaments

Instructors can create tournaments restricted to enrolled students. These are great for week-end capstones, midterm events, or final assessments.

```bash
curl -X POST https://apicombat.com/api/v1/education/modules/{moduleId}/tournament \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "entryFee": 0,
    "maxParticipants": 16
  }'
```

**Response** (201 Created):
```json
{
  "tournamentId": "f47ac10b-...",
  "message": "Class tournament created. Students can register now."
}
```

**Parameters:**
- `entryFee` — Gold cost to enter (0–10,000). Set to `0` for classroom use so students can participate freely.
- `maxParticipants` — Tournament bracket size (2–128). Default 16.

**After creation:** Students enrolled in the module can register for the tournament via the standard tournament endpoints (`POST /api/v1/tournament/{id}/register`). Only enrolled students are eligible — the tournament checks module enrollment.

Students can then check brackets, match results, and standings through the regular tournament API.

---

## 11. AI Practice & Batch Practice

These aren't education-specific endpoints, but they're essential for classroom use.

### AI Practice (single battle)

Students fight AI opponents for instant feedback — no waiting for matchmaking, no rating risk.

```bash
# List AI opponents (3 difficulty tiers)
curl https://apicombat.com/api/v1/ai/opponents \
  -H "Authorization: Bearer $TOKEN"

# Fight an AI opponent
curl -X POST https://apicombat.com/api/v1/ai/practice \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "teamId": "your-team-id",
    "opponentId": "novice-1"
  }'
```

**Key details:**
- 3 difficulty tiers: novice, intermediate, expert
- Rewards: 50% gold and XP (no rating change)
- Doesn't count against daily battle limit
- Instant results — no matchmaking queue
- Perfect for homework assignments ("beat all 3 novice opponents")

### Batch Practice (statistical analysis)

Run up to 200 simulated battles in one API call and get aggregate statistics back. No rewards, no rating impact — pure simulation.

```bash
curl -X POST https://apicombat.com/api/v1/ai/batch-practice \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "teamId": "your-team-id",
    "opponentId": "intermediate-2",
    "count": 100
  }'
```

**Response:**
```json
{
  "totalBattles": 100,
  "wins": 67,
  "losses": 33,
  "winRate": 67.0,
  "avgTurns": 8.4,
  "opponentName": "Blade Dancer"
}
```

**Classroom use cases:**
- **Data analysis assignment:** "Run 100 battles with formation A, then 100 with formation B. Which wins more? Write up your analysis."
- **Strategy optimization:** "Achieve a 75%+ win rate against the intermediate AI. Document what you changed."
- **Statistics lesson:** "Is your sample size large enough? Run 10 vs 100 vs 200 — do the win rates converge?"

---

## 12. Unenrolling

Students can unenroll from a module at any time:

```bash
curl -X DELETE https://apicombat.com/api/v1/education/enroll/{moduleId} \
  -H "Authorization: Bearer $TOKEN"
```

**Response:**
```json
{
  "message": "Unenrolled from module."
}
```

This removes the enrollment record and all progress. Students can re-enroll later, but progress starts over.

---

## 13. Pre-Built Module: API Basics 101

A starter module ships with the platform — no instructor setup needed. It walks students from zero to their first battle in 6 lessons:

| Lesson | Title | Endpoint | What Students Learn |
|--------|-------|----------|-------------------|
| 0 | Register | `POST /api/v1/auth/register` | POST requests, JSON bodies, receiving tokens |
| 1 | Log In | `POST /api/v1/auth/login` | Authentication, credential exchange |
| 2 | View Profile | `GET /api/v1/player/profile` | GET requests, Authorization headers, JWT |
| 3 | Check Roster | `GET /api/v1/player/roster` | Reading JSON arrays, understanding game state |
| 4 | Queue a Battle | `POST /api/v1/battle/queue` | POST with auth, async operations |
| 5 | Check Results | `GET /api/v1/battle/results/{id}` | Path parameters, polling patterns |

**Join code:** `BASICS01`

Use this as-is for a first-day exercise, or as a template for your own modules.

---

## 14. Endpoint Reference

All education endpoints are under `api/v1/education`. All require authentication (JWT Bearer token or cookie).

### Instructor Endpoints (require educator status)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/education/modules` | Create a curriculum module |
| `POST` | `/api/v1/education/modules/{id}/publish` | Publish a module |
| `GET` | `/api/v1/education/instructor/dashboard` | View instructor analytics |
| `POST` | `/api/v1/education/modules/{id}/tournament` | Create a class tournament |

### Student Endpoints (any authenticated user)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/education/modules` | List published modules |
| `GET` | `/api/v1/education/modules/{id}` | View module details + your progress |
| `POST` | `/api/v1/education/enroll/{id}` | Enroll by module ID |
| `POST` | `/api/v1/education/enroll/code/{code}` | Enroll by join code |
| `POST` | `/api/v1/education/modules/{id}/lessons/{index}/complete` | Complete a lesson |
| `GET` | `/api/v1/education/my-progress` | View all enrolled modules + progress |
| `GET` | `/api/v1/education/modules/{id}/leaderboard` | View class leaderboard |
| `DELETE` | `/api/v1/education/enroll/{id}` | Unenroll from a module |

### Related Endpoints (available to all players)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/ai/opponents` | List AI practice opponents |
| `POST` | `/api/v1/ai/practice` | Fight an AI opponent (instant) |
| `POST` | `/api/v1/ai/batch-practice` | Run N simulated battles for stats |
