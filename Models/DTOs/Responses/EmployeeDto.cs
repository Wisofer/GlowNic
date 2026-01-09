namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// DTO de respuesta para un trabajador/empleado
/// </summary>
public class EmployeeDto
{
    public int Id { get; set; }
    public int OwnerBarberId { get; set; }
    public string OwnerBarberName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

