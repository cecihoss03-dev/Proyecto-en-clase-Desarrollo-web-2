using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Models;

namespace SistemaVentas.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<DetalleVenta> DetalleVentas => Set<DetalleVenta>();
    public DbSet<Producto> Producto => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Limpieza" },
            new Categoria { Id = 2, Nombre = "Abarrotes" },
            new Categoria { Id = 3, Nombre = "Bebidas" }
        );
        
        modelBuilder.Entity<Producto>().HasData(
            new Producto { Id = 1, Nombre = "Jabon", Precio = 8, Stock = 100, StockMinimo = 5, CategoriaId = 1 },
            new Producto { Id = 2, Nombre = "Arroz", Precio = 16, Stock = 100, StockMinimo = 10, CategoriaId = 2 },
            new Producto { Id = 3, Nombre = "Coca cola", Precio = 13, Stock = 100, StockMinimo = 10, CategoriaId = 3 }
        );
    }
}