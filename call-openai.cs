using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.AI.OpenAI;
using System;
using static System.Environment;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Reflection;



namespace Company.Function
{
    public class call_openai
    {

        // Create a class to hold the chat messages
        private class ChatMessage
        {
            public ChatMessage(string role, string text)
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
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "Get", "post")] HttpRequestData req)
        {

            // Get the environment variables for the OpenAI endpoint, key and model
            string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null, true, true);
            string key = GetEnvironmentVariable("AZURE_OPENAI_KEY", null, true, true);
            string model = GetEnvironmentVariable("AZURE_OPENAI_MODEL", null, true, true);
            string user_message = GetEnvironmentVariable("AZURE_OPENAI_USER_MESSAGE", null, true, true);
            int max_tokens = Convert.ToInt32(GetEnvironmentVariable("AZURE_OPENAI_MAX_TOKENS", null, true, true));
            double temperature = Convert.ToDouble(GetEnvironmentVariable("AZURE_OPENAI_TEMPERATURE", "0.9", true, true));


            // Create a list of messages to send to the OpenAI chat endpoint
            List<ChatMessage> messages = new List<ChatMessage>();

            // Get the question from the query string
            var chat_question = req.Query["question"] ?? user_message;

            // Get the system message from the query string
            var system_message = req.Query["system_message"] ?? GetEnvironmentVariable("AZURE_OPENAI_SYSTEM_MESSAGE", null, true, true);

            // Get the user from the query string
            var user = req.Query["user"] ?? "user";

            //Example of how to get the headers
            //request headers 
            // var headers = req.Headers;
            // //get Host from headers
            // var host = headers.GetValues("AZURE_OPENAI_SYSTEM_MESSAGE").FirstOrDefault() ;
            
            //Log the question and user
            _logger.LogInformation($"User: {user}");
            _logger.LogInformation($"Question: {chat_question}");


            // Get the request body for the chat history
            string? requestBody = await req.ReadAsStringAsync();
          

            // Create the chat options
            ChatCompletionsOptions? chatCompletionsOptions = null;

            // If the request body is empty, then this is the first request
            if (string.IsNullOrEmpty(requestBody))
            {
                chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    Messages = 
                    {
                        // Add the system message to the list of messages to send to the OpenAI chat endpoint
                        new Azure.AI.OpenAI.ChatMessage(Azure.AI.OpenAI.ChatRole.System, system_message),
                    }
                };
            }
            else
            {
                // Deserialize the request body into a list of messages
                try
                {   
                    messages = JsonConvert.DeserializeObject<List<ChatMessage>>(requestBody) ?? new List<ChatMessage>();

                    // Check if the list of messages is empty
                    if (messages.Count == 0){
                        throw new Exception($"Error with loading history");
                    }
                    // Create the chat options
                    // Loop through the messages and add them to the list of messages to send to the OpenAI chat endpoint
                    chatCompletionsOptions = new ChatCompletionsOptions();
                    foreach (ChatMessage message in messages)
                    {
                        chatCompletionsOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(message.Role, message.Text));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex.Message}");
                    throw new Exception($"Error: {ex.Message}");
                }
            }
            // Set the chat options
            chatCompletionsOptions.MaxTokens = max_tokens;
            chatCompletionsOptions.User = user;
            chatCompletionsOptions.Temperature = (float)temperature;



            // Add the question to the list of messages to send to the OpenAI chat endpoint
            chatCompletionsOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(Azure.AI.OpenAI.ChatRole.User, chat_question));

            // Create the OpenAI client
            OpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(key));

            // Call the OpenAI chat endpoint
            Response<ChatCompletions> chat_response = null;
            try
            {
            chat_response = client.GetChatCompletions(
                            deploymentOrModelName: model,
                            chatCompletionsOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetChatCompletions Error: {ex.Message}");
                throw new Exception($"GetChatCompletions Error: {ex.Message}");
            }

            // Add the response to the list of messages
            chatCompletionsOptions.Messages.Add(chat_response.Value.Choices[0].Message);

            //Log to message
            _logger.LogInformation($"Response: {chat_response.Value.Choices[0].Message.Content}");

            // Loop through the messages and add them to the list of messages to return
            messages = new List<ChatMessage>();
            foreach (var message in chatCompletionsOptions.Messages)
            {
               messages.Add(new ChatMessage(message.Role.ToString(), message.Content));

            }
            // Convert the list of messages to JSON
            var jsonToReturn = JsonConvert.SerializeObject(messages);

            // Return the JSON
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(jsonToReturn);
            return response;
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
