namespace SistemaVentas.Models;
public class DetalleVenta
{
    //Atributos
    public int Id {get;set;}
    public int Cantidad {get;set;}
    public decimal Precio {get;set;}
    public decimal Subtotal {get;set;}
    
    //Relaciones
    public int VentaId {get;set;} // entidad venta
    public Venta? venta {get;set;} 
    public int ProductoId {get;set;}    // entidad producto
    public Producto? Producto{get;set;}

    //Metodos
    public void SubTotal(int Cantidad,decimal Precio)
    {
        Subtotal = Cantidad * Precio;
    }

}