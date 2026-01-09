namespace GlowNic.Models.DTOs.Responses;

/// <summary>
/// Resultado del intento de login
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public LoginResponse? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public LoginErrorType? ErrorType { get; set; }

    public static LoginResult SuccessResult(LoginResponse response)
    {
        return new LoginResult
        {
            Success = true,
            Response = response
        };
    }

    public static LoginResult ErrorResult(string message, LoginErrorType errorType)
    {
        return new LoginResult
        {
            Success = false,
            ErrorMessage = message,
            ErrorType = errorType
        };
    }
}

/// <summary>
/// Tipos de error en el login
/// </summary>
public enum LoginErrorType
{
    InvalidCredentials,      // Credenciales inválidas (usuario/contraseña incorrectos)
    UserInactive,            // Usuario inactivo
    BarberDeleted,           // Barbero fue eliminado
    BarberInactive,          // Barbero desactivado
    EmployeeDeleted,         // Empleado fue eliminado
    EmployeeInactive,        // Empleado desactivado
    OwnerBarberDeleted,      // Barbero dueño fue eliminado
    OwnerBarberInactive      // Barbero dueño desactivado
}


