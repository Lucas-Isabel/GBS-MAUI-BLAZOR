using Microsoft.Extensions.Logging;
using IMPORTADOR_CLIPSE.DB;
using IMPORTADOR_CLIPSE.FileReaderServices;
using IMPORTADOR_CLIPSE.Controllers;
using IMPORTADOR_CLIPSE.Global;

namespace IMPORTADOR_CLIPSE
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<DataBaseService>();
            builder.Services.AddScoped<ImportController>();
            builder.Services.AddScoped<ItensMGVService>();
            builder.Services.AddScoped<TXitensService>();
            builder.Services.AddScoped<CADitensService>();
            builder.Services.AddTransient<SerialPortService>(provider => new SerialPortService("COM3", 9600));
            builder.Services.AddSingleton<AppState>();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
