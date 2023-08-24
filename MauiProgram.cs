using AIOrchestrator.Data;
using Microsoft.Extensions.Logging;
using Radzen;

namespace AIOrchestrator
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

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<WeatherForecastService>();

            // Radzen
            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<TooltipService>();
            builder.Services.AddScoped<ContextMenuService>();

            // Load Default files
            var folderPath = "";
            var filePath = "";

            // AIOrchestrator Directory
            folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // AIOrchestratorLog.csv
            filePath = Path.Combine(folderPath, "AIOrchestratorLog.csv");

            if (!File.Exists(filePath))
            {
                using (var streamWriter = new StreamWriter(filePath))
                {
                    streamWriter.WriteLine("Application started at " + DateTime.Now);
                }
            }
            else
            {
                // File already exists
                var file_content = "";
                // Open the file to get existing content
                using (var streamReader = new StreamReader(filePath))
                {
                    file_content = streamReader.ReadToEnd();
                }

                // Append the text to csv file
                using (var streamWriter = new StreamWriter(filePath))
                {
                    streamWriter.WriteLine(file_content);
                    streamWriter.WriteLine("Application started at " + DateTime.Now);
                }
            }

            // AIOrchestratorLog.csv
            filePath = Path.Combine(folderPath, "AIOrchestratorLog.csv");

            if (!File.Exists(filePath))
            {
                using (var streamWriter = new StreamWriter(filePath))
                {
                    streamWriter.WriteLine("Application started at " + DateTime.Now);
                }
            }

            // AIOrchestratorMemory.csv
            filePath = Path.Combine(folderPath, "AIOrchestratorMemory.csv");

            if (!File.Exists(filePath))
            {
                using (var streamWriter = new StreamWriter(filePath))
                {
                    streamWriter.WriteLine("** AIOrchestratorMemory started at " + DateTime.Now);
                }
            }

            // AIOrchestratorDatabase.csv
            filePath = Path.Combine(folderPath, "AIOrchestratorDatabase.csv");

            if (!File.Exists(filePath))
            {
                using (var streamWriter = new StreamWriter(filePath))
                {
                    streamWriter.WriteLine("** AIOrchestratorDatabase started at " + DateTime.Now);
                }
            }

            // AIOrchestratorSettings.config
            filePath = Path.Combine(folderPath, "AIOrchestratorSettings.config");

            if (!File.Exists(filePath))
            {
                using (var streamWriter = new StreamWriter(filePath))
                {
                    streamWriter.WriteLine(
                        """
                        "OpenAIServiceOptions": {
                        "Organization": "** Your OpenAI Organization **",
                        "ApiKey": "** Your OpenAI ApiKey **"
                        };                        
                        """);
                }
            }

            return builder.Build();
        }
    }
}