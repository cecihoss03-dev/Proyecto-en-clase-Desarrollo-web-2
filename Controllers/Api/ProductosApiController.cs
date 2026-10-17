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
    [Route("api/productos")]
    public class ProductosApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductosApiController(AppDbContext context)
        {
            _context = context;

        }

        //GET api/productosapi
        [HttpGet]
        public async Task<ActionResult<List<Producto>>> GetAll()
        {
            return await _context.Producto
                .Include(p => p.Categoria)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }

        //GET api/productosapi/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetById(int id)
        {
            var producto = await _context.Producto
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (producto == null)
                return NotFound();

            return producto;
        }

        //POST api/productosapi
        //Body: {"nombre":"Detergente", "precio": 12.50, "stock": 50, "stockMinimo": 5, "categoriaId": 1}
        [HttpPost]
        public async Task<ActionResult<Producto>> Create(Producto producto)
        {
            _context.Producto.Add(producto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto);
        }

        //PUT api/productosapi/3
        //Body: {"id": 3, "nombre":"Coca Cola 2L", "precio": 13, "stock": 80, "stockMinimo": 10, "categoriaId": 3}
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, Producto producto)
        {
            if (id != producto.Id)
                return BadRequest("El id de la url no coincide con el del cuerpo");

            if (!await _context.Producto.AnyAsync(p => p.Id == id)) 
                return NotFound();

            _context.Producto.Update(producto);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        //DELETE api/productosapi/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            var producto = await _context.Producto.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            if (await _context.DetalleVentas.AnyAsync(d => d.ProductoId == id))
            {
                return Conflict("El producto tiene detalles de venta asociados");
            }

            _context.Producto.Remove(producto);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}