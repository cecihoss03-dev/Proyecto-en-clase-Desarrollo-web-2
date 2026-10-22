namespace SistemaVentas.Models;
public class Venta
{
    //Atributos
    public int Id {get;set;}
    public DateTime Fecha {set;get;} = DateTime.Now;
    public decimal Total {set;get;}
    public decimal Recibido {set;get;}
    public decimal Cambio {set;get;}
    public  int ClienteId  {set;get;}
    public Cliente? Cliente {set;get;}
    public List<DetalleVenta> Detalles {get;set;} = new();
    //Metodos
    public void CalcularTotal()
    {
        Total = Detalles.Sum(d => d.Subtotal);
    }
    public void CalcularCambio()
    {
        Cambio = Recibido - Total;
    }
    // (Opcional por si en algún lado usabas el otro nombre por error)
    public void CalcularVambio()
    {
        CalcularCambio();
    }
    public void Actualizarstock()
    {
     foreach(var d in Detalles)
        {
            if(d.Producto != null)
            {
                d.Producto.Stock -= d.Cantidad;
            }
        }   
    }
}