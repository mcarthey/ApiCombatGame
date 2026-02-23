using System.Text.Json;
using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Education;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class EducationService : IEducationService
{
    private readonly GameDbContext _context;
    private readonly ILogger<EducationService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public EducationService(GameDbContext context, ILogger<EducationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CurriculumModuleResponse> CreateModuleAsync(Guid instructorId, CreateModuleRequest request)
    {
        var instructor = await RequireEducatorAsync(instructorId);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Module title is required.");

        if (request.Lessons.Count == 0)
            throw new InvalidOperationException("At least one lesson is required.");

        var module = new CurriculumModule
        {
            Id = Guid.NewGuid(),
            InstructorId = instructorId,
            Title = request.Title,
            Description = request.Description,
            Difficulty = request.Difficulty,
            LessonsJson = JsonSerializer.Serialize(request.Lessons, JsonOptions),
            JoinCode = GenerateJoinCode()
        };

        _context.Set<CurriculumModule>().Add(module);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Instructor {InstructorId} created module {ModuleId}: {Title}",
            instructorId, module.Id, module.Title);

        return ToResponse(module, instructor.Username);
    }

    public async Task<List<CurriculumModuleResponse>> GetPublicModulesAsync()
    {
        return await _context.Set<CurriculumModule>()
            .AsNoTracking()
            .Where(m => m.IsPublished)
            .Include(m => m.Instructor)
            .OrderByDescending(m => m.EnrolledCount)
            .Select(m => new CurriculumModuleResponse
            {
                Id = m.Id,
                InstructorUsername = m.Instructor.Username,
                Title = m.Title,
                Description = m.Description,
                Difficulty = m.Difficulty,
                LessonCount = m.LessonsJson.Length > 2 ? CountLessons(m.LessonsJson) : 0,
                EnrolledCount = m.EnrolledCount,
                IsPublished = m.IsPublished,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ModuleDetailResponse> GetModuleDetailAsync(Guid moduleId, Guid? playerId)
    {
        var module = await _context.Set<CurriculumModule>()
            .Include(m => m.Instructor)
            .FirstOrDefaultAsync(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException("Module not found.");

        var lessons = DeserializeLessons(module.LessonsJson);

        EnrollmentProgressDto? progress = null;
        if (playerId.HasValue)
        {
            var enrollment = await _context.Set<StudentEnrollment>()
                .FirstOrDefaultAsync(e => e.PlayerId == playerId.Value && e.ModuleId == moduleId);
            if (enrollment != null)
            {
                progress = new EnrollmentProgressDto
                {
                    CurrentLesson = enrollment.CurrentLesson,
                    LessonsCompleted = enrollment.LessonsCompleted,
                    TotalLessons = lessons.Count,
                    ProgressPercent = lessons.Count > 0 ? Math.Round((double)enrollment.LessonsCompleted / lessons.Count * 100, 1) : 0,
                    IsCompleted = enrollment.IsCompleted
                };
            }
        }

        return new ModuleDetailResponse
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            Difficulty = module.Difficulty,
            Lessons = lessons.Select((l, i) => new LessonDto
            {
                Index = i,
                Title = l.Title,
                Objective = l.Objective,
                Endpoint = l.Endpoint,
                Hint = l.Hint,
                VerificationEndpoint = l.VerificationEndpoint,
                VerificationMethod = l.VerificationMethod
            }).ToList(),
            MyProgress = progress
        };
    }

    public async Task<InstructorDashboardResponse> GetInstructorDashboardAsync(Guid instructorId)
    {
        await RequireEducatorAsync(instructorId);

        var modules = await _context.Set<CurriculumModule>()
            .Where(m => m.InstructorId == instructorId)
            .ToListAsync();

        var moduleIds = modules.Select(m => m.Id).ToList();
        var enrollments = await _context.Set<StudentEnrollment>()
            .Where(e => moduleIds.Contains(e.ModuleId))
            .ToListAsync();

        var moduleStats = modules.Select(m =>
        {
            var moduleEnrollments = enrollments.Where(e => e.ModuleId == m.Id).ToList();
            var lessonCount = CountLessons(m.LessonsJson);
            return new ModuleStatsDto
            {
                Id = m.Id,
                Title = m.Title,
                EnrolledCount = moduleEnrollments.Count,
                CompletedCount = moduleEnrollments.Count(e => e.IsCompleted),
                AverageProgress = moduleEnrollments.Count > 0 && lessonCount > 0
                    ? Math.Round(moduleEnrollments.Average(e => (double)e.LessonsCompleted / lessonCount * 100), 1)
                    : 0
            };
        }).ToList();

        return new InstructorDashboardResponse
        {
            TotalModules = modules.Count,
            PublishedModules = modules.Count(m => m.IsPublished),
            TotalStudents = enrollments.Select(e => e.PlayerId).Distinct().Count(),
            StudentsCompleted = enrollments.Count(e => e.IsCompleted),
            Modules = moduleStats
        };
    }

    public async Task<EnrollmentProgressDto> EnrollAsync(Guid playerId, Guid moduleId)
    {
        var module = await _context.Set<CurriculumModule>().FindAsync(moduleId)
            ?? throw new KeyNotFoundException("Module not found.");

        if (!module.IsPublished)
            throw new InvalidOperationException("This module is not yet published.");

        var existing = await _context.Set<StudentEnrollment>()
            .FirstOrDefaultAsync(e => e.PlayerId == playerId && e.ModuleId == moduleId);

        if (existing != null)
            throw new InvalidOperationException("Already enrolled in this module.");

        var lessonCount = CountLessons(module.LessonsJson);
        var enrollment = new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            ModuleId = moduleId
        };

        module.EnrolledCount++;
        _context.Set<StudentEnrollment>().Add(enrollment);
        await _context.SaveChangesAsync();

        return new EnrollmentProgressDto
        {
            CurrentLesson = 0,
            LessonsCompleted = 0,
            TotalLessons = lessonCount,
            ProgressPercent = 0,
            IsCompleted = false
        };
    }

    public async Task<EnrollmentProgressDto> EnrollByCodeAsync(Guid playerId, string joinCode)
    {
        var module = await _context.Set<CurriculumModule>()
            .FirstOrDefaultAsync(m => m.JoinCode == joinCode)
            ?? throw new KeyNotFoundException("Invalid join code.");

        return await EnrollAsync(playerId, module.Id);
    }

    public async Task<EnrollmentProgressDto> CompleteLessonAsync(Guid playerId, Guid moduleId, int lessonIndex)
    {
        var enrollment = await _context.Set<StudentEnrollment>()
            .FirstOrDefaultAsync(e => e.PlayerId == playerId && e.ModuleId == moduleId)
            ?? throw new KeyNotFoundException("Not enrolled in this module.");

        if (enrollment.IsCompleted)
            throw new InvalidOperationException("Module already completed.");

        var module = await _context.Set<CurriculumModule>().FindAsync(moduleId)
            ?? throw new KeyNotFoundException("Module not found.");

        var lessonCount = CountLessons(module.LessonsJson);
        if (lessonIndex < 0 || lessonIndex >= lessonCount)
            throw new InvalidOperationException($"Invalid lesson index. Module has {lessonCount} lessons (0-{lessonCount - 1}).");

        var completed = DeserializeCompletedLessons(enrollment.CompletedLessonsJson);
        if (completed.Contains(lessonIndex))
            throw new InvalidOperationException("Lesson already completed.");

        completed.Add(lessonIndex);
        enrollment.CompletedLessonsJson = JsonSerializer.Serialize(completed);
        enrollment.LessonsCompleted = completed.Count;
        enrollment.CurrentLesson = Math.Min(lessonIndex + 1, lessonCount - 1);

        if (completed.Count >= lessonCount)
        {
            enrollment.IsCompleted = true;
            enrollment.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new EnrollmentProgressDto
        {
            CurrentLesson = enrollment.CurrentLesson,
            LessonsCompleted = enrollment.LessonsCompleted,
            TotalLessons = lessonCount,
            ProgressPercent = lessonCount > 0 ? Math.Round((double)completed.Count / lessonCount * 100, 1) : 0,
            IsCompleted = enrollment.IsCompleted
        };
    }

    public async Task PublishModuleAsync(Guid instructorId, Guid moduleId)
    {
        await RequireEducatorAsync(instructorId);

        var module = await _context.Set<CurriculumModule>()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.InstructorId == instructorId)
            ?? throw new KeyNotFoundException("Module not found or you are not the instructor.");

        module.IsPublished = true;
        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<List<EnrollmentProgressDto>> GetMyEnrollmentsAsync(Guid playerId)
    {
        var enrollments = await _context.Set<StudentEnrollment>()
            .Where(e => e.PlayerId == playerId)
            .Include(e => e.Module)
            .ToListAsync();

        return enrollments.Select(e =>
        {
            var lessonCount = CountLessons(e.Module.LessonsJson);
            return new EnrollmentProgressDto
            {
                CurrentLesson = e.CurrentLesson,
                LessonsCompleted = e.LessonsCompleted,
                TotalLessons = lessonCount,
                ProgressPercent = lessonCount > 0 ? Math.Round((double)e.LessonsCompleted / lessonCount * 100, 1) : 0,
                IsCompleted = e.IsCompleted
            };
        }).ToList();
    }

    public async Task<List<ClassLeaderboardEntry>> GetModuleLeaderboardAsync(Guid moduleId, Guid playerId)
    {
        var module = await _context.Set<CurriculumModule>().FindAsync(moduleId)
            ?? throw new KeyNotFoundException("Module not found.");

        // Verify the requester is enrolled or is the instructor
        var isInstructor = module.InstructorId == playerId;
        if (!isInstructor)
        {
            var enrolled = await _context.Set<StudentEnrollment>()
                .AnyAsync(e => e.PlayerId == playerId && e.ModuleId == moduleId);
            if (!enrolled)
                throw new InvalidOperationException("You must be enrolled in this module or be the instructor to view the leaderboard.");
        }

        var enrollments = await _context.Set<StudentEnrollment>()
            .AsNoTracking()
            .Where(e => e.ModuleId == moduleId)
            .Include(e => e.Player)
            .ToListAsync();

        var leaderboard = enrollments
            .Select(e => new ClassLeaderboardEntry
            {
                Username = e.Player.Username,
                Rating = e.Player.Rating,
                Wins = e.Player.TotalBattlesWon,
                Losses = e.Player.TotalBattlesPlayed - e.Player.TotalBattlesWon,
                WinRate = e.Player.TotalBattlesPlayed > 0
                    ? Math.Round((double)e.Player.TotalBattlesWon / e.Player.TotalBattlesPlayed * 100, 1)
                    : 0,
                LessonsCompleted = e.LessonsCompleted
            })
            .OrderByDescending(e => e.Rating)
            .Select((e, i) => { e.Rank = i + 1; return e; })
            .ToList();

        return leaderboard;
    }

    public async Task<Guid> CreateClassTournamentAsync(Guid instructorId, Guid moduleId, CreateClassTournamentRequest request)
    {
        var instructor = await RequireEducatorAsync(instructorId);

        var module = await _context.Set<CurriculumModule>()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.InstructorId == instructorId)
            ?? throw new KeyNotFoundException("Module not found or you are not the instructor.");

        var tournament = new Models.Domain.Tournament
        {
            Id = Guid.NewGuid(),
            Name = $"Class Tournament: {module.Title}",
            Status = "registration",
            MaxParticipants = Math.Clamp(request.MaxParticipants, 4, 32),
            EntryFee = Math.Max(0, request.EntryFee),
            ModuleId = moduleId,
            RegistrationOpens = DateTime.UtcNow,
            StartsAt = DateTime.UtcNow.AddHours(24),
            PrizePoolJson = "[]"
        };

        _context.Set<Models.Domain.Tournament>().Add(tournament);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Instructor {InstructorId} created class tournament {TournamentId} for module {ModuleId}",
            instructorId, tournament.Id, moduleId);

        return tournament.Id;
    }

    public async Task UnenrollAsync(Guid playerId, Guid moduleId)
    {
        var enrollment = await _context.Set<StudentEnrollment>()
            .FirstOrDefaultAsync(e => e.PlayerId == playerId && e.ModuleId == moduleId)
            ?? throw new KeyNotFoundException("Not enrolled in this module.");

        var module = await _context.Set<CurriculumModule>().FindAsync(moduleId);
        if (module != null && module.EnrolledCount > 0)
            module.EnrolledCount--;

        _context.Set<StudentEnrollment>().Remove(enrollment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} unenrolled from module {ModuleId}", playerId, moduleId);
    }

    private async Task<Player> RequireEducatorAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        var isVerifiedEduEmail = player.Email.EndsWith(".edu", StringComparison.OrdinalIgnoreCase)
                              && player.EmailConfirmed;

        if (!isVerifiedEduEmail && !player.IsEducator)
            throw new UnauthorizedAccessException(
                "Educator access requires a verified .edu email address or educator status granted by an administrator. "
                + "If you're an educator at an accredited institution without a .edu domain, please contact us.");

        return player;
    }

    private static CurriculumModuleResponse ToResponse(CurriculumModule m, string instructorUsername) => new()
    {
        Id = m.Id,
        InstructorUsername = instructorUsername,
        Title = m.Title,
        Description = m.Description,
        Difficulty = m.Difficulty,
        LessonCount = CountLessons(m.LessonsJson),
        EnrolledCount = m.EnrolledCount,
        IsPublished = m.IsPublished,
        JoinCode = m.JoinCode,
        CreatedAt = m.CreatedAt
    };

    private static int CountLessons(string json)
    {
        try { return JsonSerializer.Deserialize<List<CreateLessonRequest>>(json, JsonOptions)?.Count ?? 0; }
        catch (JsonException) { return 0; }
    }

    private List<LessonData> DeserializeLessons(string json)
    {
        try { return JsonSerializer.Deserialize<List<LessonData>>(json, JsonOptions) ?? new(); }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize lessons JSON");
            return new();
        }
    }

    private List<int> DeserializeCompletedLessons(string json)
    {
        try { return JsonSerializer.Deserialize<List<int>>(json) ?? new(); }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize completed lessons JSON");
            return new();
        }
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private class LessonData
    {
        public string Title { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string? Hint { get; set; }
        public string? VerificationEndpoint { get; set; }
        public string? VerificationMethod { get; set; }
    }
}
