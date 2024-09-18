using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;
using System;
using static System.Environment;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Reflection;
using Azure.Messaging;



namespace Company.Function
{
    public class call_openai
    {

        // Create a class to hold the chat messages
        private class ChatMessageTemp
        {
            public ChatMessageTemp(string role, string text)
            {
                Role = role;
                Text = text;
            }

            public string Role { get; set; }
            public string Text { get; set; }
        }

        //create logger
        private readonly ILogger _logger;

        public call_openai(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<call_openai>();
        }

        //MAin Function to call OpenAI
        [Function("call_openai")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
#pragma warning disable AOAI001 // Suppress the diagnostic warning

            // Get the environment variables for the OpenAI endpoint, key and model
            string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null, true, true);
            string key = GetEnvironmentVariable("AZURE_OPENAI_KEY", null, true, true);
            string model = GetEnvironmentVariable("AZURE_OPENAI_MODEL", null, true, true);
            string user_message = GetEnvironmentVariable("AZURE_OPENAI_USER_MESSAGE", null, true, true);
            int max_tokens = Convert.ToInt32(GetEnvironmentVariable("AZURE_OPENAI_MAX_TOKENS", null, true, true));
            double temperature = Convert.ToDouble(GetEnvironmentVariable("AZURE_OPENAI_TEMPERATURE", "0.9", true, true));

            string searchEndpoint = GetEnvironmentVariable("SEARCH_ENDPOINT", null, true, true);
            string searchKey = GetEnvironmentVariable("SEARCH_KEY", null, true, true);
            string searchIndex = GetEnvironmentVariable("SEARCH_INDEX", null, true, true);


            // Create a list of messages to send to the OpenAI chat endpoint
            List<UserChatMessage> messages = new List<UserChatMessage>();
            List<ChatMessageTemp> messagesTemp = new List<ChatMessageTemp>();

            // Get the question from the query string
            var chat_question = req.Query["question"] ?? user_message;

            // Get the system message from the query string
            var system_prompt = req.Query["system_prompt"] ?? GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_MESSAGE", null, true, true);

            // Get the user from the query string
            var user = req.Query["user"] ?? "user";

            //Get Search Index from the query string
            searchIndex = req.Query["search_index"] ?? searchIndex;

            //Log the question and user
            _logger.LogInformation($"System Prompt: {system_prompt}");
            _logger.LogInformation($"User: {user}");
            _logger.LogInformation($"Question: {chat_question}");
            _logger.LogInformation($"Search Index: {searchIndex}");

            // Get the request body for the chat history
            string? requestBody = await req.ReadAsStringAsync();
            // If the request body is empty, then this is the first request
            if (string.IsNullOrEmpty(requestBody))
            {
                messages.Add(new UserChatMessage("system", system_prompt));
                messagesTemp.Add(new ChatMessageTemp("system", system_prompt));
            }
            else
            {
                // Deserialize the request body into a list of messages
                try
                {
                    messagesTemp = JsonConvert.DeserializeObject<List<ChatMessageTemp>>(requestBody) ?? new List<ChatMessageTemp>();

                    // Check if the list of messages is empty
                    if (messagesTemp.Count == 0)
                    {
                        throw new Exception($"Error with loading history");
                    }
                    else
                    {
                        // Loop through the messages and add them to the list of messages to send to the OpenAI chat endpoint
                        foreach (ChatMessageTemp message in messagesTemp)
                        {
                            messages.Add(new UserChatMessage(message.Role, message.Text));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex.Message}");
                    throw new Exception($"Error: {ex.Message}");
                }
            }

            // Create the chat options
            // Loop through the messages and add them to the list of messages to send to the OpenAI chat endpoint
            ChatCompletionOptions options = new ChatCompletionOptions();
            // foreach (ChatMessage2 message in messages)
            // {
            //     aimessages.Add(new UserChatMessage(message.Role, message.Text));
            // }
            // Add the question to the list of messages to send to the OpenAI chat endpoint
            messages.Add(new UserChatMessage("user", chat_question));
            messagesTemp.Add(new ChatMessageTemp("user", chat_question));

            // Set the chat options
            options.MaxTokens = max_tokens;
            options.EndUserId = user;
            options.Temperature = (float)temperature;

            //add extrabody to the chat options
            options.AddDataSource(new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(searchEndpoint),
                IndexName = searchIndex,
                Authentication = DataSourceAuthentication.FromApiKey(searchKey), // Add your Azure AI Search admin key here
            });


            // Create the OpenAI client
            AzureKeyCredential credential = new(key); // Add your OpenAI API key here
            AzureOpenAIClient azureClient = new(
                new Uri(endpoint),
                credential
            );
            ChatClient client = azureClient.GetChatClient(model);
            ChatCompletion completion = client.CompleteChat(
                messages,
                new ChatCompletionOptions
                {
                    //PastMessages = 10,
                    Temperature = (float)0.7,
                    TopP = (float)0.95,
                    FrequencyPenalty = (float)0,
                    PresencePenalty = (float)0,
                    MaxTokens = 1500,
                    //StopSequences = new List<string>(),
                }
            );

            foreach (var resp_message in completion.Content)
            {
                //messages.Add(new UserChatMessage("assistant", resp_message.Text));
                messagesTemp.Add(new ChatMessageTemp("assistant", resp_message.Text));
            }


            // Process and print the response
            var jsonToReturn = JsonConvert.SerializeObject(messagesTemp);

            // Return the JSON
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(jsonToReturn);
            return response;
#pragma warning restore AOAI001 // Restore the diagnostic warning

        }
        
        // Get the environment variable
        public static string GetEnvironmentVariable(string name, string? defaultValue = null, bool throwIfNotFound = false, bool throwIfEmpty = false)
        {
            string? value = Environment.GetEnvironmentVariable(name);
            // If the environment variable is not found, then return the default value
            if (value == null)
            {
                if (defaultValue != null)
                {
                    return defaultValue;
                }
                else if (throwIfNotFound)
                {
                    throw new Exception($"Environment variable '{name}' not found.");
                }
            }
            // If the environment variable is empty, then return the default value
            else if (value == string.Empty)
            {
                if (defaultValue != null)
                {
                    return defaultValue;
                }
                else if (throwIfEmpty)
                {
                    throw new Exception($"Environment variable '{name}' is empty.");
                }
            }
            return value ?? defaultValue ?? throw new Exception($"Environment variable '{name}' is null.");
        }
    }
}
