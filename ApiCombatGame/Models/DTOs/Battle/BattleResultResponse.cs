using ApiCombatGame.Models.Domain;

namespace ApiCombatGame.Models.DTOs.Battle;

public class BattleResultResponse
{
    public Guid BattleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? WinnerId { get; set; }
    public Guid? LoserId { get; set; }
    public int Turns { get; set; }
    public List<BattleLogEntry> BattleLog { get; set; } = new();
    public BattleRewards? Rewards { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BattleRewards
{
    public int Currency { get; set; }
    public int RatingChange { get; set; }
}
