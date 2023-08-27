using Newtonsoft.Json;

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        public SettingsService SettingsService { get; set; }

        // Constructor
        public OrchestratorMethods(SettingsService _SettingsService) 
        {
            SettingsService = _SettingsService;
        }
    }
}