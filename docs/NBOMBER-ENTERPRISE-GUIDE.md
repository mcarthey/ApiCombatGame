# NBomber: Enterprise Performance Testing Guide for .NET Teams

**Prepared for:** Performance Testing Team  
**Author:** Mark  
**Date:** February 10, 2026  
**Purpose:** Tool evaluation and adoption guide for development leads

---

## Executive Summary

**NBomber** is a modern, open-source load testing framework written in C# for .NET applications. It provides an alternative to traditional tools like JMeter and LoadRunner, offering native .NET integration, superior developer experience, and seamless CI/CD integration.

**Key Benefits for .NET Teams:**
- ✅ Write load tests in C# (not XML, Groovy, or GUI)
- ✅ Reuse existing application code (models, clients, utilities)
- ✅ Debug with Visual Studio breakpoints
- ✅ Type safety and IntelliSense
- ✅ Version control friendly (code, not binary files)
- ✅ Easy CI/CD integration (runs as console app)
- ✅ Beautiful HTML reports with charts and percentiles
- ✅ Free and open source

**Recommended Use Cases:**
- API load testing (REST, gRPC, GraphQL)
- Microservices performance validation
- Database connection pool testing
- Message queue throughput testing
- WebSocket/SignalR stress testing
- Regression testing for performance SLAs

---

## Table of Contents

1. [Understanding NBomber Architecture](#1-understanding-nbomber-architecture)
2. [Installation and Setup](#2-installation-and-setup)
3. [Core Concepts](#3-core-concepts)
4. [Basic Load Test Example](#4-basic-load-test-example)
5. [Advanced Scenarios](#5-advanced-scenarios)
6. [Load Simulation Strategies](#6-load-simulation-strategies)
7. [Metrics and Reporting](#7-metrics-and-reporting)
8. [Integration with CI/CD](#8-integration-with-cicd)
9. [Comparison: NBomber vs JMeter](#9-comparison-nbomber-vs-jmeter)
10. [Best Practices](#10-best-practices)
11. [Real-World Enterprise Patterns](#11-real-world-enterprise-patterns)
12. [Troubleshooting and Performance Tuning](#12-troubleshooting-and-performance-tuning)
13. [Appendix: Code Templates](#13-appendix-code-templates)

---

## 1. Understanding NBomber Architecture

### How NBomber Works

```
┌─────────────────────────────────────────────────────────────┐
│  NBomber Test Runner                                        │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │  Scenario 1  │  │  Scenario 2  │  │  Scenario 3  │     │
│  │  (Login)     │  │  (Search)    │  │  (Checkout)  │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                  │                  │             │
│         └──────────────────┴──────────────────┘             │
│                            │                                │
│                    ┌───────▼────────┐                       │
│                    │  Load Simulator │                       │
│                    │  - Inject       │                       │
│                    │  - RampingInject│                       │
│                    │  - KeepConstant │                       │
│                    └───────┬────────┘                       │
│                            │                                │
│                    ┌───────▼────────┐                       │
│                    │ Virtual Users  │                       │
│                    │ (Concurrent    │                       │
│                    │  Executions)   │                       │
│                    └───────┬────────┘                       │
│                            │                                │
└────────────────────────────┼────────────────────────────────┘
                             │
                    ┌────────▼─────────┐
                    │  Target System   │
                    │  (Your API)      │
                    └──────────────────┘
```

### Key Components

**1. Scenario**
- A user workflow or test case
- Contains the actual code that executes against your system
- Can have multiple steps (login → browse → purchase)
- Returns `Response.Ok()` or `Response.Fail()`

**2. Load Simulation**
- Defines how virtual users are generated
- Controls ramp-up, steady state, and ramp-down
- Multiple simulation types available

**3. Feed**
- Data source for parameterized tests
- Can be static arrays, CSV files, or generated data
- Distributes data across virtual users

**4. Step**
- Individual action within a scenario
- Can be measured independently
- Used for complex multi-step workflows

**5. Report**
- Auto-generated after test run
- HTML, Markdown, CSV formats
- Contains detailed metrics and charts

---

## 2. Installation and Setup

### Prerequisites

- .NET 6.0 or higher SDK
- Visual Studio 2022, Rider, or VS Code
- Basic C# knowledge
- Target system to test

### Creating a Load Test Project

```bash
# Create new console application
dotnet new console -n MyApp.LoadTests
cd MyApp.LoadTests

# Add NBomber packages
dotnet add package NBomber
dotnet add package NBomber.Http  # For HTTP testing

# Optional: Add for additional protocols
dotnet add package NBomber.Redis
dotnet add package NBomber.Kafka

# If testing your own app, reference it
dotnet add reference ../MyApp/MyApp.csproj
```

### Project Structure

```
MyApp.LoadTests/
├── Program.cs                      # Main entry point
├── Scenarios/
│   ├── LoginScenario.cs
│   ├── SearchScenario.cs
│   └── CheckoutScenario.cs
├── Config/
│   ├── TestConfig.json            # Environment configs
│   └── LoadProfiles.json          # Load patterns
├── Utils/
│   ├── TestData.cs                # Test data generators
│   └── Helpers.cs                 # Shared utilities
└── Reports/                       # Generated reports (gitignored)
```

### Basic Program.cs Template

```csharp
using NBomber.CSharp;
using NBomber.Contracts;

// Configure logging (optional but recommended)
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

// Your scenarios here
var scenario = Scenario.Create("simple_test", async context =>
{
    // Test logic
    return Response.Ok();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
);

// Run the test
NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder("Reports")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
    .Run();
```

---

## 3. Core Concepts

### 3.1 Response Types

NBomber scenarios must return a `Response` object:

```csharp
// Success response
return Response.Ok();

// Success with payload size (for bandwidth metrics)
return Response.Ok(sizeBytes: response.Content.Length);

// Success with custom status code
return Response.Ok(statusCode: "200_OK");

// Failure response
return Response.Fail();

// Failure with error message
return Response.Fail(error: "Connection timeout");

// Failure with custom status code
return Response.Fail(statusCode: "500_SERVER_ERROR");
```

**Why payload size matters:**
- Tracks data transfer (MB/sec)
- Important for bandwidth-constrained scenarios
- Helps identify bloated responses

### 3.2 Scenario Context

Every scenario execution receives a `IScenarioContext`:

```csharp
var scenario = Scenario.Create("example", async context =>
{
    // Access scenario information
    var scenarioName = context.ScenarioInfo.ScenarioName;
    var threadId = context.ScenarioInfo.ThreadId;
    var instanceNumber = context.ScenarioInfo.InstanceNumber;
    
    // Access test duration
    var testDuration = context.TestInfo.TestDuration;
    var sessionId = context.TestInfo.SessionId;
    
    // Custom logger
    context.Logger.Information("Executing test iteration");
    
    // Get data from feed
    var testData = context.GetFeedItem<TestData>("my_feed");
    
    return Response.Ok();
});
```

### 3.3 Steps (Multi-Step Workflows)

For complex scenarios with multiple operations:

```csharp
var scenario = Scenario.Create("e_commerce_flow", async context =>
{
    var client = new HttpClient();
    
    // Step 1: Browse products
    var browseStep = await Step.Run("browse_products", context, async () =>
    {
        var response = await client.GetAsync("https://api.example.com/products");
        return response.IsSuccessStatusCode
            ? Response.Ok()
            : Response.Fail();
    });
    
    // Step 2: Add to cart (only if browse succeeded)
    if (browseStep.IsSuccess)
    {
        var addToCartStep = await Step.Run("add_to_cart", context, async () =>
        {
            var response = await client.PostAsync("https://api.example.com/cart", content);
            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        });
        
        // Step 3: Checkout
        if (addToCartStep.IsSuccess)
        {
            return await Step.Run("checkout", context, async () =>
            {
                var response = await client.PostAsync("https://api.example.com/checkout", content);
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            });
        }
    }
    
    return Response.Fail("Workflow incomplete");
});
```

**Benefits of Steps:**
- Each step reported separately in metrics
- Conditional workflow logic
- Easier debugging (know which step failed)
- Better performance analysis (bottleneck identification)

### 3.4 Feeds (Data Parameterization)

Feeds provide test data to scenarios:

```csharp
// Simple array feed
var usersFeed = Feed.CreateCircular(
    "users",
    new[] {
        new { Username = "user1", Password = "pass1" },
        new { Username = "user2", Password = "pass2" },
        new { Username = "user3", Password = "pass3" }
    }
);

var scenario = Scenario.Create("login_with_users", async context =>
{
    var user = usersFeed.GetNextItem(context.ScenarioInfo);
    
    var loginData = new { user.Username, user.Password };
    var response = await client.PostAsJsonAsync("/login", loginData);
    
    return response.IsSuccessStatusCode
        ? Response.Ok()
        : Response.Fail();
})
.WithInit(ctx => Task.FromResult(usersFeed));

// Feed types:
// - Feed.CreateCircular: Loops through data repeatedly
// - Feed.CreateRandom: Random selection
// - Feed.CreateConstant: Same data for all users
```

**CSV Feed Example:**

```csharp
// Load from CSV file
var csvFeed = Feed.CreateCircular(
    "test_data",
    File.ReadAllLines("testdata.csv")
        .Skip(1) // Skip header
        .Select(line => line.Split(','))
        .Select(parts => new { 
            UserId = parts[0], 
            Email = parts[1] 
        })
        .ToArray()
);
```

---

## 4. Basic Load Test Example

### Scenario: Testing a REST API Login Endpoint

**Target System:**
- Endpoint: `POST /api/auth/login`
- Expected: 200 OK with JWT token
- SLA: p95 latency < 200ms, error rate < 1%

**Load Test Implementation:**

```csharp
using NBomber.CSharp;
using NBomber.Contracts;
using System.Net.Http.Json;
using System.Text.Json;

public class LoginLoadTest
{
    public static void Main(string[] args)
    {
        var httpFactory = ClientFactory.Create(
            name: "http_factory",
            clientCount: 50, // Connection pool size
            initClient: (number, context) =>
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://api.example.com");
                client.DefaultRequestHeaders.Add("User-Agent", "NBomber-LoadTest");
                return Task.FromResult(client);
            }
        );
        
        var scenario = Scenario.Create("login_load_test", async context =>
        {
            var client = httpFactory.GetClient(context.ScenarioInfo);
            
            var loginRequest = new
            {
                email = "test@example.com",
                password = "TestPassword123!"
            };
            
            try
            {
                var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
                var responseSize = (int)(response.Content.Headers.ContentLength ?? 0);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var token = result.GetProperty("token").GetString();
                    
                    // Validate token exists
                    if (!string.IsNullOrEmpty(token))
                    {
                        return Response.Ok(sizeBytes: responseSize, statusCode: "200_LOGIN_SUCCESS");
                    }
                    else
                    {
                        return Response.Fail(error: "Token missing", statusCode: "200_NO_TOKEN");
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return Response.Fail(
                        error: $"HTTP {response.StatusCode}: {errorBody}",
                        statusCode: response.StatusCode.ToString()
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                return Response.Fail(error: $"Network error: {ex.Message}", statusCode: "NETWORK_ERROR");
            }
            catch (Exception ex)
            {
                return Response.Fail(error: $"Unexpected error: {ex.Message}", statusCode: "EXCEPTION");
            }
        })
        .WithoutWarmUp() // No warm-up period
        .WithLoadSimulations(
            // Ramp up from 0 to 100 users over 2 minutes
            Simulation.RampingInject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(2)
            ),
            // Maintain 100 users for 5 minutes
            Simulation.Inject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(5)
            ),
            // Ramp down from 100 to 0 over 1 minute
            Simulation.RampingInject(
                rate: 0,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)
            )
        );
        
        NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(
                new PingPlugin(new PingPluginConfig
                {
                    Hosts = new[] { "api.example.com" }
                })
            )
            .WithReportFolder("Reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md)
            .WithReportFileName($"login_loadtest_{DateTime.Now:yyyyMMdd_HHmmss}")
            .Run();
    }
}
```

**Key Points in This Example:**

1. **Connection Pooling** (`ClientFactory`): Reuses HTTP connections
2. **Error Handling**: Catches network errors, HTTP errors, and exceptions
3. **Response Validation**: Checks token exists, not just HTTP 200
4. **Payload Size Tracking**: Monitors bandwidth usage
5. **Custom Status Codes**: Differentiates success types and failure reasons
6. **Load Profile**: Realistic ramp-up/steady-state/ramp-down
7. **Ping Plugin**: Monitors network latency separately

---

## 5. Advanced Scenarios

### 5.1 Multi-Step User Journey

Simulating a complete user workflow:

```csharp
var ecommerceScenario = Scenario.Create("ecommerce_journey", async context =>
{
    var client = httpFactory.GetClient(context.ScenarioInfo);
    string authToken = null;
    
    // Step 1: Login
    var loginStep = await Step.Run("login", context, async () =>
    {
        var loginData = new { email = "user@test.com", password = "pass" };
        var response = await client.PostAsJsonAsync("/auth/login", loginData);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            authToken = result.GetProperty("token").GetString();
            return Response.Ok();
        }
        return Response.Fail();
    });
    
    if (!loginStep.IsSuccess) return loginStep;
    
    // Step 2: Browse products
    client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
    
    var browseStep = await Step.Run("browse_products", context, async () =>
    {
        var response = await client.GetAsync("/products?category=electronics");
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    });
    
    if (!browseStep.IsSuccess) return browseStep;
    
    // Step 3: Add to cart
    var addToCartStep = await Step.Run("add_to_cart", context, async () =>
    {
        var cartData = new { productId = 123, quantity = 1 };
        var response = await client.PostAsJsonAsync("/cart/add", cartData);
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    });
    
    if (!addToCartStep.IsSuccess) return addToCartStep;
    
    // Step 4: Checkout
    var checkoutStep = await Step.Run("checkout", context, async () =>
    {
        var checkoutData = new { paymentMethod = "credit_card" };
        var response = await client.PostAsJsonAsync("/checkout", checkoutData);
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    });
    
    return checkoutStep;
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(10))
);
```

**Reporting Benefits:**

Each step appears separately in reports:
- `login` - p50, p75, p95, p99 latencies
- `browse_products` - success rate, throughput
- `add_to_cart` - error distribution
- `checkout` - payload sizes

You can identify which step is the bottleneck.

### 5.2 Data-Driven Testing with Dynamic Feeds

```csharp
public class TestDataGenerator
{
    public static IEnumerable<UserCredentials> GenerateUsers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new UserCredentials
            {
                Email = $"user{i}@loadtest.com",
                Password = $"Password{i}!",
                UserId = Guid.NewGuid()
            };
        }
    }
}

var usersFeed = Feed.CreateCircular(
    "users",
    TestDataGenerator.GenerateUsers(1000).ToArray()
);

var scenario = Scenario.Create("unique_user_login", async context =>
{
    var user = usersFeed.GetNextItem(context.ScenarioInfo);
    
    var client = httpFactory.GetClient(context.ScenarioInfo);
    var response = await client.PostAsJsonAsync("/auth/login", new
    {
        user.Email,
        user.Password
    });
    
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithInit(ctx => Task.FromResult(usersFeed))
.WithLoadSimulations(
    Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
);
```

### 5.3 Think Time (User Pacing)

Simulating real user behavior with pauses:

```csharp
var scenario = Scenario.Create("realistic_browsing", async context =>
{
    var client = httpFactory.GetClient(context.ScenarioInfo);
    
    // Browse homepage
    await client.GetAsync("/");
    await Task.Delay(TimeSpan.FromSeconds(2)); // User reads page
    
    // Click category
    await client.GetAsync("/category/laptops");
    await Task.Delay(TimeSpan.FromSeconds(3)); // User browses
    
    // View product
    await client.GetAsync("/product/12345");
    await Task.Delay(TimeSpan.FromSeconds(5)); // User reads specs
    
    // Add to cart
    var response = await client.PostAsync("/cart/add", content);
    
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: 100, during: TimeSpan.FromMinutes(10))
);
```

**Think time patterns:**
- Fixed delay: `Task.Delay(TimeSpan.FromSeconds(3))`
- Random delay: `Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(1000, 5000)))`
- Normal distribution: Use `MathNet.Numerics` for realistic patterns

---

## 6. Load Simulation Strategies

NBomber offers multiple load simulation patterns. Choose based on your testing goals.

### 6.1 Inject (Constant Rate)

**Use Case:** Steady-state testing, throughput validation

```csharp
Simulation.Inject(
    rate: 100,                          // 100 requests
    interval: TimeSpan.FromSeconds(1),  // Every second
    during: TimeSpan.FromMinutes(5)     // For 5 minutes
)
// Result: 100 RPS for 5 minutes = 30,000 total requests
```

**When to use:**
- Testing sustained throughput
- Validating SLAs under constant load
- Baseline performance measurement

### 6.2 InjectPerSec (Simplified Constant Rate)

```csharp
Simulation.InjectPerSec(
    rate: 100,                         // 100 requests per second
    during: TimeSpan.FromMinutes(5)    // For 5 minutes
)
```

Same as `Inject` but clearer syntax.

### 6.3 RampingInject (Gradual Increase)

**Use Case:** Finding breaking point, gradual load increase

```csharp
Simulation.RampingInject(
    rate: 500,                          // Target rate
    interval: TimeSpan.FromSeconds(1),
    during: TimeSpan.FromMinutes(10)    // Ramp over 10 minutes
)
// Result: Gradually increases from 0 to 500 RPS over 10 minutes
```

**When to use:**
- Stress testing (find max capacity)
- Avoiding sudden spikes during ramp-up
- Gradual warmup before peak load

### 6.4 KeepConstant (Concurrent Users)

**Use Case:** Simulating fixed number of active users

```csharp
Simulation.KeepConstant(
    copies: 100,                       // 100 concurrent users
    during: TimeSpan.FromMinutes(5)    // For 5 minutes
)
// Result: Maintains exactly 100 concurrent executions
```

**Difference from Inject:**
- `Inject`: Rate-based (RPS)
- `KeepConstant`: User-based (concurrent threads)

**When to use:**
- Testing connection pool limits
- Database connection testing
- Simulating real concurrent users

### 6.5 RampingConstant (Gradual User Increase)

```csharp
Simulation.RampingConstant(
    copies: 200,                       // Target concurrent users
    during: TimeSpan.FromMinutes(5)    // Ramp over 5 minutes
)
// Result: Gradually increases from 0 to 200 concurrent users
```

### 6.6 Pause (Gap Between Simulations)

```csharp
Simulation.Pause(TimeSpan.FromSeconds(30))
```

Creates a 30-second gap in the load profile.

**When to use:**
- Letting system recover between test phases
- Simulating batch job intervals
- Multi-phase testing with rest periods

### 6.7 Combined Simulations

Real-world load often requires multiple phases:

```csharp
.WithLoadSimulations(
    // Phase 1: Warm-up
    Simulation.RampingInject(
        rate: 50,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromMinutes(2)
    ),
    
    // Phase 2: Normal load
    Simulation.Inject(
        rate: 100,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromMinutes(10)
    ),
    
    // Phase 3: Peak load (lunch hour simulation)
    Simulation.Inject(
        rate: 300,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromMinutes(5)
    ),
    
    // Phase 4: Return to normal
    Simulation.Inject(
        rate: 100,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromMinutes(5)
    ),
    
    // Phase 5: Cool down
    Simulation.RampingInject(
        rate: 0,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromMinutes(2)
    )
);
```

### Load Profile Visual Guide

```
RPS
│
300 │           ╭─────╮
    │          ╱       ╲
200 │         ╱         ╲
    │        ╱           ╲
100 │   ╭───╯             ╰────╮
    │  ╱                       ╲
  0 │ ╱                         ╰──
    └─────────────────────────────────> Time
      W  Normal  Peak  Normal  Cool
      a                        down
      r
      m
      u
      p
```

---

## 7. Metrics and Reporting

### 7.1 Understanding NBomber Reports

After a test run, NBomber generates comprehensive reports. Here's how to interpret them.

**HTML Report Sections:**

1. **Test Overview**
   - Test duration
   - Total requests
   - Scenarios executed

2. **Scenario Statistics (per scenario)**
   - Request count
   - OK / Failed count
   - RPS (Requests per second)
   - Data transfer (MB/sec)

3. **Latency Percentiles (per scenario)**
   - Min, Mean, Max
   - p50 (median)
   - p75, p95, p99
   - StdDev (standard deviation)

4. **Status Code Distribution**
   - Success codes (200, 201, etc.)
   - Error codes (400, 500, etc.)
   - Custom status codes

5. **Load Simulation Timeline**
   - Visual graph of load over time
   - RPS chart
   - Latency over time

**Example Report Output:**

```
Scenario: login_load_test
┌─────────────────────────────────────────────────────────────┐
│ Duration        │ 00:08:00                                   │
│ Total Requests  │ 48,000                                     │
│ OK              │ 47,520 (99.0%)                             │
│ Failed          │ 480 (1.0%)                                 │
│ RPS             │ 100                                        │
│ Data Transfer   │ 2.5 MB/sec                                 │
└─────────────────────────────────────────────────────────────┘

Latency (ms):
┌──────┬──────┬──────┬──────┬──────┬──────┬──────┬──────┐
│ Min  │ Mean │ Max  │ p50  │ p75  │ p95  │ p99  │ StdDev│
├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤
│ 12   │ 145  │ 3200 │ 132  │ 178  │ 256  │ 450  │ 98   │
└──────┴──────┴──────┴──────┴──────┴──────┴──────┴──────┘

Status Codes:
┌──────────────────┬────────┬────────┐
│ Status           │ Count  │ %      │
├──────────────────┼────────┼────────┤
│ 200_OK           │ 47,520 │ 99.0%  │
│ 500_SERVER_ERROR │ 320    │ 0.67%  │
│ TIMEOUT          │ 160    │ 0.33%  │
└──────────────────┴────────┴────────┘
```

### 7.2 Interpreting Metrics

**Latency Percentiles - What They Mean:**

- **p50 (Median)**: 50% of requests faster than this
  - Most "typical" user experience
  - Not affected by outliers

- **p75**: 75% of requests faster than this
  - Slightly above average experience

- **p95**: 95% of requests faster than this
  - **Most important for SLAs**
  - Represents "worst normal case"
  - 1 in 20 users experience this or worse

- **p99**: 99% of requests faster than this
  - Extreme cases
  - Important for tail latency analysis

**Example Interpretation:**

```
p50 = 100ms, p95 = 250ms, p99 = 800ms
```

**Analysis:**
- Most users (50%) see ~100ms response
- 95% of users see ≤250ms (acceptable for SLA)
- 5% of users see 250-800ms (investigate why)
- 1% of users see ≥800ms (potential issue)

**StdDev (Standard Deviation):**
- Measures variability/consistency
- Low StdDev = consistent performance
- High StdDev = unpredictable latency

```
Scenario A: Mean=100ms, StdDev=10ms  → Very consistent
Scenario B: Mean=100ms, StdDev=200ms → Highly variable
```

### 7.3 Success Criteria Examples

Define acceptable performance thresholds:

```csharp
var scenario = Scenario.Create("api_test", async context =>
{
    // ... test logic
})
.WithLoadSimulations(/* ... */)
.WithMaxFailCount(100); // Fail test if >100 failures

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportingInterval(TimeSpan.FromSeconds(10))
    .Run();

// Post-test validation
var stats = NBomberRunner
    .RegisterScenarios(scenario)
    .Run();

var scenarioStats = stats.ScenarioStats[0];

// Validate SLAs
if (scenarioStats.Ok.Latency.Percent95 > 200)
{
    Console.WriteLine("FAIL: p95 latency exceeded 200ms");
    Environment.Exit(1);
}

if (scenarioStats.Fail.Request.Count > scenarioStats.Ok.Request.Count * 0.01)
{
    Console.WriteLine("FAIL: Error rate exceeded 1%");
    Environment.Exit(1);
}

Console.WriteLine("PASS: All SLAs met");
```

### 7.4 Custom Metrics with Plugins

NBomber supports custom metrics via plugins:

```csharp
using NBomber.Contracts;
using NBomber.Contracts.Stats;

public class CustomMetricsPlugin : IWorkerPlugin
{
    private int _customCounter;
    
    public string PluginName => "CustomMetrics";
    
    public IHints Hints => new Hints();
    
    public Task Init(IBaseContext context, ILogger logger)
    {
        // Initialization
        return Task.CompletedTask;
    }
    
    public Task Start()
    {
        // Called when test starts
        return Task.CompletedTask;
    }
    
    public Task<DataSet> GetStats(TimeSpan currentDuration)
    {
        // Return custom metrics
        return Task.FromResult(new DataSet
        {
            TableName = "CustomMetrics",
            Columns = new[] { "Metric", "Value" },
            Rows = new object[][]
            {
                new object[] { "CustomCounter", _customCounter }
            }
        });
    }
    
    public Task<IHints> GetHints()
    {
        return Task.FromResult((IHints)new Hints());
    }
    
    public Task Stop() => Task.CompletedTask;
    public void Dispose() { }
}

// Usage
NBomberRunner
    .RegisterScenarios(scenario)
    .WithWorkerPlugins(new CustomMetricsPlugin())
    .Run();
```

---

## 8. Integration with CI/CD

### 8.1 Running in Azure DevOps Pipeline

**azure-pipelines.yml:**

```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  targetUrl: 'https://api.staging.example.com'

stages:
- stage: Build
  jobs:
  - job: BuildAndTest
    steps:
    - task: UseDotNet@2
      inputs:
        version: '8.x'
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: '**/*.csproj'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build solution'
      inputs:
        command: 'build'
        projects: '**/*.csproj'
        arguments: '--configuration $(buildConfiguration)'

- stage: PerformanceTest
  dependsOn: Build
  jobs:
  - job: LoadTest
    steps:
    - task: DotNetCoreCLI@2
      displayName: 'Run load tests'
      inputs:
        command: 'run'
        projects: '**/MyApp.LoadTests.csproj'
        arguments: '--configuration $(buildConfiguration)'
      env:
        TARGET_URL: $(targetUrl)
        LOAD_DURATION_MINUTES: '5'
        TARGET_RPS: '100'
    
    - task: PublishBuildArtifacts@1
      displayName: 'Publish load test reports'
      inputs:
        pathToPublish: '$(Build.SourcesDirectory)/MyApp.LoadTests/Reports'
        artifactName: 'LoadTestReports'
      condition: always()
    
    - task: PublishTestResults@2
      displayName: 'Publish test results'
      inputs:
        testResultsFormat: 'NUnit'
        testResultsFiles: '**/test-results.xml'
      condition: always()
```

### 8.2 GitHub Actions Workflow

**.github/workflows/load-test.yml:**

```yaml
name: Load Test

on:
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 2 * * *'  # Run nightly at 2 AM

jobs:
  load-test:
    runs-on: ubuntu-latest
    
    env:
      TARGET_URL: https://api.staging.example.com
      DOTNET_VERSION: '8.0.x'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run load tests
      run: |
        cd MyApp.LoadTests
        dotnet run --configuration Release
      timeout-minutes: 30
    
    - name: Upload test reports
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: load-test-reports
        path: MyApp.LoadTests/Reports/
    
    - name: Comment PR with results
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          const reportPath = 'MyApp.LoadTests/Reports/report.md';
          const report = fs.readFileSync(reportPath, 'utf8');
          
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: `## Load Test Results\n\n${report}`
          });
```

### 8.3 Parameterized Tests for CI/CD

Make tests configurable via environment variables:

```csharp
public class TestConfig
{
    public string TargetUrl { get; set; }
    public int DurationMinutes { get; set; }
    public int TargetRps { get; set; }
    public int MaxFailCount { get; set; }
    
    public static TestConfig FromEnvironment()
    {
        return new TestConfig
        {
            TargetUrl = Environment.GetEnvironmentVariable("TARGET_URL") 
                ?? "https://localhost:5001",
            DurationMinutes = int.Parse(
                Environment.GetEnvironmentVariable("LOAD_DURATION_MINUTES") ?? "5"
            ),
            TargetRps = int.Parse(
                Environment.GetEnvironmentVariable("TARGET_RPS") ?? "50"
            ),
            MaxFailCount = int.Parse(
                Environment.GetEnvironmentVariable("MAX_FAIL_COUNT") ?? "100"
            )
        };
    }
}

// Usage in Program.cs
var config = TestConfig.FromEnvironment();

var scenario = Scenario.Create("configurable_test", async context =>
{
    var client = new HttpClient { BaseAddress = new Uri(config.TargetUrl) };
    var response = await client.GetAsync("/api/health");
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.InjectPerSec(
        rate: config.TargetRps,
        during: TimeSpan.FromMinutes(config.DurationMinutes)
    )
)
.WithMaxFailCount(config.MaxFailCount);
```

**Running from CI/CD:**

```bash
export TARGET_URL="https://api.production.example.com"
export LOAD_DURATION_MINUTES="10"
export TARGET_RPS="200"
dotnet run --project MyApp.LoadTests
```

### 8.4 Performance Regression Detection

Automatically compare against baseline:

```csharp
public class PerformanceValidator
{
    public static bool ValidateAgainstBaseline(NodeStats stats, BaselineMetrics baseline)
    {
        var current = stats.ScenarioStats[0];
        
        var checks = new List<(string Name, bool Passed)>
        {
            ("p95 within threshold", 
                current.Ok.Latency.Percent95 <= baseline.P95Latency * 1.1), // 10% tolerance
            
            ("Error rate acceptable", 
                current.Fail.Request.Percent <= baseline.MaxErrorRate),
            
            ("Throughput maintained", 
                current.Ok.Request.RPS >= baseline.MinRps * 0.9) // 90% of baseline
        };
        
        foreach (var (name, passed) in checks)
        {
            Console.WriteLine($"{(passed ? "✓" : "✗")} {name}");
        }
        
        return checks.All(c => c.Passed);
    }
}

public class BaselineMetrics
{
    public double P95Latency { get; set; } = 200; // ms
    public double MaxErrorRate { get; set; } = 1.0; // percent
    public double MinRps { get; set; } = 100;
}

// Usage
var stats = NBomberRunner.RegisterScenarios(scenario).Run();
var baseline = new BaselineMetrics();

if (!PerformanceValidator.ValidateAgainstBaseline(stats, baseline))
{
    Console.WriteLine("Performance regression detected!");
    Environment.Exit(1);
}
```

---

## 9. Comparison: NBomber vs JMeter

### Feature Comparison Matrix

| Feature | NBomber | JMeter | Winner |
|---------|---------|--------|--------|
| **Language** | C# | Java/Groovy | NBomber (for .NET teams) |
| **Test Definition** | Code | GUI/XML | NBomber |
| **IDE Support** | Full (VS, Rider) | Limited | NBomber |
| **Debugging** | Breakpoints | Limited | NBomber |
| **Version Control** | Git-friendly | Binary files | NBomber |
| **CI/CD Integration** | Native | Complex | NBomber |
| **Learning Curve** | Low (if know C#) | Medium | NBomber |
| **Protocols** | HTTP, gRPC, WebSocket, Redis, Kafka | Many (JDBC, FTP, LDAP, etc.) | JMeter |
| **Reporting** | HTML, CSV, Markdown | HTML, XML | Tie |
| **Community** | Growing | Mature | JMeter |
| **Cost** | Free | Free | Tie |
| **Distributed Testing** | Yes (clustering) | Yes | Tie |
| **Resource Usage** | Lower | Higher | NBomber |

### Code Comparison Example

**JMeter (XML):**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<jmeterTestPlan version="1.2">
  <hashTree>
    <TestPlan guiclass="TestPlanGui" testclass="TestPlan" testname="Login Test">
      <elementProp name="TestPlan.user_defined_variables" elementType="Arguments">
        <collectionProp name="Arguments.arguments"/>
      </elementProp>
    </TestPlan>
    <hashTree>
      <ThreadGroup guiclass="ThreadGroupGui" testclass="ThreadGroup" testname="Users">
        <intProp name="ThreadGroup.num_threads">100</intProp>
        <intProp name="ThreadGroup.ramp_time">60</intProp>
        <boolProp name="ThreadGroup.scheduler">true</boolProp>
        <stringProp name="ThreadGroup.duration">300</stringProp>
      </ThreadGroup>
      <hashTree>
        <HTTPSamplerProxy guiclass="HttpTestSampleGui" testclass="HTTPSamplerProxy" testname="Login Request">
          <stringProp name="HTTPSampler.domain">api.example.com</stringProp>
          <stringProp name="HTTPSampler.port">443</stringProp>
          <stringProp name="HTTPSampler.protocol">https</stringProp>
          <stringProp name="HTTPSampler.path">/api/auth/login</stringProp>
          <stringProp name="HTTPSampler.method">POST</stringProp>
          <boolProp name="HTTPSampler.follow_redirects">true</boolProp>
          <elementProp name="HTTPsampler.Arguments" elementType="Arguments">
            <collectionProp name="Arguments.arguments">
              <elementProp name="" elementType="HTTPArgument">
                <boolProp name="HTTPArgument.always_encode">false</boolProp>
                <stringProp name="Argument.value">{"email":"test@test.com","password":"pass"}</stringProp>
                <stringProp name="Argument.metadata">=</stringProp>
              </elementProp>
            </collectionProp>
          </elementProp>
        </HTTPSamplerProxy>
        <!-- ... 50+ more lines ... -->
      </hashTree>
    </hashTree>
  </hashTree>
</jmeterTestPlan>
```

**NBomber (C#):**

```csharp
var scenario = Scenario.Create("login_test", async context =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
    var loginData = new { email = "test@test.com", password = "pass" };
    var response = await client.PostAsJsonAsync("/api/auth/login", loginData);
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
    Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
);

NBomberRunner.RegisterScenarios(scenario).Run();
```

**Lines of Code:**
- JMeter: ~100 lines of XML
- NBomber: ~10 lines of C#

### When to Choose NBomber

✅ **Choose NBomber if:**
- Your team uses .NET
- You want code-based tests (not GUI)
- You need version control for tests
- You want CI/CD integration
- You're testing HTTP/gRPC/WebSocket APIs
- You want to reuse application code in tests
- You need debugger support

### When to Choose JMeter

✅ **Choose JMeter if:**
- You need protocols NBomber doesn't support (JDBC, SMTP, FTP, LDAP)
- Your team doesn't know .NET
- You prefer GUI-based test creation
- You have existing JMeter tests
- You need enterprise support/training

---

## 10. Best Practices

### 10.1 Test Design Principles

**1. Isolate System Under Test**

```csharp
// ❌ BAD: Testing entire production stack
var client = new HttpClient 
{ 
    BaseAddress = new Uri("https://production.example.com") 
};

// ✅ GOOD: Testing isolated staging environment
var client = new HttpClient 
{ 
    BaseAddress = new Uri("https://staging-api.example.com") 
};
```

**2. Use Connection Pooling**

```csharp
// ❌ BAD: New client per request
var scenario = Scenario.Create("test", async context =>
{
    var client = new HttpClient(); // Creates new connection each time
    await client.GetAsync("https://api.example.com");
    return Response.Ok();
});

// ✅ GOOD: Reuse clients from pool
var httpFactory = ClientFactory.Create(
    name: "http_factory",
    clientCount: 50, // Pool of 50 clients
    initClient: (number, context) => Task.FromResult(new HttpClient())
);

var scenario = Scenario.Create("test", async context =>
{
    var client = httpFactory.GetClient(context.ScenarioInfo);
    await client.GetAsync("https://api.example.com");
    return Response.Ok();
});
```

**3. Handle Errors Gracefully**

```csharp
// ❌ BAD: Unhandled exceptions crash test
var scenario = Scenario.Create("test", async context =>
{
    var response = await client.GetAsync("/api/data"); // May throw
    return Response.Ok();
});

// ✅ GOOD: Catch and categorize errors
var scenario = Scenario.Create("test", async context =>
{
    try
    {
        var response = await client.GetAsync("/api/data");
        return response.IsSuccessStatusCode
            ? Response.Ok()
            : Response.Fail(statusCode: response.StatusCode.ToString());
    }
    catch (HttpRequestException ex)
    {
        return Response.Fail(error: "Network error", statusCode: "NETWORK_ERROR");
    }
    catch (TaskCanceledException)
    {
        return Response.Fail(error: "Timeout", statusCode: "TIMEOUT");
    }
    catch (Exception ex)
    {
        return Response.Fail(error: ex.Message, statusCode: "EXCEPTION");
    }
});
```

**4. Set Realistic Timeouts**

```csharp
var httpFactory = ClientFactory.Create(
    name: "http_factory",
    clientCount: 50,
    initClient: (number, context) =>
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30), // Realistic timeout
            BaseAddress = new Uri("https://api.example.com")
        };
        return Task.FromResult(client);
    }
);
```

**5. Validate Responses**

```csharp
// ❌ BAD: Only check HTTP status
return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();

// ✅ GOOD: Validate response content
if (response.IsSuccessStatusCode)
{
    var data = await response.Content.ReadFromJsonAsync<ApiResponse>();
    if (data?.IsValid == true && data.Data != null)
    {
        return Response.Ok();
    }
    return Response.Fail(error: "Invalid response data", statusCode: "INVALID_DATA");
}
```

### 10.2 Performance Optimization

**1. Minimize Allocations**

```csharp
// ❌ BAD: Creates new objects in hot path
var scenario = Scenario.Create("test", async context =>
{
    var requestData = new { id = context.ScenarioInfo.InstanceNumber }; // New allocation
    var json = JsonSerializer.Serialize(requestData); // New allocation
    var content = new StringContent(json); // New allocation
    // ...
});

// ✅ GOOD: Reuse objects when possible
var scenario = Scenario.Create("test", async context =>
{
    // Use from Feed or pre-allocated pool
    var requestData = feed.GetNextItem(context.ScenarioInfo);
    // ...
});
```

**2. Use Async/Await Properly**

```csharp
// ❌ BAD: Blocking calls
var scenario = Scenario.Create("test", async context =>
{
    var response = client.GetAsync("/api/data").Result; // Blocks thread
    return Response.Ok();
});

// ✅ GOOD: Proper async
var scenario = Scenario.Create("test", async context =>
{
    var response = await client.GetAsync("/api/data");
    return Response.Ok();
});
```

**3. Batch Operations When Possible**

```csharp
// ❌ BAD: Sequential requests
for (int i = 0; i < 10; i++)
{
    await client.GetAsync($"/api/item/{i}");
}

// ✅ GOOD: Parallel requests
var tasks = Enumerable.Range(0, 10)
    .Select(i => client.GetAsync($"/api/item/{i}"));
await Task.WhenAll(tasks);
```

### 10.3 Reporting Best Practices

**1. Use Descriptive Names**

```csharp
// ❌ BAD
var scenario = Scenario.Create("test1", /* ... */);

// ✅ GOOD
var scenario = Scenario.Create("user_registration_with_email_verification", /* ... */);
```

**2. Custom Status Codes for Analysis**

```csharp
if (response.StatusCode == HttpStatusCode.OK)
{
    var user = await response.Content.ReadFromJsonAsync<User>();
    if (user.IsEmailVerified)
        return Response.Ok(statusCode: "200_VERIFIED_USER");
    else
        return Response.Ok(statusCode: "200_UNVERIFIED_USER");
}
```

This creates separate buckets in reports for verified vs unverified users.

**3. Meaningful File Names**

```csharp
NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFileName($"api_loadtest_{Environment.GetEnvironmentVariable("BUILD_ID")}_{DateTime.Now:yyyyMMdd_HHmmss}")
    .Run();
```

---

## 11. Real-World Enterprise Patterns

### 11.1 Multi-Environment Testing

```csharp
public enum TestEnvironment
{
    Local,
    Dev,
    Staging,
    Production
}

public class EnvironmentConfig
{
    public static Dictionary<TestEnvironment, string> BaseUrls = new()
    {
        { TestEnvironment.Local, "http://localhost:5000" },
        { TestEnvironment.Dev, "https://dev-api.example.com" },
        { TestEnvironment.Staging, "https://staging-api.example.com" },
        { TestEnvironment.Production, "https://api.example.com" }
    };
    
    public static TestEnvironment Current =>
        Enum.Parse<TestEnvironment>(
            Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Staging"
        );
    
    public static string BaseUrl => BaseUrls[Current];
}

// Usage
var client = new HttpClient { BaseAddress = new Uri(EnvironmentConfig.BaseUrl) };
```

### 11.2 Authentication Strategies

**OAuth 2.0 / JWT Token:**

```csharp
public class AuthHelper
{
    private static string _cachedToken;
    private static DateTime _tokenExpiry;
    
    public static async Task<string> GetAuthToken(HttpClient client)
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }
        
        var loginData = new
        {
            email = "loadtest@example.com",
            password = Environment.GetEnvironmentVariable("TEST_PASSWORD")
        };
        
        var response = await client.PostAsJsonAsync("/auth/login", loginData);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        _cachedToken = result.GetProperty("token").GetString();
        _tokenExpiry = DateTime.UtcNow.AddMinutes(55); // Refresh before expiry
        
        return _cachedToken;
    }
}

var scenario = Scenario.Create("authenticated_request", async context =>
{
    var client = httpFactory.GetClient(context.ScenarioInfo);
    var token = await AuthHelper.GetAuthToken(client);
    
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var response = await client.GetAsync("/api/protected-resource");
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
});
```

### 11.3 Database Seeding for Tests

```csharp
public class TestDataSeeder
{
    public static async Task SeedTestData(string apiBaseUrl)
    {
        var client = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        
        // Seed 1000 test users
        for (int i = 0; i < 1000; i++)
        {
            var userData = new
            {
                email = $"loadtest{i}@example.com",
                username = $"LoadTestUser{i}",
                password = "TestPassword123!"
            };
            
            await client.PostAsJsonAsync("/auth/register", userData);
        }
        
        Console.WriteLine("Test data seeded successfully");
    }
}

// Run before load test
public class Program
{
    public static async Task Main(string[] args)
    {
        var baseUrl = EnvironmentConfig.BaseUrl;
        
        // Seed data
        await TestDataSeeder.SeedTestData(baseUrl);
        
        // Run load test
        var scenario = /* ... */;
        NBomberRunner.RegisterScenarios(scenario).Run();
    }
}
```

### 11.4 Distributed Load Testing

For testing beyond single machine capacity:

```csharp
// Coordinator node
var coordinator = NBomberRunner
    .RegisterScenarios(scenario)
    .WithTargetScenarios("login_scenario")
    .LoadConfig("cluster-config.json")
    .RunInCluster();

// Worker nodes (run on separate machines)
var worker = NBomberRunner
    .RegisterScenarios(scenario)
    .LoadConfig("cluster-config.json")
    .RunAsWorker();
```

**cluster-config.json:**

```json
{
  "ClusterSettings": {
    "Coordinator": {
      "Host": "192.168.1.100",
      "Port": 5555
    },
    "Workers": [
      { "Host": "192.168.1.101", "Port": 5556 },
      { "Host": "192.168.1.102", "Port": 5556 },
      { "Host": "192.168.1.103", "Port": 5556 }
    ]
  }
}
```

---

## 12. Troubleshooting and Performance Tuning

### 12.1 Common Issues

**Issue: "Socket exhaustion" / "Cannot assign requested address"**

**Cause:** Too many connections created without proper pooling

**Solution:**
```csharp
// Use ClientFactory with appropriate pool size
var httpFactory = ClientFactory.Create(
    name: "http_factory",
    clientCount: 100, // Adjust based on load
    initClient: (number, context) => Task.FromResult(new HttpClient())
);
```

**Issue: High memory usage**

**Cause:** Not disposing of resources, large payloads

**Solution:**
```csharp
// Ensure clients are reused, not created per request
// Use streaming for large responses
var stream = await response.Content.ReadAsStreamAsync();
```

**Issue: Inconsistent latency (high StdDev)**

**Possible Causes:**
1. Garbage collection pauses (monitor GC)
2. Network issues (use ping plugin)
3. Target system throttling
4. Database connection pool exhaustion

**Diagnosis:**
```csharp
NBomberRunner
    .RegisterScenarios(scenario)
    .WithWorkerPlugins(
        new PingPlugin(new PingPluginConfig 
        { 
            Hosts = new[] { "api.example.com" } 
        })
    )
    .Run();
```

### 12.2 Load Test Machine Sizing

**Recommendations:**

| Target Load | CPU Cores | RAM | Notes |
|-------------|-----------|-----|-------|
| < 1,000 RPS | 2-4 | 4 GB | Single machine |
| 1,000-5,000 RPS | 4-8 | 8 GB | Monitor CPU |
| 5,000-10,000 RPS | 8-16 | 16 GB | May need distribution |
| > 10,000 RPS | Distributed | - | Multiple machines |

**Monitor NBomber process:**
```bash
# CPU and memory usage
dotnet-counters monitor -p <process-id>
```

### 12.3 Target System Preparation

**Before running load tests:**

1. **Disable rate limiting** (or use appropriate test accounts)
2. **Scale up resources** (if testing capacity, not current config)
3. **Enable detailed logging** (identify bottlenecks)
4. **Set up monitoring** (APM, database metrics)
5. **Notify stakeholders** (so alerts don't cause panic)
6. **Take database backup** (in case of data corruption)

---

## 13. Appendix: Code Templates

### A. Basic HTTP GET Test

```csharp
using NBomber.CSharp;

var httpFactory = ClientFactory.Create(
    name: "http_factory",
    clientCount: 50,
    initClient: (number, context) =>
    {
        var client = new HttpClient { BaseAddress = new Uri("https://api.example.com") };
        return Task.FromResult(client);
    }
);

var scenario = Scenario.Create("http_get_test", async context =>
{
    var client = httpFactory.GetClient(context.ScenarioInfo);
    
    try
    {
        var response = await client.GetAsync("/api/items");
        return response.IsSuccessStatusCode
            ? Response.Ok(sizeBytes: (int)(response.Content.Headers.ContentLength ?? 0))
            : Response.Fail(statusCode: response.StatusCode.ToString());
    }
    catch (Exception ex)
    {
        return Response.Fail(error: ex.Message);
    }
})
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder("Reports")
    .WithReportFormats(ReportFormat.Html)
    .Run();
```

### B. HTTP POST with JSON Body

```csharp
using System.Net.Http.Json;

var scenario = Scenario.Create("http_post_test", async context =>
{
    var client = httpFactory.GetClient(context.ScenarioInfo);
    
    var requestData = new
    {
        name = "Test Item",
        value = context.ScenarioInfo.InstanceNumber
    };
    
    try
    {
        var response = await client.PostAsJsonAsync("/api/items", requestData);
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    }
    catch (Exception ex)
    {
        return Response.Fail(error: ex.Message);
    }
});
```

### C. Multi-Step Workflow Template

```csharp
var scenario = Scenario.Create("multi_step_workflow", async context =>
{
    var client = httpFactory.GetClient(context.ScenarioInfo);
    
    // Step 1
    var step1 = await Step.Run("step_1", context, async () =>
    {
        var response = await client.GetAsync("/step1");
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    });
    
    if (!step1.IsSuccess) return step1;
    
    // Step 2
    var step2 = await Step.Run("step_2", context, async () =>
    {
        var response = await client.PostAsync("/step2", content);
        return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
    });
    
    return step2;
});
```

### D. Data-Driven Test Template

```csharp
var testDataFeed = Feed.CreateCircular(
    "test_data",
    Enumerable.Range(1, 1000)
        .Select(i => new { UserId = i, Email = $"user{i}@test.com" })
        .ToArray()
);

var scenario = Scenario.Create("data_driven_test", async context =>
{
    var testData = testDataFeed.GetNextItem(context.ScenarioInfo);
    var client = httpFactory.GetClient(context.ScenarioInfo);
    
    var response = await client.GetAsync($"/api/users/{testData.UserId}");
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithInit(ctx => Task.FromResult(testDataFeed));
```

### E. Complete Enterprise Template

```csharp
using NBomber.CSharp;
using NBomber.Contracts;
using Serilog;
using System.Net.Http.Json;

public class EnterpriseLoadTest
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();
    
    public static async Task Main(string[] args)
    {
        var config = LoadTestConfig.FromEnvironment();
        
        // HTTP client factory
        var httpFactory = ClientFactory.Create(
            name: "http_factory",
            clientCount: 100,
            initClient: (number, context) =>
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(config.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
                client.DefaultRequestHeaders.Add("User-Agent", "NBomber-LoadTest");
                return Task.FromResult(client);
            }
        );
        
        // Test data
        var usersFeed = Feed.CreateCircular(
            "users",
            GenerateTestUsers(config.UserCount).ToArray()
        );
        
        // Scenario
        var scenario = Scenario.Create("api_load_test", async context =>
        {
            var user = usersFeed.GetNextItem(context.ScenarioInfo);
            var client = httpFactory.GetClient(context.ScenarioInfo);
            
            try
            {
                // Your test logic here
                var response = await client.PostAsJsonAsync("/api/endpoint", user);
                
                return response.IsSuccessStatusCode
                    ? Response.Ok(sizeBytes: (int)(response.Content.Headers.ContentLength ?? 0))
                    : Response.Fail(statusCode: response.StatusCode.ToString());
            }
            catch (HttpRequestException ex)
            {
                return Response.Fail(error: "Network error", statusCode: "NETWORK_ERROR");
            }
            catch (TaskCanceledException)
            {
                return Response.Fail(error: "Timeout", statusCode: "TIMEOUT");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error in scenario");
                return Response.Fail(error: ex.Message, statusCode: "EXCEPTION");
            }
        })
        .WithInit(ctx => Task.FromResult(usersFeed))
        .WithLoadSimulations(
            Simulation.RampingInject(
                rate: config.TargetRps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(config.RampUpMinutes)
            ),
            Simulation.InjectPerSec(
                rate: config.TargetRps,
                during: TimeSpan.FromMinutes(config.SteadyStateMinutes)
            )
        )
        .WithMaxFailCount(config.MaxFailCount);
        
        // Run test
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("Reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .WithReportFileName($"loadtest_{DateTime.Now:yyyyMMdd_HHmmss}")
            .Run();
        
        // Validate results
        var scenarioStats = stats.ScenarioStats[0];
        ValidateResults(scenarioStats, config);
    }
    
    private static IEnumerable<TestUser> GenerateTestUsers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new TestUser
            {
                UserId = i,
                Email = $"loadtest{i}@example.com",
                Name = $"Test User {i}"
            };
        }
    }
    
    private static void ValidateResults(ScenarioStats stats, LoadTestConfig config)
    {
        var passed = true;
        
        if (stats.Ok.Latency.Percent95 > config.MaxP95Latency)
        {
            Logger.Error($"FAIL: p95 latency {stats.Ok.Latency.Percent95}ms exceeds {config.MaxP95Latency}ms");
            passed = false;
        }
        
        if (stats.Fail.Request.Percent > config.MaxErrorRate)
        {
            Logger.Error($"FAIL: Error rate {stats.Fail.Request.Percent}% exceeds {config.MaxErrorRate}%");
            passed = false;
        }
        
        if (passed)
        {
            Logger.Information("PASS: All performance criteria met");
        }
        else
        {
            Environment.Exit(1);
        }
    }
}

public class LoadTestConfig
{
    public string BaseUrl { get; set; }
    public int TargetRps { get; set; }
    public int RampUpMinutes { get; set; }
    public int SteadyStateMinutes { get; set; }
    public int UserCount { get; set; }
    public int MaxFailCount { get; set; }
    public double MaxP95Latency { get; set; }
    public double MaxErrorRate { get; set; }
    
    public static LoadTestConfig FromEnvironment()
    {
        return new LoadTestConfig
        {
            BaseUrl = Environment.GetEnvironmentVariable("TARGET_URL") ?? "http://localhost:5000",
            TargetRps = int.Parse(Environment.GetEnvironmentVariable("TARGET_RPS") ?? "100"),
            RampUpMinutes = int.Parse(Environment.GetEnvironmentVariable("RAMP_UP_MINUTES") ?? "2"),
            SteadyStateMinutes = int.Parse(Environment.GetEnvironmentVariable("STEADY_STATE_MINUTES") ?? "5"),
            UserCount = int.Parse(Environment.GetEnvironmentVariable("USER_COUNT") ?? "1000"),
            MaxFailCount = int.Parse(Environment.GetEnvironmentVariable("MAX_FAIL_COUNT") ?? "100"),
            MaxP95Latency = double.Parse(Environment.GetEnvironmentVariable("MAX_P95_LATENCY") ?? "200"),
            MaxErrorRate = double.Parse(Environment.GetEnvironmentVariable("MAX_ERROR_RATE") ?? "1.0")
        };
    }
}

public class TestUser
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}
```

---

## Conclusion

NBomber provides a modern, developer-friendly approach to load testing for .NET teams. By leveraging C# and the .NET ecosystem, teams can:

- Write tests in their primary programming language
- Reuse existing application code and libraries
- Integrate seamlessly with CI/CD pipelines
- Debug tests with familiar tools
- Version control tests like any other code

For .NET organizations, NBomber represents a significant improvement over traditional tools like JMeter, particularly for API and microservice testing scenarios.

**Next Steps:**
1. Install NBomber in a test project
2. Create a simple HTTP load test
3. Run against a staging environment
4. Analyze reports and establish baselines
5. Integrate into CI/CD pipeline
6. Expand test coverage across critical endpoints

**Resources:**
- NBomber Documentation: https://nbomber.com/docs/
- GitHub Repository: https://github.com/PragmaticFlow/NBomber
- Examples: https://github.com/PragmaticFlow/NBomber/tree/dev/examples

---

**Document Version:** 1.0  
**Last Updated:** February 10, 2026  
**Prepared by:** Mark  
**For:** Performance Testing Team
