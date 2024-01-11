namespace FiapStoreDurableFunction.Models;

public class OrderRequest
{
    public int IdCliente { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<LinkedListItem> IdProdutoXQuantidade { get; set; } = [];

}
public class LinkedListItem
{
    public int IdProduto { get; set; }
    public int Quantidade { get; set; }
}
