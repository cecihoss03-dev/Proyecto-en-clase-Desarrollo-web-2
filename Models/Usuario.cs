using System.Security.Claims;

namespace SistemaVentas.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;   // siempre en minúsculas
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;    // nunca la contraseña en texto plano
    public string Rol { get; set; } = Roles.Vendedor;           // "Administrador" o "Vendedor"
    public bool Activo { get; set; } = true;

    // Datos que se guardan en la sesión (cookie) al iniciar sesión
    public List<Claim> ObtenerClaims() => new()
    {
        new(ClaimTypes.NameIdentifier, Id.ToString()),
        new(ClaimTypes.Name, NombreUsuario),
        new(ClaimTypes.Role, Rol),
        new("NombreCompleto", NombreCompleto)
    };
}