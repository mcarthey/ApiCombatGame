# Development Environment Setup

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows, Mac, or Linux)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development without Docker)
- Git

## Quick Start with Docker

### 1. Clone Repository
```bash
git clone https://github.com/yourusername/api-combat-game.git
cd api-combat-game
```

### 2. Start All Services
```bash
docker compose up -d
```

This starts:
- PostgreSQL (port 5432)
- Redis (port 6379)
- API (port 5000)

### 3. Verify API is Running
```bash
curl http://localhost:5000/health
# Should return: {"status":"healthy"}
```

### 4. View Logs
```bash
# All services
docker compose logs -f

# Just API
docker compose logs -f api

# Just database
docker compose logs -f postgres
```

### 5. Stop Services
```bash
docker compose down
```

### 6. Clean Everything (including database)
```bash
docker compose down -v
```

## Optional: Database Management GUI

Start pgAdmin alongside other services:
```bash
docker compose --profile tools up -d
```

Access pgAdmin at: http://localhost:5050
- Email: admin@apicombat.local
- Password: admin

Add server connection:
- Host: postgres (use container name, not localhost)
- Port: 5432
- Database: api_combat_game
- Username: gameadmin
- Password: dev_password_change_in_prod

## Local Development WITHOUT Docker

If you prefer running .NET locally:

### 1. Start just the database
```bash
docker compose up postgres redis -d
```

### 2. Update connection string in appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=api_combat_game;Username=gameadmin;Password=dev_password_change_in_prod"
  }
}
```

### 3. Run migrations
```bash
cd ApiCombatGame
dotnet ef database update
```

### 4. Run API locally
```bash
dotnet run
```

API will be available at: https://localhost:7000 (HTTPS) or http://localhost:5000 (HTTP)

## Useful Docker Commands

### Rebuild after code changes
```bash
docker compose up --build
```

### Access PostgreSQL directly
```bash
docker exec -it api-combat-db psql -U gameadmin -d api_combat_game
```

### Access Redis CLI
```bash
docker exec -it api-combat-redis redis-cli
```

### View resource usage
```bash
docker stats
```

### Clean up unused images/containers
```bash
docker system prune -a
```

---

# Railway Deployment Guide

## Initial Setup

### 1. Create Railway Account
- Go to [Railway.app](https://railway.app)
- Sign in with GitHub

### 2. Create New Project
- Click "New Project"
- Select "Deploy from GitHub repo"
- Authorize Railway to access your repo
- Select `api-combat-game` repository

### 3. Add PostgreSQL
- In project dashboard, click "+ New"
- Select "Database" → "PostgreSQL"
- Railway auto-provisions and injects `DATABASE_URL` environment variable

### 4. Configure Environment Variables

In Railway dashboard → Settings → Variables, add:

**Required:**
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:$PORT
JWT__Secret=<generate-strong-secret-min-32-chars>
JWT__Issuer=ApiCombatGame
JWT__Audience=ApiCombatGamePlayers
JWT__ExpirationMinutes=60
```

**Optional (for Redis):**
Click "+ New" → "Database" → "Redis", then add:
```
Redis__ConnectionString=${{Redis.REDIS_URL}}
```

Railway automatically maps `${{Postgres.DATABASE_URL}}` to your connection string.

### 5. Configure Build Settings

Railway auto-detects Dockerfile. If needed, override in `railway.toml`:

```toml
[build]
builder = "DOCKERFILE"
dockerfilePath = "Dockerfile"

[deploy]
startCommand = "dotnet ApiCombatGame.dll"
healthcheckPath = "/health"
healthcheckTimeout = 100
restartPolicyType = "ON_FAILURE"
restartPolicyMaxRetries = 10
```

### 6. Deploy

Railway auto-deploys on push to `main` branch:
```bash
git add .
git commit -m "Initial deployment"
git push origin main
```

Watch deployment in Railway dashboard.

### 7. Get Public URL

Railway provides a URL like: `https://api-combat-game-production.up.railway.app`

Test:
```bash
curl https://your-app.up.railway.app/health
```

## Database Migrations on Railway

### Option 1: Automatic migrations on startup

In `Program.cs`:
```csharp
if (app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        db.Database.Migrate(); // Auto-run migrations
    }
}
```

### Option 2: Manual migrations via Railway CLI

Install Railway CLI:
```bash
# macOS
brew install railway

# Windows
scoop install railway
```

Run migration:
```bash
railway login
railway link  # Link to your project
railway run dotnet ef database update
```

## Monitoring & Logs

### View Logs
```bash
railway logs
```

Or in Railway dashboard → Deployments → Click deployment → Logs tab

### Metrics
Railway dashboard shows:
- CPU usage
- Memory usage
- Network traffic
- Request count

### Alerts
Configure in Settings → Alerts:
- High memory usage
- Deployment failures
- Crash detection

## Scaling on Railway

### Vertical Scaling (more resources per instance)
Settings → Change plan tier

### Horizontal Scaling (multiple instances)
Not available on Hobby plan. Upgrade to Pro for:
- Load balancing
- Multiple replicas
- Auto-scaling

## Cost Estimates

**Hobby Plan** (pay-as-you-go):
- $0.000231/GB-hour memory
- $0.000463/vCPU-hour
- Estimate: ~$5-10/month for POC

**Pro Plan** ($20/month):
- Includes $20 usage credit
- Priority support
- Team collaboration
- Higher resource limits

**PostgreSQL Plugin**:
- $5/month for 1GB
- $10/month for 8GB

**Total POC Cost**: ~$10-15/month

## Rollback Strategy

If deployment breaks:
```bash
railway rollback
```

Or in dashboard → Deployments → Click previous deployment → Redeploy

## Custom Domain

Railway provides free HTTPS:
1. Settings → Networking → Custom Domain
2. Add your domain (e.g., `api.apicombatgame.com`)
3. Update DNS with Railway's CNAME
4. SSL auto-provisions

## CI/CD Pipeline

Railway auto-deploys on push. For more control:

### GitHub Actions (optional)
```yaml
# .github/workflows/deploy.yml
name: Deploy to Railway

on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Test
        run: dotnet test
  
  deploy:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: bervProject/railway-deploy@main
        with:
          railway_token: ${{ secrets.RAILWAY_TOKEN }}
          service: api-combat-game
```

This runs tests before allowing Railway to deploy.

## Troubleshooting

### Deployment Fails
- Check logs: `railway logs`
- Verify Dockerfile builds locally: `docker build .`
- Ensure all env variables set

### Database Connection Issues
- Verify `DATABASE_URL` is injected
- Check PostgreSQL service is running
- Review connection string format

### Port Issues
Railway injects `$PORT` variable. Ensure:
```csharp
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
```

### High Memory Usage
- Enable response compression
- Implement caching (Redis)
- Optimize EF queries (disable tracking for reads)

---

## Development Workflow Summary

**Local Development:**
```bash
docker compose up -d          # Start services
# Make code changes
docker compose restart api    # Restart just API
docker compose logs -f api    # Watch logs
```

**Deploying to Railway:**
```bash
git add .
git commit -m "Feature X"
git push origin main          # Auto-deploys
railway logs                  # Watch deployment
```

**Database Management:**
```bash
# Local
docker exec -it api-combat-db psql -U gameadmin -d api_combat_game

# Railway
railway connect postgres
```

That's it! You have:
- ✅ Local development with Docker
- ✅ One-command deployment to Railway
- ✅ Automatic migrations
- ✅ Monitoring and logs
- ✅ Easy rollbacks
