namespace GlowNic.Models.DTOs.Responses;

public class NotificationLogDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTime SentAt { get; set; }
    public int? DeviceId { get; set; }
    public int? TemplateId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
