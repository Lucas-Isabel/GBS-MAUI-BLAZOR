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
    public class ItensMGVService : IImportService
    {
        private readonly DataBaseService _dataBaseService;

        public ItensMGVService()
        {
            _dataBaseService = new DataBaseService();
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

                        if (string.IsNullOrWhiteSpace(linha) || linha.Length < 34)
                        {
                            Console.WriteLine("Linha do arquivo TXT inválida: " + linha);
                            continue;
                        }

                        var venda = int.Parse(linha.Substring(2, 1));
                        var plu = int.Parse(linha.Substring(3, 6));

                        var precoStr = linha.Substring(9, 6);
                        var preco = double.Parse(precoStr, CultureInfo.InvariantCulture) / 100;

                        var validade = int.Parse(linha.Substring(15, 3));
                        var descricao = linha.Substring(18, 15).Trim();

                        var produto = new Produto
                        {
                            Plu = plu,
                            Descricao = descricao,
                            Preco = preco,
                            Venda = venda,
                            Validade = validade
                        };

                        if (produto.Plu > 0 && produto.Plu <= 200)
                        {
                            produtos.Add(produto);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao ler o arquivo: " + ex.Message);
            }

            return produtos;
        }

        public async Task ProcessarProdutosAsync(List<Produto> produtos)
        {
            foreach (var produto in produtos)
            {
                bool existe = await ExisteProdutoAsync(produto.Plu);

                if (existe)
                {
                    bool ehIgual = await ComparaDBAsync(produto.Plu, produto.Descricao, produto.Preco, produto.Venda, produto.Validade);

                    if (!ehIgual)
                    {
                        await EditProductAsync(produto);
                    }
                }
                else
                {
                    await CriaNovoProdutoAsync(produto);
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

