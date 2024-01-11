namespace FiapStoreDurableFunction.Models;

public class Order
{
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public DateTime DataPedido { get; set; }
    public decimal ValorTotal { get; set; }
    public bool Pago { get; set; } = false;
    public List<Item> Items { get; set; } = new List<Item>();
    public bool Aprovado { get; set; } = false;
}
