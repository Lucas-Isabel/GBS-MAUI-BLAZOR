using IMPORTADOR_CLIPSE.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMPORTADOR_CLIPSE.FileReaderServices;
public interface IImportService
{
    Task<List<Produto>> LerItensDoArquivoAsync(string caminho);
    Task ProcessarProdutosAsync(List<Produto> produtos);
}
