# API Combat — Load Tests

Smoke and load tests for [apicombat.com](https://apicombat.com) using [k6](https://k6.io).

## Install k6

```bash
# macOS
brew install k6

# Windows (winget)
winget install k6 --source winget

# Windows (choco)
choco install k6

# Docker
docker pull grafana/k6
```

## Run Tests

### Smoke test against production

```bash
k6 run k6-smoke-test.js
```

### Against local dev server

```bash
k6 run -e BASE_URL=http://localhost:5000 k6-smoke-test.js
```

### Quick single-user test

```bash
k6 run --vus 1 --duration 30s k6-smoke-test.js
```

### Docker

```bash
docker run --rm -i grafana/k6 run - <k6-smoke-test.js
```

## What It Tests

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /health` | No | Health check |
| `GET /api/v1/version` | No | Version info |
| `GET /api/v1/sdk/status` | No | Game status & metrics |
| `GET /api/v1/sdk/quickstart` | No | SDK quick-start guide |
| `GET /api/v1/sdk/endpoints` | No | Endpoint catalog |
| `POST /api/v1/auth/register` | No | Player registration |
| `POST /api/v1/auth/login` | No | JWT login |
| `GET /api/v1/roster` | JWT | Player roster |
| `GET /api/v1/leaderboard` | JWT | Global leaderboard |

## Thresholds

- **p95 response time** < 500ms
- **Error rate** < 1%

## Load Profile (Smoke Test)

- Ramp up to 10 virtual users over 30 seconds
- Sustain 10 VUs for 1 minute
- Ramp down over 30 seconds
- Auth flow runs every 5th iteration per VU to avoid flooding registrations

## Notes

- Load test accounts use emails ending in `@loadtest.local` — clean up via admin if needed
- Run against production sparingly; shared hosting has resource limits
- For sustained load testing, run against a local dev instance first
