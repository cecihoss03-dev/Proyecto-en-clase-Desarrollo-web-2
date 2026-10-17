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
    [Route("api/detalleventas")]
    public class DetalleVentaApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DetalleVentaApiController(AppDbContext context)
        {
            _context = context;

        }

        //GET api/detalleventaapi
        [HttpGet]
        public async Task<ActionResult<List<DetalleVenta>>> GetAll()
        {
            return await _context.DetalleVentas
                .Include(d => d.Producto)
                .OrderBy(d => d.Id)
                .ToListAsync();
        }

        //GET api/detalleventaapi/1
        [HttpGet("{id}")]
        public async Task<ActionResult<DetalleVenta>> GetById(int id)
        {
            var detalle = await _context.DetalleVentas
                .Include(d => d.Producto)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (detalle == null)
                return NotFound();

            return detalle;
        }

        //POST api/detalleventaapi
        //Body: {"ventaId": 1, "productoId": 2, "cantidad": 3, "precio": 16}
        [HttpPost]
        public async Task<ActionResult<DetalleVenta>> Create(DetalleVenta detalle)
        {
            // Calcula automáticamente el subtotal usando el método de tu modelo
            detalle.SubTotal(detalle.Cantidad, detalle.Precio);

            _context.DetalleVentas.Add(detalle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = detalle.Id }, detalle);
        }

        //PUT api/detalleventaapi/3
        //Body: {"id": 3, "ventaId": 1, "productoId": 2, "cantidad": 5, "precio": 16}
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, DetalleVenta detalle)
        {
            if (id != detalle.Id)
                return BadRequest("El id de la url no coincide con el del cuerpo");

            if (!await _context.DetalleVentas.AnyAsync(d => d.Id == id)) 
                return NotFound();

            // Recalcula el subtotal en la actualización
            detalle.SubTotal(detalle.Cantidad, detalle.Precio);

            _context.DetalleVentas.Update(detalle);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        //DELETE api/detalleventaapi/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            var detalle = await _context.DetalleVentas.FindAsync(id);
            if (detalle == null)
            {
                return NotFound();
            }

            _context.DetalleVentas.Remove(detalle);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}