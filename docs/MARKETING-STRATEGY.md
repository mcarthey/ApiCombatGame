# API Combat Game - Marketing & Promotion Strategy

**Version:** 1.0  
**Date:** February 10, 2026  
**Author:** Mark (Learned Geek Consulting)  
**Purpose:** Complete go-to-market strategy for developer-focused API game

---

## Table of Contents

1. [Core Positioning](#core-positioning)
2. [Platform-by-Platform Strategy](#platform-by-platform-strategy)
3. [Launch Timeline](#launch-timeline)
4. [Content Marketing](#content-marketing)
5. [Influencer Outreach](#influencer-outreach)
6. [Educational Partnerships](#educational-partnerships)
7. [Monetization Channels](#monetization-channels)
8. [Growth Metrics & KPIs](#growth-metrics--kpis)
9. [Cross-Promotion Opportunities](#cross-promotion-opportunities)
10. [Ready-to-Use Templates](#ready-to-use-templates)

---

## Core Positioning

### Target Audience

**Primary:**
- Backend developers (especially .NET)
- API enthusiasts
- DevOps engineers who love automation
- Computer science students
- Developers who play incremental/idle games

**Secondary:**
- Coding bootcamp students
- Self-taught developers building portfolios
- Tech lead managers looking for team-building activities
- Developer advocates at tech companies

### Unique Selling Proposition (USP)

**One-liner:** "The only game where building the UI is part of the game."

**Variations:**
- "An API-only combat game for developers who love optimizing code"
- "No graphics. No UI. Just pure API-driven strategic combat."
- "Build your client, write your strategy, dominate the leaderboard."
- "The game where Postman is your game controller"

### Why This Isn't a Steam Game

**Steam users expect:**
- ✅ Downloadable executable
- ✅ Visual graphics
- ✅ Controller support
- ✅ Achievements (visual)
- ✅ Traditional game progression

**Your game offers:**
- ❌ API endpoints
- ❌ JSON responses
- ❌ Terminal-based interaction
- ✅ Achievements (via API)
- ✅ Progression (data-driven)

**Conclusion:** Steam is the wrong platform. Your audience lives on GitHub, HN, and Reddit.

---

## Platform-by-Platform Strategy

### Primary Platforms (Essential)

#### 1. GitHub

**Purpose:** Open-source game engine, client SDKs, community showcase

**Repositories to Create:**

```
api-combat-game/
├── game-engine (main repo)
├── client-sdk-csharp (official C# client)
├── client-sdk-python (official Python client)
├── client-sdk-javascript (official JS client)
├── example-clients
│   ├── cli-client
│   ├── web-dashboard
│   └── mobile-mockup
├── awesome-api-combat-game (community showcase)
└── documentation
```

**README.md Template:**

```markdown
# API Combat Game

An API-only strategic combat game where developers build their own clients.

## What is this?

No GUI provided. You interact with the game entirely through a RESTful API. 
Build your own dashboard, automate your strategies, and compete on the leaderboard.

## Quick Start

```bash
# Register account
curl -X POST https://api.combatgame.dev/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"YourPass123!"}'

# Get your roster
curl -X GET https://api.combatgame.dev/v1/player/roster \
  -H "Authorization: Bearer YOUR_TOKEN"

# Queue a battle
curl -X POST https://api.combatgame.dev/v1/battle/queue \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"teamId":"your-team-id","mode":"ranked"}'
```

## Why?

Because writing code to play a game is more fun than clicking buttons.

## Getting Started

[Link to full documentation]

## Community Clients

Check out what others have built: [awesome-api-combat-game](link)

## License

MIT
```

**Marketing Actions:**

- [ ] Create organization: `api-combat-game`
- [ ] Publish main repo (public)
- [ ] Add topics: `game`, `api`, `dotnet`, `csharp`, `developer-tools`
- [ ] Create detailed README with badges (build status, license, Discord)
- [ ] Add CONTRIBUTING.md to encourage community
- [ ] Set up GitHub Discussions for strategy talk
- [ ] Pin important repos
- [ ] Create GitHub Project board (public roadmap)

**Expected Impact:** 500-2000 stars in first 6 months

---

#### 2. Product Hunt

**Perfect Fit:** Developer tools and indie projects dominate PH

**Launch Checklist:**

**Pre-Launch (2 weeks before):**
- [ ] Create Product Hunt account
- [ ] Engage in community (upvote, comment on other products)
- [ ] Prepare assets:
  - [ ] 3-5 screenshots of different client UIs
  - [ ] 1-minute demo video (Loom or similar)
  - [ ] Logo/icon (256x256px)
  - [ ] Gallery images (1270x760px)
- [ ] Write product description (260 chars max for tagline)
- [ ] Identify 5-10 "hunter" friends to upvote at launch

**Tagline Options:**
- "An API-only game where developers build their own clients"
- "No GUI, just APIs. Strategy game for backend devs."
- "The game where coding your client is part of the game"

**Description Template:**

```
API Combat Game is a strategic PvP game with a twist: there's no UI. 

You interact entirely through a RESTful API. Build your own client 
(web, mobile, CLI, whatever), configure your battle strategy, and 
compete on the global leaderboard.

FEATURES:
• RESTful API for all game actions
• Configure AI-driven battle strategies (JSON or scripting)
• Async battles (queue and check results later)
• Official SDKs in C#, Python, JavaScript
• Public leaderboards and tournaments

PERFECT FOR:
• Backend developers who love optimization
• Developers learning API consumption
• Teams looking for a unique hackathon project
• Anyone who enjoys incremental/automation games

Try it free: [link]
Build a client: [GitHub link]
Join Discord: [link]
```

**Launch Day Strategy:**

**Timing:** Tuesday-Thursday, 12:01 AM PST (first in queue for the day)

**Hour-by-Hour Plan:**

- **12:01 AM**: Submit product
- **6:00 AM**: Post on Twitter announcing PH launch
- **7:00 AM**: Email personal network asking for upvotes
- **8:00 AM**: Post in relevant Discord servers
- **9:00 AM - 6:00 PM**: Monitor comments, respond immediately
- **12:00 PM**: Post progress update on Twitter
- **6:00 PM**: Final push - reminder tweets
- **11:59 PM**: Celebrate or learn

**Comment Response Strategy:**

Prepare canned responses for common questions:

```
Q: "Is there a demo?"
A: "Yes! Try it in Postman: [link to collection]. Or use our CLI client: [link]"

Q: "What tech stack?"
A: ".NET 8 API, PostgreSQL, hosted on Railway. Open source: [GitHub link]"

Q: "Can I build a mobile app?"
A: "Absolutely! The API is just REST, so any platform works. We'd love to see it!"

Q: "Pricing?"
A: "Free tier: 10 battles/day. Premium ($5/mo): unlimited + priority queue."
```

**Post-Launch:**

- [ ] Screenshot the PH page at peak ranking
- [ ] Write blog post: "We launched on Product Hunt and here's what happened"
- [ ] Thank everyone who upvoted (Twitter thread)
- [ ] Add "Product Hunt Product of the Day" badge to site

**Expected Impact:** 300-2000 upvotes, 5000-20,000 site visitors on launch day

---

#### 3. Hacker News (Show HN)

**Perfect Audience:** Technical, skeptical, loves unique projects

**Submission Guidelines:**

**Title Format:** "Show HN: [Product Name] – [One-line description]"

**Winning Titles:**
- "Show HN: An API-only combat game where developers build their own clients"
- "Show HN: I made a game with no GUI, just a REST API"
- "Show HN: Combat game where your client code is your player"

**Avoid:**
- Hyperbole ("Amazing", "Revolutionary")
- Marketing speak ("Disruptive", "Game-changer")
- ALL CAPS or excessive punctuation

**URL to Submit:**
- Link directly to live demo or GitHub
- NOT to a landing page (HN hates marketing sites)
- Include working API endpoint they can curl immediately

**Submission Text (2000 char max):**

```
I built a strategic combat game with a weird constraint: no GUI.

You interact entirely through a REST API. To play, you build your own 
client (CLI, web dashboard, whatever), configure your team's battle AI, 
queue battles, and check results.

Battles resolve server-side using the strategies you upload (JSON-based 
rules for now, scripting coming later). You can queue a battle, go to 
lunch, come back to results.

Tech stack: .NET 8, PostgreSQL, hosted on Railway. Fully open source.

Live demo:
curl https://api.combatgame.dev/v1/leaderboard

Try it: [link]
GitHub: [link]
Docs: [link]

This started as a thought experiment ("what if the API *was* the game?") 
and turned into a decent portfolio piece. Also using it to teach API 
design at the local tech college.

Would love feedback on the API design and any creative client 
implementations you come up with!
```

**Best Practices:**

**Timing:**
- Weekday mornings, 8-10 AM PST (US audience waking up)
- Avoid Friday afternoons (low engagement)
- Avoid major tech news days (you'll get buried)

**Engagement:**
- Respond to EVERY comment within first 2 hours
- Be humble, not defensive
- Admit limitations honestly
- Thank people for feedback
- Offer to answer technical questions
- Share interesting implementation details

**Common HN Questions - Prepare Answers:**

```
Q: "How do you prevent API abuse?"
A: "Rate limiting + JWT auth. Free tier = 10 battles/day. Considering 
   IP-based throttling for extreme cases."

Q: "Why not just use webhooks for real-time?"
A: "Async-first design respects player time. You don't need to be 
   glued to your computer. But webhooks are on the roadmap for 
   tournaments!"

Q: "Have you considered [alternative tech stack]?"
A: "I went with .NET because it's what I know best, but the API is 
   standard REST so clients can use anything."

Q: "What prevents someone from scripting thousands of accounts?"
A: "Email verification + rate limits. If it becomes a problem, I'll 
   add CAPTCHA, but trying to avoid friction for real users."
```

**If You Hit Front Page:**

- [ ] Prepare for 10,000-50,000 visitors in 24 hours
- [ ] Monitor server health (Railway auto-scales, but watch it)
- [ ] Screenshot your ranking (#1 is badge of honor)
- [ ] Engage in comments for at least 6-8 hours
- [ ] Post follow-up update after 24 hours with stats

**Expected Impact:** Front page = 10,000-50,000 visitors, 200-1000 signups

---

#### 4. Reddit

**Target Subreddits:**

| Subreddit | Members | Best Day/Time | Post Type |
|-----------|---------|---------------|-----------|
| r/programming | 2M+ | Sat morning | Text post |
| r/gamedev | 1M+ | Weekend | Text post |
| r/webdev | 1.5M+ | Weekday | Link post |
| r/dotnet | 200K+ | Weekday | Link post |
| r/csharp | 200K+ | Weekday | Link post |
| r/incremental_games | 150K+ | Any | Text post |
| r/SideProject | 150K+ | Weekend | Text post |
| r/InternetIsBeautiful | 17M+ | Weekend | Link post |

**Post Templates:**

**r/programming (Text Post):**

```
Title: I built a combat game with no GUI, just a REST API

Hey r/programming,

I had this weird idea: what if the API *was* the game?

So I built a strategic combat game where there's no provided UI. 
You interact entirely through REST endpoints. To play, you build 
your own client (web, CLI, mobile, whatever) and use the API to:

- Configure your team
- Upload battle strategies (JSON-based AI)
- Queue battles (they resolve server-side)
- Check leaderboards

It's async-first so you're not glued to your computer. Queue a 
battle, grab coffee, check results.

Tech: .NET 8, PostgreSQL, open source on GitHub.

Try it: [link]
GitHub: [link]
Sample curl commands: [link to docs]

Would love to hear your thoughts on the API design, or see what 
kind of clients you build!
```

**r/incremental_games (Text Post):**

```
Title: An incremental/idle game where you code your own automation

This might be a weird fit for this sub, but hear me out.

I made a combat game where you program your strategy and let it 
run automatically. It's API-based, so you can build whatever level 
of automation you want:

- Simple: curl commands in a bash script
- Medium: Python script that checks results every hour
- Advanced: Full web dashboard with real-time updates
- Insane: ML model that optimizes strategies based on meta

Battles happen server-side, so you set it and forget it. Pure idle 
optimization gameplay.

Thought some of you might find this interesting!

[link]
```

**r/SideProject (Text Post):**

```
Title: My side project: API-only game for developers

**What I built:** A strategic PvP game with no GUI

**The twist:** You interact entirely via REST API. Build your own 
client, configure battle strategies, compete on leaderboards.

**Why?** I wanted to learn Railway deployment and showcase API 
design skills for my consulting business. Also using it to teach 
API concepts at the local tech college.

**Tech stack:** .NET 8, PostgreSQL, Railway

**Status:** Live and free to play

**Feedback wanted:**
- API design critique
- Ideas for game mechanics
- Suggestions for monetization

Try it: [link]
Code: [link]

Happy to answer any questions!
```

**Reddit Best Practices:**

**Do:**
- ✅ Post on Saturday mornings (8-10 AM local to subreddit)
- ✅ Engage authentically in comments
- ✅ Admit it's your project (transparency)
- ✅ Offer technical insights when asked
- ✅ Cross-post after 24 hours (not simultaneously)

**Don't:**
- ❌ Spam multiple subreddits at once (looks like spam)
- ❌ Delete and repost if it doesn't gain traction
- ❌ Argue with critics (be gracious)
- ❌ Over-promote (let the project speak)
- ❌ Use alt accounts to upvote yourself (bannable)

**Expected Impact:** Front page on 1-2 subreddits = 5,000-15,000 visitors

---

#### 5. Dev.to

**Content Strategy:** Multi-part blog series

**Series Structure:**

**Part 1: "Why I Built a Game With No Graphics"**

```markdown
# Why I Built a Game With No Graphics

## The Problem
I'm a .NET developer and consultant. I needed a portfolio piece 
that showcased API design, but I'm terrible at frontend work.

## The Idea
What if the lack of a GUI was *the feature*?

## The Experiment
I built a strategic combat game where you interact entirely 
through a REST API. No UI provided. If you want to play, you 
build your own client.

## What I Learned
- Developers LOVE automation
- The "meta" is optimizing API calls
- Teaching tool: students build clients as course projects

Try it: [link]
GitHub: [link]

[Continue reading →]
```

**Part 2: "Designing an API for Gameplay"**

```markdown
# Designing an API for Gameplay

When designing a game API, I had to rethink everything.

## Traditional Game Design
- Tight input/response loops
- Real-time feedback
- Visual rewards

## API-First Design
- Async operations (queue battles, check later)
- JSON responses (parse however you want)
- Data-driven rewards (leaderboard positions)

## Key Design Decisions

### 1. Versioned from Day One
`/api/v1/battle/queue`

Why? When I add scripting support, it becomes `/v2/battle/queue` 
with extended schema. Old clients keep working.

### 2. Pagination Everywhere
Even if you have 5 items now, you'll have 5000 later.

### 3. Rate Limiting as a Game Mechanic
Free tier: 10 battles/day
Premium: Unlimited

This creates strategic decision: optimize 10 battles, or pay for more.

[Code examples, API endpoints, design patterns...]

[Continue reading →]
```

**Part 3: "How Players Optimize My API-Only Game"**

```markdown
# How Players Optimize My API-Only Game

The meta-game is fascinating.

## Strategy 1: HTTP Header Optimization
One player discovered that setting `Accept-Encoding: gzip` reduced 
response payload by 70%, letting them squeeze in more API calls.

## Strategy 2: Concurrent Battle Queuing
The API allows queuing multiple battles. Smart players:
1. Queue 10 battles simultaneously
2. Test different strategies in parallel
3. Analyze which performed best
4. Iterate

## Strategy 3: Database Scraping
Players realized they could enumerate all units by incrementing IDs:
`/api/v1/units/1`, `/api/v1/units/2`, etc.

I *could* block this, but it's clever optimization. I left it.

## Strategy 4: Machine Learning
One player built a decision tree model that predicts opponent 
strategies based on leaderboard ranking. Wild.

[More examples, code snippets, lessons learned...]

[Continue reading →]
```

**Part 4: "Lessons Learned From 1000 Developer-Players"**

```markdown
# Lessons Learned From 1000 Developer-Players

After 6 months and 1000 players, here's what I learned.

## Lesson 1: Developers Hate Ambiguous Docs
Every vague API response caused 10 support tickets.

## Lesson 2: Rate Limiting Must Be Transparent
Return limits in headers:
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 42
X-RateLimit-Reset: 1675959600
```

## Lesson 3: The Meta Shifts Weekly
Players discover synergies, everyone copies it, I nerf it, new meta emerges.

## Lesson 4: Community is Everything
Discord server became the real game. Strategy discussions, 
tournaments, client showcases.

[Metrics, user stories, business lessons...]

[Conclusion]
```

**Dev.to Best Practices:**

- [ ] Publish one article per week (consistency matters)
- [ ] Use cover images (generate at canva.com)
- [ ] Add tags: `#gamedev`, `#api`, `#dotnet`, `#showdev`
- [ ] Cross-post to Medium, Hashnode for extra reach
- [ ] Engage with comments within 24 hours
- [ ] Link to previous articles in series

**Expected Impact:** 5,000-20,000 views per article

---

#### 6. Indie Hackers

**Perfect Audience:** Developers building side projects

**Post Type:** Build Log / Case Study

**Title:** "Building a Developer Tool Disguised as a Game"

**Post Template:**

```
**What I'm Building:**
API-only combat game for developers

**Current Status:**
- Live at [link]
- 250 registered devs
- $150 MRR (30 premium subs)

**The Journey:**

Month 1: Built MVP in .NET, deployed to Railway
Month 2: Soft launch (HN, Reddit) - 100 users
Month 3: Product Hunt launch - 500 users
Month 4: University partnerships - 800 users
Month 5: Added premium tier - first revenue!
Month 6: Hit $150 MRR

**What's Working:**
- Free tier → premium conversion: 12%
- Educational licenses: 3 universities ($500 each)
- Word of mouth from Discord community

**What's Not:**
- Churn is high (40% monthly) - need stickier features
- Support takes 5-10 hrs/week
- Meta gets stale quickly

**Next Steps:**
- Add scripting engine (keep meta fresh)
- Build mobile SDK
- Reach out to more bootcamps

**Open Questions:**
1. Should I raise prices? ($5/mo feels low)
2. How to reduce churn?
3. Worth pursuing enterprise licenses?

Would love feedback from folks who've done B2D (business-to-developer) SaaS!
```

**Engagement Strategy:**

- Post weekly/bi-weekly updates
- Be radically transparent (show revenue, costs, struggles)
- Ask for specific advice (not generic "what do you think?")
- Support others' projects (comment, upvote)
- Join the Weekly Megathread

**Expected Impact:** 500-2000 views, quality feedback, potential customers

---

### Secondary Platforms

#### 7. Twitter/X (Developer Twitter)

**Strategy:** Build in public

**Content Pillars:**

1. **Development updates** (30%)
   - "Just shipped scripting engine! Now you can write Lua code for your battle AI."
   - "Scaled to 10,000 concurrent battles. Railway's auto-scaling is impressive."

2. **Interesting discoveries** (30%)
   - "A player discovered you can predict opponent strategies by analyzing leaderboard movement patterns. This is some next-level meta."
   - "Someone built a client in React Native. The API is so portable!"

3. **Community highlights** (20%)
   - "Check out this gorgeous web client by @username [screenshot]"
   - "Tournament winner used a decision tree ML model. Code: [link]"

4. **Educational content** (10%)
   - "Thread: How I designed the battle resolution algorithm 🧵"
   - "Quick tip: Use ETags for efficient polling of battle results"

5. **Transparent metrics** (10%)
   - "Revenue update: Hit $500 MRR! 🎉"
   - "Churn spiked to 45% this month. Ouch. Time to ship features."

**Posting Frequency:** 1-2 tweets per day

**Hashtags to Use:**
- #buildinpublic
- #indiehacker
- #gamedev
- #API
- #dotnet

**Influencer Engagement:**

Find developers with 10K-100K followers who might care:

```
@ThePrimeagen (backend dev, loves APIs)
@t3dotgg (full stack, appreciates weird projects)
@Dayhaysoos (indie hacker)
@swyx (learning in public advocate)
@levelsio (digital nomad, indie products)
```

**Engagement tactics:**
- Reply to their tweets (add value, don't self-promote)
- Quote tweet with relevant insight
- DM with personalized pitch (once you have traction)

**Example outreach DM:**

```
Hey [Name], love your content on [topic]. 

I built something weird you might find interesting: an API-only 
game for developers. No GUI - you build your own client to play.

1000+ devs playing so far. Thought it might make a fun video/tweet.

Demo: [link]
GitHub: [link]

No pressure! Just wanted to share.
```

**Expected Impact:** Slow build, but compounds over time. 1000 followers in 6 months.

---

#### 8. YouTube

**Content Ideas:**

**1. Tutorial: "Building a Client in 30 Minutes"**
- Format: Live coding, single take
- Tools: VS Code, Postman
- Deliverable: Working CLI client by end
- Length: 25-35 minutes

**2. Explainer: "I Made a Game With No Graphics"**
- Format: Talking head + screen recording
- Style: Fireship-inspired (fast-paced, funny)
- Length: 5-8 minutes
- Thumbnail: "NO GUI" in big text

**3. Deep Dive: "How Battles Are Resolved Server-Side"**
- Format: Code walkthrough
- Audience: Senior devs, system designers
- Length: 15-20 minutes

**4. Showcase: "Tour of the Best Community Clients"**
- Format: Screen recordings of 5-7 clients
- Include creator credits
- Length: 10-12 minutes

**5. Live Stream: "Tournament Finals Commentary"**
- Format: Live commentary on battle logs
- Include player interviews
- Length: 1-2 hours

**SEO Optimization:**

**Titles:**
- "I Built a Game With No Graphics (And Developers Loved It)"
- "Coding a Game Client in 30 Minutes - API Combat Game Tutorial"
- "Building an API-Only Game - Full Stack Development"

**Descriptions:**
```
I built a strategic combat game where there's no GUI. You interact 
entirely through a REST API. Here's how it works.

🎮 Try it: [link]
💻 GitHub: [link]
📚 Docs: [link]
💬 Discord: [link]

Timestamps:
0:00 - Introduction
2:15 - How the API works
5:30 - Building a simple client
10:45 - Battle strategies
15:20 - Advanced optimizations

Tech stack: .NET 8, PostgreSQL, Railway
```

**Tags:**
- game development
- api design
- rest api
- dotnet
- csharp
- programming tutorial
- web development

**Expected Impact:** One viral video = 50K-200K views, 500-2000 signups

---

#### 9. Discord Server

**Essential For:** Community building, real-time support

**Channel Structure:**

```
📢 ANNOUNCEMENTS
├─ #announcements (read-only)
├─ #changelog
└─ #maintenance

💬 GENERAL
├─ #general
├─ #introductions
└─ #off-topic

🎮 GAMEPLAY
├─ #strategy-discussion
├─ #team-builds
├─ #meta-analysis
└─ #tournaments

💻 DEVELOPMENT
├─ #client-showcase
├─ #api-feedback
├─ #bug-reports
└─ #feature-requests

📚 HELP
├─ #getting-started
├─ #troubleshooting
└─ #api-docs

🏆 LEADERBOARDS
├─ #top-players
└─ #hall-of-fame
```

**Moderation:**

- [ ] Set up AutoMod (filter spam, slurs)
- [ ] Create welcome message with quick start guide
- [ ] Pin important resources (#getting-started)
- [ ] React with ✅ to answered questions

**Community Events:**

**Weekly:**
- Strategy Sunday: Share your builds
- Screenshot Saturday: Show your client UI

**Monthly:**
- Tournament (bracket-style)
- Meta Report (what's dominating)

**Quarterly:**
- Season reset with exclusive rewards
- Community AMA

**Expected Impact:** 200-1000 members in first 6 months

---

#### 10. Newsletter / Blog

**Platform Options:**

1. **Substack** (easiest)
   - Pros: Built-in audience, easy setup, free
   - Cons: Less control, takes 10% of paid subs

2. **Self-hosted on learnedgeek.com** (best for branding)
   - Pros: Full control, integrates with consulting site
   - Cons: Requires setup, need email service (Mailchimp, ConvertKit)

**Recommendation:** Start with Substack, migrate to self-hosted later

**Content Cadence:** Weekly dev blog

**Newsletter Structure:**

```
Subject: API Combat Weekly #12 - Scripting Engine Shipped 🚀

---

Hey developers,

This week we shipped the scripting engine! You can now write Lua 
code for your battle AI instead of just JSON configs.

🔥 WHAT'S NEW
- Lua scripting for advanced strategies
- 5 new units (Dragon, Necromancer, Paladin, Assassin, Bard)
- Performance optimization (30% faster battle resolution)

📊 BY THE NUMBERS
- 1,247 active players (+15% vs last week)
- 45,320 battles this week
- Current meta: Mage/Healer/Tank comp dominating

🏆 COMMUNITY SPOTLIGHT
@username built an incredible mobile client using React Native. 
Check it out: [link]

🧠 STRATEGY TIP
Facing the Mage meta? Try high-speed Rangers with burst damage. 
They can eliminate Mages before they cast ultimates.

🗓️ UPCOMING
- Tournament this Saturday (prize: 3 months premium)
- AMA with top player on Discord Friday

Keep building,
Mark

---

P.S. If you're enjoying the game, I'd love a GitHub star ⭐
```

**Email Capture:**

- Embedded form on website
- Offer: "Weekly strategy tips + API updates"
- Incentive: "Sign up to enter monthly premium giveaway"

**Expected Impact:** 100-1000 subscribers in first year

---

### Niche Platforms (High Value)

#### 11. CodeProject

**Audience:** .NET developers specifically

**Article Format:** Technical tutorial

**Title:** "Building an API-Only Game in C# - A Case Study"

**Outline:**

```
1. Introduction
   - Why API-only?
   - Target audience (developers)

2. Architecture
   - .NET 8 Web API
   - PostgreSQL data model
   - Battle resolution engine

3. Key Design Decisions
   - Versioned endpoints
   - Rate limiting strategy
   - Authentication (JWT)

4. Code Walkthrough
   - Scenario model
   - Strategy pattern for battle resolution
   - Background service for async battles

5. Deployment
   - Railway setup
   - CI/CD with GitHub Actions

6. Lessons Learned
   - Developer UX is different from user UX
   - Documentation is critical
   - Community drives retention

7. Try It Yourself
   - Links to GitHub, live demo
   - Sample client code
```

**CodeProject Best Practices:**

- Include lots of code snippets (readers expect it)
- Explain WHY, not just HOW
- Professional tone (CodeProject skews senior)
- Respond to questions in comments

**Expected Impact:** 10,000+ developer views, 200+ upvotes

---

#### 12. Daily.dev

**How It Works:** Aggregates dev content, personalized feeds

**Strategy:** Publish on Dev.to, gets auto-synced to Daily.dev

**Optimization:**

- Use relevant tags on Dev.to
- Include code snippets (Daily.dev favors technical)
- Catchy titles (click-through matters)

**Expected Impact:** Additional 5,000-10,000 impressions per article

---

#### 13. TechCrunch / The Verge (Long Shot)

**Pitch Angle:** "Developer makes game where the UI is homework"

**When to Reach Out:**

- After you have 1,000+ players
- After a successful Product Hunt launch
- When you have an interesting milestone (10K users, $10K MRR)

**Press Release Template:**

```
FOR IMMEDIATE RELEASE

Developer Builds Combat Game With No Graphics, Developers Love It

WAUKESHA, WI - February 2026 - Mark [Last Name], a .NET consultant 
and educator, has launched API Combat Game, a strategic multiplayer 
game with an unusual constraint: there's no user interface.

Players interact entirely through a REST API, building their own 
custom clients to compete. Since launching 3 months ago, the game 
has attracted over 5,000 developers and is being used as a teaching 
tool in computer science courses.

"I wanted to showcase API design skills for my consulting business," 
says [Mark]. "I never expected it to become a legitimate game with 
tournaments and a meta."

The game has been adopted by [3] universities as a course project 
for teaching API consumption and is generating $[amount]/month in 
premium subscriptions.

Try it: [link]
Media kit: [link]
Contact: [email]
```

**Expected Impact:** 50,000+ visitors if they cover it (big if)

---

### Educational Partnerships

#### 14. Universities / Bootcamps

**Target Personas:**

1. **CS Professors** (teaching API consumption, web dev)
2. **Bootcamp Instructors** (need engaging projects)
3. **CS Club Advisors** (hackathon ideas)

**Outreach Email Template:**

```
Subject: Free API Game for Teaching Web Development

Hi Professor [Name],

I'm Mark, a .NET consultant and part-time instructor at Waukesha 
County Technical College.

I built an API-only game designed specifically for teaching API 
consumption. Students build their own clients (web, mobile, CLI) 
to interact with the game, making it a perfect final project for 
web dev or software engineering courses.

Benefits:
• Students get hands-on REST API experience
• Flexible (works with any language/framework)
• Engaging (it's a game, not another CRUD app)
• Free educational licenses for your students

We've had success at WCTC and [2] other universities. I'd love to 
offer you a free trial for your class.

Curriculum materials: [link]
Demo: [link]
GitHub: [link]

Would you be open to a quick 15-minute call to discuss?

Best,
Mark
[Contact info]
```

**Deliverables to Create:**

- [ ] Teacher's guide (curriculum integration)
- [ ] Sample assignments ("Build a CLI client in Python")
- [ ] Grading rubric
- [ ] Video tutorials for students
- [ ] Discord channel for student questions

**Pricing:**

- **Free tier:** Individual students (10 battles/day)
- **Educational license:** $500/semester (unlimited for all students)
- **Enterprise:** $1000/year + custom features

**Expected Impact:** 3-5 universities in first year = $1,500-2,500 revenue + 100-250 students as users

---

#### 15. Online Learning Platforms

**Targets:**

1. **Udemy instructors** (API courses, web dev)
2. **Pluralsight authors** (.NET courses)
3. **Coursera partners** (university courses)

**Pitch to Course Creators:**

```
Subject: Use My API Game as a Course Project

Hi [Instructor Name],

I saw your Udemy course "[Course Name]" - fantastic content!

I built an API-only game that could work perfectly as a hands-on 
project for your students. Instead of building yet another todo 
app, they build a game client.

Benefits:
• More engaging than typical API tutorials
• Students can share their clients (portfolio piece)
• I'll provide a free promo code for your students

Example project:
"Build a Python client for API Combat Game using the requests 
library. Implement authentication, team management, and automated 
battle queuing."

I'd love to collaborate. Would you be interested?

Best,
Mark
```

**Revenue Model:**

- Revenue share: Instructor gets 10% of premium subs from their students
- Free access for instructor + students during course
- Affiliate link tracking

**Expected Impact:** 5-10 course integrations = 500-2000 students exposed

---

## Launch Timeline

### Phase 1: Soft Launch (Month 1-2)

**Goal:** Get first 50 beta testers, validate concept

**Week 1:**
- [ ] Launch personal blog post announcement
- [ ] Post on Twitter (personal network)
- [ ] Share in relevant Discord servers (web dev, game dev)
- [ ] Email personal network (friends, colleagues)

**Week 2:**
- [ ] Post to r/dotnet
- [ ] Post to r/csharp
- [ ] Engage heavily in comments

**Week 3:**
- [ ] Show HN post
- [ ] Monitor for 12+ hours
- [ ] Respond to every comment

**Week 4:**
- [ ] Gather feedback via Discord/email
- [ ] Fix critical bugs
- [ ] Prepare for public launch

**Expected Results:**
- 50-200 registered users
- 10-20 active daily users
- 5-10 custom clients built
- Validated that developers actually want this

---

### Phase 2: Public Launch (Month 3)

**Goal:** Hit 1,000 users, get media attention

**Pre-Launch Week:**
- [ ] Prepare Product Hunt assets (screenshots, video, description)
- [ ] Write Dev.to article series (schedule for publication)
- [ ] Create Reddit posts (don't submit yet)
- [ ] Reach out to 5-10 developer YouTubers
- [ ] Alert Discord community about upcoming launch

**Launch Week:**

**Tuesday 12:01 AM:**
- [ ] Submit to Product Hunt

**Tuesday 6:00 AM:**
- [ ] Tweet announcing Product Hunt launch
- [ ] Post in Indie Hackers
- [ ] Email newsletter subscribers

**Tuesday 9:00 AM - 9:00 PM:**
- [ ] Respond to every PH comment within 30 minutes
- [ ] Monitor server health
- [ ] Engage on Twitter

**Wednesday:**
- [ ] Publish Dev.to article Part 1
- [ ] Cross-post to Medium, Hashnode

**Thursday:**
- [ ] Post to r/programming (morning)
- [ ] Post to r/SideProject (afternoon)

**Friday:**
- [ ] Post to r/gamedev
- [ ] Post to r/webdev

**Saturday:**
- [ ] Post to r/incremental_games
- [ ] Dev.to article Part 2

**Sunday:**
- [ ] Rest and monitor

**Expected Results:**
- 1,000-5,000 registered users
- 100-300 active daily users
- Product Hunt: 300-2000 upvotes
- HN front page: 10,000-50,000 visitors
- Reddit front page (1-2 subs): 5,000-15,000 visitors

---

### Phase 3: Growth (Month 4-6)

**Goal:** Sustained growth, educational adoption

**Month 4:**
- [ ] Launch premium tier ($5/mo)
- [ ] Weekly Dev.to articles
- [ ] Reach out to 10 universities/bootcamps
- [ ] First Discord tournament

**Month 5:**
- [ ] Add 20+ new units (keep meta fresh)
- [ ] Ship scripting engine (Lua support)
- [ ] CodeProject article
- [ ] YouTube tutorial series (3-5 videos)

**Month 6:**
- [ ] Launch educational licensing ($500/semester)
- [ ] Second major Product Hunt update
- [ ] Reach out to tech press (TechCrunch, etc.)
- [ ] Quarterly tournament with prizes

**Expected Results:**
- 5,000-10,000 registered users
- 500-1,000 active daily users
- $500-2,000 MRR (premium subs + edu licenses)
- 3-5 university partnerships
- 2,000-5,000 GitHub stars

---

### Phase 4: Sustainability (Month 7-12)

**Goal:** Profitable, self-sustaining product

**Ongoing Activities:**

**Weekly:**
- [ ] Dev blog update
- [ ] Discord community engagement
- [ ] Bug fixes and balance patches

**Monthly:**
- [ ] Feature release (new units, game modes)
- [ ] Meta report (analytics on strategies)
- [ ] Newsletter to subscribers

**Quarterly:**
- [ ] Major update (e.g., mobile SDK, new game mode)
- [ ] Tournament with cash prizes
- [ ] Revenue/metrics transparency post

**Expected Results:**
- 10,000-20,000 registered users
- $2,000-5,000 MRR
- 10+ university partnerships
- Consulting leads from portfolio showcase

---

## Content Marketing

### Blog Post Ideas (for learnedgeek.com)

**Technical Deep Dives:**

1. **"Why I Built a Game With No Graphics"**
   - Philosophy, constraints, design decisions
   - Target: Indie Hackers, Dev.to
   - Length: 1500 words

2. **"API Design Lessons From Building a Combat Game"**
   - Versioning, rate limiting, pagination
   - Target: CodeProject, Dev.to
   - Length: 2000 words

3. **"Deploying a .NET API to Railway: A Complete Guide"**
   - Step-by-step tutorial
   - Target: Dev.to, r/dotnet
   - Length: 2500 words

4. **"Building a Battle Resolution Engine in C#"**
   - Turn-based combat algorithm
   - Target: CodeProject
   - Length: 3000 words

**Case Studies:**

5. **"The Player Who Beat My Game By Optimizing HTTP Headers"**
   - Creative optimization story
   - Target: Hacker News, Dev.to
   - Length: 1200 words

6. **"What 1000 API Calls Taught Me About Developer UX"**
   - Insights from user behavior
   - Target: Indie Hackers, Dev.to
   - Length: 1500 words

7. **"Teaching .NET API Development Through Gameplay"**
   - Educational use case
   - Target: Education-focused sites
   - Length: 1800 words

**Transparency / Business:**

8. **"Economics of Running a Free Developer Game"**
   - Costs, revenue, unit economics
   - Target: Indie Hackers
   - Length: 1000 words

9. **"From Idea to $5K MRR: Building an API Game"**
   - Journey, milestones, lessons
   - Target: Indie Hackers
   - Length: 2000 words

10. **"How I Got 3 Universities to Adopt My Side Project"**
    - B2B sales process
    - Target: Indie Hackers
    - Length: 1500 words

---

### Video Content Ideas

**Tutorials:**

1. **"Building a Client in 30 Minutes"**
   - Live coding, Python or C#
   - Format: Screen recording + commentary
   - Length: 25-35 minutes
   - Platform: YouTube

2. **"Getting Started with API Combat Game"**
   - Beginner-friendly walkthrough
   - Format: Talking head + screen
   - Length: 10-15 minutes
   - Platform: YouTube

3. **"Advanced Strategy Optimization"**
   - Deep dive on meta-game
   - Format: Screen recording + data analysis
   - Length: 20-25 minutes
   - Platform: YouTube

**Showcases:**

4. **"Tour of the Best Community Clients"**
   - Highlight 5-7 impressive UIs
   - Format: Screen recordings + interviews
   - Length: 10-12 minutes
   - Platform: YouTube

5. **"How I Built an API-Only Game (Full Stack)"**
   - Technical overview
   - Format: Fireship-style (fast, dense)
   - Length: 5-8 minutes
   - Platform: YouTube, Twitter

**Events:**

6. **"Tournament Finals Commentary"**
   - Live event coverage
   - Format: Live stream
   - Length: 1-2 hours
   - Platform: YouTube Live, Twitch

7. **"Developer AMA - Building in Public"**
   - Q&A about the project
   - Format: Live stream
   - Length: 1 hour
   - Platform: YouTube Live

---

### Podcast Appearances

**Target Podcasts:**

1. **The Changelog** (developer tools focus)
   - Pitch: "I built a game for developers to learn APIs"
   - Contact: changelog.com/request

2. **Syntax.fm** (web dev)
   - Pitch: "API-first design through game development"
   - Contact: Twitter DM to @syntaxfm

3. **The Indie Hackers Podcast** (side projects)
   - Pitch: "From idea to $5K MRR with an API game"
   - Contact: IndieHackers.com

4. **Software Engineering Daily** (technical)
   - Pitch: "Designing APIs for gameplay"
   - Contact: softwareengineeringdaily.com

5. **CodeNewbie** (beginner-friendly)
   - Pitch: "Teaching API concepts through games"
   - Contact: codenewbie.org

**Pitch Template:**

```
Subject: Podcast Guest Pitch - API-Only Game for Developers

Hi [Host Name],

I'm Mark, a .NET consultant who built something unusual: a combat 
game with no GUI. Developers interact entirely through a REST API.

It's grown to 5,000+ players and is being used by universities to 
teach API consumption. I think your audience would find it interesting.

Topics we could discuss:
• Designing APIs for usability vs optimization
• Developer UX vs traditional user UX
• Building in public / indie hacking
• Teaching programming through games

Show: [Name]
Website: [link]
GitHub: [link]

Would you be interested in having me on?

Best,
Mark
```

---

## Influencer Outreach

### Developer YouTubers

**Tier 1 (1M+ subs):**

1. **Fireship** (2M+ subs)
   - Style: Fast-paced, unique projects
   - Pitch: "I built a game in 100 seconds (it has no GUI)"
   - Contact: Twitter DM

2. **ThePrimeagen** (500K+ subs)
   - Style: Backend dev, loves APIs
   - Pitch: "API-only game, no frontend bloat"
   - Contact: Twitch chat, Twitter

3. **WebDevSimplified** (1M+ subs)
   - Style: Beginner-friendly tutorials
   - Pitch: "Learn REST APIs by building a game client"
   - Contact: Email via website

**Tier 2 (100K-500K subs):**

4. **Nick Chapsas** (300K+ subs)
   - Style: .NET specialist
   - Pitch: ".NET API game, performance optimizations"
   - Contact: Twitter DM

5. **CodeAesthetic** (500K+ subs)
   - Style: Software design deep dives
   - Pitch: "API design for gameplay"
   - Contact: Email via website

6. **TechLead** (1M+ subs)
   - Style: Tech career, side projects
   - Pitch: "Ex-Google TechLead builds API game as a service"
   - Contact: Twitter, YouTube comments

**Tier 3 (10K-100K subs):**

7. **DevOps Toolkit** (50K+ subs)
   - Style: Cloud, DevOps
   - Pitch: "Deploying a game API to Railway"
   - Contact: Email

8. **IAmTimCorey** (300K+ subs)
   - Style: C# tutorials
   - Pitch: "Building a RESTful game in C#"
   - Contact: Email via website

**Pitch Template:**

```
Subject: Video Idea - API-Only Game for Developers

Hey [YouTuber Name],

Love your content on [specific topic they cover].

I built something your audience might find interesting: a strategic 
combat game where there's no UI. Developers build their own clients 
using your API.

It's gotten 5,000+ players and is being used to teach API consumption 
at universities. I think it could make a fun video.

Quick overview:
• REST API for all game actions
• Developers build custom clients (web, mobile, CLI)
• Async battles (queue, check results later)
• Active meta-game and tournaments

Demo: [link]
GitHub: [link]
Sample clients: [link]

Would you be interested in covering it? Happy to provide assets, 
code walkthrough, or whatever you need.

No pressure either way!

Best,
Mark
[Contact info]
```

---

### Tech Bloggers / Journalists

**Tier 1 (Major Publications):**

1. **TechCrunch**
   - Contact: tips@techcrunch.com
   - Pitch: When you hit 10K users or $10K MRR

2. **The Verge**
   - Contact: tips@theverge.com
   - Pitch: "Game with no graphics gets 10K developer-players"

3. **Hacker Noon**
   - Contact: stories@hackernoon.com
   - Pitch: Guest article about API design

**Tier 2 (Tech-Focused):**

4. **Ars Technica**
   - Contact: tips@arstechnica.com
   - Pitch: Technical deep dive on battle resolution

5. **Fast Company**
   - Contact: https://www.fastcompany.com/contact-us
   - Pitch: "How constraints drive creativity"

**Tier 3 (Developer-Focused):**

6. **InfoQ**
   - Contact: editors@infoq.com
   - Pitch: Case study on API-first development

7. **SitePoint**
   - Contact: https://www.sitepoint.com/write-for-us/
   - Pitch: Tutorial on building game clients

**Press Release Distribution:**

- [ ] PRWeb (paid, $99-399)
- [ ] Send directly to journalist emails
- [ ] Post on company blog
- [ ] Share on social media

---

### Hacker News Influencers

**Power Users to Engage:**

These users have high karma and influence on HN:

- **patio11** (Patrick McKenzie) - SaaS expert
- **tptacek** (Thomas Ptacek) - Security expert
- **danso** (Dan Nguyen) - Data journalist
- **edw519** - Developer, active commenter
- **pg** (Paul Graham) - YC founder (rare, but huge)

**Engagement Strategy:**

1. Don't pitch directly (feels spammy)
2. Engage authentically with their comments
3. When you post Show HN, they may organically engage
4. If they comment on your post, respond thoughtfully

---

## Educational Partnerships

### University Outreach

**Target Schools:**

**Tier 1 (Local - Start Here):**
- Waukesha County Technical College (you teach here!)
- University of Wisconsin - Milwaukee
- Milwaukee School of Engineering
- Marquette University

**Tier 2 (Regional):**
- University of Wisconsin - Madison
- Northwestern University
- University of Illinois
- University of Minnesota

**Tier 3 (National):**
- Stanford (CS department)
- MIT (software engineering)
- Carnegie Mellon (game development program)
- UC Berkeley (web development)

**Outreach Process:**

**Step 1: Identify Contact**
- Search "[University] computer science faculty"
- Look for professors teaching:
  - Web Development
  - API Design
  - Software Engineering
  - Intro to Programming

**Step 2: Find Email**
- Usually on department website
- Format: firstname.lastname@university.edu

**Step 3: Send Personalized Email**

```
Subject: Free API Game for Teaching [Course Name]

Dear Professor [Last Name],

I'm Mark, a .NET consultant and part-time instructor at Waukesha 
County Technical College (WCTC).

I noticed you teach [Course Name] at [University]. I built an 
API-only game specifically designed for teaching REST API consumption, 
and I think it could work well as a project for your students.

Instead of building another todo app, students build a game client 
(CLI, web, or mobile). They get hands-on experience with:
• HTTP requests and authentication
• JSON parsing and data modeling
• Error handling and rate limiting
• Async operations

We've used it successfully at WCTC, where students found it more 
engaging than typical CRUD projects.

I'd love to offer you a free educational license for your class 
(normally $500/semester). No strings attached - just want to help 
students learn.

Materials I can provide:
• Curriculum guide with sample assignments
• Grading rubrics
• Video tutorials for students
• Dedicated Discord support channel

Demo: [link]
GitHub: [link]
Sample assignment: [link]

Would you be open to a 15-minute call to discuss?

Best regards,
Mark [Last Name]
[Title], Learned Geek Consulting
Part-time Instructor, WCTC
[Contact info]
```

**Step 4: Follow Up**

If no response in 1 week:
```
Subject: Re: Free API Game for Teaching [Course Name]

Hi Professor [Last Name],

Following up on my email from last week about using API Combat Game 
as a teaching tool for [Course Name].

I know you're busy, so I'll keep this brief:
• Free educational license (no cost ever)
• Ready-to-use assignments and rubrics
• Students find it more engaging than typical projects

Happy to send more info or hop on a quick call.

Best,
Mark
```

**Step 5: Close the Deal**

If interested:
- Schedule 15-min call
- Demo the game
- Share curriculum materials
- Provide free license code
- Add to Discord edu channel
- Ask for testimonial after semester

**Expected Conversion:** 10-20% respond, 50% of those adopt

---

### Bootcamp Outreach

**Target Bootcamps:**

1. **General Assembly**
2. **Flatiron School**
3. **Hack Reactor**
4. **App Academy**
5. **Lambda School**
6. **Coding Dojo**
7. **Fullstack Academy**
8. **Tech Elevator**

**Contact Method:**

Most bootcamps have "Partner with us" or "Curriculum" pages.

**Pitch Angle:**

```
Subject: Engaging Final Project for Web Development Bootcamp

Hi [Curriculum Director Name],

I'm reaching out because I built something that could work perfectly 
as a final project for your web development bootcamp.

It's an API-only game where students build clients to compete. 
Instead of another portfolio CRUD app, they build something unique 
that demonstrates API mastery.

Benefits:
• More engaging than typical projects (it's a game!)
• Students can showcase it in job interviews
• Covers all key API concepts (auth, REST, async, error handling)
• Free for bootcamp use

Bootcamps currently using it:
• [Bootcamp 1] - 50 students last cohort
• [Bootcamp 2] - Adopted as capstone project
• [Bootcamp 3] - Optional weekend hackathon

I'd love to discuss how we could tailor it for [Bootcamp Name].

Demo: [link]
Student testimonials: [link]

Best,
Mark
```

**Revenue Model:**

- Free for students
- $1000/year partnership fee (optional)
- Bootcamp gets branded landing page
- Leaderboard for bootcamp students only

---

## Monetization Channels

### 1. Freemium Subscription Model

**Free Tier:**
- 10 battles per day
- Access to basic units (20 units)
- Public leaderboards
- Standard matchmaking
- 3 team slots

**Premium Tier - $5/month:**
- Unlimited battles
- Access to all units (50+ units)
- Advanced units (legendary/rare)
- Priority matchmaking (faster queue)
- 10 team slots
- No ads (if you add any)
- Early access to new features
- Custom profile badges
- Replay storage (unlimited vs 7 days)

**Premium Plus - $10/month:**
- Everything in Premium
- Scripting engine access (Lua)
- Advanced analytics dashboard
- API rate limit 2x higher
- Private tournaments
- 1-on-1 coaching session per month

**Expected Conversion:**
- Free to Premium: 5-10%
- Premium to Premium Plus: 20-30%

**Projected Revenue (at 10K users):**
- 500-1000 Premium subs = $2,500-5,000/mo
- 100-300 Premium Plus subs = $1,000-3,000/mo
- **Total: $3,500-8,000 MRR**

---

### 2. Battle Pass (Seasonal)

**Model:** 8-week seasons, $10 per pass

**What's Included:**
- 10 exclusive seasonal units
- Special cosmetic rewards (profile themes, badges)
- 2x XP multiplier
- Seasonal tournament entry (prize pool)
- Exclusive Discord role

**Revenue Potential:**
- 10% of active users buy = 1,000 x $10 = $10,000 per season
- 6 seasons per year = $60,000 annual

---

### 3. Educational Licensing

**Pricing Tiers:**

**Individual Educator - Free**
- Up to 30 students
- Basic support
- Curriculum materials

**Small Institution - $500/semester**
- Up to 100 students
- Dedicated support channel
- Custom assignments
- Branded leaderboard

**Large Institution - $1,000/semester**
- Unlimited students
- Priority support
- Custom features
- Onsite training (optional)
- Co-marketing opportunities

**Enterprise - $5,000/year**
- Multiple campuses
- White-label option
- API access for LMS integration
- Dedicated account manager

**Revenue Potential:**
- 10 small institutions x $500 x 2 semesters = $10,000/year
- 3 large institutions x $1,000 x 2 semesters = $6,000/year
- 1 enterprise x $5,000 = $5,000/year
- **Total: $21,000/year**

---

### 4. Corporate Team Building

**Offering:** Private tournaments for companies

**Package:**
- Private server instance (1 week)
- Tournament setup and management
- Prizes for winners
- Post-tournament analytics

**Pricing:** $2,000-5,000 per event

**Target Companies:**
- Tech companies (Google, Microsoft, etc.)
- Developer-focused startups
- Agencies with dev teams

**Revenue Potential:**
- 1 event per quarter = $8,000-20,000/year

---

### 5. GitHub Sponsors

**Tiers:**

- **$5/month - Supporter**
  - Name in credits
  - Supporter Discord role

- **$20/month - Contributor**
  - Everything above
  - Early access to features (1 week)
  - Vote on feature roadmap

- **$100/month - Partner**
  - Everything above
  - 1-hour monthly call
  - Influence roadmap
  - Consulting on your API projects

**Revenue Potential:**
- 20 x $5 = $100/mo
- 10 x $20 = $200/mo
- 2 x $100 = $200/mo
- **Total: $500/mo**

---

### 6. API-as-a-Service (White Label)

**Offering:** License the game engine to other developers

**Use Cases:**
- Other educators wanting private instances
- Companies building internal training tools
- Developers creating themed variants

**Pricing:**
- **Self-hosted license:** $500/year (access to private repo)
- **Hosted instance:** $100/month (we manage infrastructure)

**Revenue Potential:**
- 5 self-hosted x $500 = $2,500/year
- 3 hosted x $100 x 12 = $3,600/year
- **Total: $6,100/year**

---

### 7. Marketplace (Future)

**Model:** Player-created content marketplace

**What Gets Sold:**
- Custom battle strategies (scripts)
- Client themes/skins
- Unit sprites/icons
- Sound packs

**Revenue Share:** 70% creator, 30% platform

**Potential:** $1,000-5,000/month (at scale)

---

### 8. Consulting Cross-Promotion

**Value:** Game as portfolio piece drives consulting leads

**Strategy:**
- Footer on website: "Built by Learned Geek Consulting"
- Case study on learnedgeek.com
- LinkedIn posts showcasing the tech

**Consulting Services to Offer:**
- "Build Your Own API-First Product" workshop ($5,000-10,000)
- Custom game development for corporate training ($20,000-50,000)
- API design review services ($2,000-5,000 per project)

**Revenue Potential:**
- 1 workshop per quarter = $20,000-40,000/year
- 1 custom dev project per year = $20,000-50,000
- 2-3 API reviews per year = $4,000-15,000
- **Total: $44,000-105,000/year**

---

### Total Revenue Projection (Year 1)

**Conservative:**
- Freemium subs: $3,500/mo x 12 = $42,000
- Educational licenses: $10,000
- GitHub Sponsors: $500/mo x 12 = $6,000
- Consulting leads: $20,000
- **Total: $78,000/year**

**Optimistic:**
- Freemium subs: $8,000/mo x 12 = $96,000
- Battle passes: $60,000
- Educational licenses: $21,000
- Corporate events: $10,000
- GitHub Sponsors: $6,000
- Consulting leads: $50,000
- **Total: $243,000/year**

**Realistic (Year 1):** $100,000-150,000

---

## Growth Metrics & KPIs

### Week 1 Metrics

**User Acquisition:**
- [ ] Registered users: 100
- [ ] Active daily users (DAU): 20
- [ ] Battles queued: 500

**Community:**
- [ ] GitHub stars: 50
- [ ] Discord members: 30

**Technical:**
- [ ] API uptime: 99%+
- [ ] Average response time: <200ms
- [ ] Error rate: <1%

---

### Month 1 Metrics

**User Acquisition:**
- [ ] Registered users: 1,000
- [ ] DAU: 100
- [ ] Weekly active users (WAU): 300
- [ ] Battles queued: 50,000

**Engagement:**
- [ ] Average battles per user: 50
- [ ] Client implementations: 20
- [ ] Retention (7-day): 40%

**Community:**
- [ ] GitHub stars: 500
- [ ] Discord members: 200
- [ ] Dev.to followers: 100

**Revenue:**
- [ ] MRR: $0 (pre-monetization)

---

### Month 6 Metrics

**User Acquisition:**
- [ ] Registered users: 10,000
- [ ] DAU: 1,000
- [ ] WAU: 3,000
- [ ] Battles queued: 500,000

**Engagement:**
- [ ] Average battles per user: 200
- [ ] Client implementations: 100
- [ ] Retention (7-day): 35%
- [ ] Retention (30-day): 20%

**Community:**
- [ ] GitHub stars: 2,000
- [ ] Discord members: 1,000
- [ ] Newsletter subscribers: 500

**Revenue:**
- [ ] MRR: $2,000
- [ ] Premium subscribers: 300
- [ ] Educational licenses: 5
- [ ] LTV: $30

**Content:**
- [ ] Blog posts published: 12
- [ ] YouTube videos: 5
- [ ] Total content views: 50,000

---

### Year 1 Metrics (Goals)

**User Acquisition:**
- [ ] Registered users: 25,000
- [ ] DAU: 2,500
- [ ] MAU: 10,000

**Engagement:**
- [ ] Client implementations: 500
- [ ] Retention (30-day): 25%

**Community:**
- [ ] GitHub stars: 5,000
- [ ] Discord members: 2,000
- [ ] Newsletter subscribers: 2,000

**Revenue:**
- [ ] MRR: $8,000
- [ ] ARR: $100,000
- [ ] Premium conversion: 8%

**Partnerships:**
- [ ] University partnerships: 10
- [ ] Bootcamp partnerships: 5

**Marketing:**
- [ ] Total content views: 200,000
- [ ] Organic search traffic: 5,000/month
- [ ] Backlinks: 100+

**Press:**
- [ ] Featured in 1 major tech publication
- [ ] 5+ blog mentions
- [ ] 2+ podcast appearances

---

## Cross-Promotion Opportunities

### With Learned Geek Consulting

**learnedgeek.com Integration:**

**Homepage:**
```
[Hero Section]
Building Software. Teaching Developers. Creating Tools.

[Projects Section]
Featured Project: API Combat Game
A strategic combat game for developers. No GUI, just APIs.
[Learn More] [Play Now]

[About Section]
Mark builds developer tools and teaches .NET at WCTC.
Recent work includes API Combat Game, a multiplayer game 
designed to teach API consumption.
```

**Services Page:**
```
API Design & Development
• RESTful API architecture
• Performance optimization
• Developer experience (DX) consulting

Case Study: Built API Combat Game, serving 10,000+ developers 
with 99.9% uptime and sub-200ms response times.

[View Case Study]
```

**Blog Integration:**
- Tag all game-related posts: "API Combat Game"
- Cross-link between game blog and consulting blog
- Mention in portfolio

**Email Signature:**
```
Mark [Last Name]
Senior .NET Consultant | Learned Geek Consulting
Creator of API Combat Game (10K+ developers)

Website: learnedgeek.com
Game: combatgame.dev
```

---

### With WCTC Teaching

**Course Integration:**

**Web Development Course:**
- Final project option: "Build a client for API Combat Game"
- Extra credit: "Top the class leaderboard"
- Bonus: "Contribute to open source (submit PR to game repo)"

**Software Engineering Course:**
- Case study: "Analyzing API Combat Game architecture"
- Team project: "Build a new game mode"

**Course Syllabus:**
```
Week 12-14: Final Project Options
1. E-commerce site (traditional)
2. Portfolio site (safe)
3. API Combat Game client (challenging, portfolio-worthy)

Students choosing option 3 will build a functional game client 
and present their implementation to the class.

Rubric:
• Authentication & API integration (40%)
• UI/UX quality (30%)
• Code quality (20%)
• Creativity (10%)
```

**Guest Lecture:**
- Title: "Building APIs Developers Love: Lessons from API Combat Game"
- Duration: 90 minutes
- Content: Live API design, student Q&A

---

### With The Stones Remember (Your Novel)

**Cross-Promotion Strategy:**

**Novel's About the Author:**
```
About Mark [Last Name]

Mark is a software developer and educator based in Wisconsin. 
When not writing about Celtic mythology, he builds developer 
tools like API Combat Game, a strategic game for programmers.

Connect:
• Blog: learnedgeek.com
• Game: combatgame.dev
```

**Shared Themes:**
Both projects appeal to:
- Creative technical people
- Systems thinkers
- Strategy enthusiasts
- Those who appreciate emergent complexity

**Content Ideas:**
1. Blog post: "What Writing a Novel Taught Me About Building Games"
2. Blog post: "Strategy Systems in Fiction and Code"
3. Newsletter: "Two projects about combat and strategy"

**Audience Overlap:**
- Fantasy readers who code (surprisingly large)
- D&D players (often developers)
- WorldAnvil users (game devs, writers)

---

## Ready-to-Use Templates

### Product Hunt Launch Post

**Tagline:**
"An API-only game where developers build their own clients"

**Description:**
```
API Combat Game is a strategic PvP game with a twist: there's no UI.

You interact entirely through a RESTful API. Build your own client 
(web, mobile, CLI, whatever), configure your battle strategy, and 
compete on the global leaderboard.

🎮 HOW IT WORKS
• Configure teams via JSON
• Upload battle AI strategies
• Queue battles (they resolve server-side)
• Check results and climb the leaderboard

💻 FEATURES
• RESTful API for all game actions
• Official SDKs in C#, Python, JavaScript
• Async battles (no need to stay online)
• Public leaderboards and tournaments
• Open source game engine

🎯 PERFECT FOR
• Backend developers who love optimization
• Anyone learning API development
• Teams looking for a unique hackathon project
• Fans of incremental/automation games

🆓 FREE TIER
• 10 battles per day
• Access to basic units
• Public leaderboards

💎 PREMIUM ($5/mo)
• Unlimited battles
• Advanced units
• Priority matchmaking
• Scripting engine access

Try it free: [link]
Build a client: [GitHub]
Join Discord: [link]

Built with .NET 8 on Railway. Fully open source.
```

**Gallery Images:**

1. Screenshot: Terminal showing curl commands
2. Screenshot: Web dashboard (community-built)
3. Screenshot: Mobile client (React Native)
4. Screenshot: Battle log JSON response
5. Screenshot: Leaderboard

**Maker Comment (First Comment):**
```
Hey Product Hunt! 👋

I'm Mark, and I built API Combat Game.

This started as a weird experiment: what if the API *was* the game?

I'm a .NET consultant and needed a portfolio piece that showcased 
API design, but I'm terrible at frontend. So I made the lack of UI 
a feature.

Since soft launching 3 months ago:
• 5,000+ registered developers
• Being used in CS courses at 3 universities
• 100+ community-built clients

Tech stack: .NET 8, PostgreSQL, hosted on Railway

The game is fully open source (MIT license): [GitHub link]

I'm here all day to answer questions! Ask me anything about:
• API design decisions
• Building in public
• Teaching with games
• .NET on Railway

Thanks for checking it out! 🚀
```

---

### Show HN Post

**Title:**
"Show HN: API Combat Game – Strategic PvP with no GUI"

**URL:**
https://combatgame.dev

**Text:**
```
I built a strategic combat game with a weird constraint: no GUI.

You interact entirely through a REST API. To play, you build your 
own client (CLI, web dashboard, whatever), configure your team's 
battle AI, queue battles, and check results.

Battles resolve server-side using the strategies you upload (JSON-based 
rules for now, scripting engine coming soon). You can queue a battle, 
go to lunch, come back to results.

Tech stack: .NET 8, PostgreSQL, Railway. Fully open source.

Live demo:
curl https://api.combatgame.dev/v1/leaderboard

Try it: https://combatgame.dev
GitHub: https://github.com/api-combat-game/game-engine
Docs: https://docs.combatgame.dev

This started as a thought experiment ("what if the API *was* the game?") 
and turned into a decent portfolio piece. Also using it to teach API 
design at the local tech college.

Sample client in Python:
https://github.com/api-combat-game/client-sdk-python/blob/main/examples/simple_client.py

Would love feedback on the API design and any creative client 
implementations you come up with!
```

---

### University Outreach Email

**Subject:** Free API Game for Teaching Web Development

**Body:**
```
Dear Professor [Last Name],

I'm Mark [Last Name], a .NET consultant and part-time instructor at 
Waukesha County Technical College.

I noticed you teach [Course Name] at [University]. I built an API-only 
game specifically designed for teaching REST API consumption, and I 
think it could work well as a project for your students.

WHAT IT IS:
Instead of building another todo app, students build a game client 
(CLI, web, or mobile). They get hands-on experience with:
• HTTP requests and authentication
• JSON parsing and data modeling  
• Error handling and rate limiting
• Async operations

WHY IT WORKS:
We've used it successfully at WCTC, where students found it more 
engaging than typical CRUD projects. It's also being used at:
• [University 1] - 50 students last semester
• [University 2] - Final project for Web Dev II
• [Bootcamp] - Capstone project

WHAT I'M OFFERING:
Free educational license for your class (normally $500/semester). 
No strings attached - just want to help students learn.

MATERIALS INCLUDED:
• Curriculum guide with sample assignments
• Grading rubrics
• Video tutorials for students
• Dedicated Discord support channel
• API documentation and examples

QUICK DEMO:
• Live API: https://api.combatgame.dev
• GitHub: https://github.com/api-combat-game/game-engine
• Sample assignment: [link to PDF]

Would you be open to a 15-minute call to discuss?

Best regards,
Mark [Last Name]
Senior Consultant, Learned Geek Consulting
Part-time Instructor, Waukesha County Technical College

Email: [email]
Phone: [phone]
Website: learnedgeek.com
```

---

### YouTuber Outreach

**Subject:** Video Idea - API-Only Game for Developers

**Body:**
```
Hey [YouTuber Name],

Love your content on [specific topic]. I've been following since 
[mention specific video].

I built something your audience might find interesting: a strategic 
combat game where there's no UI. Developers build their own clients 
using a REST API.

QUICK OVERVIEW:
• No GUI provided - you build your own client
• Strategic PvP combat via JSON configs
• 5,000+ players, active meta-game
• Fully open source (.NET 8)

WHY IT MIGHT WORK FOR A VIDEO:
• Unique concept (API-as-game)
• Multiple angles (game design, API design, community)
• Visually interesting (show different client UIs)
• Could be tutorial (build a client in 20 mins)

WHAT I CAN PROVIDE:
• Full code walkthrough
• B-roll of different clients
• Access to top players for interviews
• Whatever assets you need

Demo: https://combatgame.dev
GitHub: https://github.com/api-combat-game/game-engine
Top client showcase: [link]

Would you be interested? No pressure either way - just thought it 
might align with your content.

Thanks for your time!

Best,
Mark [Last Name]
[Contact info]

P.S. Totally understand if this isn't a fit. Keep up the great work!
```

---

### Newsletter Template

**Subject:** API Combat Weekly #1 - Welcome to the Arena 🎮

**Body:**
```
Hey developers,

Welcome to the first issue of API Combat Weekly!

I'm Mark, and I built this game as a side project. Every week I'll 
share updates, strategy tips, and community highlights.

🔥 WHAT'S NEW THIS WEEK
• Launched premium tier ($5/mo)
• Added 5 new units (Berserker, Priest, Rogue, Sorcerer, Guardian)
• Performance optimization (50% faster battle resolution)

📊 BY THE NUMBERS
• 247 active players
• 12,453 battles this week  
• Current meta: Tank/Healer/Mage dominating

🏆 COMMUNITY SPOTLIGHT
@github_username built an incredible React dashboard with real-time 
battle updates. Check it out: [link]

🧠 STRATEGY TIP OF THE WEEK
New to the game? Start with a balanced team:
• 1 Tank (soak damage)
• 1 Healer (sustain)
• 1 DPS (damage dealer)

This gives you time to learn mechanics before optimizing.

🗓️ UPCOMING EVENTS
• Tournament Saturday, 2 PM EST (prize: 3 months premium)
• AMA on Discord Friday at 7 PM EST

🛠️ BUILD SOMETHING COOL?
Share your client in #client-showcase on Discord. Best submission 
this month wins swag + feature in newsletter.

Keep coding,
Mark

---

P.S. If you're enjoying the game, I'd love a star on GitHub ⭐
https://github.com/api-combat-game/game-engine

---

Unsubscribe | Update preferences | View in browser
```

---

### Reddit Post Template (r/programming)

**Title:** I built a combat game with no GUI, just a REST API

**Body:**
```
Hey r/programming,

I had this weird idea: what if the API *was* the game?

So I built a strategic combat game where there's no provided UI. 
You interact entirely through REST endpoints. To play, you build 
your own client (web, CLI, mobile, whatever) and use the API to:

• Configure your team
• Upload battle strategies (JSON-based AI)
• Queue battles (they resolve server-side)
• Check leaderboards

It's async-first so you're not glued to your computer. Queue a 
battle, grab coffee, check results.

**TECH STACK:**
• Backend: .NET 8 Web API
• Database: PostgreSQL
• Hosting: Railway
• Open source: MIT license

**WHY I BUILT IT:**
1. Portfolio piece (I'm a .NET consultant)
2. Showcase API design skills
3. Teaching tool (using it at local tech college)

**SAMPLE API CALLS:**

Register:
```bash
curl -X POST https://api.combatgame.dev/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```

Queue battle:
```bash
curl -X POST https://api.combatgame.dev/v1/battle/queue \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"teamId":"team-1","mode":"ranked"}'
```

**LINKS:**
• Play: https://combatgame.dev
• GitHub: https://github.com/api-combat-game/game-engine
• Docs: https://docs.combatgame.dev
• Sample Python client: [link]

**QUESTIONS I'LL ANSWER:**
• API design decisions
• Battle resolution algorithm
• Scaling challenges
• How to prevent abuse

Would love to hear your thoughts on the API design, or see what 
kind of clients you build!
```

---

### Dev.to Article Template

**Title:** Why I Built a Game With No Graphics

**Body:**
```markdown
# Why I Built a Game With No Graphics

## The Problem

I'm a .NET developer and consultant. I needed a portfolio piece that 
showcased my API design skills, but there was a problem: I'm terrible 
at frontend work.

Every time I tried to build a side project, I'd spend 80% of my time 
wrestling with CSS and JavaScript, and 20% on the actual interesting 
backend work.

## The Idea

One day I thought: what if the lack of a GUI was *the feature*?

What if I built a game where:
• There's no UI provided
• You interact entirely through an API
• Building your own client is part of the game

## The Experiment

I spent 3 weekends building API Combat Game - a strategic PvP game 
with no graphics.

Here's how it works:

### 1. You Build Your Own Client

Want a web dashboard? Build it.
Want a CLI tool? Build it.
Want a mobile app? Build it.

The game is just a REST API. Your client can be anything.

### 2. You Configure Your Strategy

Upload your battle AI as JSON:

```json
{
  "formation": "defensive",
  "targetPriority": ["lowest_hp", "healers"],
  "abilities": {
    "heal": { "when": "ally_hp_below_50" },
    "attack": { "when": "always" }
  }
}
```

### 3. Battles Happen Server-Side

Queue a battle, go about your day, check results later.

No need to be glued to your computer.

## What I Learned

**Lesson 1: Developers love automation**

The #1 feature request? "Can I script my strategies?"

Turns out, the game appeals to the same people who love incremental 
games and workflow automation.

**Lesson 2: The "meta" is API optimization**

Players discovered that:
• Gzipped responses reduce payload size 70%
• Parallel battle queuing tests strategies faster
• Caching leaderboard data saves API calls

The game became about optimizing code, not clicking buttons.

**Lesson 3: It's a teaching tool**

Universities started reaching out, asking if they could use it for 
teaching API consumption.

Now it's being used in CS courses at 3 universities.

## The Tech

**Stack:**
• .NET 8 Web API
• PostgreSQL
• Railway (hosting)
• Open source (MIT)

**Cool technical bits:**
• Battle resolution uses turn-based simulation
• Rate limiting doubles as game mechanic (free tier = 10 battles/day)
• Versioned API from day one (`/v1/battle/queue`)

GitHub: https://github.com/api-combat-game/game-engine

## Try It

**Live API:**
```bash
curl https://api.combatgame.dev/v1/leaderboard
```

**Play:** https://combatgame.dev

**Sample clients:**
• Python: [link]
• C#: [link]
• JavaScript: [link]

## What's Next

I'm working on:
• Scripting engine (Lua support)
• Mobile SDK
• Tournament system

Would love your feedback on the API design!

---

*Built by Mark, a .NET consultant who teaches at WCTC. 
Connect: [Twitter] [GitHub] [Website]*
```

---

## Summary & Action Items

### Immediate Actions (This Week)

- [ ] Set up GitHub organization: `api-combat-game`
- [ ] Create placeholder website: combatgame.dev
- [ ] Write initial README.md
- [ ] Draft Product Hunt description
- [ ] Draft Show HN post
- [ ] Create Discord server
- [ ] Plan first blog post

### Pre-Launch (Week 2-4)

- [ ] Build 3 example clients (CLI, web, mobile mockup)
- [ ] Create video demo (3-5 minutes)
- [ ] Write comprehensive documentation
- [ ] Set up analytics (Plausible or Google Analytics)
- [ ] Prepare Product Hunt assets
- [ ] Write 3 blog posts (schedule for launch week)

### Launch Week

- [ ] Tuesday 12:01 AM: Product Hunt
- [ ] Tuesday 6 AM: Twitter announcement
- [ ] Wednesday: Show HN
- [ ] Thursday: Reddit r/programming
- [ ] Friday: Dev.to article #1
- [ ] Saturday: Reddit r/SideProject

### Post-Launch (Month 2-3)

- [ ] Weekly blog posts
- [ ] Engage in Discord daily
- [ ] Reach out to 10 universities
- [ ] Launch premium tier
- [ ] First tournament

### Long-Term (Month 4-12)

- [ ] YouTube content (5-10 videos)
- [ ] Podcast appearances (3-5 shows)
- [ ] Educational partnerships (10+ schools)
- [ ] Consulting leads from portfolio
- [ ] Hit $5K MRR

---

## Final Thoughts

**This game is NOT a Steam game.** It's a developer tool disguised as a game.

Your audience lives on:
• GitHub (showcase code)
• Hacker News (technical community)
• Reddit (r/programming, r/gamedev)
• Dev.to (developer bloggers)
• YouTube (developer content)

**Marketing Budget:** $0 (except hosting ~$50/month)

Everything is organic, content-driven, community-focused. 

The game markets itself through:
1. Unique concept (natural virality)
2. Open source (GitHub discovery)
3. Educational use (universities amplify)
4. Developer UX (quality speaks)

**Your Advantages:**
• You already teach (credibility with educators)
• You already consult (credibility with enterprises)
• You already blog (distribution channel)
• You understand the audience (you are the audience)

**Timeline to Profitability:**
• Month 1-2: Soft launch, validate
• Month 3: Public launch, hit 1K users
• Month 4: Monetize, hit $500 MRR
• Month 6: Educational licenses, hit $2K MRR
• Month 12: Multiple revenue streams, hit $8K MRR

**Year 1 Goal:** $100K revenue (50% from subs, 30% from edu, 20% from consulting leads)

---

**Now go build it and spam the market methodically using this doc as your checklist! 🚀**

---

*Document Version: 1.0*  
*Last Updated: February 10, 2026*  
*Maintained by: Mark @ Learned Geek Consulting*
