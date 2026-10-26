using System.ComponentModel.DataAnnotations;

namespace SistemaVentas.Models;

public class UsuarioCrearViewModel
{
    [Required(ErrorMessage = "Escribe el nombre completo.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el nombre de usuario.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]{3,30}$", ErrorMessage = "Usa de 3 a 30 letras, números, punto, guion o guion bajo, sin espacios.")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repite la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = Roles.Vendedor;
}

public class UsuarioEditarViewModel
{
    public int Id { get; set; }

    // Solo se muestra; el nombre de usuario no se cambia
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el nombre completo.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = Roles.Vendedor;

    [Display(Name = "Usuario activo")]
    public bool Activo { get; set; } = true;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña (opcional)")]
    public string? NuevaPassword { get; set; }

    [Compare(nameof(NuevaPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir nueva contraseña")]
    public string? ConfirmarPassword { get; set; }
}

public class CambiarPasswordViewModel
{
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la nueva contraseña.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string PasswordNueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repite la nueva contraseña.")]
    [Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir nueva contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}
