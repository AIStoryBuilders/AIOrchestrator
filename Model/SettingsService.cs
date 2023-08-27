using Newtonsoft.Json;

namespace AIOrchestrator.Model
{
    public class SettingsService
    {
        // Properties
        public string Organization { get; set; }
        public string ApiKey { get; set; }

        // Constructor
        public SettingsService() 
        {
            ReloadSettings();
        }

        public void ReloadSettings()
        {
            // Get OpenAI API key from appsettings.json
            // AIOrchestrator Directory
            var AIOrchestratorSettingsPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator/AIOrchestratorSettings.config";

            string AIOrchestratorSettings = "";

            // Open the file to get existing content
            using (var streamReader = new StreamReader(AIOrchestratorSettingsPath))
            {
                AIOrchestratorSettings = streamReader.ReadToEnd();
            }

            // Convert the JSON to a dynamic object
            dynamic AIOrchestratorSettingsObject = JsonConvert.DeserializeObject(AIOrchestratorSettings);

            Organization = AIOrchestratorSettingsObject.OpenAIServiceOptions.Organization;
            ApiKey = AIOrchestratorSettingsObject.OpenAIServiceOptions.ApiKey;
        }
    }
}