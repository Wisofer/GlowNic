namespace GlowNic.Models.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty; // "Administrador" o "Normal"
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

