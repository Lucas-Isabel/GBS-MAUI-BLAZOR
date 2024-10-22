using System;

namespace IMPORTADOR_CLIPSE.Models
{
    public class Produto
    {
        public int Plu { get; set; }          // Código PLU do produto
        public string Descricao { get; set; } // Descrição do produto (máx. 15 caracteres)
        public double Preco { get; set; }     // Preço do produto (deve estar entre 0 e 1000)
        public int Venda { get; set; }        // Quantidade de vendas (máx. 10)
        public int Validade { get; set; }     // Validade em dias (máx. 200)
        public DateTime CreatedAt { get; set; } // Data de criação do produto
        public DateTime UpdatedAt { get; set; } // Data da última atualização
        public string UpdateBy { get; set; }    // Nome de quem fez a última atualização (máx. 30 caracteres)
    }
}
