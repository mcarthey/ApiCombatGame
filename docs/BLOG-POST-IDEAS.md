# API Combat Game - Blog Post Ideas & Snippets

> Marketing funnel content spanning beginner to expert level developers.
> All posts centered around real implementation from apicombat.com

---

## 🎯 Beginner-Friendly Posts

### 1. "The API Is The Game: Building a Game Where Code Is The Controller"

**Hook:** "What if playing a video game meant writing code instead of pressing buttons?"

**Snippet:**
```csharp
// Players don't click buttons - they write API calls
POST /api/v1/battle/queue
{
  "teamId": "abc-123",
  "mode": "ranked"
}

// The game plays automatically using their strategy
{
  "formation": "aggressive",
  "targetPriority": ["healers", "lowest_hp"],
  "abilities": {
    "Fireball": {
      "when": "enemy_count_gte_2",
      "target": "priority"
    }
  }
}
```

**Key Points:**
- Educational gaming: Learn APIs by playing
- Real HTTP requests = real gameplay
- JSON becomes your game controller
- Perfect for bootcamps, coding education
- Built with ASP.NET Core 8

**Call to Action:** "Try it at apicombat.com - registration opens [date]"

---

### 2. "Material Design 3 Without Component Libraries: Custom CSS in 2026"

**Hook:** "Why we ditched MUI, Vuetify, and every other component library"

**Snippet:**
```css
/* Custom M3 button - no library needed */
.md3-btn-filled {
  background: var(--md-sys-color-primary);
  color: var(--md-sys-color-on-primary);
  border-radius: var(--md-sys-shape-corner-full);
  padding: 10px 24px;
  font-weight: 500;
  transition: all 0.2s;
  box-shadow: var(--md-sys-elevation-1);
}

.md3-btn-filled:hover {
  box-shadow: var(--md-sys-elevation-2);
  background: color-mix(in srgb, var(--md-sys-color-primary) 92%, black);
}
```

**Key Points:**
- Why component libraries become technical debt
- CSS custom properties for theming
- Dark mode with zero JavaScript
- Smaller bundle size, faster load times
- Full control over design

**Visual:** Side-by-side comparison of bundle sizes (custom CSS vs. component library)

---

### 3. "From SQLite to SQL Server: Zero-Downtime Database Migration"

**Hook:** "We migrated from SQLite to MSSQL without losing a single record"

**Snippet:**
```csharp
// Before: Development-only approach
await context.Database.EnsureCreatedAsync();

// After: Production-ready migrations
if (context.Database.IsRelational())
    await context.Database.MigrateAsync();
else
    await context.Database.EnsureCreatedAsync(); // Tests only

// Migration file auto-generated
dotnet ef migrations add InitialCreate
```

**Key Points:**
- Why SQLite isn't production-ready
- EF Core migrations explained
- Testing with in-memory vs. LocalDB
- Seed data strategies
- Connection string management

---

### 4. "reCAPTCHA v3: The Invisible Bot Blocker"

**Hook:** "Stop bots without annoying users with 'select all traffic lights'"

**Snippet:**
```javascript
// Frontend: Invisible to users
grecaptcha.execute(siteKey, {action: 'contact_form'})
  .then(token => {
    document.getElementById('recaptcha-token').value = token;
    form.submit();
  });
```

```csharp
// Backend: Score-based validation
var result = await _recaptchaService.ValidateAsync(token);
if (result.Score < 0.5) {
    _logger.LogWarning("Potential bot: score {Score}", result.Score);
    return BadRequest("Please try again.");
}
```

**Key Points:**
- v3 vs v2: No more checkboxes
- Score-based validation (0.0 to 1.0)
- Honeypot as first line of defense
- Dev mode bypass for testing
- GDPR considerations

---

## 🔧 Intermediate Posts

### 5. "HATEOAS in Practice: Self-Documenting APIs"

**Hook:** "Your API clients shouldn't need to memorize URLs"

**Snippet:**
```json
{
  "id": "battle-123",
  "status": "completed",
  "winner": "player-456",
  "_links": {
    "self": { "href": "/api/v1/battles/battle-123" },
    "replay": { "href": "/api/v1/battles/battle-123/replay" },
    "rematch": {
      "href": "/api/v1/battle/queue",
      "method": "POST"
    }
  }
}
```

```csharp
// Builder pattern for link generation
public static class Links
{
    public static ApiLink Self(string battleId) =>
        new ApiLink("/api/v1/battles/" + battleId, "GET");

    public static ApiLink Replay(string battleId) =>
        new ApiLink($"/api/v1/battles/{battleId}/replay", "GET");
}
```

**Key Points:**
- What is HATEOAS (Hypermedia as the Engine of Application State)
- Benefits: Discoverability, versioning flexibility
- Implementation without over-engineering
- When to use it (and when not to)
- RESTful maturity model (Level 3)

**Visual:** Richardson Maturity Model diagram

---

### 6. "Dual Authentication: JWT for API, Cookies for Web UI"

**Hook:** "One app, two auth schemes - and they play nice together"

**Snippet:**
```csharp
// Policy-based authentication
builder.Services.AddAuthentication()
    .AddJwtBearer()
    .AddCookie();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("JWT_OR_COOKIE", policy =>
        policy.AddAuthenticationSchemes("Bearer", "Cookies")
              .RequireAuthenticatedUser());
});

// Route-specific scheme selection
[Authorize(AuthenticationSchemes = "Cookies,Bearer")]
public async Task<IActionResult> GetAnalytics() { }
```

**Key Points:**
- Why you need both
- API calls from browser (cookie) vs. external clients (JWT)
- Policy scheme routing
- Security considerations
- Token refresh strategies

---

### 7. "AI Bots That Don't Suck: Building Believable Game AI"

**Hook:** "Our bots have dev-culture names and scale with player skill"

**Snippet:**
```csharp
// Bot names that hint but don't reveal
var botNames = new[] {
    "CodeMonkey", "RubberDuck", "StackOverflow",
    "GitBlame", "NullPointer", "SegFault"
};

// Rating-based team composition
if (rating < 1000) {
    // Beginner: Simple 2-1-1-1 comp
    team = [2×Warrior, 1×Mage, 1×Healer, 1×Tank];
} else if (rating > 1600) {
    // Expert: Meta-optimized with complex strategies
    team = GenerateMetaComp();
    strategy = new() {
        Formation = "aggressive",
        TargetPriority = ["healers", "highest_threat", "lowest_hp"]
    };
}
```

**Key Points:**
- Why games need bots (ghost town effect)
- Rating-based difficulty scaling
- Strategy patterns for believability
- Matchmaking fallback logic
- Natural density reduction as playerbase grows

---

### 8. "Background Jobs in ASP.NET Core: When Cron Isn't Enough"

**Hook:** "10 background jobs running simultaneously in one app"

**Snippet:**
```csharp
// Daily challenge generation - runs at midnight UTC
public class DailyChallengeGenerationJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var tomorrow = now.Date.AddDays(1);
            var delay = tomorrow - now;

            await Task.Delay(delay, stoppingToken);
            await GenerateDailyChallenges();
        }
    }
}

// Register in Program.cs
builder.Services.AddHostedService<DailyChallengeGenerationJob>();
```

**Jobs we run:**
- Daily challenge generation
- Weekly modifier rotation
- Strategy decay (balancing)
- Guild boss spawns
- Notification cleanup
- Tournament processing
- Admin alerting

**Key Points:**
- IHostedService vs. BackgroundService
- Scheduling patterns (cron-like)
- Graceful shutdown handling
- Monitoring and logging
- Testing background jobs

---

### 9. "Freemium Done Right: Tiered Features Without Being Evil"

**Hook:** "Monetization that doesn't make your users hate you"

**Snippet:**
```csharp
// Feature gating with clear upgrade path
public bool IsPremiumPlus => CurrentTier == SubscriptionTier.PremiumPlus;

// Premium gets a taste, Premium Plus gets the full feast
@if (Model.IsPremiumPlus) {
    <div>@Analytics.LongestWinStreak wins</div>
} else {
    <div style="opacity: 0.6;">
        <span class="lock-icon">🔒</span>
        Longest: Premium Plus
    </div>
}
```

**Tier Structure:**
- **Free**: 10 battles/day, basic units, 3 team slots
- **Premium** ($5/mo): Unlimited, 50+ units, priority matchmaking, analytics
- **Premium Plus** ($10/mo): Lua scripting, advanced analytics, 2× API limits

**Key Points:**
- Give free tier enough to be valuable
- Show locked features (create desire)
- Clear value proposition for each tier
- No dark patterns, no predatory tactics
- "Taste and tease" strategy

---

## 🚀 Advanced/Technical Deep Dives

### 10. "Custom OpenAPI Documentation Renderer: Ditch Swagger UI"

**Hook:** "We built our own API docs because Swagger UI felt... generic"

**Snippet:**
```csharp
// Custom Razor partial components
@await Html.PartialAsync("_EndpointGroup", new EndpointGroupModel {
    Name = "Authentication",
    Endpoints = authEndpoints
})

@await Html.PartialAsync("_Endpoint", new EndpointModel {
    Method = "POST",
    Path = "/api/v1/auth/login",
    Description = "Authenticate with email and password",
    RequestBody = loginRequestSchema,
    Responses = new[] {
        new ResponseModel(200, "Success", loginResponseSchema),
        new ResponseModel(401, "Invalid credentials")
    }
})
```

**Why Custom:**
- Brand consistency
- Better mobile experience
- Integrated with site navigation
- Code examples in multiple languages
- No JavaScript dependency for docs

**Key Points:**
- OpenAPI spec generation
- Razor partial components
- Syntax highlighting
- Schema table rendering
- SEO optimization for API docs

---

### 11. "Email Templates That Work: Fighting Gmail's HTML Mangling"

**Hook:** "Gmail doesn't respect your CSS. Here's how to fight back."

**Snippet:**
```html
<!-- Gmail strips <style> tags and some inline styles -->

<!-- ❌ Won't work -->
<a href="..." style="color: #6366f1;">Click here</a>

<!-- ✅ Works in Gmail -->
<a href="..." style="color: #ffffff !important;
                     background: #6366f1 !important;
                     padding: 12px 24px;
                     text-decoration: none;
                     display: inline-block;">
    Click here
</a>

<!-- Table-based layouts for reliability -->
<table width="100%" cellpadding="0" cellspacing="0">
    <tr>
        <td align="center">
            <!-- Content -->
        </td>
    </tr>
</table>
```

**Key Points:**
- Gmail's CSS filtering
- Table-based layouts (yes, in 2026)
- Inline styles with !important
- Testing across clients (Gmail, Outlook, Apple Mail)
- Dark mode considerations
- Plain text fallbacks

---

### 12. "Testing Strategy: 457 Tests and Growing"

**Hook:** "How we maintain 100% test pass rate across integration and unit tests"

**Snippet:**
```csharp
// Unit test: Fast, isolated
[Fact]
public async Task OnGetAsync_PremiumUser_SetsIsPremiumPlusToFalse()
{
    var context = TestDbContextFactory.Create();
    var player = TestDbContextFactory.CreatePlayer(
        context, "premiumuser", SubscriptionTier.Premium);
    var service = new PlayerAnalyticsService(context);

    var result = await service.GetAnalyticsAsync(player.Id);

    Assert.False(result.IsPremiumPlus);
}

// Integration test: Full stack
[Fact]
public async Task RegistrationFlow_WithReCAPTCHA_CreatesPlayer()
{
    var (client, _) = await CreateAuthenticatedClient();

    var response = await client.PostAsJsonAsync("/api/v1/auth/register", new {
        username = "testuser",
        email = "test@example.com",
        password = "SecurePass123!"
    });

    response.EnsureSuccessStatusCode();
}
```

**Test Distribution:**
- Unit tests: ~350 (services, helpers, calculations)
- Integration tests: ~100 (full API calls, page handlers)
- Test helpers: In-memory DB factory, mock generators

**Key Points:**
- When to unit test vs. integration test
- In-memory database for speed
- Test data factories
- Mocking external services
- CI/CD integration
- Maintaining test quality

---

### 13. "Rating Systems: Beyond Elo"

**Hook:** "How we calculate 'Arena Power Index' for fair matchmaking"

**Snippet:**
```csharp
// K-factor varies by tier and experience
private int GetKFactor(Player player)
{
    // New players: Higher volatility (faster rating change)
    if (player.TotalBattlesPlayed < 20)
        return 40;

    // Premium users: Tighter rating (more battles = more data)
    if (player.CurrentTier != SubscriptionTier.Free)
        return 24;

    // Standard K-factor
    return 32;
}

// Expected score with rating difference
private double ExpectedScore(int ratingA, int ratingB)
{
    return 1.0 / (1.0 + Math.Pow(10, (ratingB - ratingA) / 400.0));
}

// New rating calculation
var expected = ExpectedScore(winner.Rating, loser.Rating);
var actual = 1.0; // Win = 1, Loss = 0
var change = (int)(kFactor * (actual - expected));
```

**Key Points:**
- Elo formula explained
- K-factor variations
- Rating inflation/deflation
- Tier-based matchmaking ranges
- Provisional ratings for new players
- Anti-smurf measures

---

### 14. "Deployment to Shared Hosting: Yes, It's Possible in 2026"

**Hook:** "Don't believe the hype - you don't need Kubernetes for everything"

**Snippet:**
```xml
<!-- MSDeploy publish profile -->
<PropertyGroup>
  <WebPublishMethod>MSDeploy</WebPublishMethod>
  <MSDeployServiceURL>https://site4now.net:8172/MSDeploy.axd</MSDeployServiceURL>
  <DeployIisAppPath>apicombat.com</DeployIisAppPath>
  <EnableMSDeployAppOffline>true</EnableMSDeployAppOffline>
  <SkipExtraFilesOnServer>false</SkipExtraFilesOnServer>
  <RetryAttemptsForDeployment>10</RetryAttemptsForDeployment>
</PropertyGroup>
```

**Lessons Learned:**
- Dedicated app pool isolation (avoid ERROR_FILE_IN_USE)
- Shadow copy NOT supported on shared hosting (causes 502.5)
- Connection string management
- OutOfProcess hosting model
- Deployment retries for reliability

**Key Points:**
- When shared hosting makes sense (cost vs. complexity)
- MSDeploy best practices
- IIS configuration
- Troubleshooting deployment issues
- Monitoring on shared hosting

---

### 15. "SEO for Web Apps: Making SPAs Discoverable"

**Hook:** "Razor Pages + SEO = discoverability without SSR complexity"

**Snippet:**
```html
<!-- ViewData-driven meta tags -->
@{
    ViewData["Title"] = "Battle Analytics";
    ViewData["Description"] = "Advanced battle analytics and performance insights for your API Combat Game career.";
    ViewData["OgImage"] = "/images/og-analytics.png";
    ViewData["OgType"] = "website";
}

<!-- _Layout.cshtml renders SEO tags -->
<title>@ViewData["Title"] - API Combat</title>
<meta name="description" content="@ViewData["Description"]" />
<meta property="og:title" content="@ViewData["Title"]" />
<meta property="og:description" content="@ViewData["Description"]" />
<meta property="og:image" content="@ViewData["OgImage"]" />
<link rel="canonical" href="https://apicombat.com@ViewData["CanonicalPath"]" />
```

**Sitemap Generation:**
```csharp
[Route("sitemap.xml")]
public class SitemapController : Controller
{
    public IActionResult Index()
    {
        var urls = new[] {
            new { Loc = "https://apicombat.com", Priority = 1.0 },
            new { Loc = "https://apicombat.com/api-docs/v1", Priority = 0.9 },
            // Dynamic pages
        };
        return Content(GenerateSitemapXml(urls), "application/xml");
    }
}
```

**Key Points:**
- Server-side rendering advantages
- Structured data (JSON-LD)
- Dynamic sitemap generation
- Canonical URLs
- Social media previews
- robots.txt configuration

---

## 📊 Case Studies & Problem-Solving

### 16. "Fixing Dark Mode Dropdown Contrast: A CSS Detective Story"

**Hook:** "Users couldn't read dropdown options in dark mode. Here's the 3-line fix."

**Before/After:**
```css
/* Before: Invisible in dark mode */
select.md3-input option {
    /* Inherits system colors - broken in dark mode */
}

/* After: Explicit colors for both modes */
select.md3-input option {
    background: var(--md-sys-color-surface-container-low);
    color: var(--md-sys-color-on-surface);
}

.dark select.md3-input option {
    background: var(--md-sys-color-surface-container);
    color: var(--md-sys-color-on-surface);
}
```

**The Journey:**
1. User reports "can't see options"
2. Screenshot reveals the issue
3. Browser dev tools show color conflict
4. CSS specificity investigation
5. Solution: Explicit background for `<option>`

**Lesson:** Always test form elements in both themes

---

### 17. "The Migration: From SQLite to MSSQL Without Downtime"

**Hook:** "47 database tables, 51 relationships, zero data loss"

**Timeline:**
```
Day 1: Add migration infrastructure
Day 2: Test migrations on LocalDB
Day 3: Export SQLite data, import to MSSQL
Day 4: Deploy with migration
Day 5: Verify seed data re-creation
```

**Critical Decision:**
```csharp
// Seed data checks before insert
var existingBot = await context.Players
    .FirstOrDefaultAsync(p => p.Username == botName);

if (existingBot == null) {
    context.Players.Add(newBot);
}
```

**Why it worked:**
- Check-before-insert pattern in seed data
- Template units re-created automatically
- Admin account preserved via config
- Testing on exact copy of prod data

---

### 18. "Premium Plus Gating: The Psychology of Locked Features"

**Hook:** "Show them what they're missing (without being evil)"

**UI Pattern:**
```html
<div style="opacity: 0.6; position: relative;">
    <!-- Show the content but dimmed -->
    <div class="performance-metrics">
        <p>Avg. Rating/Win: +15.3</p>
        <p>Best Day: 12 wins</p>
    </div>

    <!-- Overlay with upgrade CTA -->
    <div class="lock-overlay">
        <span class="lock-icon">🔒</span>
        <p>PREMIUM PLUS</p>
    </div>
</div>
```

**Psychology:**
- **Visibility**: They see what they're missing (creates desire)
- **Accessibility**: Not hidden, just locked (feels attainable)
- **Clarity**: Clear tier requirement (no confusion)
- **No resentment**: Free tier is still valuable

**Metrics to track:**
- Conversion rate from locked feature interactions
- Time spent on gated pages
- Upgrade clicks from locked features

---

## 🎓 Educational Content

### 19. "Learn REST APIs by Building a Game Character"

**Hook:** "Tutorial series: Your first API calls create a warrior"

**Tutorial Flow:**
```bash
# Lesson 1: Register (POST)
curl -X POST https://apicombat.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username": "student", "email": "student@school.edu", "password": "Learn123!"}'

# Lesson 2: Login (Authentication)
curl -X POST https://apicombat.com/api/v1/auth/login \
  -d '{"email": "student@school.edu", "password": "Learn123!"}'

# Response includes token
{
  "token": "eyJhbGci...",
  "playerId": "abc-123"
}

# Lesson 3: Get your profile (GET with auth)
curl https://apicombat.com/api/v1/players/me \
  -H "Authorization: Bearer eyJhbGci..."

# Lesson 4: Create a team (POST with complex data)
# And so on...
```

**Perfect for:**
- Coding bootcamps
- CS courses (HTTP/REST unit)
- Self-learners
- API documentation practice

---

### 20. "From Idea to Production: 90-Day Game Development Journey"

**Hook:** "How we built and deployed a game in one development cycle"

**Phase Breakdown:**

**Phase 1 (Days 1-30): Core Mechanics**
- Authentication system
- Battle engine
- Unit system
- Basic matchmaking
- Razor Pages UI

**Phase 2 (Days 31-60): Engagement**
- Guilds
- Challenges
- Seasons
- Leaderboards
- Achievements

**Phase 3 (Days 61-75): Monetization**
- Subscription system
- Stripe integration
- Tiered features
- Analytics

**Phase 4 (Days 76-90): Polish**
- SEO optimization
- Email templates
- Contact form
- Bot system
- Performance tuning

**Lessons Learned:**
- Start with core loop
- Test early, test often
- Don't over-engineer v1
- Ship features incrementally
- User feedback > planned features

---

## 📈 Marketing & Growth

### 21. "Building in Public: Why We Share Everything"

**Hook:** "Our roadmap, our code decisions, our failures - all public"

**What we share:**
- GitHub issues (public roadmap)
- Technical blog posts
- Design decisions and tradeoffs
- Performance metrics
- User count milestones

**Benefits:**
- Transparency builds trust
- Attracts contributors
- Free QA from community
- SEO benefits (content marketing)
- Portfolio piece for developers

---

### 22. "The API-First Game: A New Category"

**Hook:** "We invented a genre: Educational API Gaming"

**Market Position:**
- **Not** a traditional game (no graphics engine)
- **Not** just a tutorial (actual competitive gameplay)
- **Not** a code challenge site (persistent progression)

**It's all three:**
- ✅ Learn by playing
- ✅ Compete on leaderboards
- ✅ Build real API skills

**Potential Markets:**
- Coding bootcamps ($100B+ education market)
- CS programs (hands-on API learning)
- Corporate training
- Individual learners
- Hackathon practice

---

## 🔥 Controversial/Hot Takes

### 23. "Why We Don't Use ASP.NET Identity (And You Shouldn't Either)"

**Hook:** "Identity is overkill for 99% of apps"

**Custom Auth Advantages:**
```csharp
// Our auth: 3 files, ~300 lines
- AuthService.cs (login, register, password validation)
- JwtService.cs (token generation)
- AuthController.cs (endpoints)

// ASP.NET Identity: 100+ files
- UserManager, SignInManager, RoleManager
- Identity DbContext changes
- 27+ database tables
- Scaffolded UI pages
```

**When Identity makes sense:**
- Enterprise apps with complex role hierarchies
- Apps needing external auth providers (OAuth)
- Regulatory requirements for specific auth flows

**When simple is better:**
- Startups/MVPs
- Clear, simple user model
- Full control over auth flow
- BCrypt + JWT is enough

**Disclaimer:** "Controversial opinion. Your mileage may vary."

---

### 24. "Stop Using ORMs for Everything (But Use EF Core for Most Things)"

**Hook:** "When the abstraction becomes the problem"

**When EF Core is perfect:**
```csharp
// CRUD operations - beautiful
var player = await context.Players.FindAsync(playerId);
player.Currency += reward;
await context.SaveChangesAsync();
```

**When raw SQL is better:**
```csharp
// Complex analytics query
var stats = await context.Database.SqlQueryRaw<BattleStats>(@"
    SELECT
        p.Username,
        COUNT(b.Id) as TotalBattles,
        AVG(CASE WHEN b.WinnerId = p.Id THEN 1.0 ELSE 0.0 END) as WinRate,
        RANK() OVER (ORDER BY p.Rating DESC) as GlobalRank
    FROM Players p
    LEFT JOIN Battles b ON b.Player1Id = p.Id OR b.Player2Id = p.Id
    WHERE b.Status = 'Completed'
    GROUP BY p.Id, p.Username, p.Rating
").ToListAsync();
```

**The Middle Ground:**
- Use EF for 90% of queries
- Use raw SQL for complex aggregations
- Use stored procedures for critical performance
- Profile, then optimize

---

## 🎨 Design & UX

### 25. "Material Design 3 Expressive: The Geek Aesthetic"

**Hook:** "How we made a technical app feel playful"

**Design Choices:**
- **Rating tiers**: Rubber Duck → Copy Pasta → Code Monkey → 10x Dev → Wizard
- **Color scheme**: Purple/indigo (tech/gaming hybrid)
- **Typography**: Monospace for code, sans-serif for UI
- **Icons**: Material Symbols (outline style)
- **Animations**: Subtle (no motion sickness)

**Brand Voice:**
- Geek culture references
- Developer in-jokes
- Technical but approachable
- Serious game, fun flavor

**Visual Examples:**
- OG image: Dark background + crossed swords + tagline
- Error messages: "404: Battle not found (did you typo the ID?)"
- Success messages: "🎉 Team created successfully"

---

## 💡 Quick Tips Series

### 26. "5 ASP.NET Core Performance Tips We Use"

1. **Response caching**: `[ResponseCache(Duration = 300)]`
2. **Compiled Razor views**: `AddRazorPages().AddRazorRuntimeCompilation()`
3. **Background processing**: Don't block request threads
4. **Connection pooling**: Reuse DB connections
5. **Async all the way**: Never mix sync/async

---

### 27. "10 JWT Gotchas and How to Avoid Them"

1. Don't store sensitive data in JWTs
2. Use short expiration times
3. Validate issuer and audience
4. Don't trust the client
5. HTTPS only for token transmission
6. Implement token refresh
7. Revocation strategies
8. Secret key rotation
9. Token size matters (cookies have limits)
10. Consider using cookies for browsers

---

### 28. "Razor Pages vs. Blazor vs. SPA: When to Use What"

**Use Razor Pages when:**
- SEO matters
- Simple interactivity
- Fast page loads critical
- Hosting on shared servers

**Use Blazor when:**
- Complex client-side state
- Real-time updates
- Heavy client interaction
- C# everywhere preference

**Use SPA (React/Vue) when:**
- Existing frontend team
- Rich UI requirements
- Mobile app planned (React Native)
- Separate API consumers

---

## 📚 Series Ideas

### "Building API Combat: A Series"

**Episode 1:** Concept and architecture decisions
**Episode 2:** Authentication and authorization
**Episode 3:** Battle engine and game logic
**Episode 4:** Matchmaking system
**Episode 5:** Subscription and payments
**Episode 6:** SEO and discoverability
**Episode 7:** Testing strategies
**Episode 8:** Deployment and monitoring
**Episode 9:** Bot system implementation
**Episode 10:** Lessons learned and what's next

---

### "Code Review: API Combat"

Walk through actual code with before/after examples:
- Refactoring for testability
- Extracting services
- Dependency injection patterns
- Error handling strategies
- Performance optimizations

---

## 🎯 Call-to-Action Ideas

**For each post:**

1. **Try it:** "Play at apicombat.com - registration opens [date]"
2. **Learn more:** "Full API docs at apicombat.com/api-docs/v1"
3. **Discuss:** "What's your approach to [topic]? Comment below"
4. **Connect:** "Follow me for more technical deep dives"
5. **Share:** "Know someone learning APIs? Share this post"
6. **Source code:** "Check out the implementation on GitHub [link]"
7. **Hire me:** "Need help with your project? Let's connect"

---

## 📊 Content Calendar Strategy

**Week 1:** Beginner post (wide reach)
**Week 2:** Intermediate post (engaged audience)
**Week 3:** Advanced post (technical credibility)
**Week 4:** Case study or problem-solving (relatability)

**LinkedIn optimal posting:**
- Tuesday-Thursday, 8-10 AM (engagement peak)
- Include code snippets as images (better engagement)
- First comment with additional resources
- Tag relevant technologies (#aspnetcore #webdev #api)

---

## 🎬 Bonus: Video/Screencast Ideas

1. "Building a feature in 15 minutes" (team creation flow)
2. "Debugging in production" (troubleshooting real issues)
3. "Code review livestream" (community engagement)
4. "API testing with Postman/Bruno" (beginner-friendly)
5. "Deploying with GitHub Actions" (DevOps intro)

---

## 🏆 Success Metrics

**Track for each post:**
- Views
- Engagement (likes, comments, shares)
- Click-through to apicombat.com
- New registrations (if mentioning registration)
- LinkedIn connections gained
- Job inquiries/consulting leads

**Content that performs best:**
- Code snippets with clear before/after
- Problem-solving stories
- Hot takes (controversial but defensible)
- Tutorials with tangible outcomes
- Behind-the-scenes of decisions

---

## 💬 Engagement Tactics

**In comments:**
- Answer every question
- Ask follow-up questions
- Share additional resources
- Be humble about mistakes
- Give credit to others' ideas

**Community building:**
- Create a Discord for players/developers
- Host office hours (AMA sessions)
- Feature community contributions
- Create a "Built With API Combat" showcase

---

_All content based on real implementation at apicombat.com
"The API is the game" - Come build strategies, battle for ranking_
