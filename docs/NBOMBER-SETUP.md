# NBomber Load Testing Setup

## Create Test Project

```bash
# From your solution root
dotnet new console -n ApiCombatGame.LoadTests
cd ApiCombatGame.LoadTests

# Add NBomber
dotnet add package NBomber
dotnet add package NBomber.Http

# Reference your main API project (to reuse models/clients)
dotnet add reference ../ApiCombatGame/ApiCombatGame.csproj

# Add to solution
cd ..
dotnet sln add ApiCombatGame.LoadTests/ApiCombatGame.LoadTests.csproj
```

## Example Load Test: Battle Queue Simulation

Create `Program.cs`:

```csharp
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

// Test configuration
var baseUrl = "http://localhost:5000";
var testUserEmail = "loadtest@test.com";
var testUserPassword = "LoadTest123!";

// Helper to create HTTP client with auth
HttpClient CreateAuthenticatedClient(string token)
{
    var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
}

// Scenario 1: User Registration
var registerScenario = Scenario.Create("user_registration", async context =>
{
    var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    
    var registerData = new
    {
        email = $"user{context.ScenarioInfo.ThreadId}_{DateTime.Now.Ticks}@test.com",
        username = $"LoadTestUser{context.ScenarioInfo.ThreadId}",
        password = "Test123!"
    };
    
    var content = new StringContent(
        JsonSerializer.Serialize(registerData),
        Encoding.UTF8,
        "application/json"
    );
    
    var response = await client.PostAsync("/api/v1/auth/register", content);
    
    return response.IsSuccessStatusCode
        ? Response.Ok(sizeBytes: (int)response.Content.Headers.ContentLength!)
        : Response.Fail($"Status: {response.StatusCode}");
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

// Scenario 2: Login and Profile Access
var loginScenario = Scenario.Create("login_and_profile", async context =>
{
    var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    
    // Step 1: Login
    var loginData = new { email = testUserEmail, password = testUserPassword };
    var loginContent = new StringContent(
        JsonSerializer.Serialize(loginData),
        Encoding.UTF8,
        "application/json"
    );
    
    var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
    if (!loginResponse.IsSuccessStatusCode)
        return Response.Fail("Login failed");
    
    var loginResult = await JsonSerializer.DeserializeAsync<JsonElement>(
        await loginResponse.Content.ReadAsStreamAsync()
    );
    var token = loginResult.GetProperty("token").GetString();
    
    // Step 2: Get Profile
    var authClient = CreateAuthenticatedClient(token!);
    var profileResponse = await authClient.GetAsync("/api/v1/player/profile");
    
    return profileResponse.IsSuccessStatusCode
        ? Response.Ok(sizeBytes: (int)profileResponse.Content.Headers.ContentLength!)
        : Response.Fail($"Profile fetch failed: {profileResponse.StatusCode}");
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(
    Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
    Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(1))
);

// Scenario 3: Battle Queue Stress Test
var battleScenario = Scenario.Create("battle_queue", async context =>
{
    var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    
    // Login first
    var loginData = new { email = testUserEmail, password = testUserPassword };
    var loginContent = new StringContent(
        JsonSerializer.Serialize(loginData),
        Encoding.UTF8,
        "application/json"
    );
    
    var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
    if (!loginResponse.IsSuccessStatusCode)
        return Response.Fail("Login failed");
    
    var loginResult = await JsonSerializer.DeserializeAsync<JsonElement>(
        await loginResponse.Content.ReadAsStreamAsync()
    );
    var token = loginResult.GetProperty("token").GetString();
    
    // Queue battle
    var authClient = CreateAuthenticatedClient(token!);
    var battleData = new { teamId = "default-team", mode = "ranked" };
    var battleContent = new StringContent(
        JsonSerializer.Serialize(battleData),
        Encoding.UTF8,
        "application/json"
    );
    
    var battleResponse = await authClient.PostAsync("/api/v1/battle/queue", battleContent);
    
    return battleResponse.IsSuccessStatusCode
        ? Response.Ok(sizeBytes: (int)battleResponse.Content.Headers.ContentLength!)
        : Response.Fail($"Battle queue failed: {battleResponse.StatusCode}");
})
.WithWarmUpDuration(TimeSpan.FromSeconds(10))
.WithLoadSimulations(
    Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(3))
);

// Scenario 4: Read-Heavy Leaderboard Load
var leaderboardScenario = Scenario.Create("leaderboard_reads", async context =>
{
    var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    var response = await client.GetAsync("/api/v1/leaderboard?limit=100");
    
    return response.IsSuccessStatusCode
        ? Response.Ok(sizeBytes: (int)response.Content.Headers.ContentLength!)
        : Response.Fail($"Status: {response.StatusCode}");
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: 200, during: TimeSpan.FromMinutes(2))
);

// Run all scenarios
NBomberRunner
    .RegisterScenarios(
        registerScenario,
        loginScenario,
        battleScenario,
        leaderboardScenario
    )
    .WithReportFolder("load-test-results")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md)
    .WithReportFileName("battle_load_test")
    .Run();
```

## Run Load Test

```bash
# Start your API first
cd ApiCombatGame
dotnet run

# In another terminal, run load test
cd ApiCombatGame.LoadTests
dotnet run
```

## Understanding Results

NBomber generates a detailed HTML report in `load-test-results/` showing:

- **RPS** (Requests per second)
- **Latency percentiles** (p50, p75, p95, p99)
- **Error rate**
- **Data transfer** (MB/sec)
- **Timeline graphs** (visual representation of load)

### Good Performance Benchmarks for POC:

- **p95 latency < 200ms** for reads
- **p95 latency < 500ms** for writes
- **Error rate < 1%** under load
- **Sustained 100+ RPS** on basic hardware

## Advanced: Data-Driven Load Test

Create `BattleLoadTest.cs` for more complex scenarios:

```csharp
using NBomber.CSharp;
using NBomber.Contracts;

public class BattleLoadTest
{
    public static void Run()
    {
        // Feed data: different team configurations
        var teamConfigs = new[]
        {
            new { TeamId = "aggressive", Strategy = "rush" },
            new { TeamId = "defensive", Strategy = "tank" },
            new { TeamId = "balanced", Strategy = "standard" }
        };
        
        var feed = Feed.CreateCircular("team_configs", teamConfigs);
        
        var scenario = Scenario.Create("varied_teams", async context =>
        {
            var config = feed.GetNextItem(context.ScenarioInfo);
            
            // Use different team config for each virtual user
            var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
            
            // ... queue battle with specific team config
            
            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 50, during: TimeSpan.FromMinutes(2))
        );
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}
```

## Comparing with JMeter

### JMeter Way (XML Pain):
```xml
<TestPlan>
  <ThreadGroup>
    <HTTPSamplerProxy>
      <stringProp name="HTTPSampler.domain">localhost</stringProp>
      <stringProp name="HTTPSampler.port">5000</stringProp>
      <stringProp name="HTTPSampler.path">/api/v1/auth/login</stringProp>
      <!-- ...100 more lines of XML... -->
    </HTTPSamplerProxy>
  </ThreadGroup>
</TestPlan>
```

### NBomber Way (C# Bliss):
```csharp
var response = await client.PostAsync("/api/v1/auth/login", content);
```

**NBomber wins** because:
- ✅ Write in C# (not XML or Groovy)
- ✅ Reuse your API client code
- ✅ IntelliSense and type safety
- ✅ Debug with breakpoints
- ✅ Run in CI/CD easily (`dotnet test`)
- ✅ Better reports (HTML with charts)

## CI/CD Integration

Add to GitHub Actions:

```yaml
name: Load Test

on:
  pull_request:
    branches: [main]

jobs:
  load-test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      
      - name: Start API
        run: |
          cd ApiCombatGame
          dotnet run &
          sleep 10  # Wait for API to start
      
      - name: Run Load Test
        run: |
          cd ApiCombatGame.LoadTests
          dotnet run
      
      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: load-test-results
          path: ApiCombatGame.LoadTests/load-test-results/
```

## Performance Monitoring

Combine NBomber with:

**Prometheus + Grafana** (metrics):
```csharp
// Add to your API
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());
```

**Serilog** (structured logging):
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

Then during load tests, watch:
- API logs for errors
- Prometheus metrics for CPU/memory
- NBomber reports for client-side latency

## Recommended Load Test Progression

**Phase 1: Smoke Test** (validate API works)
- 10 users, 1 minute
- Catch obvious bugs

**Phase 2: Baseline Test** (establish normal performance)
- 50 users, 5 minutes
- Record metrics as baseline

**Phase 3: Stress Test** (find breaking point)
- Ramp 0 → 500 users over 10 minutes
- Find where errors start

**Phase 4: Soak Test** (memory leaks, stability)
- 100 users, 1 hour
- Watch for degradation over time

**Phase 5: Spike Test** (sudden traffic burst)
- 10 users → 500 users instantly
- Back to 10 users
- Tests autoscaling, crash recovery

---

**Bottom Line**: NBomber is perfect for C# teams. Way better than JMeter for your use case. Your team at work will thank you for introducing it.
