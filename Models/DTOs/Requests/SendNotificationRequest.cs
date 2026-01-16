namespace GlowNic.Models.DTOs.Requests;

public class SendNotificationRequest
{
    public int? TemplateId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public List<int>? UserIds { get; set; } // null = enviar a todos
    public Dictionary<string, string>? ExtraData { get; set; }
    public bool DataOnly { get; set; } = false;
}
