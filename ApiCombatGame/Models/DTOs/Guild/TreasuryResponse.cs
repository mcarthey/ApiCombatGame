namespace ApiCombatGame.Models.DTOs.Guild;

public class TreasuryResponse
{
    public int Balance { get; set; }
    public int GoldBonusPercent { get; set; }
    public int MaxRaidAttempts { get; set; }
    public int MaxMembers { get; set; }
    public List<GuildUpgradeOption> AvailableUpgrades { get; set; } = new();
}

public class GuildUpgradeOption
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; }
    public bool CanAfford { get; set; }
    public bool AlreadyPurchased { get; set; }
}

public class TreasurySpendRequest
{
    public string UpgradeId { get; set; } = string.Empty;
}

public class TreasuryDepositRequest
{
    [System.ComponentModel.DataAnnotations.Range(1, 1000000)]
    public int Amount { get; set; }
}
