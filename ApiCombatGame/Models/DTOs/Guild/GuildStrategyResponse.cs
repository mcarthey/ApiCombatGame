namespace ApiCombatGame.Models.DTOs.Guild;

public class GuildStrategyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatorUsername { get; set; } = string.Empty;
    public object? Strategy { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PublishGuildStrategyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object Strategy { get; set; } = new { };
}

public class UpdateGuildStrategyRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public object? Strategy { get; set; }
}
