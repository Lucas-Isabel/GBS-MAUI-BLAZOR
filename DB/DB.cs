using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using IMPORTADOR_CLIPSE.Models;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using System.Runtime.CompilerServices;

namespace IMPORTADOR_CLIPSE.DB
{
    public class DataBaseService
    {
        private string _dbPath;
        public DataBaseService()
        {
            string appDirectory = AppContext.BaseDirectory; // Obtém o diretório onde o programa está sendo executado
            string dbDirectory = Path.Combine(appDirectory, "DB"); // Cria a pasta "DB" dentro do diretório do programa

            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory); // Cria a pasta "DB" se ela não existir
            }

            _dbPath = Path.Combine(dbDirectory, "andine.db"); // Define o caminho completo do banco de dados
            CreateDatabaseAsync().Wait();
        }

        private async Task CreateDatabaseAsync()
        {
            if (!File.Exists(_dbPath))
            {
                await using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS produtos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    plu INTEGER UNIQUE CHECK(plu > 0 AND plu <= 200),
                    descricao TEXT CHECK(length(descricao) <= 15 AND length(descricao) > 0),
                    venda INTEGER CHECK(venda >= 0 AND venda <= 10),
                    validade INTEGER CHECK(validade >= 0 AND validade <= 200),
                    preco DOUBLE CHECK(preco > 0 AND preco < 1000),
                    createdAt DATE DEFAULT (datetime('now')),
                    updatedAt DATE,
                    updateBy TEXT CHECK(length(updateBy) <= 30)
                );
                
                CREATE TABLE IF NOT EXISTS balancas (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    descricao TEXT CHECK(length(descricao) <= 30),
                    event_date DATE
                );
                ";

                await command.ExecuteNonQueryAsync();
                Console.WriteLine("Banco de dados e tabelas criados com sucesso.");

                // Verifica se o evento inicial "import_event" já existe na tabela balancas
              
            }
            await using var newconnection = new SqliteConnection($"Data Source={_dbPath}");
            await newconnection.OpenAsync();

            var newcommand = newconnection.CreateCommand();

            newcommand.CommandText = "SELECT COUNT(1) FROM balancas WHERE descricao = 'import_event'";
            var count = (long)await newcommand.ExecuteScalarAsync();

            // Se o registro não existir, insere o registro com a data 01/01/1999
            if (count == 0)
            {
                newcommand.CommandText = @"
                    INSERT INTO balancas (descricao, event_date) 
                    VALUES ('import_event', '1999-01-01')";
                await newcommand.ExecuteNonQueryAsync();
                Debug.WriteLine("Registro 'import_event' inserido na tabela 'balancas'.");
            }
        }

        public async Task<List<string>> ObterCodigosFaltantesAsync()
        {
            var codigosFaltantes = new List<string>();

            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                // Consulta os códigos PLU existentes
                var command = connection.CreateCommand();
                command.CommandText = "SELECT plu FROM produtos";
                var codigosPresentes = new HashSet<int>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int codigo = reader.GetInt32(0);
                    codigosPresentes.Add(codigo);
                }

                // Verifica quais códigos estão faltando de 1 a 200
                for (int i = 1; i <= 200; i++)
                {
                    if (!codigosPresentes.Contains(i))
                    {
                        string codigoFaltante = i.ToString("D3"); // Formata para 3 dígitos
                        codigosFaltantes.Add(codigoFaltante);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter códigos faltantes: {ex.Message}");
            }

            return codigosFaltantes;
        }

        public async Task UpdateImportEvent()
        {
            var eventDate = DateTime.Now;
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE balancas
                SET event_date = @event_date
                WHERE descricao = @descricao";

                command.Parameters.AddWithValue("@descricao", "import_event");
                command.Parameters.AddWithValue("@event_date", eventDate);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                Debug.WriteLine(rowsAffected > 0 ? "Evento atualizado com sucesso." : "Nenhum evento foi encontrado com a descrição fornecida.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao editar o produto: {ex.Message}");
            }
        }


        public async Task InsertIntoBalancasAsync(string descricao)
        {
            var eventDate = DateTime.Now;

            await using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO balancas (descricao, event_date) VALUES (@descricao, @event_date)";
            command.Parameters.AddWithValue("@descricao", descricao);
            command.Parameters.AddWithValue("@event_date", eventDate);

            await command.ExecuteNonQueryAsync();
            Console.WriteLine("Dados inseridos na tabela 'balancas'.");
        }


        public async Task<List<string>> ObterDadosProdutosAsync()
        {
            var produtos = new List<string>();
            var allProdutos = await GetAllProdutosAsync();

            foreach (var produto in allProdutos)
            {
                string precoStr = TransformarPreco(produto.Preco);
                string strVenda = produto.Venda == 1 ? "\x20" : "\x10";
                string descricao = PreencherDescricao(produto.Descricao, 15);
                string produtoStr = $"\x03{produto.Plu:D3}{descricao.ToUpper()}00{precoStr}{produto.Validade:D3}{strVenda}";
                string produtoECodigo = produtoStr + "£" + produto.Plu;
                Debug.WriteLine(produtoECodigo);
                produtos.Add(produtoECodigo);
            }

            return produtos;
        }

        public async Task<List<string>> ObterDadosProdutosAtualizadosAsync()
        {
            var produtos = new List<string>();
            var allProdutos = await GetProdutosAtualizadosDesdeImportAsync();

            foreach (var produto in allProdutos)
            {
                string precoStr = TransformarPreco(produto.Preco);
                string strVenda = produto.Venda == 1 ? "\x20" : "\x10";
                string descricao = PreencherDescricao(produto.Descricao, 15);
                string produtoStr = $"\x03{produto.Plu:D3}{descricao.ToUpper()}00{precoStr}{produto.Validade:D3}{strVenda}";
                string produtoECodigo = produtoStr + "£" + produto.Plu;
                Debug.WriteLine(produtoECodigo);
                produtos.Add(produtoECodigo);
            }

            return produtos;
        }


        public string TransformarPreco(double preco)
        {
            string precoStr = preco.ToString("F2").Replace(".", "").Replace(",","");
            string precoComZeros = precoStr.PadLeft(6, '0');
            return precoComZeros;
        }

        public string PreencherDescricao(string descricao, int tamanho)
        {
            string descricaoPreenchida = descricao.PadRight(tamanho);
            return descricaoPreenchida;
        }

        public async Task InsertIntoProdutosAsync(Produto produto)
        {
            DateTime datetime = DateTime.Now;
            try
            {
                await using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                INSERT INTO produtos (plu, descricao, preco, venda, validade, createdAt, updatedAt, updateBy) 
                VALUES (@plu, @descricao, @preco, @venda, @validade, @datetime, @datetime, @updateBy)";

                command.Parameters.AddWithValue("@plu", produto.Plu);
                command.Parameters.AddWithValue("@descricao", produto.Descricao.ToUpper());
                command.Parameters.AddWithValue("@preco", produto.Preco);
                command.Parameters.AddWithValue("@venda", produto.Venda);
                command.Parameters.AddWithValue("@datetime", datetime);
                command.Parameters.AddWithValue("@validade", produto.Validade);
                command.Parameters.AddWithValue("@updateBy", "Sistema"); // Ajuste conforme necessário

                await command.ExecuteNonQueryAsync();
                Console.WriteLine("Produto inserido com sucesso na tabela 'produtos'.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao inserir produto: {ex.Message}");
            }
        }

        public async Task<List<Produto>> GetAllProdutosAsync()
        {
            var produtos = new List<Produto>();

            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var query = "SELECT plu, descricao, preco, venda, validade FROM produtos ORDER BY plu";
                using var command = new SqliteCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var produto = new Produto
                    {
                        Plu = reader.GetInt32(0),
                        Descricao = reader.GetString(1),
                        Preco = reader.GetDouble(2),
                        Venda = reader.GetInt32(3),
                        Validade = reader.GetInt32(4)
                    };
                    produtos.Add(produto);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar produtos: {ex.Message}");
            }

            return produtos;
        }

        public async Task ExcluirProdutoAsync(int plu)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM produtos WHERE plu = @plu";
                command.Parameters.AddWithValue("@plu", plu);

                await command.ExecuteNonQueryAsync();
                Console.WriteLine("Produto excluído com sucesso.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao excluir produto: {ex.Message}");
            }
        }

        public async Task<List<Produto>> GetProdutosAtualizadosDesdeImportAsync()
        {
            var produtos = new List<Produto>();

            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();

                // Consulta para obter a data event_date da tabela balancas
                var eventDateQuery = "SELECT event_date FROM balancas WHERE descricao = 'import_event'";
                using var eventDateCommand = new SqliteCommand(eventDateQuery, connection);
                var eventDate = (string)await eventDateCommand.ExecuteScalarAsync();

                if (eventDate != null)
                {
                    // Agora consulta os produtos que foram atualizados após a data de importação
                    var query = @"
                SELECT plu, descricao, preco, venda, validade 
                FROM produtos 
                WHERE updatedAt > @eventDate 
                ORDER BY plu";

                    using var command = new SqliteCommand(query, connection);
                    command.Parameters.AddWithValue("@eventDate", eventDate);

                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var produto = new Produto
                        {
                            Plu = reader.GetInt32(0),
                            Descricao = reader.GetString(1),
                            Preco = reader.GetDouble(2),
                            Venda = reader.GetInt32(3),
                            Validade = reader.GetInt32(4)
                        };
                        produtos.Add(produto);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar produtos atualizados: {ex.Message}");
            }

            return produtos;
        }


        public async Task EditarProdutoAsync(Produto produto)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                await connection.OpenAsync();
                DateTime updateAt = DateTime.Now;
                var command = connection.CreateCommand();
                command.CommandText = @"
                UPDATE produtos
                SET descricao = @descricao,
                    preco = @preco,
                    venda = @venda,
                    validade = @validade,
                    updatedAt = @datetime,
                    updateBy = @updateBy
                WHERE plu = @plu";

                command.Parameters.AddWithValue("@plu", produto.Plu);
                command.Parameters.AddWithValue("@descricao", produto.Descricao.ToUpper());
                command.Parameters.AddWithValue("@preco", produto.Preco);
                command.Parameters.AddWithValue("@venda", produto.Venda);
                command.Parameters.AddWithValue("@validade", produto.Validade);
                command.Parameters.AddWithValue("@datetime", updateAt);
                command.Parameters.AddWithValue("@updateBy", "Sistema");

                var rowsAffected = await command.ExecuteNonQueryAsync();
                Console.WriteLine(rowsAffected > 0 ? "Produto atualizado com sucesso." : "Nenhum produto foi encontrado com o PLU fornecido.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao editar o produto: {ex.Message}");
            }
        }
    }
}
