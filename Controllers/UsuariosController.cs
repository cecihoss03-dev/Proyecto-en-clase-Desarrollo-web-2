using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;
using SistemaVentas.Models;

namespace SistemaVentas.Controllers;

// Solo los administradores entran a esta sección
[Authorize(Roles = Roles.Administrador)]
public class UsuariosController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _hasher;

    public UsuariosController(AppDbContext context, IPasswordHasher<Usuario> hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    // GET: /Usuarios
    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();
        return View(usuarios);
    }

    // GET: /Usuarios/Create
    public IActionResult Create()
    {
        return View(new UsuarioCrearViewModel());
    }

    // POST: /Usuarios/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCrearViewModel model)
    {
        if (!Roles.Todos.Contains(model.Rol))
            ModelState.AddModelError(nameof(model.Rol), "Elige un rol válido.");

        var nombre = (model.NombreUsuario ?? string.Empty).Trim().ToLowerInvariant();
        if (nombre.Length > 0 && await _context.Usuarios.AnyAsync(u => u.NombreUsuario == nombre))
            ModelState.AddModelError(nameof(model.NombreUsuario), "Ese usuario ya existe. Elige otro nombre.");

        if (!ModelState.IsValid) return View(model);

        var usuario = new Usuario
        {
            NombreUsuario = nombre,
            NombreCompleto = model.NombreCompleto.Trim(),
            Rol = model.Rol,
            Activo = true
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, model.Password);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = $"Usuario '{usuario.NombreUsuario}' creado como {usuario.Rol}.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Usuarios/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        return View(new UsuarioEditarViewModel
        {
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol,
            Activo = usuario.Activo
        });
    }

    // POST: /Usuarios/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioEditarViewModel model)
    {
        if (id != model.Id) return NotFound();

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        // El nombre de usuario no se edita: siempre se muestra el guardado
        model.NombreUsuario = usuario.NombreUsuario;

        if (!Roles.Todos.Contains(model.Rol))
            ModelState.AddModelError(nameof(model.Rol), "Elige un rol válido.");

        // Evita que un administrador se quede sin acceso a sí mismo
        var esElMismo = User.FindFirstValue(ClaimTypes.NameIdentifier) == usuario.Id.ToString();
        if (esElMismo && (!model.Activo || model.Rol != Roles.Administrador))
            ModelState.AddModelError(string.Empty, "No puedes desactivarte ni quitarte el rol de administrador a ti mismo.");

        if (!ModelState.IsValid) return View(model);

        usuario.NombreCompleto = model.NombreCompleto.Trim();
        usuario.Rol = model.Rol;
        usuario.Activo = model.Activo;

        if (!string.IsNullOrWhiteSpace(model.NuevaPassword))
            usuario.PasswordHash = _hasher.HashPassword(usuario, model.NuevaPassword);

        await _context.SaveChangesAsync();

        TempData["Mensaje"] = $"Usuario '{usuario.NombreUsuario}' actualizado.";
        return RedirectToAction(nameof(Index));
    }
}
