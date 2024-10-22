using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IMPORTADOR_CLIPSE.DB; // Adicione o namespace para DataBaseService

public class SerialPortService
{
    public event Action<double> OnProgress;
    public event Action<bool> OnComplete;
    public event Action<string> OnError;

    private SerialPort serialPort;
    private DataBaseService dataBaseService; // Instância do DataBaseService

    public SerialPortService(string portName, int baudRate)
    {
        serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 10000,
            WriteTimeout = 10000
        };
        dataBaseService = new DataBaseService(); // Inicializa o DataBaseService
    }

    public void ConfigurarPortaEVelocidade(string portName, int baudRate)
    {
        if (serialPort.IsOpen)
        {
            serialPort.Close();
        }

        serialPort.PortName = portName;
        serialPort.BaudRate = baudRate;
    }
    public async Task Finalizado()
    {
        await dataBaseService.UpdateImportEvent();
    }
    public async Task DeleteAsync()
    {
        // Aguarda a lista de códigos faltantes
        var plus = await dataBaseService.ObterCodigosFaltantesAsync();

        string portName = serialPort.PortName; // Usa a porta já configurada
        int baudRate = serialPort.BaudRate; // Usa a taxa de transmissão já configurada

        Debug.WriteLine($"CONFIG: {portName} {baudRate}");

        // Testa se a porta está aberta
        if (serialPort.IsOpen)
        {
            Debug.WriteLine($"Porta {portName} disponível.");

            foreach (var value in plus)
            {
                string resultado = ListarItens(serialPort, $"\x04{value}\x02");
                Debug.WriteLine($"Resposta para {value}: {resultado}");
            }
        }
        else
        {
            Debug.WriteLine($"Porta {portName} não disponível.");
        }
    }

    private bool TestPort()
    {
        // Verifica se a porta está aberta
        if (serialPort.IsOpen)
        {
            Debug.WriteLine($"Porta {serialPort.PortName} disponível.");
            return true; // A porta está disponível
        }
        else
        {
            Debug.WriteLine($"Porta {serialPort.PortName} não disponível.");
            return false; // A porta não está disponível
        }
    }

    private string ListarItens(SerialPort porta, string codigo)
    {
        try
        {
            byte[] buffer = Encoding.ASCII.GetBytes(codigo);
            porta.Write(buffer, 0, buffer.Length);

            StringBuilder resposta = new StringBuilder();
            byte[] buf = new byte[1];
            int tentativas = 0;

            while (tentativas < 3) // Limitar a leitura a 3 tentativas
            {
                int n = porta.Read(buf, 0, buf.Length);
                if (n == 0) break; // Sai do loop se não há mais dados

                resposta.Append(Encoding.ASCII.GetString(buf, 0, n));
                tentativas++;
            }

            return resposta.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao ler da porta serial: {ex.Message}");
            return string.Empty; // Retorna string vazia em caso de erro
        }
    }
    
    public async Task EnviarDadosAsync(CancellationToken token, int type, bool excluiProduto)
    {
        double progress = 0.0;

        if (excluiProduto)
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }
                await DeleteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erro ao excluir produtos: "+ex.ToString());
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }

        }
        try
        {
            var produtos = await ObterDadosProdutosAtualizadosAsync();
            if (type > 1)
            {
                produtos = await ObterDadosProdutosAsync();
            }   
            double progressUnit = 100.0 / produtos.Count;

            if (!serialPort.IsOpen)
            {
                serialPort.Open();
            }

            for (int i = 0; i < produtos.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                string produto = produtos[i];
                string[] resultado = produto.Split("£");
                produto = resultado[0].Replace("£", "").Replace(",", "");
                
                int resultadoEnvio = await EnviarDadoAsync(serialPort, produto, token);

                if (resultadoEnvio == 0)
                {
                    OnError?.Invoke($"Erro ao enviar dado: {produto}");
                    break;
                }

                progress += progressUnit;
                OnProgress?.Invoke(progress);
                await Task.Delay(20);
            }

            OnComplete?.Invoke(true);
        }
        catch (TimeoutException ex)
        {
            OnError?.Invoke($"Erro: Operação expirou: {ex.Message}");
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Erro: {ex.Message}");
        }
        finally
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
    }

    private async Task<List<string>> ObterDadosProdutosAsync()
    {
        var produtos = await dataBaseService.ObterDadosProdutosAsync(); // Obtém a lista de produtos do DataBaseService
        return produtos;
    }

    private async Task<List<string>> ObterDadosProdutosAtualizadosAsync()
    {
        var produtos = await dataBaseService.ObterDadosProdutosAtualizadosAsync(); // Obtém a lista de produtos do DataBaseService
        return produtos;
    }

    private async Task<int> EnviarDadoAsync(SerialPort porta, string dado, CancellationToken token)
    {
        int tentativas = 0;

        while (tentativas < 10)
        {
            token.ThrowIfCancellationRequested();

            tentativas++;
            var XOR = CalcularXORString(dado);
            string resultado = Escrever(porta, dado + XOR);

            if (resultado != "E" && (resultado.Contains("O") || resultado.Contains("K")))
            {
                Debug.WriteLine($"Resposta para {tentativas.ToString("X2")}: {resultado} - XOR {XOR} para {dado}");
                return 1;
    
            }
            await Task.Delay(100);
        }

        return 0;
    }



    private string Escrever(SerialPort porta, string codigo)
    {
        try
        {
            // Converter a string para bytes, incluindo os caracteres hexadecimais
            byte[] buffer = Encoding.ASCII.GetBytes(codigo);

            // Enviar os bytes para a porta serial
            porta.Write(buffer, 0, buffer.Length);

            // Ler a resposta como bytes e convertê-la de volta para string
            byte[] respostaBuffer = new byte[porta.BytesToRead];
            porta.Read(respostaBuffer, 0, respostaBuffer.Length);

            string resposta = Encoding.ASCII.GetString(respostaBuffer);
            return resposta;
        }
        catch (TimeoutException ex)
        {
            Debug.WriteLine($"Timeout ao escrever na porta serial: {ex.Message}");
            return "E";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao escrever na porta serial: {ex.Message}");
            return "E";
        }
    }



    private string CalcularXORString(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        char resultado = s[0];

        for (int i = 1; i < s.Length; i++)
        {
            resultado ^= s[i];
        }

        return resultado.ToString();
    }

    // Implementações fictícias para transformação de preço e preenchimento de descrição
    private string TransformarPreco(decimal preco)
    {
        return preco.ToString("F2").Replace(",", "").Replace(".", "");
    }

    private string PreencherDescricao(string descricao, int maxLength)
    {
        if (descricao.Length > maxLength)
        {
            return descricao.Substring(0, maxLength);
        }
        return descricao.PadRight(maxLength);
    }
}
