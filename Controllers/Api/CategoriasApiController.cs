using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Models;
using SistemaVentas.Data;

namespace SistemaVentas.Controllers.Api
{
    [ApiController]
    [Route("api/categorias")] // Define la ruta base correctamente
    public class CategoriasApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriasApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/categorias
        [HttpGet] // <-- AGREGA ESTO
        public async Task<ActionResult<List<Categoria>>> GetAll()
        {
            return await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
        }

        // GET api/categorias/1
        [HttpGet] // <-- AGREGA ESTO CON EL PARÁMETRO {id}
        public async Task<ActionResult<Categoria>> GetById(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
                return NotFound();

            return categoria;
        }

        // POST api/categorias
        [HttpPost] // <-- AGREGA ESTO
        public async Task<ActionResult<Categoria>> Create(Categoria categoria)
        {
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria);
        }

        // PUT api/categorias/3
        [HttpPut] // <-- AGREGA ESTO CON EL PARÁMETRO {id}
        public async Task<ActionResult> Update(int id, Categoria categoria)
        {
            if (id != categoria.Id)
                return BadRequest("El id de la url no coincide con el del cuerpo");

            if (!await _context.Categorias.AnyAsync(c => c.Id == id))
                return NotFound();

            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/categorias/1
        [HttpDelete] // <-- UNCOMMENT Y ASEGÚRATE DE PONER ESTO
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }

            if (await _context.Producto.AnyAsync(p => p.CategoriaId == id))
            {
                return Conflict("La categoria tiene productos asociados");
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}