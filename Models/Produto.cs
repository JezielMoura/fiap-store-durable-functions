namespace FiapStoreDurableFunction.Models;

public class Produto
{
    public int IdProduto { get; set; }
    public int IdTipoProduto { get; set; }
    public string? Nome { get; set; }
    public decimal Preco { get; set; }
    public string? Descricao { get; set; }
    public int Quantidade { get; set; }
}
