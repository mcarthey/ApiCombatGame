namespace ApiCombatGame.Models.DTOs.Education;

public class CurriculumModuleResponse
{
    public Guid Id { get; set; }
    public string InstructorUsername { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int LessonCount { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsPublished { get; set; }
    public string? JoinCode { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ModuleDetailResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<LessonDto> Lessons { get; set; } = new();
    public EnrollmentProgressDto? MyProgress { get; set; }
}

public class LessonDto
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public string? VerificationEndpoint { get; set; }
    public string? VerificationMethod { get; set; }
}

public class EnrollmentProgressDto
{
    public int CurrentLesson { get; set; }
    public int LessonsCompleted { get; set; }
    public int TotalLessons { get; set; }
    public double ProgressPercent { get; set; }
    public bool IsCompleted { get; set; }
}

public class CreateModuleRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "beginner";
    public List<CreateLessonRequest> Lessons { get; set; } = new();
}

public class CreateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? Hint { get; set; }
    /// <summary>If set, calling this endpoint successfully auto-completes the lesson.</summary>
    public string? VerificationEndpoint { get; set; }
    /// <summary>HTTP method for verification (GET, POST, PUT, DELETE). Defaults to the endpoint's method.</summary>
    public string? VerificationMethod { get; set; }
}

public class InstructorDashboardResponse
{
    public int TotalModules { get; set; }
    public int PublishedModules { get; set; }
    public int TotalStudents { get; set; }
    public int StudentsCompleted { get; set; }
    public List<ModuleStatsDto> Modules { get; set; } = new();
}

public class ModuleStatsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int EnrolledCount { get; set; }
    public int CompletedCount { get; set; }
    public double AverageProgress { get; set; }
}

public class ClassLeaderboardEntry
{
    public int Rank { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Rating { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public int LessonsCompleted { get; set; }
}

public class BatchPracticeRequest
{
    public Guid TeamId { get; set; }
    public string? OpponentId { get; set; }
    public int Count { get; set; } = 10;
}

public class BatchPracticeResponse
{
    public int TotalBattles { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public double AvgTurns { get; set; }
    public string OpponentName { get; set; } = string.Empty;
}

public class CreateClassTournamentRequest
{
    public int EntryFee { get; set; } = 0;
    public int MaxParticipants { get; set; } = 16;
}
