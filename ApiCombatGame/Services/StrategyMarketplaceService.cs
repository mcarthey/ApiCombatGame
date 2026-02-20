using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class StrategyMarketplaceService : IStrategyMarketplaceService
{
    private readonly GameDbContext _context;
    private readonly IActivityLedger _ledger;
    private readonly INotificationService _notifications;
    private readonly ILogger<StrategyMarketplaceService> _logger;

    public StrategyMarketplaceService(GameDbContext context, IActivityLedger ledger, INotificationService notifications, ILogger<StrategyMarketplaceService> logger)
    {
        _context = context;
        _ledger = ledger;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<Strategy> UploadStrategy(Guid playerId, string name, string description, string strategyJson, int price)
    {
        var strategy = new Strategy
        {
            Id = Guid.NewGuid(),
            CreatorId = playerId,
            Name = name,
            Description = description,
            StrategyJson = strategyJson,
            Price = Math.Max(0, price),
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastDecayUpdate = DateTime.UtcNow
        };

        _context.Strategies.Add(strategy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} uploaded strategy {StrategyId}: {Name}",
            playerId, strategy.Id, name);

        return strategy;
    }

    public async Task<List<Strategy>> BrowseStrategies(string sortBy, int limit, int offset)
    {
        limit = Math.Clamp(limit, 1, 50);
        offset = Math.Max(0, offset);

        var query = _context.Strategies.Where(s => s.IsPublic);

        query = sortBy switch
        {
            "popular" => query.OrderByDescending(s => s.DownloadCount),
            "rating" => query.OrderByDescending(s => s.AverageRating),
            "recent" => query.OrderByDescending(s => s.CreatedAt),
            "winrate" => query.OrderByDescending(s =>
                s.WinCount + s.LossCount > 0
                    ? (double)s.WinCount / (s.WinCount + s.LossCount)
                    : 0),
            _ => query.OrderByDescending(s => s.DownloadCount)
        };

        return await query
            .Skip(offset)
            .Take(limit)
            .Include(s => s.Creator)
            .ToListAsync();
    }

    public async Task<Strategy> DownloadStrategy(Guid strategyId, Guid playerId)
    {
        var strategy = await _context.Strategies
            .Include(s => s.Creator)
            .FirstOrDefaultAsync(s => s.Id == strategyId);

        if (strategy == null)
            throw new KeyNotFoundException("Strategy not found");

        // Check if strategy costs currency
        if (strategy.Price > 0 && strategy.CreatorId != playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
                throw new KeyNotFoundException("Player not found");

            if (player.Currency < strategy.Price)
                throw new InvalidOperationException("Not enough currency to download this strategy");

            var oldBuyerCurrency = player.Currency;
            player.Currency -= strategy.Price;
            _ledger.LogPlayer(playerId, "Currency", oldBuyerCurrency, player.Currency, "Strategy", "StrategyPurchased", strategyId);

            // Creator earns 80% of the price
            var creator = await _context.Players.FindAsync(strategy.CreatorId);
            if (creator != null)
            {
                var oldCreatorCurrency = creator.Currency;
                creator.Currency += (int)(strategy.Price * 0.8);
                _ledger.LogPlayer(strategy.CreatorId, "Currency", oldCreatorCurrency, creator.Currency, "Strategy", "StrategySale", strategyId);
            }
        }

        strategy.DownloadCount++;

        // Download milestone notifications
        int[] milestones = [10, 50, 100, 250, 500, 1000];
        if (milestones.Contains(strategy.DownloadCount))
        {
            await _notifications.SendAsync(strategy.CreatorId, NotificationType.StrategyDownloadMilestone,
                $"{strategy.DownloadCount} Downloads!", $"Your strategy \"{strategy.Name}\" hit {strategy.DownloadCount} downloads!");
        }

        await _context.SaveChangesAsync();

        return strategy;
    }

    public async Task RateStrategy(Guid strategyId, Guid playerId, int rating, string comment)
    {
        rating = Math.Clamp(rating, 1, 5);

        var strategy = await _context.Strategies
            .Include(s => s.Ratings)
            .FirstOrDefaultAsync(s => s.Id == strategyId);

        if (strategy == null)
            throw new KeyNotFoundException("Strategy not found");

        // Check if player already rated
        var existingRating = strategy.Ratings.FirstOrDefault(r => r.PlayerId == playerId);
        if (existingRating != null)
        {
            existingRating.Rating = rating;
            existingRating.Comment = comment;
        }
        else
        {
            strategy.Ratings.Add(new StrategyRating
            {
                Id = Guid.NewGuid(),
                StrategyId = strategyId,
                PlayerId = playerId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Recalculate average
        strategy.AverageRating = strategy.Ratings.Average(r => r.Rating);
        strategy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notify strategy creator about the rating
        if (strategy.CreatorId != playerId)
        {
            await _notifications.SendAsync(strategy.CreatorId, NotificationType.StrategyRated,
                "Strategy Rated!", $"Someone rated your strategy \"{strategy.Name}\" {rating}/5 stars.");
        }
    }

    public async Task ApplyStrategyDecay()
    {
        var strategies = await _context.Strategies
            .Where(s => s.IsPublic)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var strategy in strategies)
        {
            var ageInWeeks = (now - strategy.CreatedAt).TotalDays / 7.0;
            // Decay formula: effectiveness decreases 5% per week, minimum 50%
            strategy.EffectivenessMultiplier = Math.Max(0.5, 1.0 - (ageInWeeks * 0.05));
            strategy.LastDecayUpdate = now;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Applied strategy decay to {Count} strategies", strategies.Count);
    }
}
