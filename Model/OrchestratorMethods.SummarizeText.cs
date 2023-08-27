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

            // **** Create AIOrchestratorDatabase.json
            // Store Tasks in the Database as an array in a single Property
            // Store the Last Read Index as a Property in Database
            // Store Summary as a Property in the Database
            dynamic AIOrchestratorSettingsObject = new
            {
                Tasks = new string[] { "Read Text", "Summarize Text" },
                LastWordRead = 0,
                Summary = ""
            };

            AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            objAIOrchestratorDatabase.WriteFile(AIOrchestratorSettingsObject);

            var AIOrchestratorSettings = JsonConvert.SerializeObject(AIOrchestratorSettingsObject, Formatting.Indented);
            return SystemMessage();

        }

        private string SystemMessage()
        {
            AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            string AIOrchestratorDatabase = objAIOrchestratorDatabase.ReadFile();

            return  "You are a program that will be repeatedly called to read a large amount of text and to produce a summary\n" +
                    "You know what your current tasks are from the Tasks property in the Database.json file\n" +
                    "Only use information gathered from reading the Text\n" +
                    "Call the ReadText function to receive json that will contain the Text property that will contain a section of the Text\n" +
                    "When a Task is completed call the WriteDatabase function to update the Tasks Property in the Database.json file by removing the task\n" +
                    "Call the WriteDatabase function to update the Summary property in the Database.json file to store the text summary and update the Tasks property in the Database.json file to remove all the tasks\n" +
                    "If the Tasks property in the Database.json file says Read Text call the ReadText function to retrieve a section of text to summarize\n" +
                    "\tCall the WriteDatabase function to update the LastWordRead property  in the Database.json file to track progress\n" +
                    "\tTo keep track of the current position in the text, call the WriteDatabase function to update the number in the LastWordRead property in the Database.json file\n" +
                    "\tCall the WriteDatabase function to update the Tasks property in the Database.json file to indicate it needs to keep reading\n" +
                    "If ReadText function returns json that indicates the CurrentWord property is equal to the TotalWords property call the WriteDatabase function to update  and remove \"Read Text\" from Tasks collection in Database.json\n" +
                    "If the Tasks property in the Database.json file says Summarize Text output **SUMMARY** followed by the contents of the Summary property in the Database.json file\n" +
                    $"Current contents of the Database.json file is: {AIOrchestratorDatabase}";
        }
    }
}