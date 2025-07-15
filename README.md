
# OpenAI Model REST API in Azure Functions

A serverless Azure Functions application that provides a REST API for interacting with Azure OpenAI models. This function supports chat completions with conversation history and optional image generation capabilities.

## Overview

This Azure Function allows you to:
- Send chat messages to Azure OpenAI models (like GPT-3.5, GPT-4)
- Maintain conversation history for context-aware responses
- Generate images based on chat responses (optional)
- Customize system messages and parameters
- Handle both single questions and multi-turn conversations

## Prerequisites

Before running this application, you need:
- Azure subscription with Azure OpenAI service enabled
- Azure OpenAI resource deployed with a chat model (e.g., gpt-35-turbo, gpt-4)
- .NET 6.0 SDK or later
- Azure Functions Core Tools (for local development)
- Visual Studio Code or Visual Studio (optional)

## Installation

1. Clone this repository:
   ```bash
   git clone https://github.com/fireblade95402/my-openai-func.git
   cd my-openai-func
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Create a `local.settings.json` file with your Azure OpenAI configuration:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "AZURE_OPENAI_ENDPOINT": "https://your-openai-resource.openai.azure.com/",
       "AZURE_OPENAI_KEY": "your-api-key-here",
       "AZURE_OPENAI_MODEL": "gpt-35-turbo",
       "AZURE_OPENAI_SYSTEM_MESSAGE": "You are a helpful assistant.",
       "AZURE_OPENAI_USER_MESSAGE": "What can you help me with?",
       "AZURE_OPENAI_MAX_TOKENS": "150",
       "AZURE_OPENAI_TEMPERATURE": "0.9",
       "AZURE_OPENAI_IMAGE_GENERATION": "false"
     }
   }
   ```

## Technical details

### .Net 

The Azure function is written in .Net 6 isolated framework.

### Environment Variables

| Variable     | Value | Comment |
|--------------|:-----|:-----------|
| AZURE_OPENAI_KEY | openai-key | Key from the Azure OpenAI Instance         |
| AZURE_OPENAI_ENDPOINT | openai-endpoint | Azure OpenAI endpoint (e.g. https://*.openai.azure.com/)          |
| AZURE_OPENAI_MODEL | openai-model | Deployed model (e.g. gpt-35-turbo, gpt-4)          |
| AZURE_OPENAI_SYSTEM_MESSAGE | You are a helpful assistant. |  Sets the context for the OpenAI engine for responses         |
| AZURE_OPENAI_USER_MESSAGE | What can you help me with? |  Default message if one isn't passed in the querystring         |
| AZURE_OPENAI_MAX_TOKENS | 150 |   The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens can't exceed the model's context length.        |
| AZURE_OPENAI_TEMPERATURE | 0.9 |   What sampling temperature to use, between 0 and 2. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 (argmax sampling) for ones with a well-defined answer.        |
| AZURE_OPENAI_IMAGE_GENERATION | false |   Enable/disable automatic image generation based on chat responses. Set to "true" to enable image creation.        |

## Local Development

1. Start the Azure Functions runtime locally:
   ```bash
   func start
   ```

2. The function will be available at `http://localhost:7071/api/call_openai`

3. Test the function using the provided test.http file or curl commands

## Deployment

### Deploy to Azure

1. Create an Azure Function App:
   ```bash
   az functionapp create --resource-group myResourceGroup --consumption-plan-location westeurope --runtime dotnet-isolated --functions-version 4 --name myOpenAIFunc --storage-account mystorageaccount
   ```

2. Configure application settings:
   ```bash
   az functionapp config appsettings set --name myOpenAIFunc --resource-group myResourceGroup --settings AZURE_OPENAI_ENDPOINT="https://your-openai-resource.openai.azure.com/" AZURE_OPENAI_KEY="your-api-key" AZURE_OPENAI_MODEL="gpt-35-turbo"
   ```

3. Deploy the function:
   ```bash
   func azure functionapp publish myOpenAIFunc
   ```


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

## Example

The below has already gone through a few questions and now asking "Who are the current drivers?" This is to get a list of drivers who are currently in F1 based on the model's training: 

```
POST http://localhost:7071/api/call_openai?question=who%20are%20the%20current%20drivers?
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
    "Text": "As an AI, I can help you with various aspects of motorsport including:\n\n1. Providing race schedules and results for different motorsport leagues.\n2. Sharing updates on the latest news and events in the world of motorsport.\n3. Offering tips and guidance on improving your driving skills and techniques.\n4. Providing insight into vehicle setup, including aerodynamics, suspension, and tire selection.\n5. Recommending accessories and equipment that can enhance your performance and safety while racing.\n6. Helping you select"
  },
  {
    "Role": "user",
    "Text": "What is F1?"
  },
  {
    "Role": "assistant",
    "Text": "F1 stands for Formula One, which is the highest level of single-seater auto racing. It is a motorsport that involves highly advanced and sophisticated cars that are designed to be the fastest and most technologically advanced cars in the world. \n\nF1 is a global sport, with races held on various circuits across multiple continents. The F1 season typically runs from March to December each year, and consists of a series of races. Points are awarded to the top ten finishers"
  },
  {
    "Role": "user",
    "Text": "What teams are there?"
  },
  {
    "Role": "assistant",
    "Text": "The Formula One grid has ten teams, each with two drivers. The current teams include major constructors like Mercedes, Red Bull Racing, Ferrari, McLaren, and others. Each team competes with their own designed cars and strategies throughout the season."
  }
]

```
The model will then return the following with the list of current drivers in F1:

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
  },
  {
    "Role": "user",
    "Text": "What is F1?"
  },
  {
    "Role": "assistant",
    "Text": "F1 stands for Formula One, which is the highest level of single-seater auto racing. It is a motorsport that involves highly advanced and sophisticated cars that are designed to be the fastest and most technologically advanced cars in the world. \n\nF1 is a global sport, with races held on various circuits across multiple continents. The F1 season typically runs from March to December each year, and consists of a series of races. Points are awarded to the top ten finishers"
  },
  {
    "Role": "user",
    "Text": "What teams are there?"
  },
  {
    "Role": "assistant",
    "Text": "The Formula One grid has ten teams, each with two drivers. The current teams include major constructors like Mercedes, Red Bull Racing, Ferrari, McLaren, and others. Each team competes with their own designed cars and strategies throughout the season."
  },
  {
    "Role": "user",
    "Text": "who are the current drivers?"
  },
  {
    "Role": "assistant",
    "Text": "The current Formula One season features 20 drivers who compete for different teams. The drivers and their respective teams change each season, but typically include top performers from various nationalities competing for teams like Mercedes, Red Bull Racing, Ferrari, McLaren, Alpine, and others. For the most up-to-date driver lineup, I'd recommend checking the official Formula 1 website as driver changes can occur during the season."
  }
]

```

## API Reference

### Endpoint: POST /api/call_openai

#### Query Parameters
- `question` (optional): The question/message to send to the OpenAI model
- `system_message` (optional): Override the default system message
- `user` (optional): User identifier for the request

#### Request Body
Optional JSON array of conversation history in the format:
```json
[
  {
    "Role": "system|user|assistant",
    "Text": "message content",
    "Image": "image_url_if_available"
  }
]
```

#### Response
JSON array containing the full conversation history including the new response.

## Features

- **Chat Completions**: Send questions and receive AI-generated responses
- **Conversation History**: Maintain context across multiple interactions
- **Image Generation**: Optional automatic image creation based on responses
- **Customizable Parameters**: Control temperature, max tokens, and system messages
- **Flexible Input**: Support both query parameters and request body for conversation history

## Troubleshooting

### Common Issues

1. **Authentication Error**: Verify your AZURE_OPENAI_KEY and AZURE_OPENAI_ENDPOINT are correct
2. **Model Not Found**: Ensure the AZURE_OPENAI_MODEL matches your deployed model name
3. **Rate Limiting**: Azure OpenAI has rate limits; implement retry logic if needed
4. **Token Limits**: Adjust AZURE_OPENAI_MAX_TOKENS based on your model's capabilities

### Logs
Check the Azure Functions logs for detailed error information when debugging issues.

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes and test thoroughly
4. Commit your changes: `git commit -am 'Add your feature'`
5. Push to the branch: `git push origin feature/your-feature`
6. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.


