using Newtonsoft.Json;
using OpenAI;
using static AIOrchestrator.Pages.Memory;

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

        #region private async Task<string> ExecuteRead(string Filename, int paramStartWordIndex, int intChunkSize)
        private async Task<string> ExecuteRead(string Filename, int paramStartWordIndex, int intChunkSize)
        {
            // Read the Text from the file
            var ReadTextResult = await ReadText(Filename, paramStartWordIndex, intChunkSize);

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
                strCurrentTask = "Summarize";
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

        #region private async Task<string> ReadText(string FileDocumentPath, int startWordIndex, int intChunkSize)
        private async Task<string> ReadText(string FileDocumentPath, int startWordIndex, int intChunkSize)
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

        #region private async Task CreateVectorEntry(string vectorcontent)
        private async Task CreateVectorEntry(string VectorContent)
        {
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
        #endregion

        #region public class ReadTextEventArgs : EventArgs
        public class ReadTextEventArgs : EventArgs
        {
            public string Message { get; set; }

            public ReadTextEventArgs(string message)
            {
                Message = message;
            }
        } 
        #endregion
    }
}