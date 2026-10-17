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
    [Route("api/clientes")]
    public class ClientesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesApiController(AppDbContext context)
        {
            _context = context;
        }

        //GET api/clientes
        [HttpGet]
        public async Task<ActionResult<List<Cliente>>> GetAll()
        {
            return await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
        }

        //GET api/clientes/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetById(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound();

            return cliente;
        }

        //POST api/clientes
        //Body: {"nombre":"Juan", "apellido":"Pérez", "email":"juan@email.com", "telefono":"70012345"}
        [HttpPost]
        public async Task<ActionResult<Cliente>> Create(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
        }

        //PUT api/clientes/3
        //Body: {"id":3, "nombre":"Juan Manuel", "apellido":"Pérez", "email":"juan@email.com", "telefono":"70012345"}
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, Cliente cliente)
        {
            if (id != cliente.Id)
                return BadRequest("El id de la url no coincide con el del cuerpo");

            if (!await _context.Clientes.AnyAsync(c => c.Id == id))
                return NotFound();

            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        //DELETE api/clientes/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }

            if (await _context.Ventas.AnyAsync(v => v.ClienteId == id))
            {
                return Conflict("El cliente tiene ventas asociadas");
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}