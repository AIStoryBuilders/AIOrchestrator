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

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        List<ChatMessage> ChatMessages = new List<ChatMessage>();

        #region public async Task<string> ReadText()
        public async Task<string> ReadText()
        {
            int intMaxLoops = 3;
            int intChunkSize = 2000;

            LogService.WriteToLog("ReadText - Start");

            string Organization = SettingsService.Organization;
            string ApiKey = SettingsService.ApiKey;
            string SystemMessage = "";
            int TotalTokens = 0;

            ChatMessages = new List<ChatMessage>();

            // **** Create AIOrchestratorDatabase.json
            // Store Tasks in the Database as an array in a single property
            // Store the Last Read Index as a Property in Database
            // Store Summary as a Property in the Database
            dynamic AIOrchestratorSettingsObject = new
            {
                CurrentTask = "Read Text",
                LastWordRead = 0,
                Summary = ""
            };

            // Save AIOrchestratorDatabase.json
            AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            objAIOrchestratorDatabase.WriteFile(AIOrchestratorSettingsObject);

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
                var CurrentText = await ExecuteRead(StartWordIndex, intChunkSize);

                // *****************************************************
                // AIOrchestratorDatabase.json
                objAIOrchestratorDatabase = new AIOrchestratorDatabase();
                dynamic Databasefile = objAIOrchestratorDatabase.ReadFileDynamic();
                string CurrentSummary = Databasefile.Summary ?? "";

                // Update System Message
                SystemMessage = CreateSystemMessage(CurrentSummary, CurrentText);

                chatPrompts = new List<Message>();

                chatPrompts.Insert(0,
                new Message(
                    Role.System,
                    SystemMessage
                    )
                );

                // Get a response from ChatGPT 
                var chatRequest = new ChatRequest(
                    chatPrompts,
                    model: "gpt-3.5-turbo-0613",
                    temperature: 0.0,
                    topP: 1,
                    frequencyPenalty: 0,
                    presencePenalty: 0);

                ChatResponseResult = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

                // Update the total number of tokens used by the API
                TotalTokens = TotalTokens + ChatResponseResult.Usage.TotalTokens ?? 0;
                LogService.WriteToLog($"TotalTokens - {TotalTokens}");

                if (Databasefile.CurrentTask == "Read Text")
                {
                    // Keep looping
                    ChatGPTCallingComplete = false;
                    CallCount = CallCount + 1;
                    StartWordIndex = Databasefile.LastWordRead;

                    // Update the AIOrchestratorDatabase.json file
                    AIOrchestratorSettingsObject = new
                    {
                        CurrentTask = "Read Text",
                        LastWordRead = Databasefile.LastWordRead,
                        Summary = ChatResponseResult.FirstChoice.Message.Content
                    };

                    // Save AIOrchestratorDatabase.json
                    objAIOrchestratorDatabase = new AIOrchestratorDatabase();
                    objAIOrchestratorDatabase.WriteFile(AIOrchestratorSettingsObject);

                    // Check if we have exceeded the maximum number of calls
                    if (CallCount > intMaxLoops)
                    {
                        // Break out of the loop
                        ChatGPTCallingComplete = true;
                        LogService.WriteToLog($"* Breaking out of loop * CallCount - {CallCount}");
                    }
                }
                else
                {
                    // Break out of the loop
                    ChatGPTCallingComplete = true;
                    LogService.WriteToLog($"CallCount - {CallCount}");
                }
            }

            // Create a new Message object with the response and other details
            // and add it to the messages list
            ChatMessages.Add(new ChatMessage
            {
                Prompt = ChatResponseResult.FirstChoice.Message,
                Role = Role.Assistant,
                Tokens = ChatResponseResult.Usage.CompletionTokens ?? 0
            });

            LogService.WriteToLog($"result.FirstChoice.Message - {ChatResponseResult.FirstChoice.Message}");
            return ChatResponseResult.FirstChoice.Message;
        }
        #endregion

        #region private async Task<string> ExecuteRead(int paramStartWordIndex, int intChunkSize)
        private async Task<string> ExecuteRead(int paramStartWordIndex, int intChunkSize)
        {
            LogService.WriteToLog($"Read_Text - {paramStartWordIndex}");

            // Read the Text from the file
            var ReadTextResult = await ReadTextFromFile(paramStartWordIndex, intChunkSize);

            // *****************************************************
            dynamic ReadTextFromFileObject = JsonConvert.DeserializeObject(ReadTextResult);
            string ReadTextFromFileText = ReadTextFromFileObject.Text;
            int intCurrentWord = ReadTextFromFileObject.CurrentWord;
            int intTotalWords = ReadTextFromFileObject.TotalWords;

            // *****************************************************
            // AIOrchestratorDatabase.json
            var objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            dynamic Databasefile = objAIOrchestratorDatabase.ReadFileDynamic();

            string strCurrentTask = Databasefile.CurrentTask;
            int intLastWordRead = intCurrentWord;
            string strSummary = Databasefile.Summary ?? "";

            // If we are done reading the text, then summarize it
            if (intCurrentWord >= intTotalWords)
            {
                strCurrentTask = "Summarize Text";
            }

            // Prepare object to save to AIOrchestratorDatabase.json
            dynamic AIOrchestratorSettingsObject = new
            {
                CurrentTask = strCurrentTask,
                LastWordRead = intLastWordRead,
                Summary = strSummary
            };

            // Update the AIOrchestratorDatabase.json file
            objAIOrchestratorDatabase.WriteFile(AIOrchestratorSettingsObject);

            return ReadTextFromFileText;
        }
        #endregion

        // Methods

        #region private string CreateSystemMessage(string paramCurrentSummary, string paramNewText)
        private string CreateSystemMessage(string paramCurrentSummary, string paramNewText)
        {
            return "You are a program that will be repeatedly called to read a large amount of text and to produce a chronological summary.\n" +
                    "Output a summary that combines the content in Current Summary combined with the contents in New Text.\n" +
                    "In the summary only use information gathered from reading the Text.\n" +
                    $"###Current Summary### is: {paramCurrentSummary}\n" +
                    $"###New Text### is: {paramNewText}\n";
        }
        #endregion

        #region private async Task<string> ReadTextFromFile(int startWordIndex, int intChunkSize)
        private async Task<string> ReadTextFromFile(int startWordIndex, int intChunkSize)
        {
            // Read the text from the file
            using var stream = await FileSystem.OpenAppPackageFileAsync("ATaleofTwoCities.txt");
            using var reader = new StreamReader(stream);

            var ATaleofTwoCitiesRaw = reader.ReadToEnd();

            // Split the text into words
            string[] ATaleofTwoCitiesWords = ATaleofTwoCitiesRaw.Split(
                               new char[] { ' ', '\t', '\n', '\r' },
                                              StringSplitOptions.RemoveEmptyEntries);

            // Get the total number of words
            int TotalWords = ATaleofTwoCitiesWords.Length;

            // Get words starting at the startWordIndex
            string[] ATaleofTwoCitiesWordsChunk = ATaleofTwoCitiesWords.Skip(startWordIndex).Take(intChunkSize).ToArray();

            // Set the current word to the startWordIndex + intChunkSize
            int CurrentWord = startWordIndex + intChunkSize;

            if (CurrentWord >= TotalWords)
            {
                // Set the current word to the total words
                CurrentWord = TotalWords;
            }

            string ReadTextFromFileResponse = """
                        {
                         "Text": "{ATaleofTwoCitiesWordsChunk}",
                         "CurrentWord": {CurrentWord},
                         "TotalWords": {TotalWords},
                        }
                        """;

            ReadTextFromFileResponse = ReadTextFromFileResponse.Replace("{ATaleofTwoCitiesWordsChunk}", string.Join(" ", ATaleofTwoCitiesWordsChunk));
            ReadTextFromFileResponse = ReadTextFromFileResponse.Replace("{CurrentWord}", CurrentWord.ToString());
            ReadTextFromFileResponse = ReadTextFromFileResponse.Replace("{TotalWords}", TotalWords.ToString());

            LogService.WriteToLog($"ReadTextFromFileResponse - {ReadTextFromFileResponse}");

            return ReadTextFromFileResponse;
        }
        #endregion
    }
}