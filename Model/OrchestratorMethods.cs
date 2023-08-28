using Newtonsoft.Json;

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        public SettingsService SettingsService { get; set; }
        public LogService LogService { get; set; }

        // Constructor
        public OrchestratorMethods(SettingsService _SettingsService, LogService _LogService) 
        {
            SettingsService = _SettingsService;
            LogService = _LogService;
        }
    }
}