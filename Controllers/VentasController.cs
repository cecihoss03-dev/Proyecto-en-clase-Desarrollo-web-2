using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;
using SistemaVentas.Models;

namespace SistemaVentas.Controllers;

public class VentasController : Controller
{
    private readonly AppDbContext _context;

    public VentasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Ventas
    public async Task<IActionResult> Index()
    {
        var ventas = await _context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Detalles!)
                .ThenInclude(d => d.Producto)
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();

        return View(ventas);
    }

    // GET: /Ventas/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Detalles!)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null) return NotFound();

        return View(venta);
    }

    // GET: /Ventas/Create
    public async Task<IActionResult> Create()
    {
        ViewData["ClienteId"] = new SelectList(
            await _context.Clientes
                .OrderBy(c => c.Nombre)
                .Select(c => new { c.Id, NombreCompleto = c.Nombre + " " + c.Apellido })
                .ToListAsync(),
            "Id", "NombreCompleto");

        ViewData["Productos"] = await _context.Producto
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return View(new Venta { Fecha = DateTime.Now });
    }

    // POST: /Ventas/Create
    [HttpPost]
    public async Task<IActionResult> Create(Venta venta, List<DetalleVenta> detalles)
    {
        if (detalles == null || !detalles.Any())
        {
            ModelState.AddModelError("", "Debe agregar al menos un producto a la venta.");
        }

        if (ModelState.IsValid)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                venta.Fecha = DateTime.Now;
                venta.Detalles = detalles;
                venta.CalcularTotal();
                venta.CalcularCambio();

                // Descontar stock de cada producto
                foreach (var detalle in detalles)
                {
                    var producto = await _context.Producto.FindAsync(detalle.ProductoId);
                    if (producto != null)
                    {
                        if (producto.Stock < detalle.Cantidad)
                        {
                            throw new Exception($"Stock insuficiente para el producto {producto.Nombre}. Stock disponible: {producto.Stock}");
                        }
                        producto.Stock -= detalle.Cantidad;
                        _context.Producto.Update(producto);
                    }
                }

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", ex.Message);
            }
        }

        // Si falla algo, recargar selectores
        ViewData["ClienteId"] = new SelectList(
            await _context.Clientes
                .OrderBy(c => c.Nombre)
                .Select(c => new { c.Id, NombreCompleto = c.Nombre + " " + c.Apellido })
                .ToListAsync(),
            "Id", "NombreCompleto", venta.ClienteId);

        ViewData["Productos"] = await _context.Producto
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return View(venta);
    }

    // GET: /Ventas/Cancel/5
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Cancel(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Detalles!)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null) return NotFound();

        return View(venta);
    }

    // POST: /Ventas/Cancel/5
    [Authorize(Roles = Roles.Administrador)]
    [HttpPost, ActionName("Cancel")]
    public async Task<IActionResult> CancelConfirmed(int id)
    {
        var venta = await _context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta != null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Reestablecer el stock de los productos vendidos
                if (venta.Detalles != null)
                {
                    foreach (var detalle in venta.Detalles)
                    {
                        var producto = await _context.Producto.FindAsync(detalle.ProductoId);
                        if (producto != null)
                        {
                            producto.Stock += detalle.Cantidad;
                            _context.Producto.Update(producto);
                        }
                    }
                }

                // Remover la venta devolviendo stock
                _context.Ventas.Remove(venta);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al cancelar la venta: " + ex.Message);
                return View(venta);
            }
        }

        return RedirectToAction(nameof(Index));
    }
}
