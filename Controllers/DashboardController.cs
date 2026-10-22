using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;
using SistemaVentas.Models;

namespace SistemaVentas.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioAño = new DateTime(hoy.Year, 1, 1);

        // 1. Ventas de Hoy
        var ventasHoyQuery = _context.Ventas.Where(v => v.Fecha >= hoy);
        ViewBag.TotalVentasHoy = await ventasHoyQuery.SumAsync(v => (decimal?)v.Total) ?? 0;
        ViewBag.CantidadVentasHoy = await ventasHoyQuery.CountAsync();

        // 2. Ventas del Mes
        var ventasMesQuery = _context.Ventas.Where(v => v.Fecha >= inicioMes);
        decimal totalVentasMes = await ventasMesQuery.SumAsync(v => (decimal?)v.Total) ?? 0;
        int cantidadVentasMes = await ventasMesQuery.CountAsync();

        ViewBag.TotalVentasMes = totalVentasMes;
        ViewBag.CantidadVentasMes = cantidadVentasMes;

        // 3. Promedio de Venta del Mes
        ViewBag.PromedioVentaMes = cantidadVentasMes > 0 ? (totalVentasMes / cantidadVentasMes) : 0;

        // 4. Ventas del Año
        ViewBag.TotalVentasAño = await _context.Ventas
            .Where(v => v.Fecha >= inicioAño)
            .SumAsync(v => (decimal?)v.Total) ?? 0;

        // 5. Clientes Registrados
        ViewBag.TotalClientes = await _context.Clientes.CountAsync();

        // 6. Productos con Stock Bajo / Crítico
        var productosBajoStockList = await _context.Producto
            .Include(p => p.Categoria)
            .Where(p => p.Stock <= (p.StockMinimo ?? 5))
            .OrderBy(p => p.Stock)
            .ToListAsync();

        ViewBag.ProductosBajoStockCount = productosBajoStockList.Count;
        ViewBag.ListaBajoStock = productosBajoStockList.Take(5).ToList();

        // 7. Generación de Alertas Importantes
        var alertas = new List<string>();

        var productosAgotados = productosBajoStockList.Where(p => p.Stock <= 0).Count();
        if (productosAgotados > 0)
        {
            alertas.Add($"¡Atención! Hay {productosAgotados} producto(s) totalmente sin stock.");
        }

        var productosPorAgotar = productosBajoStockList.Where(p => p.Stock > 0).Count();
        if (productosPorAgotar > 0)
        {
            alertas.Add($"Atención: Hay {productosPorAgotar} producto(s) a punto de agotarse (bajo stock mínimo).");
        }

        if (ViewBag.CantidadVentasHoy == 0)
        {
            alertas.Add("Aún no se han registrado ventas el día de hoy.");
        }

        ViewBag.Alertas = alertas;

        // Últimas 5 Ventas para la tabla
        var ultimasVentas = await _context.Ventas
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.Fecha)
            .Take(5)
            .ToListAsync();

        return View(ultimasVentas);
    }
}