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
    public class TXitensService : IImportService
    {
        private readonly DataBaseService _dataBaseService;

        public TXitensService()
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
            var produtos = new List<Produto>();

            try
            {
                using (var reader = new StreamReader(caminho))
                {
                    while (!reader.EndOfStream)
                    {
                        var linha = await reader.ReadLineAsync();

                        // Verifica se a linha é válida
                        if (string.IsNullOrWhiteSpace(linha) || linha.Length < 34)
                        {
                            var errMsg = $"Linha do arquivo TXT inválida: {linha}";
                            Console.WriteLine(errMsg);
                            RegistrarErro(errMsg);
                            continue;
                        }

                        // Processa os campos da linha
                        try
                        {
                            var venda = int.Parse(linha.Substring(4, 1));
                            var plu = int.Parse(linha.Substring(5, 6));
                            var precoStr = linha.Substring(11, 6);
                            var preco = double.Parse(precoStr, CultureInfo.InvariantCulture) / 100;
                            var validade = int.Parse(linha.Substring(17, 3));
                            var descricao = linha.Length < 35 ? linha.Substring(20).Trim() : linha.Substring(20, 15).Trim();

                            // Cria o objeto Produto
                            var produto = new Produto
                            {
                                Plu = plu,
                                Descricao = descricao,
                                Preco = preco,
                                Venda = venda,
                                Validade = validade
                            };

                            // Verifica se o produto é válido e o adiciona à lista
                            if (produto.Plu > 0 && produto.Plu <= 200)
                            {
                                produtos.Add(produto);
                            }
                        }
                        catch (Exception ex)
                        {
                            var errMsg = $"Erro ao processar a linha: {linha}. Erro: {ex.Message}";
                            Console.WriteLine(errMsg);
                            RegistrarErro(errMsg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao ler o arquivo: " + ex.Message);
                RegistrarErro($"Erro ao ler o arquivo: {ex.Message}");
            }

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
