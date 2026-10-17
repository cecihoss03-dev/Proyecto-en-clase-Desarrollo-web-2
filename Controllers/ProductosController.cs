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

    // GET: /Ventas/Index (o gestionando productos desde ventas según el inge)
    public async Task<IActionResult> Index()
    {
        var productos = await _context.Producto
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return View("~/Views/Productos/Index.cshtml", productos);
    }

    // GET: /Ventas/Create
    public async Task<IActionResult> Create()
    {
        ViewData["CategoriaId"] = new SelectList(await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
        return View("~/Views/Productos/Create.cshtml", new Producto());
    }

    // POST: /Ventas/Create
    [HttpPost]
    public async Task<IActionResult> Create(Producto producto)
    {
        if (!ModelState.IsValid)
        {
            ViewData["CategoriaId"] = new SelectList(await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", producto.CategoriaId);
            return View("~/Views/Productos/Create.cshtml", producto);
        }

        _context.Producto.Add(producto);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Ventas/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _context.Producto.FindAsync(id);
        if (producto == null) return NotFound();

        ViewData["CategoriaId"] = new SelectList(await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", producto.CategoriaId);
        return View("~/Views/Productos/Edit.cshtml", producto);
    }

    // POST: /Ventas/Edit/5
    [HttpPost]
    public async Task<IActionResult> Edit(int id, Producto producto)
    {
        if (id != producto.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["CategoriaId"] = new SelectList(await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", producto.CategoriaId);
            return View("~/Views/Productos/Edit.cshtml", producto);
        }

        _context.Producto.Update(producto);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Ventas/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await _context.Producto
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null) return NotFound();
        return View("~/Views/Productos/Delete.cshtml", producto);
    }

    // POST: /Ventas/Delete/5
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var producto = await _context.Producto.FindAsync(id);
        if (producto != null)
        {
            var tieneVentas = await _context.DetalleVentas.AnyAsync(d => d.ProductoId == id);
            if (tieneVentas)
            {
                ModelState.AddModelError("", "No se puede eliminar: el producto tiene ventas o detalles asociados.");
                
                await _context.Entry(producto).Reference(p => p.Categoria).LoadAsync();
                return View("~/Views/Productos/Delete.cshtml", producto);
            }

            _context.Producto.Remove(producto);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}