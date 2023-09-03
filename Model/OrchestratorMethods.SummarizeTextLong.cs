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

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        #region public async Task<string> ReadTextLong(string Filename, int intMaxLoops, int intChunkSize)
        public async Task<string> ReadTextLong(string Filename, int intMaxLoops, int intChunkSize)
        {
            LogService.WriteToLog("ReadTextLong - Start");

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
                var CurrentText = await ExecuteReadLong(Filename, StartWordIndex, intChunkSize);

                // *****************************************************
                dynamic Databasefile = AIOrchestratorDatabaseObject;

                // Update System Message
                SystemMessage = CreateSystemMessageLong(CurrentText);

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

                // Update the Summary
                Summary = Summary + ChatResponseResult.FirstChoice.Message.Content + "\n\n";

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

        #region private async Task<string> ExecuteReadLong(string Filename, int paramStartWordIndex, int intChunkSize)
        private async Task<string> ExecuteReadLong(string Filename, int paramStartWordIndex, int intChunkSize)
        {
            // Read the Text from the file
            var ReadTextResult = await ReadTextFromFileLong(Filename, paramStartWordIndex, intChunkSize);

            // *****************************************************
            dynamic ReadTextFromFileObject = JsonConvert.DeserializeObject(ReadTextResult);
            string ReadTextFromFileText = ReadTextFromFileObject.Text;
            int intCurrentWord = ReadTextFromFileObject.CurrentWord;
            int intTotalWords = ReadTextFromFileObject.TotalWords;

            // *****************************************************
            dynamic Databasefile = AIOrchestratorDatabaseObject;

            string strCurrentTask = Databasefile.CurrentTask;
            int intLastWordRead = intCurrentWord;
            string strSummary = Databasefile.Summary ?? "";

            // If we are done reading the text, then summarize it
            if (intCurrentWord >= intTotalWords)
            {
                strCurrentTask = "Summarize Text";
            }

            // Prepare object to save to AIOrchestratorDatabase.json
            AIOrchestratorDatabaseObject = new
            {
                CurrentTask = strCurrentTask,
                LastWordRead = intLastWordRead,
                Summary = strSummary
            };

            return ReadTextFromFileText;
        }
        #endregion

        // Methods

        #region private string CreateSystemMessageLong(string paramNewText)
        private string CreateSystemMessageLong(string paramNewText)
        {
            return "You are a program that will produce a summary of the content of ###New Text###.\n" +
                    "Only respond with the contents of the summary nothing else.\n" +
                    "Always output complete sentances.\n" +
                    "Only respond with the contents of the summary nothing else.\n" +
                    $"###New Text### is: {paramNewText}\n";
        }
        #endregion

        #region private async Task<string> ReadTextFromFileLong(string filename, int startWordIndex, int intChunkSize)
        private async Task<string> ReadTextFromFileLong(string FileDocumentPath, int startWordIndex, int intChunkSize)
        {
            // Read the text from the file
            string TextFileRaw = "";

            // Open the file to get existing content
            using (var streamReader = new StreamReader(FileDocumentPath))
            {
                TextFileRaw = await streamReader.ReadToEndAsync();
            }

            // Split the text into words
            string[] TextFileWords = TextFileRaw.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // Get the total number of words
            int TotalWords = TextFileWords.Length;

            // Get words starting at the startWordIndex
            string[] TextFileWordsChunk = TextFileWords.Skip(startWordIndex).Take(intChunkSize).ToArray();

            // Set the current word to the startWordIndex + intChunkSize
            int CurrentWord = startWordIndex + intChunkSize;

            if (CurrentWord >= TotalWords)
            {
                // Set the current word to the total words
                CurrentWord = TotalWords;
            }

            string ReadTextFromFileResponse = """
                        {
                         "Text": "{TextFileWordsChunk}",
                         "CurrentWord": {CurrentWord},
                         "TotalWords": {TotalWords},
                        }
                        """;

            ReadTextFromFileResponse = ReadTextFromFileResponse.Replace("{TextFileWordsChunk}", string.Join(" ", TextFileWordsChunk));
            ReadTextFromFileResponse = ReadTextFromFileResponse.Replace("{CurrentWord}", CurrentWord.ToString());
            ReadTextFromFileResponse = ReadTextFromFileResponse.Replace("{TotalWords}", TotalWords.ToString());

            return ReadTextFromFileResponse;
        }
        #endregion

    }
}