namespace ApiCombatGame.Models.Domain;

/// <summary>
/// Represents a single entry in the battle log.
/// </summary>
public class BattleLogEntry
{
    public int Turn { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int Damage { get; set; }
    public int Healing { get; set; }
    public List<string> Effects { get; set; } = new();
    public int TargetHpRemaining { get; set; }
}
