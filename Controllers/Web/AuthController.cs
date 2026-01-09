using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GlowNic.Models.Entities;
using GlowNic.Services.IServices;
using GlowNic.Utils;
using System.Security.Claims;
using System.Text.Json;

namespace GlowNic.Controllers.Web;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya está autenticado, redirigir según el rol
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectSegunRol();
        }
        
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(string NombreUsuario, string Contrasena, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(NombreUsuario) || string.IsNullOrWhiteSpace(Contrasena))
        {
            ViewBag.Error = "El nombre de usuario y la contraseña son requeridos.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Validar usuario contra la base de datos
        var usuario = _authService.ValidarUsuario(NombreUsuario, Contrasena);

        if (usuario == null)
        {
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Crear claims (información del usuario)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim("Rol", usuario.Rol),
            new Claim("NombreCompleto", usuario.NombreCompleto)
        };

        // Crear identidad y principal
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Guardar también en sesión para compatibilidad con código existente
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuario));
        HttpContext.Session.SetString("RolUsuario", usuario.Rol);
        HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);

        // Iniciar sesión (crea la cookie de autenticación)
        // No establecer ExpiresUtc explícitamente para que use la configuración de SlidingExpiration
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
        {
            IsPersistent = false // No recordar sesión después de cerrar navegador
            // ExpiresUtc se maneja automáticamente por la configuración de ExpireTimeSpan y SlidingExpiration
        });

        // Redirigir a la URL original si existe, sino según el rol
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        
        return RedirectSegunRol(usuario.Rol);
    }

    /// <summary>
    /// Redirige al usuario según su rol después del login
    /// </summary>
    private IActionResult RedirectSegunRol(string? rol = null)
    {
        // Si se proporciona el rol directamente, crear un ClaimsPrincipal temporal
        if (!string.IsNullOrEmpty(rol))
        {
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("Rol", rol)
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Temp");
            var tempPrincipal = new System.Security.Claims.ClaimsPrincipal(identity);
            return Redirect(SecurityHelper.GetRedirectUrlByRole(tempPrincipal));
        }

        // Si no, usar el usuario actual
        return Redirect(SecurityHelper.GetRedirectUrlByRole(User));
    }

    [HttpGet("/access-denied")]
    [Authorize]
    public IActionResult AccessDenied()
    {
        // Redirigir según el rol del usuario
        return RedirectSegunRol();
    }

    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        // Cerrar sesión de autenticación
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Limpiar sesión
        HttpContext.Session.Clear();
        
        return Redirect("/login");
    }

    /// <summary>
    /// Endpoint para mantener la sesión activa (keep-alive)
    /// Se llama periódicamente desde JavaScript para renovar la sesión automáticamente
    /// </summary>
    [HttpGet("/auth/keep-alive")]
    [Authorize]
    public IActionResult KeepAlive()
    {
        // Simplemente renovar la sesión tocando la cookie de autenticación
        // Con SlidingExpiration activado, esto renueva automáticamente el tiempo de expiración
        return Json(new { success = true, message = "Sesión renovada", timestamp = DateTime.Now });
    }
}
