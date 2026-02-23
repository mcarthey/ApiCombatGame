using ApiCombatGame.Data;
using ApiCombatGame.Models.Domain;
using ApiCombatGame.Models.DTOs.Cosmetic;
using ApiCombatGame.Models.Enums;
using ApiCombatGame.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiCombatGame.Services;

public class CosmeticService : ICosmeticService
{
    private readonly GameDbContext _context;
    private readonly IActivityLedger _ledger;
    private readonly ILogger<CosmeticService> _logger;

    public CosmeticService(GameDbContext context, IActivityLedger ledger, ILogger<CosmeticService> logger)
    {
        _context = context;
        _ledger = ledger;
        _logger = logger;
    }

    public async Task<CosmeticShopResponse> GetShopAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) throw new KeyNotFoundException("Player not found.");

        await EnsureShopItemsExistAsync();

        var items = await _context.Set<CosmeticItem>()
            .AsNoTracking()
            .Where(c => c.IsAvailable)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.GemPrice)
            .ToListAsync();

        var ownedList = await _context.Set<PlayerCosmetic>()
            .Where(pc => pc.PlayerId == playerId)
            .Select(pc => pc.CosmeticItemId)
            .ToListAsync();
        var ownedIds = new HashSet<Guid>(ownedList);

        return new CosmeticShopResponse
        {
            PlayerGems = player.Gems,
            PlayerGold = player.Currency,
            Items = items.Select(i => new CosmeticShopItem
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Category = i.Category,
                Rarity = i.Rarity,
                GemPrice = i.GemPrice,
                GoldPrice = i.GoldPrice,
                IsLimited = i.IsLimited,
                RequiredTier = i.RequiredTier,
                Locked = i.RequiredTier != null && !MeetsTierRequirement(player.CurrentTier, i.RequiredTier),
                Owned = ownedIds.Contains(i.Id)
            }).ToList()
        };
    }

    public async Task<List<PlayerCosmeticResponse>> GetOwnedCosmeticsAsync(Guid playerId)
    {
        return await _context.Set<PlayerCosmetic>()
            .Where(pc => pc.PlayerId == playerId)
            .Include(pc => pc.CosmeticItem)
            .Select(pc => new PlayerCosmeticResponse
            {
                CosmeticId = pc.CosmeticItemId,
                Name = pc.CosmeticItem.Name,
                Category = pc.CosmeticItem.Category,
                Rarity = pc.CosmeticItem.Rarity,
                IsEquipped = pc.IsEquipped
            })
            .ToListAsync();
    }

    public async Task<PlayerCosmeticResponse> PurchaseCosmeticAsync(Guid playerId, Guid cosmeticId, string paymentMethod)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) throw new KeyNotFoundException("Player not found.");

        var item = await _context.Set<CosmeticItem>().FindAsync(cosmeticId);
        if (item == null) throw new KeyNotFoundException("Cosmetic item not found.");

        if (!item.IsAvailable)
            throw new InvalidOperationException("This item is no longer available.");

        if (item.RequiredTier != null && !MeetsTierRequirement(player.CurrentTier, item.RequiredTier))
            throw new InvalidOperationException($"This exclusive item requires {item.RequiredTier} subscription tier.");

        var alreadyOwned = await _context.Set<PlayerCosmetic>()
            .AnyAsync(pc => pc.PlayerId == playerId && pc.CosmeticItemId == cosmeticId);

        if (alreadyOwned)
            throw new InvalidOperationException("You already own this cosmetic.");

        if (paymentMethod == "gems")
        {
            if (item.GemPrice <= 0)
                throw new InvalidOperationException("This item cannot be purchased with gems.");
            if (player.Gems < item.GemPrice)
                throw new InvalidOperationException($"Insufficient gems. Need {item.GemPrice}, have {player.Gems}.");

            var oldGems = player.Gems;
            player.Gems -= item.GemPrice;
            player.GemsSpentTotal += item.GemPrice;
            _ledger.LogPlayer(playerId, "Gems", oldGems, player.Gems, "Cosmetic", "CosmeticPurchased", cosmeticId);
        }
        else if (paymentMethod == "gold")
        {
            if (item.GoldPrice <= 0)
                throw new InvalidOperationException("This item cannot be purchased with gold.");
            if (player.Currency < item.GoldPrice)
                throw new InvalidOperationException($"Insufficient gold. Need {item.GoldPrice}, have {player.Currency}.");

            var oldCurrency = player.Currency;
            player.Currency -= item.GoldPrice;
            _ledger.LogPlayer(playerId, "Currency", oldCurrency, player.Currency, "Cosmetic", "CosmeticPurchased", cosmeticId);
        }
        else
        {
            throw new InvalidOperationException("Invalid payment method. Use 'gems' or 'gold'.");
        }

        var cosmetic = new PlayerCosmetic
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CosmeticItemId = cosmeticId
        };

        _context.Set<PlayerCosmetic>().Add(cosmetic);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player {PlayerId} purchased cosmetic {CosmeticId} with {Method}",
            playerId, cosmeticId, paymentMethod);

        return new PlayerCosmeticResponse
        {
            CosmeticId = cosmeticId,
            Name = item.Name,
            Category = item.Category,
            Rarity = item.Rarity,
            IsEquipped = false
        };
    }

    public async Task EquipCosmeticAsync(Guid playerId, Guid cosmeticId)
    {
        var playerCosmetic = await _context.Set<PlayerCosmetic>()
            .Include(pc => pc.CosmeticItem)
            .FirstOrDefaultAsync(pc => pc.PlayerId == playerId && pc.CosmeticItemId == cosmeticId);

        if (playerCosmetic == null)
            throw new InvalidOperationException("You don't own this cosmetic.");

        // Unequip all cosmetics of the same category
        var sameCategory = await _context.Set<PlayerCosmetic>()
            .Include(pc => pc.CosmeticItem)
            .Where(pc => pc.PlayerId == playerId && pc.CosmeticItem.Category == playerCosmetic.CosmeticItem.Category)
            .ToListAsync();

        foreach (var c in sameCategory)
            c.IsEquipped = false;

        playerCosmetic.IsEquipped = true;

        // Update player profile references
        var player = await _context.Players.FindAsync(playerId);
        if (player != null)
        {
            switch (playerCosmetic.CosmeticItem.Category)
            {
                case "profile_border":
                    player.ActiveProfileBorderId = cosmeticId;
                    break;
                case "card_back":
                    player.ActiveCardBackId = cosmeticId;
                    break;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<GemBalanceResponse> GetGemBalanceAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) throw new KeyNotFoundException("Player not found.");

        return new GemBalanceResponse
        {
            Gems = player.Gems,
            Gold = player.Currency,
            GemsEarnedTotal = player.GemsEarnedTotal,
            GemsSpentTotal = player.GemsSpentTotal
        };
    }

    private async Task EnsureShopItemsExistAsync()
    {
        if (await _context.Set<CosmeticItem>().AnyAsync())
            return;

        var items = new List<CosmeticItem>
        {
            // Profile Borders
            new() { Id = Guid.NewGuid(), Name = "Bronze Frame", Description = "A simple bronze profile border.", Category = "profile_border", Rarity = "common", GemPrice = 50, GoldPrice = 500 },
            new() { Id = Guid.NewGuid(), Name = "Silver Frame", Description = "A polished silver profile border.", Category = "profile_border", Rarity = "rare", GemPrice = 100, GoldPrice = 1500 },
            new() { Id = Guid.NewGuid(), Name = "Gold Frame", Description = "A gleaming gold profile border.", Category = "profile_border", Rarity = "epic", GemPrice = 250, GoldPrice = 5000 },
            new() { Id = Guid.NewGuid(), Name = "Diamond Frame", Description = "A dazzling diamond-encrusted border.", Category = "profile_border", Rarity = "legendary", GemPrice = 500, GoldPrice = 0 },

            // Card Backs
            new() { Id = Guid.NewGuid(), Name = "Forest Pattern", Description = "A nature-themed card back.", Category = "card_back", Rarity = "common", GemPrice = 30, GoldPrice = 300 },
            new() { Id = Guid.NewGuid(), Name = "Flame Pattern", Description = "A fiery card back design.", Category = "card_back", Rarity = "rare", GemPrice = 75, GoldPrice = 1000 },
            new() { Id = Guid.NewGuid(), Name = "Void Pattern", Description = "A dark, swirling void card back.", Category = "card_back", Rarity = "epic", GemPrice = 200, GoldPrice = 4000 },
            new() { Id = Guid.NewGuid(), Name = "Celestial Pattern", Description = "Stars and galaxies adorn this card back.", Category = "card_back", Rarity = "legendary", GemPrice = 400, GoldPrice = 0 },

            // Battle Effects
            new() { Id = Guid.NewGuid(), Name = "Spark Trail", Description = "Sparks trail behind your attacks.", Category = "battle_effect", Rarity = "common", GemPrice = 40, GoldPrice = 400 },
            new() { Id = Guid.NewGuid(), Name = "Shadow Strike", Description = "Dark shadows accompany your hits.", Category = "battle_effect", Rarity = "rare", GemPrice = 100, GoldPrice = 1500 },
            new() { Id = Guid.NewGuid(), Name = "Lightning Aura", Description = "Crackling electricity surrounds your units.", Category = "battle_effect", Rarity = "epic", GemPrice = 250, GoldPrice = 5000 },
            new() { Id = Guid.NewGuid(), Name = "Phoenix Blaze", Description = "Legendary flames engulf the battlefield.", Category = "battle_effect", Rarity = "legendary", GemPrice = 500, GoldPrice = 0 },

            // Unit Skins
            new() { Id = Guid.NewGuid(), Name = "Knight Armor", Description = "Classic knight armor for Warriors.", Category = "unit_skin", Rarity = "rare", GemPrice = 150, GoldPrice = 2000 },
            new() { Id = Guid.NewGuid(), Name = "Arcane Robes", Description = "Mystical robes for Mages.", Category = "unit_skin", Rarity = "rare", GemPrice = 150, GoldPrice = 2000 },
            new() { Id = Guid.NewGuid(), Name = "Shadow Cloak", Description = "Stealthy cloak for Rangers.", Category = "unit_skin", Rarity = "epic", GemPrice = 300, GoldPrice = 6000 },
            new() { Id = Guid.NewGuid(), Name = "Divine Vestments", Description = "Holy garments for Healers.", Category = "unit_skin", Rarity = "epic", GemPrice = 300, GoldPrice = 6000 },

            // Premium Plus Exclusive items
            new() { Id = Guid.NewGuid(), Name = "Platinum Frame", Description = "An exclusive platinum border only for Premium Plus members.", Category = "profile_border", Rarity = "legendary", GemPrice = 100, GoldPrice = 0, RequiredTier = "PremiumPlus" },
            new() { Id = Guid.NewGuid(), Name = "Aurora Pattern", Description = "Shimmering northern lights card back. Premium Plus exclusive.", Category = "card_back", Rarity = "legendary", GemPrice = 100, GoldPrice = 0, RequiredTier = "PremiumPlus" },
            new() { Id = Guid.NewGuid(), Name = "Cosmic Eruption", Description = "Reality-bending battle effects. Premium Plus exclusive.", Category = "battle_effect", Rarity = "legendary", GemPrice = 100, GoldPrice = 0, RequiredTier = "PremiumPlus" },
            new() { Id = Guid.NewGuid(), Name = "Gilded Champion", Description = "Golden champion armor for all unit classes. Premium Plus exclusive.", Category = "unit_skin", Rarity = "legendary", GemPrice = 150, GoldPrice = 0, RequiredTier = "PremiumPlus" },
        };

        _context.Set<CosmeticItem>().AddRange(items);
        await _context.SaveChangesAsync();
    }

    private static bool MeetsTierRequirement(SubscriptionTier playerTier, string requiredTier)
    {
        return requiredTier switch
        {
            "Premium" => playerTier >= SubscriptionTier.Premium,
            "PremiumPlus" => playerTier >= SubscriptionTier.PremiumPlus,
            _ => true
        };
    }
}
