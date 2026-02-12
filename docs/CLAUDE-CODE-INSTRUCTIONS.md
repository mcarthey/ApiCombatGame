# API Combat Game POC - Project Setup Instructions for Claude Code

**Version:** 1.0  
**Date:** February 10, 2026  
**Purpose:** Complete instructions for Claude Code to scaffold the entire POC project

---

## Overview

This document contains instructions to give to Claude Code (or any AI coding assistant) to create a complete, working POC for the API Combat Game. The goal is to have a functional .NET 8 Web API with SQLite that you can immediately run locally and start developing.

---

## Instructions to Give Claude Code

Copy everything from "START INSTRUCTIONS" to "END INSTRUCTIONS" and paste it into Claude Code.

---

**START INSTRUCTIONS**

I want you to create a complete API-only combat game POC in .NET 8. This should be a fully functional REST API where developers interact with the game through HTTP endpoints. There should be NO frontend - the API is the entire interface.

## Project Structure

Create this exact folder structure:

```
ApiCombatGame/
├── .github/
│   └── workflows/
│       └── dotnet.yml
├── ApiCombatGame/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── PlayerController.cs
│   │   ├── TeamController.cs
│   │   ├── BattleController.cs
│   │   └── LeaderboardController.cs
│   ├── Models/
│   │   ├── Domain/
│   │   │   ├── Player.cs
│   │   │   ├── Unit.cs
│   │   │   ├── Team.cs
│   │   │   ├── Battle.cs
│   │   │   ├── BattleLog.cs
│   │   │   └── Ability.cs
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterRequest.cs
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   └── AuthResponse.cs
│   │   │   ├── Team/
│   │   │   │   ├── TeamConfigRequest.cs
│   │   │   │   └── TeamResponse.cs
│   │   │   ├── Battle/
│   │   │   │   ├── BattleQueueRequest.cs
│   │   │   │   ├── BattleStatusResponse.cs
│   │   │   │   └── BattleResultResponse.cs
│   │   │   └── Strategy/
│   │   │       └── StrategyConfig.cs
│   │   └── Enums/
│   │       ├── UnitClass.cs
│   │       ├── BattleStatus.cs
│   │       └── AbilityType.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs
│   │   │   ├── IBattleService.cs
│   │   │   ├── IStrategyEngine.cs
│   │   │   └── IMatchmakingService.cs
│   │   ├── AuthService.cs
│   │   ├── BattleService.cs
│   │   ├── DeclarativeStrategyEngine.cs
│   │   ├── MatchmakingService.cs
│   │   └── BackgroundBattleProcessor.cs
│   ├── Data/
│   │   ├── GameDbContext.cs
│   │   ├── Migrations/
│   │   └── SeedData.cs
│   ├── Middleware/
│   │   └── RateLimitingMiddleware.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── ApiCombatGame.csproj
├── ApiCombatGame.Tests/
│   ├── UnitTests/
│   │   ├── BattleEngineTests.cs
│   │   └── StrategyEngineTests.cs
│   ├── IntegrationTests/
│   │   ├── AuthTests.cs
│   │   └── BattleFlowTests.cs
│   └── ApiCombatGame.Tests.csproj
├── .dockerignore
├── .gitignore
├── Dockerfile
├── docker-compose.yml
├── railway.toml
├── README.md
└── ApiCombatGame.sln
```

## Technical Requirements

**Framework & Database:**
- .NET 8 Web API
- Entity Framework Core with SQLite (easy local development)
- JWT authentication
- Swagger/OpenAPI documentation
- Background service for battle resolution

**Design Principles:**
- Clean Architecture (separate concerns)
- SOLID principles
- Async/await everywhere
- Proper error handling and validation
- Rate limiting built-in

## Detailed Implementation Requirements

### 1. Domain Models

**Player:**
```csharp
public class Player
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public int Level { get; set; } = 1;
    public int Currency { get; set; } = 1000;
    public int Rating { get; set; } = 1000; // ELO-style rating
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    
    // Navigation properties
    public List<Unit> Roster { get; set; } = new();
    public List<Team> Teams { get; set; } = new();
    public List<Battle> Battles { get; set; } = new();
}
```

**Unit:**
```csharp
public class Unit
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public UnitClass Class { get; set; }
    public int Level { get; set; } = 1;
    
    // Base stats
    public int Health { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    
    // Relationships
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public List<Ability> Abilities { get; set; } = new();
}
```

**UnitClass (Enum):**
```csharp
public enum UnitClass
{
    Warrior,
    Mage,
    Ranger,
    Healer,
    Tank
}
```

**Team:**
```csharp
public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public List<Guid> UnitIds { get; set; } = new(); // Max 5 units
    public string StrategyJson { get; set; } // Serialized StrategyConfig
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Battle:**
```csharp
public class Battle
{
    public Guid Id { get; set; }
    public Guid Player1Id { get; set; }
    public Guid Player2Id { get; set; }
    
    public Guid Team1Id { get; set; }
    public Guid Team2Id { get; set; }
    
    public BattleStatus Status { get; set; } = BattleStatus.Queued;
    public Guid? WinnerId { get; set; }
    public int Turns { get; set; }
    
    public string BattleLogJson { get; set; } // Serialized list of battle events
    
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation
    public Player Player1 { get; set; }
    public Player Player2 { get; set; }
}
```

**BattleStatus (Enum):**
```csharp
public enum BattleStatus
{
    Queued,
    InProgress,
    Completed,
    Cancelled
}
```

### 2. Strategy Configuration (JSON-based for POC)

**StrategyConfig:**
```csharp
public class StrategyConfig
{
    public string Formation { get; set; } // "aggressive", "defensive", "balanced"
    public List<string> TargetPriority { get; set; } // ["lowest_hp", "healers", "highest_threat"]
    public Dictionary<string, AbilityCondition> Abilities { get; set; }
}

public class AbilityCondition
{
    public string When { get; set; } // "always", "ally_hp_below_50", "enemy_count_gte_2"
    public string Target { get; set; } // "priority", "lowest_ally_hp", "all_enemies"
}
```

### 3. API Endpoints

**Auth Endpoints:**
```
POST   /api/v1/auth/register
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
```

**Player Endpoints:**
```
GET    /api/v1/player/profile
GET    /api/v1/player/roster
POST   /api/v1/player/roster/unlock  (unlock new unit with currency)
```

**Team Endpoints:**
```
POST   /api/v1/team/configure
GET    /api/v1/team/{teamId}
GET    /api/v1/team/list
PUT    /api/v1/team/{teamId}
DELETE /api/v1/team/{teamId}
```

**Battle Endpoints:**
```
POST   /api/v1/battle/queue
GET    /api/v1/battle/status/{battleId}
GET    /api/v1/battle/results/{battleId}
GET    /api/v1/battle/history?limit=20&offset=0
```

**Leaderboard Endpoints:**
```
GET    /api/v1/leaderboard?limit=100
GET    /api/v1/leaderboard/player/{playerId}
```

**Health Check:**
```
GET    /health
```

### 4. Authentication & Authorization

- Use JWT tokens (Bearer authentication)
- Password hashing with BCrypt
- Token expiration: 1 hour
- Include PlayerId in token claims
- All endpoints except /auth/* and /health require authentication

### 5. Battle Resolution Engine

**Requirements:**
- Turn-based combat simulation
- Deterministic (same teams + same strategies = same outcome)
- Each turn:
  1. Determine action order (by Speed stat)
  2. Execute actions based on strategy
  3. Apply damage/healing
  4. Check for victory condition
- Max 50 turns per battle (prevent infinite loops)
- Generate detailed battle log (JSON array of turn events)

**Sample Battle Log Entry:**
```json
{
  "turn": 5,
  "actor": "Warrior_A",
  "action": "attack",
  "target": "Mage_B",
  "damage": 45,
  "effects": ["critical_hit"],
  "targetHpRemaining": 55
}
```

### 6. Background Battle Processor

- Hosted service that runs continuously
- Polls for queued battles every 5 seconds
- Processes battles one at a time (simple queue)
- Updates battle status: Queued → InProgress → Completed
- Awards currency and rating changes to winner

### 7. Seed Data

Create 5 starter units per class (25 total):

**Warriors:**
1. Bronze Knight (HP:120, ATK:25, DEF:20, SPD:10)
2. Iron Gladiator (HP:130, ATK:28, DEF:18, SPD:12)
3. Steel Berserker (HP:140, ATK:30, DEF:15, SPD:15)
4. Silver Champion (HP:150, ATK:32, DEF:22, SPD:11)
5. Gold Warlord (HP:160, ATK:35, DEF:25, SPD:10)

**Mages:**
1. Apprentice Wizard (HP:80, ATK:35, DEF:10, SPD:15)
2. Fire Sorcerer (HP:85, ATK:38, DEF:12, SPD:16)
3. Ice Conjurer (HP:90, ATK:40, DEF:10, SPD:18)
4. Lightning Warlock (HP:95, ATK:42, DEF:13, SPD:17)
5. Archmage (HP:100, ATK:45, DEF:15, SPD:20)

**Rangers:**
1. Scout (HP:90, ATK:30, DEF:15, SPD:20)
2. Hunter (HP:95, ATK:32, DEF:16, SPD:22)
3. Marksman (HP:100, ATK:35, DEF:14, SPD:25)
4. Sniper (HP:105, ATK:38, DEF:17, SPD:23)
5. Master Archer (HP:110, ATK:40, DEF:18, SPD:28)

**Healers:**
1. Novice Cleric (HP:85, ATK:15, DEF:18, SPD:12)
2. Priest (HP:90, ATK:18, DEF:20, SPD:13)
3. Bishop (HP:95, ATK:20, DEF:22, SPD:14)
4. High Priest (HP:100, ATK:22, DEF:24, SPD:15)
5. Divine Oracle (HP:105, ATK:25, DEF:26, SPD:16)

**Tanks:**
1. Shield Bearer (HP:150, ATK:20, DEF:30, SPD:8)
2. Fortress Guard (HP:160, ATK:22, DEF:32, SPD:7)
3. Iron Wall (HP:170, ATK:24, DEF:35, SPD:6)
4. Bastion (HP:180, ATK:26, DEF:38, SPD:7)
5. Immovable (HP:200, ATK:28, DEF:40, SPD:5)

Each unit should have 3 abilities:
- Basic Attack (always available)
- Class-specific ability (cooldown 3 turns)
- Ultimate (cooldown 5 turns)

### 8. Configuration Files

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=api_combat_game.db"
  },
  "JWT": {
    "Secret": "your-super-secret-jwt-key-minimum-32-characters-long-change-in-production",
    "Issuer": "ApiCombatGame",
    "Audience": "ApiCombatGamePlayers",
    "ExpirationMinutes": 60
  },
  "GameSettings": {
    "StartingCurrency": 1000,
    "StartingRating": 1000,
    "MaxTeamSize": 5,
    "MaxTurnsPerBattle": 50,
    "BattleProcessingIntervalSeconds": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### 9. Program.cs

Should include:
- Service registration (DbContext, services, repositories)
- JWT authentication configuration
- Swagger/OpenAPI setup
- CORS (allow all for development)
- Health check endpoint
- Automatic migration on startup (development only)
- Background service registration
- Seed data on first run

### 10. Docker Support

**Dockerfile:**
- Multi-stage build (build → publish → runtime)
- Use .NET 8 SDK and runtime images
- Expose port 8080
- Health check endpoint

**docker-compose.yml:**
- Single service (API)
- SQLite database persisted via volume mount
- Environment variables for configuration

### 11. Testing

**Unit Tests:**
- Battle engine determinism test
- Strategy parsing test
- Damage calculation test

**Integration Tests:**
- Register → Login → Create Team → Queue Battle → Check Results

### 12. Documentation

**README.md should include:**
- Project description
- Tech stack
- Quick start (clone, run, test)
- API documentation link (Swagger)
- Sample curl commands for each endpoint
- How to run tests
- How to deploy

### 13. CI/CD

**.github/workflows/dotnet.yml:**
- Build on push to main
- Run tests
- Publish artifacts

### 14. Additional Files

**.gitignore:**
- Standard .NET gitignore
- Exclude `*.db`, `*.db-*` (SQLite files)
- Exclude `bin/`, `obj/`, `.vs/`, etc.

**.dockerignore:**
- Similar to .gitignore
- Exclude unnecessary files from Docker build context

**railway.toml:**
- Configuration for Railway deployment
- Health check path
- Start command

## Specific Implementation Notes

### Damage Calculation Formula

```csharp
int baseDamage = attacker.Attack - target.Defense;
if (baseDamage < 0) baseDamage = 1; // Minimum damage

// Apply critical hit (10% chance)
if (Random.Shared.NextDouble() < 0.1)
{
    baseDamage = (int)(baseDamage * 1.5);
    effects.Add("critical_hit");
}

// Apply class advantage/disadvantage
if (ClassAdvantage(attacker.Class, target.Class))
{
    baseDamage = (int)(baseDamage * 1.2);
    effects.Add("class_advantage");
}

return baseDamage;
```

**Class Advantages:**
- Warrior > Ranger
- Ranger > Mage
- Mage > Warrior
- Tank and Healer: neutral

### Battle Matchmaking

For POC: Simple random pairing from queue
- When player queues, find any other queued battle with similar rating (±200)
- If no match within 30 seconds, match with anyone
- If still no match, create AI opponent (future feature)

### Rate Limiting

Simple in-memory rate limiting:
- 60 requests per minute per IP
- Return 429 Too Many Requests if exceeded
- Include headers:
  - `X-RateLimit-Limit: 60`
  - `X-RateLimit-Remaining: 42`
  - `X-RateLimit-Reset: 1675959600`

## Code Quality Standards

- Use async/await for all I/O operations
- Proper exception handling with try/catch
- Input validation with Data Annotations
- Return proper HTTP status codes (200, 201, 400, 401, 404, 500)
- Use DTOs for API requests/responses (don't expose domain models)
- Follow RESTful conventions
- Add XML comments for API documentation (shows in Swagger)

## Sample API Request/Response

**Register:**
```bash
POST /api/v1/auth/register
Content-Type: application/json

{
  "username": "testplayer",
  "email": "test@example.com",
  "password": "SecurePass123!"
}

Response 201 Created:
{
  "playerId": "123e4567-e89b-12d3-a456-426614174000",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-10T15:30:00Z"
}
```

**Queue Battle:**
```bash
POST /api/v1/battle/queue
Authorization: Bearer eyJhbGci...
Content-Type: application/json

{
  "teamId": "123e4567-e89b-12d3-a456-426614174001",
  "mode": "ranked"
}

Response 201 Created:
{
  "battleId": "123e4567-e89b-12d3-a456-426614174002",
  "status": "queued",
  "queuePosition": 3,
  "estimatedWaitSeconds": 15
}
```

**Get Battle Results:**
```bash
GET /api/v1/battle/results/123e4567-e89b-12d3-a456-426614174002
Authorization: Bearer eyJhbGci...

Response 200 OK:
{
  "battleId": "123e4567-e89b-12d3-a456-426614174002",
  "status": "completed",
  "winnerId": "123e4567-e89b-12d3-a456-426614174000",
  "loserId": "123e4567-e89b-12d3-a456-426614174003",
  "turns": 12,
  "battleLog": [
    {
      "turn": 1,
      "actor": "Warrior_A",
      "action": "attack",
      "target": "Mage_B",
      "damage": 45,
      "effects": ["critical_hit"],
      "targetHpRemaining": 55
    }
  ],
  "rewards": {
    "currency": 100,
    "ratingChange": 15
  },
  "completedAt": "2026-02-10T14:25:30Z"
}
```

## Deliverables

When you're done, I should be able to:

1. Clone the repo
2. Run `dotnet restore`
3. Run `dotnet run --project ApiCombatGame`
4. Open https://localhost:7000/swagger
5. Register a user via API
6. Login and get a token
7. Configure a team
8. Queue a battle
9. Wait a few seconds
10. Get battle results

Everything should work end-to-end with zero manual configuration.

## Questions to Answer for Me

After you generate all the code, please provide:

1. **Setup Instructions:** Exact commands to run the project locally
2. **Sample API Workflow:** curl commands showing register → login → battle flow
3. **Database Schema:** Brief overview of the tables created
4. **Next Steps:** What features should be added next (prioritized list)
5. **Known Limitations:** What's missing from this POC that production would need

## Additional Context

This is a POC to validate the concept of an API-only game. Focus on:
- **Working code** over perfect code
- **Simple implementation** over complex architecture
- **Clear documentation** over extensive features
- **Easy to run** over production-ready

Once this POC works, we'll iterate and add:
- Scripting engine (Lua)
- More game mechanics
- Better matchmaking
- Analytics
- Premium features

But for now, just get the core loop working: authenticate → configure team → battle → see results.

**END INSTRUCTIONS**

---

## What to Expect from Claude Code

After you paste these instructions, Claude Code should:

1. **Create the entire project structure** with all files
2. **Implement all the code** (controllers, services, models, etc.)
3. **Configure EF Core migrations** with seed data
4. **Set up Docker** (Dockerfile, docker-compose.yml)
5. **Create tests** (unit and integration)
6. **Write documentation** (README with sample commands)
7. **Provide you with:**
   - Setup instructions
   - Sample curl commands to test the API
   - Overview of the architecture
   - Next steps for development

## After Claude Code Generates Everything

**Step 1: Review the code**
```bash
# Clone your empty repo
git clone https://github.com/yourusername/api-combat-game.git
cd api-combat-game

# Let Claude Code generate everything in this directory
# (paste the instructions above into Claude Code)
```

**Step 2: Run it locally**
```bash
# Restore packages
dotnet restore

# Run migrations (creates SQLite database)
dotnet ef database update --project ApiCombatGame

# Run the API
dotnet run --project ApiCombatGame

# Open Swagger in browser
# https://localhost:7000/swagger
```

**Step 3: Test the API**
```bash
# Register a user
curl -X POST https://localhost:7000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testplayer","email":"test@example.com","password":"Test123!"}'

# Login (copy the token from response)
curl -X POST https://localhost:7000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Get your roster
curl -X GET https://localhost:7000/api/v1/player/roster \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# Configure a team (use unit IDs from roster)
curl -X POST https://localhost:7000/api/v1/team/configure \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My First Team",
    "unitIds": ["unit-id-1", "unit-id-2", "unit-id-3"],
    "strategy": {
      "formation": "balanced",
      "targetPriority": ["lowest_hp", "healers"],
      "abilities": {
        "attack": {"when": "always", "target": "priority"},
        "heal": {"when": "ally_hp_below_50", "target": "lowest_ally_hp"}
      }
    }
  }'

# Queue a battle (use team ID from response above)
curl -X POST https://localhost:7000/api/v1/battle/queue \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"teamId":"your-team-id","mode":"ranked"}'

# Wait 10-15 seconds for background processor to resolve battle

# Get battle results (use battle ID from queue response)
curl -X GET https://localhost:7000/api/v1/battle/results/your-battle-id \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Step 4: Commit to Git**
```bash
git add .
git commit -m "Initial POC implementation - generated by Claude Code"
git push origin main
```

---

## Tips for Working with Claude Code

1. **Be specific:** The instructions above are very detailed on purpose
2. **Iterate:** If something doesn't work, tell Claude Code what failed
3. **Ask questions:** "Why did you implement X this way?"
4. **Request changes:** "Can you refactor the battle engine to be more modular?"
5. **Get explanations:** "Explain how the JWT authentication works"

## Common Issues & Solutions

**Issue:** "Migration fails"
```bash
# Delete the database and migrations folder
rm api_combat_game.db
rm -rf ApiCombatGame/Migrations

# Recreate migrations
dotnet ef migrations add InitialCreate --project ApiCombatGame
dotnet ef database update --project ApiCombatGame
```

**Issue:** "Port already in use"
```bash
# Change port in Program.cs or run on different port
dotnet run --project ApiCombatGame --urls "https://localhost:7001;http://localhost:5001"
```

**Issue:** "JWT token validation fails"
```bash
# Make sure appsettings.json JWT:Secret is at least 32 characters
# Make sure you're using the token from login response
# Token expires after 1 hour - login again if expired
```

---

## Next Steps After POC Works

Once you have the POC running locally:

1. **Test thoroughly:** Try to break it, find edge cases
2. **Review the code:** Understand how everything works
3. **Deploy to Railway:** Push to GitHub, Railway auto-deploys
4. **Build a simple client:** CLI or web dashboard to test the API
5. **Iterate on game mechanics:** Add new units, balance stats
6. **Prepare for public launch:** Follow the marketing strategy doc

---

## Expected Timeline

**With Claude Code:**
- Code generation: 15-30 minutes
- Your review: 30-60 minutes
- Testing locally: 15-30 minutes
- First deployment: 30 minutes
- **Total: 2-3 hours to working POC**

**Without Claude Code (manual):**
- Setup project structure: 1 hour
- Implement models: 1-2 hours
- Implement controllers: 2-3 hours
- Implement services: 2-3 hours
- Battle engine: 2-4 hours
- Testing: 1-2 hours
- Documentation: 1 hour
- **Total: 10-16 hours**

Claude Code saves you 8-13 hours of boilerplate coding. 🚀

---

## Validation Checklist

After Claude Code generates everything, verify:

- [ ] Project compiles with no errors
- [ ] Swagger UI loads at /swagger
- [ ] Can register a new user
- [ ] Can login and receive JWT token
- [ ] Token validates on protected endpoints
- [ ] Can view player roster (seeded units)
- [ ] Can create a team configuration
- [ ] Can queue a battle
- [ ] Background service processes battle
- [ ] Can retrieve battle results with log
- [ ] Leaderboard shows players ranked by rating
- [ ] Database persists across restarts
- [ ] Health check endpoint returns 200
- [ ] Tests pass: `dotnet test`

---

**Good luck! You'll have a working POC by tonight. 🎮**

---

*Document Version: 1.0*  
*Last Updated: February 10, 2026*  
*Prepared by: Mark @ Learned Geek Consulting*
