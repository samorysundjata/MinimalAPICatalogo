namespace MinimalAPICatalogo.Models
{
    public class Categoria
    {
        public int CategoriaId { get; set; }

        public string? Nome { get; set; }

        public string? Descricao { get; set; }

        ICollection<Produto> Produtos { get; set; } 
    }
}
