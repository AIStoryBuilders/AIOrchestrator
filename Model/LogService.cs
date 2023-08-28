using Newtonsoft.Json;
using OpenAI.Files;

namespace AIOrchestrator.Model
{
    public class LogService
    {
        // Properties
        public string[] AIOrchestratorLog { get; set; }

        // Constructor
        public LogService()
        {
            loadLog();
        }

        public void loadLog()
        {
            var AIOrchestratorLogPath =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator/AIOrchestratorLog.csv";

            // Read the lines from the .csv file
            using (var file = new System.IO.StreamReader(AIOrchestratorLogPath))
            {
                AIOrchestratorLog = file.ReadToEnd().Split('\n');
                if (AIOrchestratorLog[AIOrchestratorLog.Length - 1].Trim() == "")
                {
                    AIOrchestratorLog = AIOrchestratorLog.Take(AIOrchestratorLog.Length - 1).ToArray();
                }
            }
        }

        public void WriteToLog(string LogText)
        {
            // Open the file to get existing content
            var AIOrchestratorLogPath =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator/AIOrchestratorLog.csv";

            using (var file = new System.IO.StreamReader(AIOrchestratorLogPath))
            {
                AIOrchestratorLog = file.ReadToEnd().Split('\n');

                if (AIOrchestratorLog[AIOrchestratorLog.Length - 1].Trim() == "")
                {
                    AIOrchestratorLog = AIOrchestratorLog.Take(AIOrchestratorLog.Length - 1).ToArray();
                }
            }

            // Append the text to csv file
            using (var streamWriter = new StreamWriter(AIOrchestratorLogPath))
            {
                // Remove line breaks from the log text
                LogText = LogText.Replace("\n", " ");

                streamWriter.WriteLine(LogText);
                streamWriter.WriteLine(string.Join("\n", AIOrchestratorLog));
            }
        }
    }
}