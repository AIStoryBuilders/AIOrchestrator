using Newtonsoft.Json;

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        public event EventHandler<ReadTextEventArgs> ReadTextEvent;
        public SettingsService SettingsService { get; set; }
        public LogService LogService { get; set; }
        public string Summary { get; set; }
        dynamic AIOrchestratorDatabaseObject { get; set; }

        List<ChatMessage> ChatMessages = new List<ChatMessage>();

        // Constructor
        public OrchestratorMethods(SettingsService _SettingsService, LogService _LogService) 
        {
            SettingsService = _SettingsService;
            LogService = _LogService;
        }

        public class ReadTextEventArgs : EventArgs
        {
            public string Message { get; set; }

            public ReadTextEventArgs(string message)
            {
                Message = message;
            }
        }
    }
}