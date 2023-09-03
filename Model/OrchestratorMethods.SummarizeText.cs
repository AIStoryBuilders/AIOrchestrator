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
    public class ReadTextEventArgs : EventArgs
    {
        public string Message { get; set; }

        public ReadTextEventArgs(string message)
        {
            Message = message;
        }
    }
    public partial class OrchestratorMethods
    {
        public event EventHandler<ReadTextEventArgs> ReadTextEvent;

        dynamic AIOrchestratorDatabaseObject { get; set; }

        List<ChatMessage> ChatMessages = new List<ChatMessage>();

        #region public async Task<string> ReadText(string Filename, int intMaxLoops, int intChunkSize)
        public async Task<string> ReadText(string Filename, int intMaxLoops, int intChunkSize)
        {
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
                string CurrentSummary = AIOrchestratorDatabaseObject.Summary ?? "";

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
                var FinalChatRequest = new ChatRequest(
                    chatPrompts,
                    model: "gpt-3.5-turbo-0613",
                    temperature: 0.0,
                    topP: 1,
                    frequencyPenalty: 0,
                    presencePenalty: 0);

                ChatResponseResult = await api.ChatEndpoint.GetCompletionAsync(FinalChatRequest);

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
            // Clean up the final summary
            // Remove the System Message
            ReadTextEvent?.Invoke(this, new ReadTextEventArgs($"Clean up the final summary"));
            string RawSummary = ChatResponseResult.FirstChoice.Message.Content;

            chatPrompts = new List<Message>();

            chatPrompts.Insert(0,
            new Message(
                Role.System,
                $"Format the following summary to break it up into paragraphs: {RawSummary}"
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

            // Create a new Message object with the response and other details
            // and add it to the messages list
            ChatMessages.Add(new ChatMessage
            {
                Prompt = ChatResponseResult.FirstChoice.Message,
                Role = Role.Assistant,
                Tokens = ChatResponseResult.Usage.CompletionTokens ?? 0
            });

            // Save AIOrchestratorDatabase.json
            objAIOrchestratorDatabase.WriteFile(AIOrchestratorDatabaseObject);

            LogService.WriteToLog($"result.FirstChoice.Message - {ChatResponseResult.FirstChoice.Message}");
            return ChatResponseResult.FirstChoice.Message;
        }
        #endregion

        #region private async Task<string> ExecuteRead(string Filename, int paramStartWordIndex, int intChunkSize)
        private async Task<string> ExecuteRead(string Filename, int paramStartWordIndex, int intChunkSize)
        {
            // Read the Text from the file
            var ReadTextResult = await ReadTextFromFile(Filename, paramStartWordIndex, intChunkSize);

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

        #region private string CreateSystemMessage(string paramCurrentSummary, string paramNewText)
        private string CreateSystemMessage(string paramCurrentSummary, string paramNewText)
        {
            // The AI should keep this under 1000 words but here we will ensure it
            paramCurrentSummary = EnsureMaxWords(paramCurrentSummary, 1000);

            return "You are a program that will produce a ###New Summary### not to exceed 1000 words. \n" +
                    "Output a ###New Summary### that combines the content in ###Current Summary### combined with the content in ###New Text###. \n" +
                    "In the ###New Summary### only use information from ###Current Summary### and ###New Text###. \n" +
                    "Only respond with the contents of ###New Summary### nothing else. \n" +
                    "Do not allow the ###New Summary### to exceed 1000 words. \n" +
                    $"###Current Summary### is: {paramCurrentSummary}\n" +
                    $"###New Text### is: {paramNewText}\n";
        }
        #endregion

        #region private async Task<string> ReadTextFromFile(string filename, int startWordIndex, int intChunkSize)
        private async Task<string> ReadTextFromFile(string FileDocumentPath, int startWordIndex, int intChunkSize)
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

        #region public static string EnsureMaxWords(string paramCurrentSummary, int maxWords)
        public static string EnsureMaxWords(string paramCurrentSummary, int maxWords)
        {
            // Split the string by spaces to get words
            var words = paramCurrentSummary.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= maxWords)
            {
                // If the number of words is within the limit, return the original string
                return paramCurrentSummary;
            }

            // If the number of words exceeds the limit, return only the last 'maxWords' words
            return string.Join(" ", words.Reverse().Take(maxWords).Reverse());
        }
        #endregion
    }
}