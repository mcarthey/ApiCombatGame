using ApiCombatGame.Data;
using ApiCombatGame.Models.DTOs.Sdk;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class SdkService : ISdkService
{
    private readonly GameDbContext _context;

    public SdkService(GameDbContext context)
    {
        _context = context;
    }

    public QuickStartResponse GetQuickStart(string baseUrl)
    {
        return new QuickStartResponse
        {
            BaseUrl = baseUrl,
            OpenApiSpecUrl = $"{baseUrl}/openapi/v1.json",
            Steps = new List<QuickStartStep>
            {
                new() { Order = 1, Action = "Register", Method = "POST", Endpoint = "/api/v1/auth/register",
                    Description = "Create your player account",
                    ExampleBody = "{ \"username\": \"YourName\", \"email\": \"you@example.com\", \"password\": \"SecurePass123!\" }" },
                new() { Order = 2, Action = "Login", Method = "POST", Endpoint = "/api/v1/auth/login",
                    Description = "Get your JWT token for authenticated requests",
                    ExampleBody = "{ \"email\": \"you@example.com\", \"password\": \"SecurePass123!\" }" },
                new() { Order = 3, Action = "View Roster", Method = "GET", Endpoint = "/api/v1/player/roster/available",
                    Description = "Browse units available for purchase" },
                new() { Order = 4, Action = "Unlock Units", Method = "POST", Endpoint = "/api/v1/player/roster/unlock",
                    Description = "Spend currency to add units to your roster",
                    ExampleBody = "{ \"templateUnitId\": \"<unit-id-from-step-3>\" }" },
                new() { Order = 5, Action = "Build Team", Method = "POST", Endpoint = "/api/v1/team/configure",
                    Description = "Assemble a squad of up to 5 units with a battle strategy",
                    ExampleBody = "{ \"name\": \"Alpha Squad\", \"unitIds\": [\"<id1>\", \"<id2>\", \"<id3>\"], \"strategyConfig\": { \"formation\": \"balanced\" } }" },
                new() { Order = 6, Action = "Queue Battle", Method = "POST", Endpoint = "/api/v1/battle/queue",
                    Description = "Enter matchmaking to find an opponent",
                    ExampleBody = "{ \"teamId\": \"<team-id-from-step-5>\" }" },
                new() { Order = 7, Action = "Check Results", Method = "GET", Endpoint = "/api/v1/battle/results/{battleId}",
                    Description = "Get the full combat log and rewards" },
                new() { Order = 8, Action = "Follow the Links", Method = "—", Endpoint = "(any response)",
                    Description = "Every response includes a _links object with related endpoints. Follow them to discover the API — no need to memorize URLs." }
            },
            Auth = new AuthGuide
            {
                LoginEndpoint = $"{baseUrl}/api/v1/auth/login",
                RefreshEndpoint = $"{baseUrl}/api/v1/auth/refresh"
            },
            Tips = new List<string>
            {
                "Every response includes a \"_links\" object — follow the links to navigate the API without memorizing URLs.",
                "After a battle, _links will show you: replay, queue_again, and the winner/loser profiles.",
                "Your profile response links to your roster, teams, achievements, and notifications.",
                "Guild responses link to members, boss encounters, treasury, and shared strategies."
            },
            ExampleSnippets = new List<SdkSnippet>
            {
                new() { Language = "curl", Label = "Register (cURL)",
                    Code = $"curl -X POST {baseUrl}/api/v1/auth/register \\\n  -H 'Content-Type: application/json' \\\n  -d '{{\"username\":\"TestPlayer\",\"email\":\"test@example.com\",\"password\":\"Pass123!\"}}'" },
                new() { Language = "python", Label = "Login (Python)",
                    Code = $"import requests\n\nresp = requests.post('{baseUrl}/api/v1/auth/login',\n    json={{'email': 'test@example.com', 'password': 'Pass123!'}})\ntoken = resp.json()['token']\nheaders = {{'Authorization': f'Bearer {{token}}'}}" },
                new() { Language = "javascript", Label = "Fetch Profile (JavaScript)",
                    Code = $"const resp = await fetch('{baseUrl}/api/v1/player/profile', {{\n  headers: {{ 'Authorization': `Bearer ${{token}}` }}\n}});\nconst profile = await resp.json();" },
                new() { Language = "csharp", Label = "Queue Battle (C#)",
                    Code = $"var client = new HttpClient();\nclient.DefaultRequestHeaders.Authorization =\n    new AuthenticationHeaderValue(\"Bearer\", token);\nvar response = await client.PostAsJsonAsync(\n    \"{baseUrl}/api/v1/battle/queue\",\n    new {{ teamId = teamId }});" }
            }
        };
    }

    public EndpointCatalogResponse GetEndpointCatalog()
    {
        var categories = new List<EndpointCategoryDto>
        {
            Cat("Auth", "Registration, login, and token management", new[]
            {
                Ep("POST", "/api/v1/auth/register", "Create a new player account", false),
                Ep("POST", "/api/v1/auth/login", "Login and receive JWT token", false),
                Ep("POST", "/api/v1/auth/refresh", "Refresh an expiring token", true)
            }),
            Cat("Player", "Profile, roster, and notifications", new[]
            {
                Ep("GET", "/api/v1/player/profile", "View your player profile", true),
                Ep("GET", "/api/v1/player/roster", "List your owned units", true),
                Ep("GET", "/api/v1/player/roster/available", "Browse purchasable units", true),
                Ep("POST", "/api/v1/player/roster/unlock", "Unlock a new unit", true),
                Ep("GET", "/api/v1/player/achievements", "View achievements", true),
                Ep("GET", "/api/v1/player/notifications", "Get notifications", true),
                Ep("GET", "/api/v1/player/notifications/digest", "Get notification digest", true)
            }),
            Cat("Team", "Squad building and strategy configuration", new[]
            {
                Ep("GET", "/api/v1/team", "List your teams", true),
                Ep("POST", "/api/v1/team/configure", "Create or update a team", true),
                Ep("DELETE", "/api/v1/team/{teamId}", "Delete a team", true)
            }),
            Cat("Battle", "Matchmaking and combat", new[]
            {
                Ep("POST", "/api/v1/battle/queue", "Queue for a battle", true),
                Ep("GET", "/api/v1/battle/status/{battleId}", "Check battle status", true),
                Ep("GET", "/api/v1/battle/results/{battleId}", "Get battle results", true),
                Ep("GET", "/api/v1/battle/history", "Battle history", true)
            }),
            Cat("Leaderboard", "Rankings and competition", new[]
            {
                Ep("GET", "/api/v1/leaderboard", "Global leaderboard", true),
                Ep("GET", "/api/v1/leaderboard/top", "Top players", true)
            }),
            Cat("Guild", "Guild management and social features", new[]
            {
                Ep("POST", "/api/v1/guild/create", "Create a guild (Premium+)", true),
                Ep("GET", "/api/v1/guild/my", "View your guild", true),
                Ep("POST", "/api/v1/guild/invite", "Invite a player", true)
            }),
            Cat("Challenges", "Daily objectives", new[]
            {
                Ep("GET", "/api/v1/challenges/daily", "Get daily challenges", true),
                Ep("POST", "/api/v1/challenges/claim", "Claim challenge reward", true),
                Ep("POST", "/api/v1/challenges/refresh", "Refresh challenges (Premium)", true)
            }),
            Cat("Education", "Learning platform", new[]
            {
                Ep("GET", "/api/v1/education/modules", "Browse curriculum modules", true),
                Ep("POST", "/api/v1/education/modules", "Create a module", true),
                Ep("POST", "/api/v1/education/enroll/{moduleId}", "Enroll in a module", true)
            })
        };

        int total = categories.Sum(c => c.Endpoints.Count);

        return new EndpointCatalogResponse
        {
            TotalEndpoints = total,
            Categories = categories
        };
    }

    public async Task<GameStatusResponse> GetGameStatusAsync()
    {
        var totalPlayers = await _context.Players.CountAsync();
        var activeSeason = await _context.Seasons.CountAsync(s => s.IsActive);
        var activeTournaments = await _context.Tournaments.CountAsync(t => t.Status == "registration" || t.Status == "in_progress");
        var totalBattles = await _context.Battles.CountAsync(b => b.Status == BattleStatus.Completed);

        return new GameStatusResponse
        {
            ServerTime = DateTime.UtcNow,
            Metrics = new GameMetrics
            {
                TotalPlayers = totalPlayers,
                ActiveSeason = activeSeason,
                ActiveTournaments = activeTournaments,
                TotalBattlesCompleted = totalBattles
            }
        };
    }

    private static EndpointCategoryDto Cat(string tag, string desc, EndpointDto[] endpoints) => new()
    {
        Tag = tag, Description = desc, Endpoints = endpoints.ToList()
    };

    private static EndpointDto Ep(string method, string path, string summary, bool auth) => new()
    {
        Method = method, Path = path, Summary = summary, RequiresAuth = auth
    };
}
