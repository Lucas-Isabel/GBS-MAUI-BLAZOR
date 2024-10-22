using IMPORTADOR_CLIPSE.FileReaderServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMPORTADOR_CLIPSE.Controllers
{
    public class ImportController
    {
        private readonly IImportService _mgvService;
        private readonly IImportService _txitensService;
        private readonly IImportService _cadService;
        public ImportController(ItensMGVService mgvService, TXitensService txitensService, CADitensService cadService) //ItensOutroTipoService outroTipoService)
        {
            _mgvService = mgvService;
            _txitensService = txitensService;
            _cadService = cadService;
            //_outroTipoService = outroTipoService;
        }

        public IImportService ObterServicoPorTipoDeArquivo(string tipoDeArquivo)
        {
            return tipoDeArquivo switch
            {
                "MGV" => _mgvService,
                "TXITENS" => _txitensService,
                "CADTXT" => _cadService,
                //"OutroTipo" => _outroTipoService,
                _ => throw new Exception("Tipo de arquivo não suportado.")
            };
        }
    }
}
