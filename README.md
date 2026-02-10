# API Combat Game

A turn-based combat game played entirely through REST API endpoints. No frontend needed - the API **is** the game.

## Tech Stack

- **.NET 8** Web API
- **Entity Framework Core** with SQLite
- **JWT** authentication
- **Swagger/OpenAPI** documentation
- Background service for battle resolution
- Docker support

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run Locally

```bash
# Clone the repo
git clone <repo-url>
cd ApiCombatGame

# Restore dependencies
dotnet restore

# Run the API
dotnet run --project ApiCombatGame

# Open Swagger UI
# Navigate to: https://localhost:7000/swagger
# Or: http://localhost:5000/swagger
```

### Run with Docker

```bash
docker compose up --build
# API available at http://localhost:8080/swagger
```

### Run Tests

```bash
dotnet test
```

## API Documentation

Once running, full interactive API docs are available at `/swagger`.

## Sample API Workflow

### 1. Register a Player

```bash
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "warrior1",
    "email": "warrior1@example.com",
    "password": "SecurePass123!"
  }'
```

Response:
```json
{
  "playerId": "...",
  "token": "eyJhbGci...",
  "expiresAt": "2026-02-10T15:30:00Z"
}
```

### 2. Login

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "warrior1",
    "password": "SecurePass123!"
  }'
```

### 3. View Your Roster

```bash
curl http://localhost:5000/api/v1/player/roster \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 4. View Available Units to Unlock

```bash
curl http://localhost:5000/api/v1/player/roster/available \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5. Unlock a New Unit

```bash
curl -X POST http://localhost:5000/api/v1/player/roster/unlock \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"templateUnitId": "TEMPLATE_UNIT_ID"}'
```

### 6. Configure a Team

```bash
curl -X POST http://localhost:5000/api/v1/team/configure \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Alpha Squad",
    "unitIds": ["UNIT_ID_1", "UNIT_ID_2", "UNIT_ID_3"],
    "strategy": {
      "formation": "aggressive",
      "targetPriority": ["healers", "lowest_hp"],
      "abilities": {
        "Fireball": {"when": "always", "target": "priority"},
        "Heal": {"when": "ally_hp_below_50", "target": "lowest_ally_hp"}
      }
    }
  }'
```

### 7. Queue for Battle

```bash
curl -X POST http://localhost:5000/api/v1/battle/queue \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"teamId": "TEAM_ID", "mode": "ranked"}'
```

### 8. Check Battle Status

```bash
curl http://localhost:5000/api/v1/battle/status/BATTLE_ID \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 9. Get Battle Results

```bash
curl http://localhost:5000/api/v1/battle/results/BATTLE_ID \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 10. View Leaderboard

```bash
curl http://localhost:5000/api/v1/leaderboard \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Database Schema

| Table | Description |
|-------|-------------|
| `Players` | Player accounts with rating, currency, level |
| `Units` | Combat units (both templates and player-owned) |
| `Abilities` | Unit abilities (basic attack, class ability, ultimate) |
| `Teams` | Team configurations with unit IDs and strategy JSON |
| `Battles` | Battle records with status, logs, and results |

## Game Mechanics

### Unit Classes

| Class | Strengths | Weaknesses |
|-------|-----------|------------|
| Warrior | High HP, good ATK | Low speed |
| Mage | Highest ATK | Low HP, low DEF |
| Ranger | High speed | Moderate stats |
| Healer | Can heal allies | Low ATK |
| Tank | Highest HP & DEF | Lowest speed |

### Class Advantages

- Warrior > Ranger (1.2x damage)
- Ranger > Mage (1.2x damage)
- Mage > Warrior (1.2x damage)
- Tank & Healer: Neutral

### Formations

- **Aggressive**: +15% damage dealt
- **Defensive**: -15% damage taken
- **Balanced**: No modifiers

### Battle Resolution

1. Units act in speed order each turn
2. Strategy determines ability selection and targeting
3. Damage = (Attack * AbilityMultiplier) - TargetDefense
4. 10% critical hit chance (1.5x damage)
5. Class advantage/disadvantage modifiers
6. Max 50 turns per battle
7. Winner determined by surviving team (or most HP remaining)

### Rating System

ELO-style rating with K-factor of 32. Winners gain rating, losers lose rating. Minimum rating: 100.

## Project Structure

```
ApiCombatGame/
├── Controllers/       # API endpoint handlers
├── Models/
│   ├── Domain/        # Entity models
│   ├── DTOs/          # Request/response objects
│   └── Enums/         # Game enumerations
├── Services/          # Business logic
│   └── Interfaces/    # Service contracts
├── Data/              # EF Core DbContext & seeds
├── Middleware/         # Rate limiting
└── Program.cs         # App configuration
```

## Configuration

Key settings in `appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `GameSettings:StartingCurrency` | 1000 | Currency for new players |
| `GameSettings:StartingRating` | 1000 | Initial ELO rating |
| `GameSettings:MaxTeamSize` | 5 | Max units per team |
| `GameSettings:MaxTurnsPerBattle` | 50 | Turn limit per battle |
| `GameSettings:BattleProcessingIntervalSeconds` | 5 | Background processor poll interval |

## Known Limitations (POC)

- **No persistent sessions**: JWT only, no refresh token rotation
- **Simple matchmaking**: Rating-based pairing, no skill-based factors
- **No real-time updates**: Polling only, no WebSocket support
- **In-memory rate limiting**: Not shared across instances
- **SQLite**: Single-writer limitation, not suitable for high concurrency
- **No unit leveling**: Units don't gain XP or level up
- **No input sanitization beyond Data Annotations**: Production would need more robust validation
- **Background processor is single-instance**: Not distributed

## Next Steps (Priority Order)

1. **Unit leveling & XP system** - Units gain experience from battles
2. **WebSocket notifications** - Real-time battle status updates
3. **Lua scripting engine** - Custom AI strategies written in Lua
4. **PostgreSQL migration** - Production-ready database
5. **Redis caching** - Session caching and rate limiting
6. **Advanced matchmaking** - Skill-based, queue time weighting
7. **Tournaments** - Bracket-based tournament system
8. **Premium features** - Cosmetics, battle passes
9. **Analytics dashboard** - Win rates, popular strategies
10. **Replay system** - Watch past battles in detail

## License

MIT
