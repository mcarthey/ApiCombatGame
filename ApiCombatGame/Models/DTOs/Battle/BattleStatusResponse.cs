namespace ApiCombatGame.Models.DTOs.Battle;

public class BattleStatusResponse
{
    public Guid BattleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? QueuePosition { get; set; }
    public int? EstimatedWaitSeconds { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
