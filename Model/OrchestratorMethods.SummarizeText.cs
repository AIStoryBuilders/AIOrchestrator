using Newtonsoft.Json;
using OpenAI.Files;

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        public string ReadText() 
        {
            string Organization = SettingsService.Organization;
            string ApiKey = SettingsService.ApiKey;

            // Get AIOrchestratorDatabase.json
            AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            string AIOrchestratorDatabase = objAIOrchestratorDatabase.ReadFile();

            return AIOrchestratorDatabase;

        }
    }
}