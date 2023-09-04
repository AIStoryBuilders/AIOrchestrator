using OpenAI;
using OpenAI.Chat;
using System.Net;
using OpenAI.Files;
using OpenAI.Models;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Text.Json;
using Newtonsoft.Json;
using static AIOrchestrator.Model.OrchestratorMethods;
using Microsoft.Maui.Storage;
using static AIOrchestrator.Pages.Memory;

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        #region public async Task<string> SummarizeCharacter(string Filename, string paramSelectedCharacter, int intMaxLoops, int intChunkSize)
        public async Task<string> SummarizeCharacter(string Filename, string paramSelectedCharacter, int intMaxLoops, int intChunkSize)
        {
            LogService.WriteToLog("SummarizeCharacter - Start");

            string Summary = "";
            string Organization = SettingsService.Organization;
            string ApiKey = SettingsService.ApiKey;
            string SystemMessage = "";
            int TotalTokens = 0;

            ChatMessages = new List<ChatMessage>();

            // **** Create AIOrchestratorDatabase.json
            // Store Tasks in the Database as an array in a single property
            // Store the Last Read Index as a Property in Database
            // Store Summary as a Property in the Database
            AIOrchestratorDatabaseObject = new
            {
                CurrentTask = "Read Text",
                LastWordRead = 0,
                Summary = ""
            };

            // Save AIOrchestratorDatabase.json
            AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            objAIOrchestratorDatabase.WriteFile(AIOrchestratorDatabaseObject);

            // Create a new OpenAIClient object
            // with the provided API key and organization
            var api = new OpenAIClient(new OpenAIAuthentication(ApiKey, Organization));

            // Create a colection of chatPrompts
            ChatResponse ChatResponseResult = new ChatResponse();
            List<Message> chatPrompts = new List<Message>();

            // Call ChatGPT
            int CallCount = 0;

            // We need to start a While loop
            bool ChatGPTCallingComplete = false;
            int StartWordIndex = 0;

            while (!ChatGPTCallingComplete)
            {
                // Read Text
                var CurrentText = await ExecuteRead(Filename, StartWordIndex, intChunkSize);

                // *****************************************************
                dynamic Databasefile = AIOrchestratorDatabaseObject;

                // Update System Message
                SystemMessage = CreateSystemMessageCharacterSummary(CurrentText);

                chatPrompts = new List<Message>();

                chatPrompts.Insert(0,
                new Message(
                    Role.System,
                    SystemMessage
                    )
                );

                // Get a response from ChatGPT 
                var FinalChatRequest = new ChatRequest(
                    chatPrompts,
                    model: "gpt-3.5-turbo-0613",
                    temperature: 0.0,
                    topP: 1,
                    frequencyPenalty: 0,
                    presencePenalty: 0);

                ChatResponseResult = await api.ChatEndpoint.GetCompletionAsync(FinalChatRequest);

                var NamedCharactersFound = ChatResponseResult.FirstChoice.Message.Content;
                string[] NamedCharactersFoundArray = NamedCharactersFound.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // *******************************************************
                // Create a Vector database entry for each named Character found
                foreach (string NamedCharcter in NamedCharactersFoundArray)
                {
                    // Create a Vector database entry for each named Character found
                    if (NamedCharcter != "")
                    {
                        // Create a Vector database entry for each named Character found
                        await CreateVectorEntry(NamedCharcter, $"Character information {DateTime.Now.Ticks.ToString()}");
                    }
                }


                // *******************************************************
                // Update the Summary

                // Update the total number of tokens used by the API
                TotalTokens = TotalTokens + ChatResponseResult.Usage.TotalTokens ?? 0;

                LogService.WriteToLog($"Iteration: {CallCount} - TotalTokens: {TotalTokens} - result.FirstChoice.Message - {ChatResponseResult.FirstChoice.Message}");

                if (Databasefile.CurrentTask == "Read Text")
                {
                    // Keep looping
                    ChatGPTCallingComplete = false;
                    CallCount = CallCount + 1;
                    StartWordIndex = Databasefile.LastWordRead;

                    // Update the AIOrchestratorDatabase.json file
                    AIOrchestratorDatabaseObject = new
                    {
                        CurrentTask = "Read Text",
                        LastWordRead = Databasefile.LastWordRead,
                        Summary = ChatResponseResult.FirstChoice.Message.Content
                    };

                    // Check if we have exceeded the maximum number of calls
                    if (CallCount > intMaxLoops)
                    {
                        // Break out of the loop
                        ChatGPTCallingComplete = true;
                        LogService.WriteToLog($"* Breaking out of loop * Iteration: {CallCount}");
                        ReadTextEvent?.Invoke(this, new ReadTextEventArgs($"Break out of the loop - Iteration: {CallCount}"));
                    }
                    else
                    {
                        ReadTextEvent?.Invoke(this, new ReadTextEventArgs($"Continue to Loop - Iteration: {CallCount}"));
                    }
                }
                else
                {
                    // Break out of the loop
                    ChatGPTCallingComplete = true;
                    LogService.WriteToLog($"Iteration: {CallCount}");
                    ReadTextEvent?.Invoke(this, new ReadTextEventArgs($"Break out of the loop - Iteration: {CallCount}"));
                }
            }

            // *****************************************************
            // Output final summary

            // Save AIOrchestratorDatabase.json
            objAIOrchestratorDatabase.WriteFile(AIOrchestratorDatabaseObject);

            LogService.WriteToLog($"Summary - {Summary}");
            return Summary;
        }
        #endregion


        // Methods

        #region private string CreateSystemMessageCharacterSummary(string paramNewText)
        private string CreateSystemMessageCharacterSummary(string paramNewText)
        {
            return "You are a program that will identify the names of the named characters in the content of ###New Text###.\n" +
                    "Only respond with the names of the named characters nothing else.\n" +
                    "Only list each character name once.\n" +
                    "OList each character on a seperate line.\n" +
                    "Only respond with the names of the named characters nothing else.\n" +
                    $"###New Text### is: {paramNewText}\n";
        }
        #endregion
        private async Task CreateVectorEntry(string namedCharacter, string memoryContent)
        {
            var VectorContent = namedCharacter + ":" + memoryContent;

            // **** Call OpenAI and get embeddings for the memory text
            // Create an instance of the OpenAI client
            var api = new OpenAIClient(new OpenAIAuthentication(SettingsService.ApiKey, SettingsService.Organization));
            // Get the model details
            var model = await api.ModelsEndpoint.GetModelDetailsAsync("text-embedding-ada-002");
            // Get embeddings for the text
            var embeddings = await api.EmbeddingsEndpoint.CreateEmbeddingAsync(VectorContent, model);
            // Get embeddings as an array of floats
            var EmbeddingVectors = embeddings.Data[0].Embedding.Select(d => (float)d).ToArray();
            // Loop through the embeddings
            List<VectorData> AllVectors = new List<VectorData>();
            for (int i = 0; i < EmbeddingVectors.Length; i++)
            {
                var embeddingVector = new VectorData
                {
                    VectorValue = EmbeddingVectors[i]
                };
                AllVectors.Add(embeddingVector);
            }
            // Convert the floats to a single string
            var VectorsToSave = "[" + string.Join(",", AllVectors.Select(x => x.VectorValue)) + "]";

            // Write the memory to the .csv file
            var AIOrchestratorMemoryPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/AIOrchestrator/AIOrchestratorMemory.csv";
            using (var streamWriter = new StreamWriter(AIOrchestratorMemoryPath, true))
            {
                streamWriter.WriteLine(VectorContent + "|" + VectorsToSave);
            }
        }
    }
}