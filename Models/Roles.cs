namespace SistemaVentas.Models;

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Vendedor = "Vendedor";

    public static readonly string[] Todos = { Administrador, Vendedor };
}
