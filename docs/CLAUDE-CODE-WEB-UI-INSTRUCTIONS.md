# API Combat Game - Add Account Management Web UI (Option A)

**Version:** 1.0  
**Date:** February 10, 2026  
**Purpose:** Instructions for Claude Code to add Razor Pages-based account management to existing API project

---

## Overview

Add a traditional web UI for account management, subscriptions, and billing while keeping the game API public and documented. This creates a "safe zone" for payments and sensitive operations while maintaining the API-first game experience.

---

## Instructions to Give Claude Code

Copy everything from "START INSTRUCTIONS" to "END INSTRUCTIONS" and paste it into Claude Code.

---

**START INSTRUCTIONS**

I need you to add a web-based account management UI to my existing API Combat Game project. This should be a Razor Pages application that runs alongside the API, handling subscriptions, billing, and account settings through a traditional web interface.

## Goals

1. **Separate concerns:** Game API (public, fun) vs Account Management (secure, professional)
2. **Safe payments:** Use Stripe Checkout (hosted) instead of API endpoints for subscriptions
3. **Professional appearance:** Clean, modern UI that builds trust
4. **Cookie authentication:** Separate from JWT (JWT for API, cookies for web UI)
5. **Hide sensitive endpoints:** Payment/admin endpoints not shown in public API docs

## Technical Requirements

**Add to existing project (don't create new project):**
- ASP.NET Core Razor Pages
- Cookie-based authentication (separate from JWT)
- Stripe.net SDK for payment processing
- Tailwind CSS for styling (via CDN for simplicity)
- Account management pages
- Subscription management with Stripe Checkout
- API key management for developers

**Do NOT break existing API:**
- All existing API endpoints must continue working
- JWT authentication for API unchanged
- Swagger/OpenAPI still available at `/swagger`

## Project Structure Updates

Add these folders and files to the existing `ApiCombatGame` project:

```
ApiCombatGame/
├── Pages/                          # NEW: Razor Pages
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _Navigation.cshtml
│   │   └── _LoginPartial.cshtml
│   ├── Account/
│   │   ├── Index.cshtml            # Dashboard
│   │   ├── Index.cshtml.cs
│   │   ├── Subscription.cshtml     # Subscription management
│   │   ├── Subscription.cshtml.cs
│   │   ├── Billing.cshtml          # Payment methods & history
│   │   ├── Billing.cshtml.cs
│   │   ├── ApiKeys.cshtml          # API key management
│   │   ├── ApiKeys.cshtml.cs
│   │   ├── Settings.cshtml         # Account settings
│   │   └── Settings.cshtml.cs
│   ├── Auth/
│   │   ├── Login.cshtml            # Web login (separate from API)
│   │   ├── Login.cshtml.cs
│   │   ├── Register.cshtml         # Web registration
│   │   ├── Register.cshtml.cs
│   │   └── Logout.cshtml.cs
│   ├── Index.cshtml                # Landing page
│   ├── Index.cshtml.cs
│   ├── Privacy.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── Controllers/
│   └── Webhooks/
│       └── StripeWebhookController.cs  # NEW: Handle Stripe events
├── Services/
│   ├── Interfaces/
│   │   ├── ISubscriptionService.cs     # NEW
│   │   └── IApiKeyService.cs           # NEW
│   ├── SubscriptionService.cs          # NEW
│   └── ApiKeyService.cs                # NEW
├── Models/
│   ├── Domain/
│   │   ├── Subscription.cs             # NEW
│   │   └── ApiKey.cs                   # NEW
│   └── ViewModels/                     # NEW
│       ├── DashboardViewModel.cs
│       ├── SubscriptionViewModel.cs
│       └── BillingViewModel.cs
├── wwwroot/                        # Static files
│   ├── css/
│   │   └── site.css
│   ├── js/
│   │   └── site.js
│   └── favicon.ico
└── appsettings.json                # UPDATE: Add Stripe keys
```

## New Domain Models

### Subscription Model

```csharp
public class Subscription
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public string StripeCustomerId { get; set; }
    public string StripeSubscriptionId { get; set; }
    public string StripePriceId { get; set; }
    
    public SubscriptionTier Tier { get; set; }
    public SubscriptionStatus Status { get; set; }
    
    public decimal AmountUsd { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CancelAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum SubscriptionTier
{
    Free,
    Premium,
    PremiumPlus
}

public enum SubscriptionStatus
{
    Active,
    PastDue,
    Canceled,
    Incomplete
}
```

### ApiKey Model

```csharp
public class ApiKey
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Player Player { get; set; }
    
    public string Name { get; set; } // e.g., "Production", "Development"
    public string KeyHash { get; set; } // Hashed, never store plain text
    public string KeyPrefix { get; set; } // First 8 chars for display (pk_live_abc123...)
    
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
```

## Update Player Model

Add subscription relationship:

```csharp
public class Player
{
    // ... existing properties ...
    
    // NEW: Subscription info
    public SubscriptionTier CurrentTier { get; set; } = SubscriptionTier.Free;
    public int DailyBattlesUsed { get; set; } = 0;
    public DateTime LastBattleResetDate { get; set; } = DateTime.UtcNow.Date;
    
    // Navigation
    public Subscription Subscription { get; set; }
    public List<ApiKey> ApiKeys { get; set; } = new();
}
```

## Configuration Updates

### appsettings.json

Add Stripe configuration:

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
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_...",
    "PriceIds": {
      "Premium": "price_premium_monthly",
      "PremiumPlus": "price_premium_plus_monthly"
    }
  },
  "GameSettings": {
    "StartingCurrency": 1000,
    "StartingRating": 1000,
    "MaxTeamSize": 5,
    "MaxTurnsPerBattle": 50,
    "BattleProcessingIntervalSeconds": 5,
    "FreeTierDailyBattleLimit": 10
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_test_key",
    "PublishableKey": "pk_test_your_test_key",
    "WebhookSecret": "whsec_test_webhook_secret"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

## Program.cs Updates

Update to support both API (JWT) and Web UI (Cookies):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dual authentication: JWT for API + Cookies for Web UI
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "JWT_OR_COOKIE";
    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var jwtSettings = builder.Configuration.GetSection("JWT");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Secret"]))
    };
})
.AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // Use JWT for API requests, cookies for web pages
        if (context.Request.Path.StartsWithSegments("/api"))
            return JwtBearerDefaults.AuthenticationScheme;
        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
});

builder.Services.AddAuthorization();

// Add controllers (API)
builder.Services.AddControllers();

// Add Razor Pages (Web UI)
builder.Services.AddRazorPages();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Combat Game",
        Version = "v1",
        Description = "A strategic combat game controlled entirely through APIs"
    });
    
    // Hide payment and admin endpoints from public docs
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (apiDesc.RelativePath == null) return false;
        
        // Hide these endpoints from public documentation
        var hiddenPaths = new[] { "/payment/", "/admin/", "/webhooks/" };
        return !hiddenPaths.Any(path => apiDesc.RelativePath.Contains(path));
    });
    
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBattleService, BattleService>();
builder.Services.AddScoped<IStrategyEngine, DeclarativeStrategyEngine>();
builder.Services.AddScoped<IMatchmakingService, MatchmakingService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>(); // NEW
builder.Services.AddScoped<IApiKeyService, ApiKeyService>(); // NEW

// Background service for battle processing
builder.Services.AddHostedService<BackgroundBattleProcessor>();

// Stripe configuration
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Combat Game v1");
        c.RoutePrefix = "api-docs"; // Change from default "swagger"
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve CSS, JS, images

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // API endpoints
app.MapRazorPages(); // Web pages

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Redirect root to landing page
app.MapGet("/", () => Results.Redirect("/Index"));

// Run migrations and seed data (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    dbContext.Database.Migrate();
    await SeedData.Initialize(scope.ServiceProvider);
}

app.Run();
```

## Razor Pages Implementation

### Shared Layout (_Layout.cshtml)

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - API Combat Game</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body class="bg-gray-50">
    <nav class="bg-white shadow-sm border-b">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="flex justify-between h-16">
                <div class="flex">
                    <div class="flex-shrink-0 flex items-center">
                        <a asp-page="/Index" class="text-xl font-bold text-gray-900">
                            ⚔️ API Combat Game
                        </a>
                    </div>
                    <div class="hidden sm:ml-6 sm:flex sm:space-x-8">
                        <a asp-page="/Index" class="border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700 inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium">
                            Home
                        </a>
                        <a href="/api-docs" class="border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700 inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium">
                            API Docs
                        </a>
                        @if (User.Identity?.IsAuthenticated == true)
                        {
                            <a asp-page="/Account/Index" class="border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700 inline-flex items-center px-1 pt-1 border-b-2 text-sm font-medium">
                                Dashboard
                            </a>
                        }
                    </div>
                </div>
                <div class="flex items-center">
                    <partial name="_LoginPartial" />
                </div>
            </div>
        </div>
    </nav>

    <main class="py-8">
        @RenderBody()
    </main>

    <footer class="bg-white border-t mt-16">
        <div class="max-w-7xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
            <p class="text-center text-gray-500 text-sm">
                &copy; 2026 API Combat Game. Built by <a href="https://learnedgeek.com" class="text-blue-600 hover:underline">Learned Geek Consulting</a>
            </p>
        </div>
    </footer>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### Login Partial (_LoginPartial.cshtml)

```html
@using Microsoft.AspNetCore.Identity
@using System.Security.Claims

@if (User.Identity?.IsAuthenticated == true)
{
    <div class="flex items-center space-x-4">
        <span class="text-sm text-gray-700">
            Hello, @User.FindFirstValue(ClaimTypes.Name)!
        </span>
        <form method="post" asp-page="/Auth/Logout" class="inline">
            <button type="submit" class="text-sm text-gray-700 hover:text-gray-900">
                Logout
            </button>
        </form>
    </div>
}
else
{
    <div class="flex items-center space-x-4">
        <a asp-page="/Auth/Login" class="text-sm text-gray-700 hover:text-gray-900">
            Login
        </a>
        <a asp-page="/Auth/Register" class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
            Sign Up
        </a>
    </div>
}
```

### Landing Page (Index.cshtml)

```html
@page
@model IndexModel
@{
    ViewData["Title"] = "API Combat Game - Battle with Code";
}

<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <!-- Hero Section -->
    <div class="text-center py-16">
        <h1 class="text-5xl font-extrabold text-gray-900 mb-4">
            Battle with <span class="text-blue-600">Code</span>
        </h1>
        <p class="text-xl text-gray-600 mb-8 max-w-2xl mx-auto">
            An API-only strategic combat game for developers. No GUI provided—build your own client and compete.
        </p>
        <div class="flex justify-center space-x-4">
            <a asp-page="/Auth/Register" class="px-8 py-3 border border-transparent text-base font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700">
                Get Started Free
            </a>
            <a href="/api-docs" class="px-8 py-3 border border-gray-300 text-base font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                View API Docs
            </a>
        </div>
    </div>

    <!-- How It Works -->
    <div class="py-16 border-t">
        <h2 class="text-3xl font-bold text-center mb-12">How It Works</h2>
        <div class="grid md:grid-cols-3 gap-8">
            <div class="text-center">
                <div class="text-4xl mb-4">🔑</div>
                <h3 class="text-xl font-semibold mb-2">1. Register</h3>
                <p class="text-gray-600">Create an account and get your API key</p>
            </div>
            <div class="text-center">
                <div class="text-4xl mb-4">⚔️</div>
                <h3 class="text-xl font-semibold mb-2">2. Build</h3>
                <p class="text-gray-600">Create your team and configure battle strategies</p>
            </div>
            <div class="text-center">
                <div class="text-4xl mb-4">🏆</div>
                <h3 class="text-xl font-semibold mb-2">3. Compete</h3>
                <p class="text-gray-600">Queue battles and climb the leaderboard</p>
            </div>
        </div>
    </div>

    <!-- Sample Code -->
    <div class="py-16 bg-gray-900 -mx-4 sm:-mx-6 lg:-mx-8 px-4 sm:px-6 lg:px-8 rounded-lg">
        <h2 class="text-3xl font-bold text-white text-center mb-8">Quick Start</h2>
        <div class="max-w-3xl mx-auto">
            <pre class="bg-gray-800 text-gray-100 p-6 rounded overflow-x-auto"><code>// Queue a battle
var client = new GameClient(apiKey: "your_api_key");

var result = await client.QueueBattle(
    teamId: "team_abc123",
    mode: "ranked"
);

Console.WriteLine($"Battle queued: {result.BattleId}");
// Wait for results...
var battleResult = await client.GetBattleResults(result.BattleId);
Console.WriteLine($"Winner: {battleResult.WinnerId}");</code></pre>
        </div>
    </div>

    <!-- Pricing -->
    <div class="py-16 border-t">
        <h2 class="text-3xl font-bold text-center mb-12">Pricing</h2>
        <div class="grid md:grid-cols-3 gap-8 max-w-5xl mx-auto">
            <div class="border rounded-lg p-6">
                <h3 class="text-xl font-bold mb-2">Free</h3>
                <p class="text-3xl font-bold mb-4">$0<span class="text-sm text-gray-600">/mo</span></p>
                <ul class="space-y-2 mb-6 text-gray-600">
                    <li>✓ 10 battles/day</li>
                    <li>✓ Basic units</li>
                    <li>✓ Public leaderboards</li>
                </ul>
                <a asp-page="/Auth/Register" class="block text-center w-full bg-gray-200 text-gray-700 py-2 rounded hover:bg-gray-300">
                    Get Started
                </a>
            </div>
            <div class="border-2 border-blue-500 rounded-lg p-6 relative">
                <div class="absolute top-0 right-0 bg-blue-500 text-white px-3 py-1 text-sm rounded-bl">
                    Popular
                </div>
                <h3 class="text-xl font-bold mb-2">Premium</h3>
                <p class="text-3xl font-bold mb-4">$5<span class="text-sm text-gray-600">/mo</span></p>
                <ul class="space-y-2 mb-6 text-gray-600">
                    <li>✓ Unlimited battles</li>
                    <li>✓ 50+ units</li>
                    <li>✓ Priority queue</li>
                    <li>✓ Advanced stats</li>
                </ul>
                <a asp-page="/Auth/Register" class="block text-center w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700">
                    Start Free Trial
                </a>
            </div>
            <div class="border rounded-lg p-6">
                <h3 class="text-xl font-bold mb-2">Premium Plus</h3>
                <p class="text-3xl font-bold mb-4">$10<span class="text-sm text-gray-600">/mo</span></p>
                <ul class="space-y-2 mb-6 text-gray-600">
                    <li>✓ Everything in Premium</li>
                    <li>✓ Scripting engine</li>
                    <li>✓ Advanced analytics</li>
                    <li>✓ 1-on-1 coaching</li>
                </ul>
                <a asp-page="/Auth/Register" class="block text-center w-full bg-gray-700 text-white py-2 rounded hover:bg-gray-800">
                    Start Free Trial
                </a>
            </div>
        </div>
    </div>
</div>
```

### Account Dashboard (Account/Index.cshtml)

```html
@page
@model DashboardModel
@{
    ViewData["Title"] = "Dashboard";
}

<div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <h1 class="text-3xl font-bold mb-8">Dashboard</h1>

    <!-- Stats Overview -->
    <div class="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <div class="bg-white rounded-lg shadow p-6">
            <div class="text-sm text-gray-600 mb-1">Battles Today</div>
            <div class="text-3xl font-bold">@Model.BattlesToday / @Model.DailyLimit</div>
            <div class="text-xs text-gray-500 mt-1">
                @if (Model.CurrentTier == "Free")
                {
                    <text>Free tier</text>
                }
                else
                {
                    <text>Unlimited</text>
                }
            </div>
        </div>
        <div class="bg-white rounded-lg shadow p-6">
            <div class="text-sm text-gray-600 mb-1">Win Rate</div>
            <div class="text-3xl font-bold">@Model.WinRate%</div>
            <div class="text-xs text-gray-500 mt-1">Last 100 battles</div>
        </div>
        <div class="bg-white rounded-lg shadow p-6">
            <div class="text-sm text-gray-600 mb-1">Ranking</div>
            <div class="text-3xl font-bold">#@Model.GlobalRank</div>
            <div class="text-xs text-gray-500 mt-1">Global leaderboard</div>
        </div>
        <div class="bg-white rounded-lg shadow p-6">
            <div class="text-sm text-gray-600 mb-1">Rating</div>
            <div class="text-3xl font-bold">@Model.Rating</div>
            <div class="text-xs text-green-600 mt-1">+@Model.RatingChange since yesterday</div>
        </div>
    </div>

    <!-- Upgrade Banner (Free tier only) -->
    @if (Model.CurrentTier == "Free")
    {
        <div class="bg-blue-50 border-l-4 border-blue-400 p-4 mb-8">
            <div class="flex">
                <div class="flex-shrink-0">
                    <svg class="h-5 w-5 text-blue-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                    </svg>
                </div>
                <div class="ml-3 flex-1">
                    <p class="text-sm text-blue-700">
                        You've used <strong>@Model.BattlesToday of @Model.DailyLimit</strong> battles today.
                        <a asp-page="/Account/Subscription" class="font-medium underline">Upgrade to Premium</a> for unlimited battles.
                    </p>
                </div>
            </div>
        </div>
    }

    <!-- Quick Actions -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <a asp-page="/Account/ApiKeys" class="block bg-white rounded-lg shadow p-6 hover:shadow-md transition">
            <div class="text-lg font-semibold mb-2">🔑 API Keys</div>
            <p class="text-sm text-gray-600">Manage your API keys and access tokens</p>
        </a>
        <a asp-page="/Account/Subscription" class="block bg-white rounded-lg shadow p-6 hover:shadow-md transition">
            <div class="text-lg font-semibold mb-2">💳 Subscription</div>
            <p class="text-sm text-gray-600">Upgrade or manage your plan</p>
        </a>
        <a href="/api-docs" class="block bg-white rounded-lg shadow p-6 hover:shadow-md transition">
            <div class="text-lg font-semibold mb-2">📖 API Docs</div>
            <p class="text-sm text-gray-600">View the complete API reference</p>
        </a>
    </div>

    <!-- Recent Activity -->
    <div class="bg-white rounded-lg shadow">
        <div class="px-6 py-4 border-b">
            <h2 class="text-lg font-semibold">Recent Battles</h2>
        </div>
        <div class="divide-y">
            @foreach (var battle in Model.RecentBattles)
            {
                <div class="px-6 py-4 flex items-center justify-between">
                    <div>
                        <div class="font-medium">
                            vs @battle.OpponentName
                        </div>
                        <div class="text-sm text-gray-500">
                            @battle.CompletedAt.ToString("MMM dd, yyyy HH:mm")
                        </div>
                    </div>
                    <div>
                        @if (battle.IsWin)
                        {
                            <span class="px-3 py-1 bg-green-100 text-green-800 text-sm font-medium rounded">
                                Won
                            </span>
                        }
                        else
                        {
                            <span class="px-3 py-1 bg-red-100 text-red-800 text-sm font-medium rounded">
                                Lost
                            </span>
                        }
                    </div>
                </div>
            }
        </div>
    </div>
</div>
```

### Subscription Management (Account/Subscription.cshtml)

Complete implementation with Stripe Checkout integration:

```html
@page
@model SubscriptionModel
@{
    ViewData["Title"] = "Subscription";
}

<div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8">
    <h1 class="text-3xl font-bold mb-8">Subscription Management</h1>

    @if (Model.ShowSuccessMessage)
    {
        <div class="bg-green-50 border-l-4 border-green-400 p-4 mb-8">
            <div class="flex">
                <div class="flex-shrink-0">
                    <svg class="h-5 w-5 text-green-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                    </svg>
                </div>
                <div class="ml-3">
                    <p class="text-sm text-green-700">
                        Subscription successfully updated! Your new benefits are active now.
                    </p>
                </div>
            </div>
        </div>
    }

    <!-- Current Plan Status -->
    <div class="bg-white rounded-lg shadow p-6 mb-8">
        <div class="flex items-center justify-between">
            <div>
                <h2 class="text-lg font-semibold">Current Plan</h2>
                <p class="text-3xl font-bold text-blue-600 mt-2">@Model.CurrentTier</p>
                @if (Model.NextBillingDate.HasValue)
                {
                    <p class="text-sm text-gray-600 mt-1">
                        Next billing: @Model.NextBillingDate.Value.ToString("MMMM dd, yyyy") ($@Model.MonthlyAmount)
                    </p>
                }
            </div>
            @if (Model.CanCancel)
            {
                <div>
                    <button type="button" onclick="confirmCancel()" class="text-sm text-red-600 hover:text-red-700">
                        Cancel Subscription
                    </button>
                </div>
            }
        </div>
    </div>

    <!-- Plan Comparison -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <!-- Free Tier -->
        <div class="@(Model.CurrentTier == "Free" ? "border-2 border-blue-500" : "border") rounded-lg p-6 bg-white">
            @if (Model.CurrentTier == "Free")
            {
                <div class="bg-blue-100 text-blue-800 text-xs font-semibold px-3 py-1 rounded-full inline-block mb-4">
                    Current Plan
                </div>
            }
            <h3 class="text-2xl font-bold mb-2">Free</h3>
            <p class="text-4xl font-bold mb-6">$0<span class="text-lg text-gray-600">/month</span></p>
            <ul class="space-y-3 mb-6">
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>10 battles per day</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>20 basic units</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Public leaderboards</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>3 team slots</span>
                </li>
            </ul>
            @if (Model.CurrentTier != "Free")
            {
                <form method="post" asp-page-handler="Downgrade">
                    <button type="submit" class="w-full bg-gray-200 text-gray-700 py-2 px-4 rounded hover:bg-gray-300">
                        Downgrade to Free
                    </button>
                </form>
            }
            else
            {
                <button disabled class="w-full bg-blue-100 text-blue-600 py-2 px-4 rounded cursor-not-allowed">
                    Current Plan
                </button>
            }
        </div>

        <!-- Premium Tier -->
        <div class="@(Model.CurrentTier == "Premium" ? "border-2 border-blue-500" : "border") rounded-lg p-6 bg-white relative">
            @if (Model.CurrentTier != "Premium")
            {
                <div class="absolute top-0 right-0 bg-purple-500 text-white px-3 py-1 text-xs font-semibold rounded-bl-lg">
                    POPULAR
                </div>
            }
            @if (Model.CurrentTier == "Premium")
            {
                <div class="bg-blue-100 text-blue-800 text-xs font-semibold px-3 py-1 rounded-full inline-block mb-4">
                    Current Plan
                </div>
            }
            <h3 class="text-2xl font-bold mb-2">Premium</h3>
            <p class="text-4xl font-bold mb-6">$5<span class="text-lg text-gray-600">/month</span></p>
            <ul class="space-y-3 mb-6">
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span class="font-semibold">Unlimited battles</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>50+ units (all rarities)</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Priority matchmaking</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>10 team slots</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Advanced statistics</span>
                </li>
            </ul>
            @if (Model.CurrentTier != "Premium")
            {
                <form method="post" asp-page-handler="Upgrade">
                    <input type="hidden" name="tier" value="premium" />
                    <button type="submit" class="w-full bg-purple-600 text-white py-2 px-4 rounded hover:bg-purple-700 font-semibold">
                        Upgrade to Premium
                    </button>
                </form>
            }
            else
            {
                <button disabled class="w-full bg-blue-100 text-blue-600 py-2 px-4 rounded cursor-not-allowed">
                    Current Plan
                </button>
            }
        </div>

        <!-- Premium Plus Tier -->
        <div class="@(Model.CurrentTier == "PremiumPlus" ? "border-2 border-blue-500" : "border") rounded-lg p-6 bg-white">
            @if (Model.CurrentTier == "PremiumPlus")
            {
                <div class="bg-blue-100 text-blue-800 text-xs font-semibold px-3 py-1 rounded-full inline-block mb-4">
                    Current Plan
                </div>
            }
            <h3 class="text-2xl font-bold mb-2">Premium Plus</h3>
            <p class="text-4xl font-bold mb-6">$10<span class="text-lg text-gray-600">/month</span></p>
            <ul class="space-y-3 mb-6">
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span class="font-semibold">Everything in Premium</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Scripting engine (Lua)</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Advanced analytics dashboard</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>Higher API rate limits (2x)</span>
                </li>
                <li class="flex items-start">
                    <svg class="h-6 w-6 text-green-500 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    <span>1-on-1 coaching (monthly)</span>
                </li>
            </ul>
            @if (Model.CurrentTier != "PremiumPlus")
            {
                <form method="post" asp-page-handler="Upgrade">
                    <input type="hidden" name="tier" value="premium_plus" />
                    <button type="submit" class="w-full bg-gray-900 text-white py-2 px-4 rounded hover:bg-gray-800 font-semibold">
                        Upgrade to Premium Plus
                    </button>
                </form>
            }
            else
            {
                <button disabled class="w-full bg-blue-100 text-blue-600 py-2 px-4 rounded cursor-not-allowed">
                    Current Plan
                </button>
            }
        </div>
    </div>

    <!-- Trust Signals -->
    <div class="mt-8 bg-gray-50 rounded-lg p-6">
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 text-center text-sm text-gray-600">
            <div>
                <svg class="h-8 w-8 mx-auto mb-2 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                </svg>
                <p class="font-semibold">Secure Payments</p>
                <p>Powered by Stripe</p>
            </div>
            <div>
                <svg class="h-8 w-8 mx-auto mb-2 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                </svg>
                <p class="font-semibold">Cancel Anytime</p>
                <p>No questions asked</p>
            </div>
            <div>
                <svg class="h-8 w-8 mx-auto mb-2 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
                <p class="font-semibold">Money-Back Guarantee</p>
                <p>First 30 days</p>
            </div>
        </div>
    </div>
</div>

<!-- Cancel Confirmation Modal (JavaScript) -->
<script>
function confirmCancel() {
    if (confirm('Are you sure you want to cancel your subscription? Your access will continue until the end of your billing period.')) {
        document.getElementById('cancelForm').submit();
    }
}
</script>

<form id="cancelForm" method="post" asp-page-handler="Cancel" style="display:none;">
    <input type="hidden" />
</form>
```

### API Keys Management (Account/ApiKeys.cshtml)

```html
@page
@model ApiKeysModel
@{
    ViewData["Title"] = "API Keys";
}

<div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
    <h1 class="text-3xl font-bold mb-2">API Keys</h1>
    <p class="text-gray-600 mb-8">
        Use these keys to authenticate with the Combat API. Keep them secret!
    </p>

    @if (!string.IsNullOrEmpty(Model.NewlyCreatedKey))
    {
        <div class="bg-yellow-50 border-l-4 border-yellow-400 p-4 mb-8">
            <div class="flex">
                <div class="flex-shrink-0">
                    <svg class="h-5 w-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                    </svg>
                </div>
                <div class="ml-3 flex-1">
                    <p class="text-sm text-yellow-700 font-semibold mb-2">
                        Save this API key now! You won't be able to see it again.
                    </p>
                    <div class="bg-white rounded p-3 font-mono text-sm break-all border border-yellow-200">
                        @Model.NewlyCreatedKey
                    </div>
                    <button onclick="copyToClipboard('@Model.NewlyCreatedKey')" class="mt-2 text-sm text-blue-600 hover:text-blue-700">
                        📋 Copy to clipboard
                    </button>
                </div>
            </div>
        </div>
    }

    <!-- API Keys List -->
    <div class="space-y-4 mb-8">
        @foreach (var apiKey in Model.ApiKeys)
        {
            <div class="bg-white rounded-lg shadow p-6">
                <div class="flex items-start justify-between">
                    <div class="flex-1">
                        <h3 class="text-lg font-semibold mb-1">@apiKey.Name</h3>
                        <div class="font-mono text-sm text-gray-600 mb-2">
                            @apiKey.KeyPrefix•••••••••••••••
                        </div>
                        <div class="text-sm text-gray-500 space-y-1">
                            <div>Created: @apiKey.CreatedAt.ToString("MMM dd, yyyy")</div>
                            <div>
                                Last used: 
                                @if (apiKey.LastUsedAt.HasValue)
                                {
                                    <text>@apiKey.LastUsedAt.Value.ToString("MMM dd, yyyy HH:mm")</text>
                                }
                                else
                                {
                                    <text>Never</text>
                                }
                            </div>
                        </div>
                    </div>
                    <div class="ml-4 space-x-2">
                        <form method="post" asp-page-handler="Revoke" class="inline">
                            <input type="hidden" name="apiKeyId" value="@apiKey.Id" />
                            <button type="submit" onclick="return confirm('Are you sure you want to revoke this API key? This cannot be undone.')" 
                                    class="text-sm text-red-600 hover:text-red-700">
                                Revoke
                            </button>
                        </form>
                    </div>
                </div>
            </div>
        }

        @if (!Model.ApiKeys.Any())
        {
            <div class="bg-gray-50 rounded-lg p-8 text-center">
                <svg class="h-12 w-12 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/>
                </svg>
                <p class="text-gray-600">No API keys yet. Create one to get started!</p>
            </div>
        }
    </div>

    <!-- Create New Key -->
    <div class="bg-white rounded-lg shadow p-6">
        <h2 class="text-lg font-semibold mb-4">Create New API Key</h2>
        <form method="post" asp-page-handler="Create">
            <div class="mb-4">
                <label for="keyName" class="block text-sm font-medium text-gray-700 mb-2">
                    Key Name
                </label>
                <input type="text" id="keyName" name="keyName" required
                       placeholder="e.g., Production, Development, Testing"
                       class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
            </div>
            <button type="submit" class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">
                Create API Key
            </button>
        </form>
    </div>

    <!-- Usage Warning -->
    <div class="mt-8 bg-red-50 border-l-4 border-red-400 p-4">
        <div class="flex">
            <div class="flex-shrink-0">
                <svg class="h-5 w-5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
                </svg>
            </div>
            <div class="ml-3">
                <p class="text-sm text-red-700">
                    <strong>Security Warning:</strong> Never share your API keys or commit them to version control.
                    Treat them like passwords. If a key is compromised, revoke it immediately and create a new one.
                </p>
            </div>
        </div>
    </div>
</div>

<script>
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(function() {
        alert('API key copied to clipboard!');
    });
}
</script>
```

## Stripe Integration

### SubscriptionService.cs

```csharp
public class SubscriptionService : ISubscriptionService
{
    private readonly GameDbContext _context;
    private readonly IConfiguration _configuration;
    
    public SubscriptionService(GameDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    public async Task<string> CreateCheckoutSession(Guid playerId, string tier)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) throw new Exception("Player not found");
        
        var priceId = tier.ToLower() == "premium" 
            ? _configuration["Stripe:PriceIds:Premium"]
            : _configuration["Stripe:PriceIds:PremiumPlus"];
        
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            SuccessUrl = $"{_configuration["AppUrl"]}/Account/Subscription?success=true",
            CancelUrl = $"{_configuration["AppUrl"]}/Account/Subscription?canceled=true",
            CustomerEmail = player.Email,
            ClientReferenceId = playerId.ToString(),
        };
        
        var service = new SessionService();
        var session = await service.CreateAsync(options);
        
        return session.Url;
    }
    
    public async Task HandleSubscriptionCreated(Stripe.Subscription stripeSubscription)
    {
        var playerIdStr = stripeSubscription.ClientReferenceId;
        if (string.IsNullOrEmpty(playerIdStr)) return;
        
        var playerId = Guid.Parse(playerIdStr);
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;
        
        // Determine tier from price ID
        var tier = DetermineTierFromPriceId(stripeSubscription.Items.Data[0].Price.Id);
        
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            StripeCustomerId = stripeSubscription.CustomerId,
            StripeSubscriptionId = stripeSubscription.Id,
            StripePriceId = stripeSubscription.Items.Data[0].Price.Id,
            Tier = tier,
            Status = SubscriptionStatus.Active,
            AmountUsd = stripeSubscription.Items.Data[0].Price.UnitAmount.Value / 100m,
            CurrentPeriodStart = stripeSubscription.CurrentPeriodStart,
            CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        player.CurrentTier = tier;
        player.DailyBattlesUsed = 0; // Reset battle count
        
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
    }
    
    public async Task CancelSubscription(Guid playerId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.Status == SubscriptionStatus.Active);
        
        if (subscription == null) return;
        
        var service = new SubscriptionService();
        await service.CancelAsync(subscription.StripeSubscriptionId);
        
        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
    
    private SubscriptionTier DetermineTierFromPriceId(string priceId)
    {
        if (priceId == _configuration["Stripe:PriceIds:Premium"])
            return SubscriptionTier.Premium;
        if (priceId == _configuration["Stripe:PriceIds:PremiumPlus"])
            return SubscriptionTier.PremiumPlus;
        return SubscriptionTier.Free;
    }
}
```

### StripeWebhookController.cs

```csharp
[ApiController]
[Route("api/webhooks/stripe")]
[ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger
public class StripeWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;
    
    public StripeWebhookController(
        ISubscriptionService subscriptionService,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _configuration = configuration;
        _logger = logger;
    }
    
    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _configuration["Stripe:WebhookSecret"]
            );
            
            _logger.LogInformation($"Received Stripe webhook: {stripeEvent.Type}");
            
            switch (stripeEvent.Type)
            {
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    await _subscriptionService.HandleSubscriptionCreated(subscription);
                    break;
                    
                case "customer.subscription.deleted":
                    var deletedSubscription = stripeEvent.Data.Object as Stripe.Subscription;
                    await _subscriptionService.HandleSubscriptionCanceled(deletedSubscription.Id);
                    break;
                    
                case "invoice.payment_succeeded":
                    var invoice = stripeEvent.Data.Object as Invoice;
                    _logger.LogInformation($"Payment succeeded for {invoice.CustomerEmail}");
                    break;
                    
                case "invoice.payment_failed":
                    var failedInvoice = stripeEvent.Data.Object as Invoice;
                    _logger.LogWarning($"Payment failed for {failedInvoice.CustomerEmail}");
                    // TODO: Send email notification to user
                    break;
            }
            
            return Ok();
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe webhook error");
            return BadRequest();
        }
    }
}
```

## Database Migration

Add new migration for subscription tables:

```bash
dotnet ef migrations add AddSubscriptionAndApiKeys --project ApiCombatGame
```

## Package Dependencies

Add these NuGet packages:

```bash
dotnet add package Stripe.net
```

## Testing the Implementation

### Manual Test Checklist

After implementation, test:

**Web UI:**
- [ ] Can register via web form
- [ ] Can login via web form
- [ ] Dashboard shows correct stats
- [ ] Can navigate between pages
- [ ] Subscription page displays tiers correctly
- [ ] Can click "Upgrade to Premium"

**Stripe Checkout:**
- [ ] Clicking "Upgrade" redirects to Stripe
- [ ] Can enter test card (4242 4242 4242 4242)
- [ ] After payment, redirects back with success=true
- [ ] Subscription status updates in database
- [ ] Player's CurrentTier updates

**API Still Works:**
- [ ] Can still register via API (POST /api/v1/auth/register)
- [ ] Can still login via API (POST /api/v1/auth/login)
- [ ] JWT tokens still work
- [ ] Battle queue still works
- [ ] Swagger still accessible at /api-docs

**API Keys:**
- [ ] Can create new API key
- [ ] Key is displayed only once
- [ ] Can revoke API key
- [ ] Can use API key for authentication (future feature)

## Environment Variables for Railway

When deploying to Railway, set these environment variables:

```
DATABASE_URL=<auto-provided by Railway>
STRIPE_SECRET_KEY=sk_live_...
STRIPE_PUBLISHABLE_KEY=pk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_PRICE_ID_PREMIUM=price_...
STRIPE_PRICE_ID_PREMIUM_PLUS=price_...
APP_URL=https://your-app.up.railway.app
```

## Stripe Test Cards

Use these for testing:

```
Success: 4242 4242 4242 4242
Decline: 4000 0000 0000 0002
Insufficient funds: 4000 0000 0000 9995
Exp: Any future date (e.g., 12/34)
CVC: Any 3 digits (e.g., 123)
ZIP: Any 5 digits (e.g., 12345)
```

## Next Steps After Implementation

1. Set up Stripe account (test mode first)
2. Create products and prices in Stripe Dashboard
3. Configure webhook endpoint in Stripe Dashboard
4. Test subscription flow end-to-end
5. Deploy to Railway
6. Switch to Stripe live mode
7. Test with real payment (refund yourself)

## Questions to Answer for Me

After you generate all the code, please provide:

1. **Files Created:** List of all new files added
2. **Database Changes:** What new tables were added
3. **Configuration Required:** What I need to set in appsettings.json
4. **Testing Instructions:** Step-by-step to test locally
5. **Stripe Setup:** What I need to do in Stripe Dashboard
6. **Known Issues:** Anything that needs manual fixing

**END INSTRUCTIONS**

---

## What to Expect from Claude Code

After pasting these instructions, Claude Code should:

1. **Add Razor Pages** to your existing project
2. **Implement all page models** with proper logic
3. **Create subscription service** with Stripe integration
4. **Add webhook controller** for Stripe events
5. **Update Program.cs** for dual authentication
6. **Create migration** for new tables
7. **Provide you with:**
   - List of files created
   - Configuration instructions
   - Stripe setup guide
   - Local testing steps

## After Claude Code Generates Everything

**Step 1: Install Stripe package**
```bash
dotnet add package Stripe.net
```

**Step 2: Run new migration**
```bash
dotnet ef migrations add AddSubscriptionAndApiKeys --project ApiCombatGame
dotnet ef database update --project ApiCombatGame
```

**Step 3: Update appsettings.Development.json**
```json
{
  "Stripe": {
    "SecretKey": "sk_test_your_test_key_here",
    "PublishableKey": "pk_test_your_test_key_here",
    "WebhookSecret": "whsec_your_webhook_secret_here",
    "PriceIds": {
      "Premium": "price_test_premium",
      "PremiumPlus": "price_test_premium_plus"
    }
  },
  "AppUrl": "https://localhost:7000"
}
```

**Step 4: Run the application**
```bash
dotnet run --project ApiCombatGame
```

**Step 5: Test the web UI**
- Open browser: https://localhost:7000
- Click "Sign Up"
- Create an account
- Navigate to /Account/Subscription
- Try clicking "Upgrade to Premium" (will fail without Stripe keys)

**Step 6: Set up Stripe (Test Mode)**

1. Create account at https://stripe.com
2. Go to Dashboard → Developers → API keys
3. Copy "Publishable key" and "Secret key"
4. Go to Products → Add Product:
   - Name: "API Combat Game - Premium"
   - Price: $5/month recurring
   - Copy the Price ID (starts with "price_")
5. Repeat for Premium Plus ($10/month)
6. Go to Developers → Webhooks → Add endpoint
   - URL: https://your-app.railway.app/api/webhooks/stripe
   - Events: `customer.subscription.*`, `invoice.*`
   - Copy webhook secret (starts with "whsec_")
7. Update appsettings.Development.json with all these values

**Step 7: Test subscription flow**
```
1. Navigate to /Account/Subscription
2. Click "Upgrade to Premium"
3. Should redirect to Stripe Checkout
4. Use test card: 4242 4242 4242 4242
5. Complete checkout
6. Should redirect back to your site
7. Check database - Subscription table should have new row
8. Check Player table - CurrentTier should be "Premium"
```

---

## Troubleshooting

**Issue: "Razor Pages not found"**
```bash
# Make sure you added this to Program.cs
builder.Services.AddRazorPages();
app.MapRazorPages();
```

**Issue: "Stripe API key invalid"**
- Make sure you're using test keys (sk_test_... and pk_test_...)
- Keys are from Stripe Dashboard → Developers → API keys

**Issue: "Webhook signature invalid"**
- Make sure webhook secret matches Stripe Dashboard
- In development, use Stripe CLI: `stripe listen --forward-to localhost:7000/api/webhooks/stripe`

**Issue: "Cookie authentication not working"**
- Clear browser cookies
- Make sure you're on /Auth/Login (not /api/v1/auth/login)

---

## Success Criteria

After implementation, you should be able to:

- [x] Visit landing page at /
- [x] Register via web form at /Auth/Register
- [x] Login via web form at /Auth/Login
- [x] See dashboard at /Account
- [x] View subscription options at /Account/Subscription
- [x] Click "Upgrade" and see Stripe Checkout
- [x] Complete payment (test mode)
- [x] See "Current Plan: Premium" after payment
- [x] Create API keys at /Account/ApiKeys
- [x] API endpoints still work (test with Swagger at /api-docs)
- [x] Webhook receives Stripe events
- [x] Database updates after subscription changes

---

**Good luck! You'll have a professional-looking web UI for account management by tomorrow. 🎨💳**

---

*Document Version: 1.0*  
*Last Updated: February 10, 2026*  
*Prepared by: Mark @ Learned Geek Consulting*
