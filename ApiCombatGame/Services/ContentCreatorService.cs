using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Creator;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class ContentCreatorService : IContentCreatorService
{
    private readonly GameDbContext _context;
    private readonly ILogger<ContentCreatorService> _logger;

    public ContentCreatorService(GameDbContext context, ILogger<ContentCreatorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreatorProfileResponse> ApplyAsync(Guid playerId, ApplyCreatorRequest request)
    {
        var player = await _context.Players.FindAsync(playerId)
            ?? throw new KeyNotFoundException("Player not found.");

        var existing = await _context.Set<ContentCreator>()
            .FirstOrDefaultAsync(c => c.PlayerId == playerId);

        if (existing != null)
            throw new InvalidOperationException("You already have a creator profile.");

        if (string.IsNullOrWhiteSpace(request.CreatorName))
            throw new InvalidOperationException("Creator name is required.");

        var creator = new ContentCreator
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CreatorName = request.CreatorName,
            Bio = request.Bio
        };

        _context.Set<ContentCreator>().Add(creator);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} applied as content creator: {Name}", playerId, request.CreatorName);

        return ToResponse(creator, player.Username);
    }

    public async Task<CreatorProfileResponse?> GetProfileAsync(Guid playerId)
    {
        var creator = await _context.Set<ContentCreator>()
            .Include(c => c.Player)
            .FirstOrDefaultAsync(c => c.PlayerId == playerId);

        return creator == null ? null : ToResponse(creator, creator.Player.Username);
    }

    public async Task<List<CreatorProfileResponse>> GetVerifiedCreatorsAsync()
    {
        return await _context.Set<ContentCreator>()
            .Where(c => c.IsVerified)
            .Include(c => c.Player)
            .OrderByDescending(c => c.TotalDownloads)
            .Select(c => new CreatorProfileResponse
            {
                Id = c.Id,
                CreatorName = c.CreatorName,
                Username = c.Player.Username,
                Bio = c.Bio,
                IsVerified = c.IsVerified,
                GemsEarned = c.GemsEarned,
                TotalDownloads = c.TotalDownloads,
                ModulesCreated = c.ModulesCreated,
                IsSpotlighted = c.IsSpotlighted,
                AppliedAt = c.AppliedAt
            })
            .ToListAsync();
    }

    public async Task<SpotlightResponse> GetSpotlightAsync()
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");

        var spotlighted = await _context.Set<ContentCreator>()
            .Where(c => c.IsSpotlighted && c.SpotlightMonth == currentMonth)
            .Include(c => c.Player)
            .ToListAsync();

        // If no spotlight for this month, pick top 3 verified creators by downloads
        if (spotlighted.Count == 0)
        {
            spotlighted = await _context.Set<ContentCreator>()
                .Where(c => c.IsVerified)
                .Include(c => c.Player)
                .OrderByDescending(c => c.TotalDownloads)
                .Take(3)
                .ToListAsync();

            foreach (var creator in spotlighted)
            {
                creator.IsSpotlighted = true;
                creator.SpotlightMonth = currentMonth;
            }

            if (spotlighted.Count > 0)
                await _context.SaveChangesAsync();
        }

        return new SpotlightResponse
        {
            Month = currentMonth,
            FeaturedCreators = spotlighted.Select(c => ToResponse(c, c.Player.Username)).ToList()
        };
    }

    public async Task<CreatorStatsResponse> GetCreatorStatsAsync(Guid playerId)
    {
        var creator = await _context.Set<ContentCreator>()
            .FirstOrDefaultAsync(c => c.PlayerId == playerId)
            ?? throw new KeyNotFoundException("No creator profile found.");

        var strategies = await _context.Strategies
            .Where(s => s.CreatorId == playerId)
            .ToListAsync();

        var modules = await _context.CurriculumModules
            .Where(m => m.InstructorId == playerId)
            .ToListAsync();

        var moduleIds = modules.Select(m => m.Id).ToList();
        var totalStudents = await _context.StudentEnrollments
            .CountAsync(e => moduleIds.Contains(e.ModuleId));

        // Update creator stats
        creator.TotalDownloads = strategies.Sum(s => s.DownloadCount);
        creator.ModulesCreated = modules.Count;
        await _context.SaveChangesAsync();

        return new CreatorStatsResponse
        {
            CreatorName = creator.CreatorName,
            TotalStrategiesUploaded = strategies.Count,
            TotalStrategyDownloads = creator.TotalDownloads,
            AverageStrategyRating = strategies.Count > 0
                ? Math.Round(strategies.Where(s => s.AverageRating > 0).DefaultIfEmpty().Average(s => s?.AverageRating ?? 0), 1)
                : 0,
            ModulesCreated = modules.Count,
            StudentsEnrolled = totalStudents,
            GemsEarned = creator.GemsEarned,
            IsVerified = creator.IsVerified
        };
    }

    public async Task VerifyCreatorAsync(Guid creatorId)
    {
        var creator = await _context.Set<ContentCreator>().FindAsync(creatorId)
            ?? throw new KeyNotFoundException("Creator not found.");

        creator.IsVerified = true;
        creator.VerifiedAt = DateTime.UtcNow;

        // Grant the "creator" badge to the player
        var player = await _context.Players.FindAsync(creator.PlayerId);
        if (player != null && string.IsNullOrEmpty(player.Badge))
        {
            player.Badge = "creator";
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Creator {CreatorId} verified for player {PlayerId}", creatorId, creator.PlayerId);
    }

    private static CreatorProfileResponse ToResponse(ContentCreator c, string username) => new()
    {
        Id = c.Id,
        CreatorName = c.CreatorName,
        Username = username,
        Bio = c.Bio,
        IsVerified = c.IsVerified,
        GemsEarned = c.GemsEarned,
        TotalDownloads = c.TotalDownloads,
        ModulesCreated = c.ModulesCreated,
        IsSpotlighted = c.IsSpotlighted,
        AppliedAt = c.AppliedAt
    };
}
