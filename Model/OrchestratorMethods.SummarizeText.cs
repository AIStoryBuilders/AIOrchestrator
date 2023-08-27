using OpenAI;
using OpenAI.Chat;
using System.Net;
using OpenAI.Files;
using OpenAI.Models;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace AIOrchestrator.Model
{
    public partial class OrchestratorMethods
    {
        List<ChatMessage> ChatMessages = new List<ChatMessage>();

        #region public async Task<string> ReadText()
        public async Task<string> ReadText()
        {
            string Organization = SettingsService.Organization;
            string ApiKey = SettingsService.ApiKey;
            string SystemMessage = "";
            int TotalTokens = 0;

            // **** Create AIOrchestratorDatabase.json
            // Store Tasks in the Database as an array in a single property
            // Store the Last Read Index as a Property in Database
            // Store Summary as a Property in the Database
            dynamic AIOrchestratorSettingsObject = new
            {
                Tasks = new string[] { "Read Text", "Summarize Text" },
                LastWordRead = 0,
                Summary = ""
            };

            // Save AIOrchestratorDatabase.json
            AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            objAIOrchestratorDatabase.WriteFile(AIOrchestratorSettingsObject);

            // Pass AIOrchestratorDatabase.json to create the SystemMessage
            var paramAIOrchestratorDatabase = Newtonsoft.Json.JsonConvert.SerializeObject(AIOrchestratorSettingsObject, Newtonsoft.Json.Formatting.Indented);
            SystemMessage = CreateSystemMessage(paramAIOrchestratorDatabase);

            // Create a new OpenAIClient object
            // with the provided API key and organization
            var api =
            new OpenAIClient(new OpenAIAuthentication(ApiKey, Organization));

            // Create a colection of chatPrompts
            List<Message> chatPrompts = new List<Message>();

            // Add the existing Chat messages to chatPrompts
            chatPrompts = AddExistingChatMessags(chatPrompts, SystemMessage);

            // Call ChatGPT
            // Create a new ChatRequest object with the chat prompts and pass
            // it to the API's GetCompletionAsync method
            var chatRequest = new ChatRequest(
                chatPrompts,
                functions: GetDefinedFunctions(),
                functionCall: "auto",
                model: "gpt-3.5-turbo-0613", // Must use this model or higher
                temperature: 0.0,
                topP: 1,
                frequencyPenalty: 0,
                presencePenalty: 0);

            var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

            // *****************************************************
            // See if as a response ChatGPT wants to call a function
            if (result.FirstChoice.FinishReason == "function_call")
            {
                // Chat GPT wants to call a function

                // To allow ChatGPT to call multiple functions
                // We need to start a While loop
                bool FunctionCallingComplete = false;

                while (!FunctionCallingComplete)
                {
                    // Call the function
                    chatPrompts = await ExecuteFunction(result, chatPrompts);

                    // Get a response from ChatGPT (now that is has the results of the function)
                    chatRequest = new ChatRequest(
                        chatPrompts,
                        functions: GetDefinedFunctions(),
                        functionCall: "auto",
                        model: "gpt-3.5-turbo-0613", // Must use this model or higher
                        temperature: 0.0,
                        topP: 1,
                        frequencyPenalty: 0,
                        presencePenalty: 0);

                    result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

                    if (result.FirstChoice.FinishReason == "function_call")
                    {
                        // Keep looping
                        FunctionCallingComplete = false;
                    }
                    else
                    {
                        // Break out of the loop
                        FunctionCallingComplete = true;
                    }
                }
            }
            else
            {
                // ChatGPT did not want to call a function
            }

            // Create a new Message object with the response and other details
            // and add it to the messages list
            ChatMessages.Add(new ChatMessage
            {
                Prompt = result.FirstChoice.Message,
                Role = Role.Assistant,
                Tokens = result.Usage.CompletionTokens ?? 0
            });

            // Update the total number of tokens used by the API
            TotalTokens = TotalTokens + result.Usage.TotalTokens ?? 0;

            return "";
        }
        #endregion

        #region private async Task<List<Message>> ExecuteFunction(ChatResponse ChatResponseResult, List<Message> ParamChatPrompts)
        private async Task<List<Message>> ExecuteFunction(
        ChatResponse ChatResponseResult, List<Message> ParamChatPrompts)
        {
            // Get the arguments
            var functionArgs =
            ChatResponseResult.FirstChoice.Message.Function.Arguments.ToString();

            // Get the function name
            var functionName = ChatResponseResult.FirstChoice.Message.Function.Name;

            // Variable to hold the function result
            string functionResult = "";

            //Use select case to call the function
            switch (functionName)
            {
                case "Read_Text":
                    var objReadRequest =
                    JsonSerializer.Deserialize<ReadRequest>(functionArgs);

                    if (objReadRequest != null)
                    {
                        functionResult = ReadTextFromFile(objReadRequest.Readrequest.StartWordIndex);
                    }
                    break;
                case "Write_Database":
                    var objWriteDatabaseRequest =
                    JsonSerializer.Deserialize<WriteDatabaseRequest>(functionArgs);

                    if (objWriteDatabaseRequest != null)
                    {
                        // Save AIOrchestratorDatabase.json
                        AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
                        await objAIOrchestratorDatabase.WriteFile(objWriteDatabaseRequest.WriteDatabaserequest.file_contents);
                        functionResult = "{}";
                    }
                    break;
                default:
                    break;
            }

            // Create a new Message object with the user's prompt and other
            // details and add it to the messages list
            ChatMessages.Add(new ChatMessage
            {
                Prompt = functionResult,
                Role = Role.Function,
                FunctionName = functionName,
                Tokens = ChatResponseResult.Usage.PromptTokens ?? 0
            });

            // Call ChatGPT again with the results of the function
            ParamChatPrompts.Add(
                new Message(Role.Function, functionResult, functionName)
            );

            return ParamChatPrompts;
        }
        #endregion

        #region Function Parameters
        public class ReadRequest
        {
            public readRequest Readrequest { get; set; }
        }

        public class readRequest
        {
            public int StartWordIndex { get; set; }
        }

        public class WriteDatabaseRequest
        {
            public Writedatabaserequest WriteDatabaserequest { get; set; }
        }

        public class Writedatabaserequest
        {
            public string file_contents { get; set; }
        }
        #endregion

        #region private List<Function> GetDefinedFunctions()
        private List<Function> GetDefinedFunctions()
        {
            var DefinedFunctions = new List<Function>
                {
                    new Function(
                        "Read_Text",
                        @"Used to read a section of text to be summarized.
                          Use this function to retrieve a section of text.".Trim(),
                        new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["ReadRequest"] = new JsonObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = new JsonObject
                                    {
                                        ["StartWordIndex"] = new JsonObject
                                        {
                                            ["type"] = "integer",
                                            ["description"] = @"The index position of 
                                                                the word in the text to start retrieving text.".Trim()
                                        }
                                    },
                                    ["required"] = new JsonArray { "StartWordIndex" }
                                }
                            },
                            ["required"] = new JsonArray { "ReadRequest" }
                        }),
                    new Function(
                        "Write_Database",
                        @"Used to update the AIOrchestratorDatabase.json file.
                          Use this function set the contents of the AIOrchestratorDatabase.json.".Trim(),
                        new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["WriteDatabaseRequest"] = new JsonObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = new JsonObject
                                    {
                                        ["file_contents"] = new JsonObject
                                        {
                                            ["type"] = "string",
                                            ["description"] = @"The new contents of the AIOrchestratorDatabase.json file."
                                        }
                                    },
                                    ["required"] = new JsonArray { "file_contents" }
                                }
                            },
                            ["required"] = new JsonArray { "WriteDatabaseRequest" }
                        })
                };

            return DefinedFunctions;
        }
        #endregion

        #region private List<Message> AddExistingChatMessags(List<Message> chatPrompts, string SystemMessage)
        private List<Message> AddExistingChatMessags(List<Message> chatPrompts, string SystemMessage)
        {
            // Create a new LinkedList of ChatMessages
            LinkedList<ChatMessage> ChatPromptsLinkedList = new LinkedList<ChatMessage>();

            // Loop through the ChatMessages and add them to the LinkedList
            foreach (var item in ChatMessages)
            {
                ChatPromptsLinkedList.AddLast(item);
            }

            // Set the current word count to 0
            int CurrentWordCount = 0;

            // Reverse the chat messages to start from the most recent messages
            foreach (var item in ChatPromptsLinkedList.Reverse())
            {
                if (item.Prompt != null)
                {
                    int promptWordCount = item.Prompt.Split(
                        new char[] { ' ', '\t', '\n', '\r' },
                        StringSplitOptions.RemoveEmptyEntries).Length;

                    if (CurrentWordCount + promptWordCount >= 1000)
                    {
                        // This message would cause the total to exceed 1000 words,
                        // so break out of the loop
                        break;
                    }
                    // Add the message to the chat prompts
                    chatPrompts.Insert(
                        0,
                        new Message(item.Role, item.Prompt, item.FunctionName));
                    CurrentWordCount += promptWordCount;
                }
            }

            // Add the first message to the chat prompts to indicate the System message
            chatPrompts.Insert(0,
                new Message(
                    Role.System,
                    SystemMessage
                )
            );

            return chatPrompts;
        }
        #endregion

        #region private string CreateSystemMessage(string paramAIOrchestratorDatabase)
        private string CreateSystemMessage(string paramAIOrchestratorDatabase)
        {
            AIOrchestratorDatabase objAIOrchestratorDatabase = new AIOrchestratorDatabase();
            string AIOrchestratorDatabase = objAIOrchestratorDatabase.ReadFile();

            return "You are a program that will be repeatedly called to read a large amount of text and to produce a summary\n" +
                    "You know what your current task is from the Tasks property in the Database.json file\n" +
                    "Only use information gathered from reading the Text\n" +
                    "Call the Read_Text function to receive json that will have a Text property that will contain a section of the Text to be summarized\n" +
                    "When a Task is completed call the Write_Database function to update the Tasks Property in the Database.json file by removing the task\n" +
                    "Call the Write_Database function to update the Summary property in the Database.json file to store the text summary and update the Tasks property in the Database.json file to remove all the tasks\n" +
                    "If the Tasks property in the Database.json file says \"Read Text\" call the Read_Text function to retrieve a section of text to summarize\n" +
                    "\tCall the Write_Database function to update the LastWordRead property in the Database.json file to track progress\n" +
                    "\tTo keep track of the current position in the text, call the Write_Database function to update the number in the LastWordRead property in the Database.json file\n" +
                    "\tCall the Write_Database function to update the Tasks property in the Database.json file to indicate it needs to keep reading\n" +
                    "If Read_Text function returns json that indicates the CurrentWord property is equal to the TotalWords property call the Write_Database function to update and remove \"Read Text\" from Tasks collection in Database.json\n" +
                    "If the Tasks property in the Database.json file says \"Summarize Text\" output **SUMMARY** followed by the contents of the Summary property in the Database.json file\n" +
                    $"Current contents of the Database.json file is: {paramAIOrchestratorDatabase}";
        }
        #endregion

        #region private string ReadTextFromFile(int startWordIndex)
        private string ReadTextFromFile(int startWordIndex)
        {
            string ReadTextFromFileResponse = """
                        {
                         "Text": "",
                         "CurrentWord": 1,
                         "TotalWords": 100,
                        }
                        """;

            return ReadTextFromFileResponse;
        } 
        #endregion
    }
}