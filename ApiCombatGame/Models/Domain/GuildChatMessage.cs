using System.ComponentModel.DataAnnotations;

namespace ApiCombatGame.Models.Domain;

public class GuildChatMessage
{
    [Key]
    public Guid Id { get; set; }

    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    /// <summary>Null for system messages.</summary>
    public Guid? PlayerId { get; set; }
    public Player? Player { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>"chat" or "system"</summary>
    [MaxLength(20)]
    public string MessageType { get; set; } = "chat";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
