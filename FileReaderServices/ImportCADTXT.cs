using IMPORTADOR_CLIPSE.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMPORTADOR_CLIPSE.DB;

namespace IMPORTADOR_CLIPSE.FileReaderServices
{
    public class CADitensService : IImportService
    {
        private readonly DataBaseService _dataBaseService;

        public CADitensService()
        {
            _dataBaseService = new DataBaseService();
        }

        // Função para registrar erros em um arquivo de log
        private void RegistrarErro(string errMsg)
        {
            string logFilePath = "log-erro.txt";
            try
            {
                using (var arquivo = new StreamWriter(logFilePath, true))
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errMsg}";
                    arquivo.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao escrever no arquivo de log: " + ex.Message);
            }
        }

        public async Task<List<Produto>> LerItensDoArquivoAsync(string caminho)
        {
            // Abre o arquivo
            using var arquivo = new StreamReader(caminho);
            var produtos = new List<Produto>();

            string linha;
            while ((linha = await arquivo.ReadLineAsync()) != null)
            {
                if (linha.Length < 6)
                {
                    Console.WriteLine($"Linha do arquivo TXT inválida: {linha}");
                    continue;
                }

                string charVenda = linha.Substring(6, 1);
                string linhaCodigo = linha.Substring(0, 6);
                string linhaDescricao = linha.Substring(7, 15);
                string linhaValidade = linha.Substring(36, 3);
                string linhaPreco = linha.Substring(29, 7); // 7 para pegar 6 e o espaço

                int venda = charVenda.Equals("U", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                int plu = int.Parse(linhaCodigo);
                double preco = double.Parse(linhaPreco, CultureInfo.InvariantCulture) / 100;
                int validade = int.Parse(linhaValidade);
                string descricao = linhaDescricao.Trim();

                var produto = new Produto
                {
                    Plu = plu,
                    Descricao = descricao,
                    Preco = preco,
                    Venda = venda,
                    Validade = validade
                };

                if (produto.Plu <= 200 && produto.Plu > 0)
                {
                    produtos.Add(produto);
                }
            }

            // Verifica se houve erro durante a leitura
            if (arquivo.EndOfStream)
            {
                Console.WriteLine("Erro ao ler o arquivo TXT.");
            }

            Console.WriteLine($"Total de produtos lidos: {produtos.Count}");

            // Processa produtos
            return produtos;
        }
        public async Task ProcessarProdutosAsync(List<Produto> produtos)
        {
            foreach (var produto in produtos)
            {
                try
                {
                    bool existe = await ExisteProdutoAsync(produto.Plu);

                    // Atualiza ou cria o produto
                    if (existe)
                    {
                        bool ehIgual = await ComparaDBAsync(produto.Plu, produto.Descricao, produto.Preco, produto.Venda, produto.Validade);
                        if (!ehIgual)
                        {
                            await EditProductAsync(produto);
                            Console.WriteLine("Produto atualizado: " + produto.Plu);
                        }
                    }
                    else
                    {
                        await CriaNovoProdutoAsync(produto);
                        Console.WriteLine("Produto criado: " + produto.Plu);
                    }
                }
                catch (Exception ex)
                {
                    var errMsg = $"Erro ao processar o produto PLU {produto.Plu}: {ex.Message}";
                    Console.WriteLine(errMsg);
                    RegistrarErro(errMsg);
                }
            }
        }

        private async Task<bool> ExisteProdutoAsync(int plu)
        {
            var produtos = await _dataBaseService.GetAllProdutosAsync();
            return produtos.Any(p => p.Plu == plu);
        }

        private async Task<bool> ComparaDBAsync(int plu, string descricao, double preco, int venda, int validade)
        {
            var produtos = await _dataBaseService.GetAllProdutosAsync();
            var produtoDb = produtos.FirstOrDefault(p => p.Plu == plu);

            if (produtoDb != null)
            {
                return produtoDb.Descricao == descricao &&
                       Math.Abs(produtoDb.Preco - preco) < 0.01 &&
                       produtoDb.Venda == venda &&
                       produtoDb.Validade == validade;
            }
            return false;
        }

        private async Task EditProductAsync(Produto produto)
        {
            await _dataBaseService.EditarProdutoAsync(produto);
        }

        private async Task CriaNovoProdutoAsync(Produto produto)
        {
            await _dataBaseService.InsertIntoProdutosAsync(produto);
        }
    }
}
