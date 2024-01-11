namespace FiapStoreDurableFunction.Models;

public class Item
{
    public int IdItem { get; set; }
    public int IdPedido { get; set; }
    public Produto? Produto { get; set; }
    public decimal Preco { get; set; }
    public int Quantidade { get; set; }
    public decimal SubTotal { get; set; }
}
