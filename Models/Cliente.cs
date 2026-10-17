namespace SistemaVentas.Models;
public class Cliente
{
    public int Id {get;set;}
    public string Nombre {get;set;} = string.Empty;
    public string? Apellido {get;set;}
    public string? Email{set;get;}
    public string? Telefono {get;set;}

    public List<Venta> Ventas {get;set;} = new();
}