using Newtonsoft.Json;

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        public string ReadText() 
        {
            string Organization = SettingsService.Organization;
            string ApiKey = SettingsService.ApiKey;

            return Organization;
        }

    }
}