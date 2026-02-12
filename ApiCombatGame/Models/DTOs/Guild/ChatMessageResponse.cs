namespace ApiCombatGame.Models.DTOs.Guild;

public class ChatMessageResponse
{
    public Guid MessageId { get; set; }
    public Guid? PlayerId { get; set; }
    public string? Username { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageType { get; set; } = "chat";
    public DateTime CreatedAt { get; set; }
}

public class PostChatRequest
{
    public string Message { get; set; } = string.Empty;
}
