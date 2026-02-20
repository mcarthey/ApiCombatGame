using System.Security.Claims;
using ApiCombatGame.Filters.Attributes;
using ApiCombatGame.Models.DTOs.Education;
using ApiCombatGame.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiCombatGame.Controllers.Api;

/// <summary>
/// Educational platform for teaching API concepts through gameplay.
/// </summary>
[ApiController]
[Route("api/v1/education")]
[Authorize]
[Tags("Education")]
[ApiCategoryMeta("book", "#3b82f6", Order = 25)]
public class EducationController : ControllerBase
{
    private readonly IEducationService _educationService;

    public EducationController(IEducationService educationService)
    {
        _educationService = educationService;
    }

    /// <summary>Browse published curriculum modules.</summary>
    /// <remarks>
    /// Lists all published educational modules, sorted by popularity. Each module
    /// teaches API concepts through guided game challenges — from basic REST to
    /// advanced strategies.
    /// </remarks>
    /// <response code="200">List of published modules.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Start with a beginner module to learn the game's API endpoints step by step.")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("modules")]
    [ProducesResponseType(typeof(List<CurriculumModuleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CurriculumModuleResponse>>> GetModules()
    {
        var modules = await _educationService.GetPublicModulesAsync();
        return Ok(modules);
    }

    /// <summary>Get detailed module info and your progress.</summary>
    /// <param name="moduleId">Module ID.</param>
    /// <response code="200">Module details with lessons and your progress.</response>
    /// <response code="404">Module not found.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpGet("modules/{moduleId:guid}")]
    [ProducesResponseType(typeof(ModuleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleDetailResponse>> GetModuleDetail(Guid moduleId)
    {
        try
        {
            var playerId = GetPlayerId();
            var detail = await _educationService.GetModuleDetailAsync(moduleId, playerId);
            return Ok(detail);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Create a curriculum module (instructor).</summary>
    /// <remarks>
    /// Requires educator access: a verified .edu email address or admin-granted educator status.
    /// Define lessons that guide students through specific API endpoints. The module starts
    /// as unpublished — publish it when you're ready for students to enroll.
    /// </remarks>
    /// <param name="request">Module definition with lessons.</param>
    /// <response code="201">Module created. Use the join code or publish it for public access.</response>
    /// <response code="400">Invalid module data.</response>
    /// <response code="403">Not an educator. Requires verified .edu email or admin-granted educator status.</response>
    [ApiDifficulty("intermediate")]
    [ApiGameTip("Structure lessons from simple (register, login) to complex (strategies, battles) for best learning flow.")]
    [ApiPrerequisite("Register and login")]
    [ApiExample("Create a beginner module", Request = "{\n  \"title\": \"API Basics 101\",\n  \"description\": \"Learn REST fundamentals through combat\",\n  \"difficulty\": \"beginner\",\n  \"lessons\": [\n    { \"title\": \"Register\", \"objective\": \"Create your first account\", \"endpoint\": \"POST /api/v1/auth/register\", \"hint\": \"Use a unique username and valid email\" },\n    { \"title\": \"Login\", \"objective\": \"Get your JWT token\", \"endpoint\": \"POST /api/v1/auth/login\", \"hint\": \"Save the token for future requests\" }\n  ]\n}")]
    [HttpPost("modules")]
    [ProducesResponseType(typeof(CurriculumModuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CurriculumModuleResponse>> CreateModule([FromBody] CreateModuleRequest request)
    {
        try
        {
            var playerId = GetPlayerId();
            var module = await _educationService.CreateModuleAsync(playerId, request);
            return Created($"/api/v1/education/modules/{module.Id}", module);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Publish a module for public access.</summary>
    /// <param name="moduleId">Module ID to publish.</param>
    /// <response code="200">Module published.</response>
    /// <response code="403">Not an educator.</response>
    /// <response code="404">Module not found or you are not the instructor.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Create a module")]
    [HttpPost("modules/{moduleId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PublishModule(Guid moduleId)
    {
        try
        {
            var playerId = GetPlayerId();
            await _educationService.PublishModuleAsync(playerId, moduleId);
            return Ok(new { message = "Module published and visible to all players." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Enroll in a module.</summary>
    /// <param name="moduleId">Module ID to enroll in.</param>
    /// <response code="200">Enrolled successfully.</response>
    /// <response code="400">Already enrolled or module not published.</response>
    /// <response code="404">Module not found.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpPost("enroll/{moduleId:guid}")]
    [ProducesResponseType(typeof(EnrollmentProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentProgressDto>> Enroll(Guid moduleId)
    {
        try
        {
            var playerId = GetPlayerId();
            var progress = await _educationService.EnrollAsync(playerId, moduleId);
            return Ok(progress);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Enroll using a join code.</summary>
    /// <param name="code">The instructor's join code.</param>
    /// <response code="200">Enrolled successfully.</response>
    /// <response code="404">Invalid join code.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Register and login")]
    [HttpPost("enroll/code/{code}")]
    [ProducesResponseType(typeof(EnrollmentProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentProgressDto>> EnrollByCode(string code)
    {
        try
        {
            var playerId = GetPlayerId();
            var progress = await _educationService.EnrollByCodeAsync(playerId, code);
            return Ok(progress);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Mark a lesson as complete.</summary>
    /// <param name="moduleId">Module ID.</param>
    /// <param name="lessonIndex">0-based lesson index.</param>
    /// <response code="200">Lesson completed. Progress updated.</response>
    /// <response code="400">Lesson already completed or invalid index.</response>
    /// <response code="404">Not enrolled.</response>
    [ApiDifficulty("beginner")]
    [ApiGameTip("Complete lessons in order for the best learning experience, but you can skip ahead if you already know the concepts.")]
    [ApiPrerequisite("Enroll in a module")]
    [HttpPost("modules/{moduleId:guid}/lessons/{lessonIndex:int}/complete")]
    [ProducesResponseType(typeof(EnrollmentProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentProgressDto>> CompleteLesson(Guid moduleId, int lessonIndex)
    {
        try
        {
            var playerId = GetPlayerId();
            var progress = await _educationService.CompleteLessonAsync(playerId, moduleId, lessonIndex);
            return Ok(progress);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get your enrolled modules and progress.</summary>
    /// <response code="200">List of enrollments with progress.</response>
    [ApiDifficulty("beginner")]
    [ApiPrerequisite("Enroll in at least one module")]
    [HttpGet("my-progress")]
    [ProducesResponseType(typeof(List<EnrollmentProgressDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EnrollmentProgressDto>>> GetMyProgress()
    {
        var playerId = GetPlayerId();
        var enrollments = await _educationService.GetMyEnrollmentsAsync(playerId);
        return Ok(enrollments);
    }

    /// <summary>Instructor dashboard with student analytics.</summary>
    /// <remarks>
    /// View stats for all modules you've created: enrollment counts, completion rates,
    /// and average student progress. Use this data to improve your curriculum.
    /// </remarks>
    /// <response code="200">Dashboard with module stats.</response>
    /// <response code="403">Not an educator.</response>
    [ApiDifficulty("intermediate")]
    [ApiPrerequisite("Create at least one module")]
    [HttpGet("instructor/dashboard")]
    [ProducesResponseType(typeof(InstructorDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstructorDashboardResponse>> GetInstructorDashboard()
    {
        try
        {
            var playerId = GetPlayerId();
            var dashboard = await _educationService.GetInstructorDashboardAsync(playerId);
            return Ok(dashboard);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }

    private Guid GetPlayerId()
    {
        var claim = User.FindFirst("PlayerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var playerId))
            throw new UnauthorizedAccessException("Invalid token.");
        return playerId;
    }
}
