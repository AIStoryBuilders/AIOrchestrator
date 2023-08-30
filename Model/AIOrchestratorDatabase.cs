using Newtonsoft.Json;
using OpenAI.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIOrchestrator.Model
{
    public class AIOrchestratorDatabase
    {
        // Constructor
        public AIOrchestratorDatabase() { }

        public string ReadFile()
        {
            string response;
            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator";
            string filePath = Path.Combine(folderPath, "AIOrchestratorDatabase.json");

            // Open the file to get existing content
            using (var streamReader = new StreamReader(filePath))
            {
                response = streamReader.ReadToEnd();
            }

            return response;
        }

        public dynamic ReadFileDynamic()
        {
            string FileContents;
            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator";
            string filePath = Path.Combine(folderPath, "AIOrchestratorDatabase.json");

            // Open the file to get existing content
            using (var streamReader = new StreamReader(filePath))
            {
                FileContents = streamReader.ReadToEnd();
            }

            dynamic AIOrchestratorDatabaseObject = JsonConvert.DeserializeObject(FileContents);

            return AIOrchestratorDatabaseObject;
        }

        public async Task WriteFile(dynamic AIOrchestratorDatabaseObject)
        {
            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator";
            string filePath = Path.Combine(folderPath, "AIOrchestratorDatabase.json");

            // Convert the dynamic object back to JSON
            var AIOrchestratorSettings = JsonConvert.SerializeObject(AIOrchestratorDatabaseObject, Formatting.Indented);

            // Write the JSON to the file
            using (var streamWriter = new StreamWriter(filePath))
            {
                await streamWriter.WriteAsync(AIOrchestratorSettings);
            }
        }

        public async Task WriteFile(string AIOrchestratorDatabaseContent)
        {
            string folderPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator";
            string filePath = Path.Combine(folderPath, "AIOrchestratorDatabase.json");

            // Write the JSON to the file
            using (var streamWriter = new StreamWriter(filePath))
            {
                await streamWriter.WriteAsync(AIOrchestratorDatabaseContent);
            }
        }
    }
}