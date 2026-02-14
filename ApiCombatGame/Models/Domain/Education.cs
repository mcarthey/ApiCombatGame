using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

/// <summary>
/// A curriculum module that instructors create to teach API concepts through game challenges.
/// </summary>
public class CurriculumModule
{
    [Key]
    public Guid Id { get; set; }

    public Guid InstructorId { get; set; }
    public Player Instructor { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>"beginner", "intermediate", "advanced"</summary>
    [MaxLength(20)]
    public string Difficulty { get; set; } = "beginner";

    /// <summary>Ordered lessons as JSON array: [{ "title": "...", "objective": "...", "endpoint": "POST /api/v1/auth/register", "hint": "..." }]</summary>
    public string LessonsJson { get; set; } = "[]";

    /// <summary>Optional join code for students.</summary>
    [MaxLength(20)]
    public string? JoinCode { get; set; }

    public bool IsPublished { get; set; } = false;
    public int EnrolledCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tracks a student's enrollment and progress through a curriculum module.
/// </summary>
public class StudentEnrollment
{
    [Key]
    public Guid Id { get; set; }

    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public Guid ModuleId { get; set; }
    public CurriculumModule Module { get; set; } = null!;

    /// <summary>0-based index of the current lesson the student is on.</summary>
    public int CurrentLesson { get; set; } = 0;

    /// <summary>Total lessons completed.</summary>
    public int LessonsCompleted { get; set; } = 0;

    /// <summary>JSON array of completed lesson indices.</summary>
    public string CompletedLessonsJson { get; set; } = "[]";

    public bool IsCompleted { get; set; } = false;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
