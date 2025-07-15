
# OpenAI Model Rest API in Azure Functions

*Last Updated: July 15, 2025*

A .NET 6 Azure Function that provides a REST API for interacting with Azure OpenAI services. This function enables conversational AI capabilities with context management and flexible message handling.

## Prerequisites

Before running this Azure Function, ensure you have:

- An Azure subscription
- An Azure OpenAI service instance deployed
- A deployed OpenAI model (e.g., GPT-3.5-turbo, GPT-4)
- .NET 6 SDK installed locally for development
- Azure Functions Core Tools (for local development)

## Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/fireblade95402/my-openai-func.git
   cd my-openai-func
   ```

2. **Configure environment variables** (see Environment Variables section below)

3. **Build and run locally:**
   ```bash
   dotnet build
   func start
   ```

4. **Test the function:**
   ```
   GET http://localhost:7071/api/call_openai?user=demo-user&question=Hello, how can you help me?
   ```

## Project Structure

- `call-openai.cs` - Main Azure Function implementation
- `Program.cs` - Function host configuration
- `my-openai-func.csproj` - Project dependencies and configuration
- `host.json` - Azure Functions runtime configuration
- `test.http` - HTTP test requests for development

## Dependencies

This project uses the following key packages:
- `Azure.AI.OpenAI` (v1.0.0-beta.7) - Azure OpenAI SDK
- `Microsoft.Azure.Functions.Worker` (v1.19.0) - Azure Functions isolated runtime
- `Newtonsoft.Json` (v13.0.1) - JSON serialization

## Technical details

### .Net 

The Azure function is written in .Net 6 isolated framework.

### Environment Variables

| Variable     | Value | Comment |
|--------------|:-----|:-----------|
| AZURE_OPENAI_KEY | openai-key | Key from the Azure OpenAI Instance         |
| AZURE_OPENAI_ENDPOINT | openai-endpoint | Azure OpenAI endpoint (e.g. https://*.openai.azure.com/)          |
| AZURE_OPENAI_MODEL | openai-model | Deployed model (e.g. gpt-35-turbo)          |
| AZURE_OPENAI_SYSTEM_MESSAGE | You are a helpful assistant on motorsport. |  Sets the context for the OpanAI engine for responses         |
| AZURE_OPENAI_USER_MESSAGE | What can you help me with? |  Default message if one isn't passed in the querystring         |
| AZURE_OPENAI_MAX_TOKENS | 150 |   The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens can't exceed the model's context length. Most models have a context length of 2048 tokens (except for the newest models, which support 4096).        |
| AZURE_OPENAI_TEMPERATURE | 0.9 |   What sampling temperature to use, between 0 and 2. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 (argmax sampling) for ones with a well-defined answer. We generally recommend altering this or top_p but not both (tbc).        |


### Running the Function

The function looks for a question in the querystring to pass to the openai model. If not found it defaults to AZURE_OPENAI_USER_MESSAGE from the environment variables.

``` http://localhost:7071/api/call_openai?user=demo-user&question=what are the tyre suppliers ```



If a request body is posted.The function will use this for history to drive context. Plus, add the question from the querystring if it exists. 

The below shows what it looks like if no question is passed:

```
[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What can you help me with?"
  }
]

```
The list of messages are then passed to the openai model to generate a response.

The response returned is in json format and formatted is a list of messages.


```
[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What can you help me with?"
  },
  {
    "Role": "assistant",
    "Text": "As an AI, I can help you with various aspects of motorsport including:\n\n1. Providing race schedules and results for different motorsport leagues.\n2. Sharing updates on the latest news and events in the world of motorsport.\n3. Offering tips and guidance on improving your driving skills and techniques.\n4. Providing insight into vehicle setup, including aerodynamics, suspension, and tire selection.\n5. Recommending accessories and equipment that can enhance your performance and safety while racing.\n6. Helping you select"
  }
]

```

You can drive the model using the request body only if required. 

## Example Usage

The below example demonstrates a conversation flow where the user asks about available assistance, followed by specific questions. This shows how the function maintains context across multiple interactions:

```
POST http://localhost:7071/api/call_openai?question=what%20services%20do%20you%20provide?
content-type: application/json

[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What can you help me with?"
  },
  {
    "Role": "assistant",
    "Text": "As an AI assistant, I can help you with various aspects of motorsport including race information, technical details, driver statistics, team information, and general motorsport knowledge."
  }
]
```

The model will return the conversation history with the new response appended:

```
[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What can you help me with?"
  },
  {
    "Role": "assistant",
    "Text": "As an AI assistant, I can help you with various aspects of motorsport including race information, technical details, driver statistics, team information, and general motorsport knowledge."
  },
  {
    "Role": "user",
    "Text": "what services do you provide?"
  },
  {
    "Role": "assistant",
    "Text": "I provide comprehensive motorsport assistance including:\n\n1. Race schedules and results across different series\n2. Technical information about vehicles and regulations\n3. Driver and team statistics\n4. Historical motorsport data and records\n5. Current news and updates from the motorsport world\n6. Setup and performance optimization guidance"
  }
]
```

## Deployment

### Deploy to Azure

1. **Create Azure resources:**
   ```bash
   # Create resource group
   az group create --name myResourceGroup --location eastus
   
   # Create storage account
   az storage account create --name mystorageaccount --resource-group myResourceGroup --location eastus --sku Standard_LRS
   
   # Create function app
   az functionapp create --resource-group myResourceGroup --consumption-plan-location eastus --runtime dotnet-isolated --functions-version 4 --name my-openai-func --storage-account mystorageaccount
   ```

2. **Configure application settings:**
   ```bash
   az functionapp config appsettings set --name my-openai-func --resource-group myResourceGroup --settings AZURE_OPENAI_KEY="your-key" AZURE_OPENAI_ENDPOINT="your-endpoint" AZURE_OPENAI_MODEL="your-model"
   ```

3. **Deploy the function:**
   ```bash
   func azure functionapp publish my-openai-func
   ```

### Local Development

For local development, create a `local.settings.json` file:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_OPENAI_KEY": "your-openai-key",
    "AZURE_OPENAI_ENDPOINT": "https://your-instance.openai.azure.com/",
    "AZURE_OPENAI_MODEL": "gpt-35-turbo",
    "AZURE_OPENAI_SYSTEM_MESSAGE": "You are a helpful assistant.",
    "AZURE_OPENAI_USER_MESSAGE": "What can you help me with?",
    "AZURE_OPENAI_MAX_TOKENS": "150",
    "AZURE_OPENAI_TEMPERATURE": "0.9"
  }
}
```



