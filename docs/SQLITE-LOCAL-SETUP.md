# Running Locally with SQLite (No Docker Required)

Since you already have SQLite installed, you can skip Docker entirely for local development.

## Prerequisites

- .NET 8 SDK
- SQLite (already installed)
- Your favorite IDE (Visual Studio, Rider, or VS Code)

## Quick Start

### 1. Create Project Structure

```bash
dotnet new webapi -n ApiCombatGame
cd ApiCombatGame
```

### 2. Add Required NuGet Packages

```bash
# Entity Framework Core with SQLite
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design

# JWT Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Swagger/OpenAPI
dotnet add package Swashbuckle.AspNetCore

# Optional but recommended
dotnet add package Serilog.AspNetCore
```

### 3. Update appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=api_combat_game.db"
  },
  "JWT": {
    "Secret": "your-super-secret-jwt-key-minimum-32-characters-long",
    "Issuer": "ApiCombatGame",
    "Audience": "ApiCombatGamePlayers",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### 4. Configure DbContext in Program.cs

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add SQLite
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Other services...
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-run migrations in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.Migrate();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 5. Run Migrations

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply to database
dotnet ef database update
```

This creates `api_combat_game.db` in your project root.

### 6. Run the API

```bash
dotnet run
```

API available at:
- HTTPS: https://localhost:7000
- HTTP: http://localhost:5000
- Swagger: https://localhost:7000/swagger

## Database Management

### View/Edit SQLite Database

**Option 1: VS Code Extension**
- Install "SQLite Viewer" extension
- Right-click `.db` file → "Open Database"

**Option 2: DB Browser for SQLite** (GUI app)
- Download from: https://sqlitebrowser.org/
- Open `api_combat_game.db`

**Option 3: Command Line**
```bash
sqlite3 api_combat_game.db

# Common commands
.tables              # List all tables
.schema users        # Show table structure
SELECT * FROM users; # Query data
.quit                # Exit
```

### Reset Database

```bash
# Delete the database file
rm api_combat_game.db

# Recreate from migrations
dotnet ef database update
```

Or programmatically in your code:
```csharp
db.Database.EnsureDeleted();
db.Database.EnsureCreated();
```

## Migrating to PostgreSQL Later

When you're ready to scale, just change the connection string and provider:

### 1. Add PostgreSQL package
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### 2. Update Program.cs
```csharp
// Replace UseSqlite with UseNpgsql
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 3. Update connection string
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=api_combat_game;Username=gameadmin;Password=yourpassword"
  }
}
```

### 4. Recreate migrations
```bash
# Delete old migrations
rm -rf Migrations/

# Create new ones for PostgreSQL
dotnet ef migrations add InitialCreate
dotnet ef database update
```

That's it! Entity Framework Core handles the provider differences automatically.

## Performance Testing with SQLite

SQLite is **fast enough for load testing** your POC:
- Handles 1000+ requests/second easily
- 100,000+ database writes per second
- Perfect for validating your API logic before production

Run NBomber tests against SQLite - if it performs well here, it'll be even better with PostgreSQL.

## Development Workflow

```bash
# Make code changes
# API auto-reloads with dotnet watch

dotnet watch run

# In another terminal, run tests
dotnet test

# Load test
cd LoadTests
dotnet run
```

## Advantages of SQLite for POC

✅ **No infrastructure** - just a file  
✅ **Instant setup** - no Docker, no server config  
✅ **Fast** - all queries are in-process  
✅ **Portable** - copy `.db` file = copy entire database  
✅ **Easy to reset** - delete file, done  
✅ **Perfect for testing** - create new DB per test run  
✅ **Version control friendly** - can commit seed data in .db file  

## When to Migrate to PostgreSQL

Only migrate when you need:
- **Multiple API servers** (SQLite = single writer)
- **Advanced queries** (full-text search, JSON operations)
- **High concurrency** (1000+ simultaneous writes)
- **Larger datasets** (100GB+ databases)

For POC → early beta, SQLite is **genuinely perfect**. Don't prematurely optimize.

---

## Railway Deployment with SQLite

SQLite works on Railway too! But there's a catch:

**Ephemeral filesystem** - Railway containers reset on deploy, losing your .db file.

**Solutions:**

### Option 1: SQLite + Railway Volume (persistent storage)
```toml
# railway.toml
[deploy]
volumeMounts = [
  { mountPath = "/app/data", name = "sqlite-data" }
]
```

Then use connection string: `Data Source=/app/data/api_combat_game.db`

**Cost**: +$5/month for 1GB volume

### Option 2: Switch to PostgreSQL on Railway (recommended)
Railway's PostgreSQL plugin is **so easy**:
1. Click "+ New" → Database → PostgreSQL
2. Railway auto-injects `$DATABASE_URL`
3. Update connection string, done

**Cost**: +$5/month for managed PostgreSQL

### Option 3: Use Railway's built-in SQLite (testing only)
Works but data lost on every deploy. Fine for testing deploys, not production.

## My Recommendation

**POC (now)**: SQLite locally, no Docker  
**Beta testing**: SQLite locally, PostgreSQL on Railway  
**Production**: PostgreSQL everywhere  

Keep your DbContext provider-agnostic (use EF Core abstractions), and switching is literally a 5-minute job.

---

**Bottom line**: Start simple. SQLite gets you coding faster and learning Docker/Railway is separate from learning game development. Master one thing at a time.
