using System.Text.Json;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Education;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Data;

public static class EducationSeedData
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task InitializeAsync(GameDbContext context)
    {
        if (await context.Set<CurriculumModule>().AnyAsync(m => m.Title == "API Basics 101"))
            return;

        // Find admin player to use as instructor, or create a dedicated educator account
        var instructor = await context.Players.FirstOrDefaultAsync(p => p.IsAdmin && p.IsEducator);
        if (instructor == null)
        {
            instructor = await context.Players.FirstOrDefaultAsync(p => p.IsAdmin);
            if (instructor != null)
            {
                instructor.IsEducator = true;
                await context.SaveChangesAsync();
            }
        }

        // If no admin exists yet (e.g., fresh dev DB), create a dedicated educator
        if (instructor == null)
        {
            instructor = new Player
            {
                Id = Guid.NewGuid(),
                Username = "Instructor_Demo",
                Email = "instructor@apicombat.edu",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                IsEducator = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            context.Players.Add(instructor);
            await context.SaveChangesAsync();
        }

        var lessons = new List<CreateLessonRequest>
        {
            new()
            {
                Title = "Register an Account",
                Objective = "Create your API Combat player account. You'll receive a JWT token, 3 starter units, and a default team.",
                Endpoint = "POST /api/v1/auth/register",
                Hint = "Send JSON with username, email, and password. Save the token from the response — you'll need it for every request after this.",
                VerificationEndpoint = "GET /api/v1/player/profile",
                VerificationMethod = "GET"
            },
            new()
            {
                Title = "Log In & Get Your Token",
                Objective = "Authenticate with your email and password to receive a fresh JWT Bearer token (valid for 60 minutes).",
                Endpoint = "POST /api/v1/auth/login",
                Hint = "Use your email (not username) to log in. If your token expires mid-session, just log in again.",
                VerificationEndpoint = "POST /api/v1/auth/refresh",
                VerificationMethod = "POST"
            },
            new()
            {
                Title = "View Your Profile",
                Objective = "Fetch your player profile to see your rating, currency, units, and teams. This is your home base.",
                Endpoint = "GET /api/v1/player/profile",
                Hint = "Add the header: Authorization: Bearer <your_token>. Notice the _links in the response — they show you where to go next.",
                VerificationEndpoint = "GET /api/v1/player/profile",
                VerificationMethod = "GET"
            },
            new()
            {
                Title = "Check Your Roster",
                Objective = "View the 3 starter units you received on registration. Each has a class, stats, and abilities.",
                Endpoint = "GET /api/v1/player/units",
                Hint = "Each unit has a class (Warrior, Mage, Rogue, Healer, Tank) with different strengths. Warriors beat Rogues, Rogues beat Mages, Mages beat Warriors.",
                VerificationEndpoint = "GET /api/v1/player/units",
                VerificationMethod = "GET"
            },
            new()
            {
                Title = "Queue for Battle",
                Objective = "Enter the matchmaking queue. Your Starter Team is used automatically if you don't specify a team ID.",
                Endpoint = "POST /api/v1/battle/queue",
                Hint = "Send {\"mode\":\"casual\"} for practice or {\"mode\":\"ranked\"} for rating. Save the battleId from the response.",
                VerificationEndpoint = "GET /api/v1/battle/history",
                VerificationMethod = "GET"
            },
            new()
            {
                Title = "Check Battle Results",
                Objective = "Retrieve the full turn-by-turn combat log, rewards, and rating change from your battle.",
                Endpoint = "GET /api/v1/battle/results/{battleId}",
                Hint = "Battles resolve asynchronously — wait 10-15 seconds after queuing, then poll. The response includes a replay link and _links to queue again."
            }
        };

        var module = new CurriculumModule
        {
            Id = Guid.NewGuid(),
            InstructorId = instructor.Id,
            Title = "API Basics 101",
            Description = "Learn REST API fundamentals through gameplay. Walk through registration, authentication, team building, and your first battle in 6 hands-on lessons.",
            Difficulty = "beginner",
            LessonsJson = JsonSerializer.Serialize(lessons, JsonOptions),
            JoinCode = "BASICS01",
            IsPublished = true,
            EnrolledCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Set<CurriculumModule>().Add(module);
        await context.SaveChangesAsync();
    }
}
