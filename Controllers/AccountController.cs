using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;
using SistemaVentas.Models;

namespace SistemaVentas.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        AppDbContext context,
        IPasswordHasher<Usuario> hasher,
        ILogger<AccountController> logger)
    {
        _context = context;
        _hasher = hasher;
        _logger = logger;
    }

    // GET: /Account/Login
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var nombre = model.NombreUsuario.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombre && u.Activo);

        PasswordVerificationResult resultado;
        if (usuario is null)
        {
            // Gasta el mismo tiempo que una verificación real, para que no se pueda
            // adivinar qué usuarios existen midiendo cuánto tarda la respuesta.
            _hasher.HashPassword(new Usuario(), model.Password);
            resultado = PasswordVerificationResult.Failed;
        }
        else
        {
            resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Password);
        }

        // TEMPORAL (diagnóstico): borrar cuando el login funcione.
        // No registra la contraseña, solo su longitud.
        _logger.LogWarning(
            "Login '{Usuario}': usuario encontrado = {Encontrado}, resultado = {Resultado}, longitud password recibida = {Largo}",
            nombre, usuario is not null, resultado, model.Password?.Length);

        if (usuario is null || resultado == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        // Si el algoritmo de hash se actualizó, guardamos el hash nuevo
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = _hasher.HashPassword(usuario, model.Password);
            await _context.SaveChangesAsync();
        }

        var identity = new ClaimsIdentity(usuario.ObtenerClaims(), CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = model.Recordarme,
                ExpiresUtc = model.Recordarme ? DateTimeOffset.UtcNow.AddDays(14) : null
            });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/AccesoDenegado  (cuando alguien entra a algo que su rol no permite)
    [HttpGet]
    public IActionResult AccesoDenegado()
    {
        return View();
    }

    // GET: /Account/CambiarPassword
    [HttpGet]
    public IActionResult CambiarPassword()
    {
        return View(new CambiarPasswordViewModel());
    }

    // POST: /Account/CambiarPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return RedirectToAction(nameof(Login));

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
            return RedirectToAction(nameof(Login));

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.PasswordActual);
        if (resultado == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.PasswordActual), "La contraseña actual no es correcta.");
            return View(model);
        }

        usuario.PasswordHash = _hasher.HashPassword(usuario, model.PasswordNueva);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Tu contraseña se actualizó.";
        return RedirectToAction(nameof(CambiarPassword));
    }
}