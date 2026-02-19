using System.Reflection;
using System.Text;
using ApiCombatGame.BackgroundJobs;
using ApiCombatGame.Data;
using ApiCombatGame.Filters;
using ApiCombatGame.Middleware;
using ApiCombatGame.Models;
using ApiCombatGame.Services;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dual authentication: JWT for API + Cookies for Web UI
var jwtSecret = builder.Configuration["JWT:Secret"]
    ?? throw new InvalidOperationException("JWT:Secret is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "JWT_OR_COOKIE";
    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
})
.AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // Use JWT for API requests or when Bearer token is present
        string? authorization = context.Request.Headers.Authorization;
        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return JwtBearerDefaults.AuthenticationScheme;

        if (context.Request.Path.StartsWithSegments("/api"))
            return JwtBearerDefaults.AuthenticationScheme;

        // Use cookies for web pages
        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// Services
builder.Services.AddScoped<IPlayerProgressionService, PlayerProgressionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBattleService, BattleService>();
builder.Services.AddScoped<IStrategyEngine, DeclarativeStrategyEngine>();
builder.Services.AddScoped<IMatchmakingService, MatchmakingService>();
builder.Services.AddScoped<ISubscriptionService, ApiCombatGame.Services.SubscriptionService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// AI Practice Opponents
builder.Services.AddScoped<IAiOpponentService, AiOpponentService>();

// Ranked Seasons
builder.Services.AddScoped<ISeasonService, SeasonService>();

// Loot Drops
builder.Services.AddScoped<ILootService, LootService>();

// Easter Egg Hunt
builder.Services.AddScoped<IEasterEggService, EasterEggService>();

// Referral System
builder.Services.AddScoped<IReferralService, ReferralService>();

// Rival System
builder.Services.AddScoped<IRivalService, RivalService>();

// Battle Pass
builder.Services.AddScoped<IBattlePassService, BattlePassService>();

// Guild Wars
builder.Services.AddScoped<IGuildWarService, GuildWarService>();

// Tournaments
builder.Services.AddScoped<ITournamentService, TournamentService>();

// Cosmetics
builder.Services.AddScoped<ICosmeticService, CosmeticService>();

// Premium Plus
builder.Services.AddScoped<IPremiumPlusService, PremiumPlusService>();

// Activity Feed & Ledger
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();
builder.Services.AddScoped<IActivityLedger, ActivityLedger>();

// Education
builder.Services.AddScoped<IEducationService, EducationService>();

// SDK
builder.Services.AddScoped<ISdkService, SdkService>();

// Discord
builder.Services.AddScoped<IDiscordService, DiscordService>();

// Content Creators
builder.Services.AddScoped<IContentCreatorService, ContentCreatorService>();

// Player Analytics
builder.Services.AddScoped<IPlayerAnalyticsService, PlayerAnalyticsService>();

// Bot System
builder.Services.AddSingleton<IBotNameGenerator, BotNameGenerator>();
builder.Services.AddScoped<IBotTeamGenerator, BotTeamGenerator>();

// Phase 3: Guild & Engagement Services
builder.Services.AddScoped<IGuildService, GuildService>();
builder.Services.AddScoped<IGuildTreasuryService, GuildTreasuryService>();
builder.Services.AddScoped<IModifierService, ModifierService>();
builder.Services.AddScoped<IGuildBossService, GuildBossService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IStrategyMarketplaceService, StrategyMarketplaceService>();
builder.Services.AddScoped<IMasteryService, MasteryService>();
builder.Services.AddScoped<IReplayService, ReplayService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IGuildChatService, GuildChatService>();
builder.Services.AddScoped<IGuildStrategyService, GuildStrategyService>();

// Contact page: reCAPTCHA + Email
builder.Services.Configure<RecaptchaSettings>(builder.Configuration.GetSection("Recaptcha"));
builder.Services.AddHttpClient<IRecaptchaService, RecaptchaService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Admin
builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
builder.Services.AddScoped<IAppLogService, AppLogService>();

// Phase 4: Notifications & Logging
builder.Services.AddScoped<INotificationService, NotificationService>();

// Background services
builder.Services.AddHostedService<BackgroundBattleProcessor>();

// Phase 3: Background Jobs
builder.Services.AddHostedService<WeeklyModifierRotationJob>();
builder.Services.AddHostedService<DailyChallengeGenerationJob>();
builder.Services.AddHostedService<StrategyDecayJob>();
builder.Services.AddHostedService<GuildBossSpawnJob>();
builder.Services.AddHostedService<GuildInviteExpiryJob>();

// Phase 5: Background Jobs
builder.Services.AddHostedService<GuildWarMatchingJob>();
builder.Services.AddHostedService<TournamentProcessingJob>();

// Phase 4: Background Jobs
builder.Services.AddHostedService<NotificationCleanupJob>();
builder.Services.AddHostedService<AdminAlertJob>();

// Filters
builder.Services.AddScoped<TierGatingActionFilter>();

// Controllers (API)
builder.Services.AddControllers(options =>
    {
        options.Filters.AddService<TierGatingActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err =>
                {
                    var message = err.ErrorMessage;

                    // Improve cryptic JSON deserialization errors
                    if (!string.IsNullOrEmpty(err.Exception?.Message))
                    {
                        if (err.Exception.Message.Contains("could not be converted"))
                            message = $"Invalid value for '{e.Key}'. Check the field type and format — see the Strategy Guide in the API docs for valid values.";
                        else
                            message = $"Invalid value for '{e.Key}': {err.Exception.Message}";
                    }

                    if (string.IsNullOrEmpty(message))
                        message = $"The '{e.Key}' field is invalid.";

                    return message;
                }))
                .ToList();

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                error = errors.Count == 1 ? errors[0] : "One or more validation errors occurred.",
                details = errors
            });
        };
    });

// Razor Pages (Web UI)
builder.Services.AddRazorPages();

// OpenAPI specification + Scalar API reference
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var appVersion = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "1.0.0";

    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Combat Game",
        Version = appVersion,
        Description =
            "**The API IS the game.** A turn-based combat game played entirely through REST endpoints.\n\n" +
            "Register an account, recruit units to your roster, assemble teams, configure declarative battle strategies, " +
            "and compete against other players — all via HTTP requests. No GUI provided; building your own client is part of the fun.\n\n" +
            "## Quick Start\n" +
            "1. **Register** — `POST /api/v1/auth/register` with a username, email, and password\n" +
            "2. **Browse units** — `GET /api/v1/player/roster/available` to see what you can recruit\n" +
            "3. **Unlock units** — `POST /api/v1/player/roster/unlock` to spend currency on combatants\n" +
            "4. **Build a team** — `POST /api/v1/team/configure` with up to 5 units and a strategy\n" +
            "5. **Enter battle** — `POST /api/v1/battle/queue` to find an opponent\n" +
            "6. **Check results** — `GET /api/v1/battle/results/{id}` for the full combat log\n\n" +
            "## Authentication\n" +
            "Most endpoints require a JWT Bearer token. Register or login to receive one, " +
            "then include it as `Authorization: Bearer <token>` in all subsequent requests. " +
            "Tokens expire after 60 minutes — use the refresh endpoint to get a new one.\n\n" +
            "## Game Concepts\n" +
            "- **Units** — Six classes (Warrior, Mage, Ranger, Healer, Tank, Assassin) with unique abilities and a rock-paper-scissors balance\n" +
            "- **Strategies** — Declarative JSON configs that control unit AI: formations, target priority, and conditional ability usage\n" +
            "- **API Rating** — Arena Power Index matchmaking. Climb from Rubber Duck to I Use Arch btw\n" +
            "- **Modifiers** — Weekly environmental effects that change battle rules for everyone\n" +
            "- **Guilds** — Join a guild to tackle cooperative boss raids\n" +
            "- **Mastery** — The more you use a unit, the deeper your mastery grows\n\n" +
            "## Discoverable Links (`_links`)\n" +
            "Every response includes a `_links` object with related endpoints you can follow. " +
            "No need to memorize URLs — just follow the links.\n\n" +
            "```json\n" +
            "\"_links\": {\n" +
            "  \"self\": { \"href\": \"/api/v1/battle/results/abc-123\", \"method\": \"GET\" },\n" +
            "  \"replay\": { \"href\": \"/api/v1/replays/abc-123\", \"method\": \"GET\", \"title\": \"Watch replay\" },\n" +
            "  \"queue_again\": { \"href\": \"/api/v1/battle/queue\", \"method\": \"POST\" }\n" +
            "}\n" +
            "```",
        Contact = new OpenApiContact
        {
            Name = "API Combat Game"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token (without the `Bearer` prefix). " +
                      "Obtain one from `POST /api/v1/auth/login`.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Hide webhook endpoints from public docs
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var path = apiDesc.RelativePath;
        if (path == null) return false;
        return !path.Contains("webhooks/", StringComparison.OrdinalIgnoreCase);
    });

    // Include XML comments from generated documentation file
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Tag descriptions for sidebar grouping
    c.DocumentFilter<TagDescriptionsDocumentFilter>();

    // Inject custom game metadata (tips, examples, difficulty) into OpenAPI extensions
    c.OperationFilter<GameMetadataOperationFilter>();

    // Render enums as string names instead of integers
    c.SchemaFilter<EnumSchemaFilter>();
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("https://apicombat.com", "https://www.apicombat.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GameDbContext>();

// Configure Stripe
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}

var app = builder.Build();

// Configure the HTTP request pipeline

// Error handling (must be early in pipeline)
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");
app.UseMiddleware<GlobalExceptionMiddleware>();

// API routes: return JSON for error status codes (401, 403, 404, etc.)
// Runs inside StatusCodePagesMiddleware so writing a body prevents HTML re-execute
app.Use(async (context, next) =>
{
    await next();

    if (!context.Response.HasStarted
        && context.Response.StatusCode >= 400
        && context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.ContentType = "application/json";
        var error = context.Response.StatusCode switch
        {
            401 => "Unauthorized. Provide a valid Bearer token via the Authorization header.",
            403 => "Forbidden. You don't have permission to access this resource.",
            404 => "The requested endpoint was not found.",
            405 => "HTTP method not allowed for this endpoint.",
            429 => "Too many requests. Please slow down.",
            _ => $"Request failed with status {context.Response.StatusCode}."
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error }));
    }
});

app.UseHttpsRedirection();

app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
});

// API docs are rendered via Pages/ApiDocs.cshtml within the site layout

// Rate limiting middleware (only for API endpoints)
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    appBuilder => appBuilder.UseMiddleware<RateLimitingMiddleware>());

app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Player activity tracking (after auth so we have claims)
app.UseMiddleware<PlayerActivityMiddleware>();

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health");

app.MapGet("/version", () =>
{
    var asm = typeof(Program).Assembly;
    var infoVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "unknown";
    return Results.Ok(new { version = infoVersion });
}).ExcludeFromDescription();

// Apply database migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<GameDbContext>>();

    try
    {
        // Use migrations for relational DBs (SqlServer); EnsureCreated for InMemory (tests)
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();

        await SeedData.InitializeAsync(context);
        await Phase3SeedData.InitializeAsync(context);
        await AdminSeedData.InitializeAsync(context, config);

        // Seed bot players for matchmaking
        var botNameGenerator = scope.ServiceProvider.GetRequiredService<IBotNameGenerator>();
        await BotSeedData.InitializeAsync(context, botNameGenerator);

        // Seed weekly easter eggs (auto-generates if none exist for current week)
        var eggService = scope.ServiceProvider.GetRequiredService<IEasterEggService>();
        await eggService.GenerateWeeklyEggsAsync(DateTime.UtcNow);

        // Backfill: copy abilities from templates to player-owned units that are missing them
        // (fixes registration bug where .Include(u => u.Abilities) was missing)
        var unitsWithoutAbilities = await context.Units
            .Where(u => !u.IsTemplate && !u.Abilities.Any())
            .ToListAsync();

        if (unitsWithoutAbilities.Count > 0)
        {
            var templates = await context.Units
                .Include(u => u.Abilities)
                .Where(u => u.IsTemplate)
                .ToListAsync();

            foreach (var unit in unitsWithoutAbilities)
            {
                var template = templates.FirstOrDefault(t => t.Name == unit.Name && t.Class == unit.Class);
                if (template != null)
                {
                    foreach (var a in template.Abilities)
                    {
                        context.Abilities.Add(new ApiCombatGame.Models.Domain.Ability
                        {
                            Id = Guid.NewGuid(),
                            UnitId = unit.Id,
                            Name = a.Name,
                            Type = a.Type,
                            Damage = a.Damage,
                            Healing = a.Healing,
                            CooldownTurns = a.CooldownTurns,
                            Description = a.Description,
                            TargetsAllies = a.TargetsAllies,
                            IsAoE = a.IsAoE
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
            startupLogger.LogInformation("Backfilled abilities for {Count} player units", unitsWithoutAbilities.Count);
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(ex, "Database startup failed");
        throw;
    }
}

await app.RunAsync();

// Make Program accessible for integration tests
public partial class Program { }
