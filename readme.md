# Projeto .NET MAUI

Este é um projeto .NET MAUI que visa demonstrar a criação de aplicativos multiplataforma utilizando a mais recente tecnologia da Microsoft. O projeto é direcionado para a plataforma .NET 8 e é compatível com máquinas x64.

## Requisitos

- .NET 8 SDK
- Visual Studio 2022 (ou superior) com suporte para .NET MAUI
- Máquina x64

## Como Executar

1. Clone o repositório para sua máquina local.
2. Abra o projeto no Visual Studio 2022.
3. Selecione o destino de execução (Android, iOS, Windows, etc.).
4. Pressione F5 para compilar e executar o aplicativo.

# Serviços UtilizadosServiços Utilizados
- DataBaseService: Serviço singleton para gerenciamento de banco de dados.
- ImportController: Controlador responsável pelas operações de importação.
- ItensMGVService: Serviço para gerenciamento de itens MGV.
- TXitensService: Serviço para gerenciamento de itens TX.
- CADitensService: Serviço para gerenciamento de itens CAD.
- SerialPortService: Serviço transient para comunicação via porta serial configurada para "COM3" e baud rate de 9600.
- AppState: Serviço singleton para gerenciamento do estado da aplicação.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests.

## Licença

Este projeto está licenciado sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.
