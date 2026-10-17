using System.Text.Json.Serialization;

namespace SistemaVentas.Models;

public class Categoria
{
    public int Id { get; set; }
    public string? Nombre { get; set; } = string.Empty;

    [JsonIgnore] // Evita que se devuelva la lista de productos vacía o pesada en las respuestas de la API
    public List<Producto> Productos { get; set; } = new();
}