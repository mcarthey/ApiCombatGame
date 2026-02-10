namespace ApiCombatGame.Models.DTOs.Modifier;

public class ModifierResponse
{
    public Guid? ModifierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
