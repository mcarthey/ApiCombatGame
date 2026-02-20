using ApiCombatGame.Models.DTOs.Education;

namespace ApiCombatGame.Services.Interfaces;

public interface IEducationService
{
    Task<CurriculumModuleResponse> CreateModuleAsync(Guid instructorId, CreateModuleRequest request);
    Task<List<CurriculumModuleResponse>> GetPublicModulesAsync();
    Task<ModuleDetailResponse> GetModuleDetailAsync(Guid moduleId, Guid? playerId);
    Task<InstructorDashboardResponse> GetInstructorDashboardAsync(Guid instructorId);
    Task<EnrollmentProgressDto> EnrollAsync(Guid playerId, Guid moduleId);
    Task<EnrollmentProgressDto> EnrollByCodeAsync(Guid playerId, string joinCode);
    Task<EnrollmentProgressDto> CompleteLessonAsync(Guid playerId, Guid moduleId, int lessonIndex);
    Task PublishModuleAsync(Guid instructorId, Guid moduleId);
    Task<List<EnrollmentProgressDto>> GetMyEnrollmentsAsync(Guid playerId);
    Task<List<ClassLeaderboardEntry>> GetModuleLeaderboardAsync(Guid moduleId, Guid playerId);
    Task<Guid> CreateClassTournamentAsync(Guid instructorId, Guid moduleId, CreateClassTournamentRequest request);
    Task UnenrollAsync(Guid playerId, Guid moduleId);
}
