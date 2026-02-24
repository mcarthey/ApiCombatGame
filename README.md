# API Combat Game

A turn-based combat game played entirely through REST API endpoints. No frontend needed — **the API is the game.**

**Live at [apicombat.com](https://apicombat.com)** | [Full API Docs](https://apicombat.com/api-docs/v1) | [OpenAPI Spec](https://apicombat.com/openapi/v1.json)

> 100+ endpoints · 51 database tables · 10 background services · 630+ tests

---

## 30-Second Quickstart

**0. See it live (no account needed):**

```bash
curl https://apicombat.com/api/v1/leaderboard?limit=5
```

**1. Register:**

```bash
curl -X POST https://apicombat.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"warrior1","email":"you@example.com","password":"Pass123!"}'
```

Save the `token` from the response — it's your key to everything.

**2. Queue your first battle:**

```bash
curl -X POST https://apicombat.com/api/v1/battle/queue \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"mode":"ranked"}'
```

No need to pick a team — your Starter Team (3 units) is used automatically.

**3. Check results:**

```bash
curl https://apicombat.com/api/v1/battle/results/BATTLE_ID \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**That's it.** You're playing. Full docs: [apicombat.com/api-docs/v1](https://apicombat.com/api-docs/v1)

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8, Razor Pages + API Controllers |
| Database | MSSQL (EF Core 8 with migrations) |
| Auth | JWT Bearer + Cookies + API Key (`X-Api-Key: acg_...`) |
| Frontend | Material Design 3 Expressive (custom CSS) + Tailwind CDN |
| API Docs | Custom renderer at `/api-docs/v1` (not Swagger UI) |
| Hosting | SmarterASP.NET via MSDeploy |
| Background | 10 hosted services (battles, tournaments, challenges, guild wars, etc.) |
| Testing | xUnit + WebApplicationFactory + Moq + Playwright |

---

## Features

### Core Combat
- **5 unit classes**: Warrior, Rogue, Mage, Healer, Tank — with rock-paper-scissors advantages
- **Team building**: 1–5 units per team with custom battle strategies
- **Formations**: Aggressive (+15% damage), Defensive (-15% damage taken), Balanced
- **Async battles**: Queue-based matchmaking, turn-by-turn combat logs
- **AI opponents**: 9 practice bots across 3 difficulty tiers (no rating impact)

### Competitive
- **Ranked seasons**: 8-week seasons with Bronze → Diamond → Legend tiers
- **Tournaments**: Weekly 16-player single-elimination brackets, seeded by rating
- **ELO rating**: K-factor 32, minimum 100 — with dev meme tier names (see below)
- **Rivals**: Weekly rival assignments for targeted matchups

### Social
- **Guilds**: Create/join guilds with roles, treasury, and chat
- **Guild wars**: Weekly matched wars with point-based scoring
- **Guild bosses**: Raid encounters with damage leaderboards
- **Activity feed**: Social stream of battles, achievements, and guild events

### Economy
- **Strategy marketplace**: Buy/sell battle strategies, earn currency on sales
- **Loot drops**: 15% base chance, up to 25% on win streaks — currency, XP, rare titles
- **Cosmetics**: Skins and visual effects for units
- **Battle pass**: Seasonal progression with exclusive rewards

### Developer Experience
- **HATEOAS links**: Every response includes `_links` for self-discoverable navigation
- **API keys**: `X-Api-Key` header auth for third-party integrations
- **SDK endpoints**: `/api/v1/sdk/status`, `/api/v1/sdk/endpoints`, `/api/v1/sdk/quickstart`
- **OpenAPI spec**: Machine-readable at `/openapi/v1.json`

### Community
- **Achievements**: Combat, progression, social, and secret categories
- **Daily challenges**: Rotating objectives with currency rewards
- **Battle replays**: Shareable URLs for turn-by-turn replay viewing
- **Referral program**: Invite bonuses for both referrer and referred
- **Discord integration**: Webhooks for battle results and guild events
- **Education mode**: Curriculum modules for teaching API concepts

---

## Rating Tiers (API — Arena Power Index)

| Rating | Tier | Flavor |
|--------|------|--------|
| 0–799 | Rubber Duck | At least someone's listening |
| 800–1099 | Copy Pasta | ctrl+c, ctrl+v, ship it |
| 1100–1299 | Code Monkey | It works. Don't touch it. |
| 1300–1499 | Bug Hunter | It's not a bug, it's a feature |
| 1500–1699 | 10x Dev | Mythical creature spotted |
| 1700–1899 | Wizard | Any sufficiently advanced code... |
| 1900+ | I Use Arch btw | You mass-assigned everything. Legendary. |

---

## Game Mechanics

### Unit Classes

| Class | Strengths | Weaknesses |
|-------|-----------|------------|
| Warrior | High HP, good ATK | Low speed |
| Mage | Highest ATK | Low HP, low DEF |
| Rogue | High speed | Moderate stats |
| Healer | Can heal allies | Low ATK |
| Tank | Highest HP & DEF | Lowest speed |

### Class Advantages (1.2x damage)

- Warrior > Rogue
- Rogue > Mage
- Mage > Warrior
- Tank & Healer: Neutral

### Battle Resolution

1. Units act in speed order each turn
2. Strategy determines ability selection and targeting
3. Damage = (Attack × AbilityMultiplier) − TargetDefense
4. 10% critical hit chance (1.5× damage)
5. Class advantage/disadvantage modifiers
6. Max 50 turns per battle
7. Winner determined by surviving team (or most HP remaining)

---

## Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (included with Visual Studio) or MSSQL instance

### Run Locally

```bash
git clone https://github.com/mcarthey/ApiCombatGame.git
cd ApiCombatGame
dotnet restore
dotnet run --project ApiCombatGame
```

The app auto-migrates the database on startup. API docs available at `https://localhost:7000/api-docs/v1`.

### Run with Docker

```bash
docker compose up --build
# API available at http://localhost:8080/api-docs/v1
```

> Note: Docker config uses SQLite for portability. Production uses MSSQL.

### Run Tests

```bash
dotnet test
# 630+ tests: 595 unit/integration + 12 Playwright + load tests
```

---

## Project Structure

```
ApiCombatGame/
├── Authentication/     # API key auth handler
├── BackgroundJobs/     # 10 hosted services (battles, tournaments, etc.)
├── Controllers/        # 36 API controllers
├── Data/               # EF Core DbContext, migrations, seed data
├── Filters/            # OpenAPI doc filters, tier gating
├── Helpers/            # Rating tiers, utilities
├── Middleware/          # Correlation ID, rate limiting, error handling, activity tracking
├── Models/
│   ├── Domain/         # Entity models (51 tables)
│   ├── DTOs/           # Request/response objects (31 feature groups)
│   ├── Enums/          # Game constants
│   └── ViewModels/     # Razor page models
├── Pages/              # Razor Pages (web UI + API docs)
├── Services/           # 111 service files (business logic)
│   └── Interfaces/     # Service contracts
├── wwwroot/            # Static assets
└── Program.cs          # App configuration

ApiCombatGame.Tests/           # xUnit: unit + integration tests
ApiCombatGame.PlaywrightTests/ # Browser automation tests
tests/load/                    # k6 load tests
```

---

## Configuration

Key settings in `appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `GameSettings:StartingCurrency` | 1000 | Currency for new players |
| `GameSettings:StartingRating` | 1000 | Initial ELO rating |
| `GameSettings:MaxTeamSize` | 5 | Max units per team |
| `GameSettings:MaxTurnsPerBattle` | 50 | Turn limit per battle |
| `GameSettings:BattleProcessingIntervalSeconds` | 5 | Battle processor poll interval |
| `GameSettings:FreeTierDailyBattleLimit` | 10 | Free tier daily battle cap |
| `JWT:ExpirationMinutes` | 60 | Token lifetime |

---

## License

MIT
