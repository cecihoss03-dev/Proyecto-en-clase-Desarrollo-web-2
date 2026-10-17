using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using SistemaVentas.Models;
using SistemaVentas.Data;
using Microsoft.Extensions.FileProviders;

namespace SistemaVentas.Controllers.Api
{

    //Este apicontroller valida un Json automaticamente 400,
    //en caso de funcionar dara status 200

    [ApiController]
    [Route("api/ventas")]
    public class VentasApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VentasApiController(AppDbContext context)
        {
            _context = context;

        }

        //GET api/ventasapi
        [HttpGet]
        public async Task<ActionResult<List<Venta>>> GetAll()
        {
            return await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();
        }

        //GET api/ventasapi/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Venta>> GetById(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
                return NotFound();

            return venta;
        }

        //POST api/ventasapi
        //Body: {"clienteId": 1, "recibido": 50, "detalles": [{"productoId": 1, "cantidad": 2, "precio": 8}]}
        [HttpPost]
        public async Task<ActionResult<Venta>> Create(Venta venta)
        {
            // Calcula los subtotales de cada detalle antes del total general
            foreach (var detalle in venta.Detalles)
            {
                detalle.SubTotal(detalle.Cantidad, detalle.Precio);
            }

            // Invoca los métodos definidos en tu modelo Venta
            venta.CalcularTotal();
            venta.CalcularVambio();

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = venta.Id }, venta);
        }

        //PUT api/ventasapi/3
        //Body: {"id": 3, "clienteId": 1, "recibido": 100, "detalles": [...]}
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, Venta venta)
        {
            if (id != venta.Id)
                return BadRequest("El id de la url no coincide con el del cuerpo");

            if (!await _context.Ventas.AnyAsync(v => v.Id == id)) 
                return NotFound();

            foreach (var detalle in venta.Detalles)
            {
                detalle.SubTotal(detalle.Cantidad, detalle.Precio);
            }

            venta.CalcularTotal();
            venta.CalcularVambio();

            _context.Ventas.Update(venta);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        //DELETE api/ventasapi/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
            {
                return NotFound();
            }

            _context.Ventas.Remove(venta);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}