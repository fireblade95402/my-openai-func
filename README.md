
# OpenAI Model Rest API in Azure Functions


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

## Example

The below has already gone through a few questions and now asking "Who are the drivers?" This is to get a list of drivers who are in F1 based on the model's training: 

```
POST http://localhost:7071/api/call_openai?question=who%20are%20the%20drivers?
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
    "Text": "F1 stands for Formula One, which is the highest level of single-seater auto racing. It is a motorsport that involves highly advanced and sophisticated cars that are designed to be the fastest and most technologically advanced cars in the world. \n\nF1 is a global sport, with races held on various circuits across multiple continents. The F1 season typically runs from March to December each year, and consists of a series of 20-23 races. Points are awarded to the top ten finishers"
  },
  {
    "Role": "user",
    "Text": "What teams are there?"
  },
  {
    "Role": "assistant",
    "Text": "The Formula One grid has ten teams, each with two drivers. The current teams (2021 season) are:\n\n1. Mercedes-AMG Petronas Formula One Team (drivers: Lewis Hamilton and Valtteri Bottas)\n2. Red Bull Racing (drivers: Max Verstappen and Sergio Perez)\n3. McLaren F1 Team (drivers: Lando Norris and Daniel Ricciardo)\n4. Scuderia Ferrari Mission Winnow (drivers: Charles Leclerc and Carlos"
  }
]

```
The model will then return the following with the list of drivers in F1:

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
    "Text": "F1 stands for Formula One, which is the highest level of single-seater auto racing. It is a motorsport that involves highly advanced and sophisticated cars that are designed to be the fastest and most technologically advanced cars in the world. \n\nF1 is a global sport, with races held on various circuits across multiple continents. The F1 season typically runs from March to December each year, and consists of a series of 20-23 races. Points are awarded to the top ten finishers"
  },
  {
    "Role": "user",
    "Text": "What teams are there?"
  },
  {
    "Role": "assistant",
    "Text": "The Formula One grid has ten teams, each with two drivers. The current teams (2021 season) are:\n\n1. Mercedes-AMG Petronas Formula One Team (drivers: Lewis Hamilton and Valtteri Bottas)\n2. Red Bull Racing (drivers: Max Verstappen and Sergio Perez)\n3. McLaren F1 Team (drivers: Lando Norris and Daniel Ricciardo)\n4. Scuderia Ferrari Mission Winnow (drivers: Charles Leclerc and Carlos"
  },
  {
    "Role": "user",
    "Text": "who are the drivers?"
  },
  {
    "Role": "assistant",
    "Text": "The 2021 Formula One season features 20 drivers who compete for different teams. The drivers and their respective teams are:\n\n1. Lewis Hamilton (Mercedes-AMG Petronas Formula One Team)\n2. Valtteri Bottas (Mercedes-AMG Petronas Formula One Team)\n3. Max Verstappen (Red Bull Racing)\n4. Sergio Perez (Red Bull Racing)\n5. Lando Norris (McLaren F1 Team)\n6. Daniel Ricciardo (McLaren F1 Team)\n7. Charles Leclerc (Scuderia Ferrari Mission Winnow)\n8. Carlos Sainz Jr. (Scuderia Ferrari Mission Winnow)\n9. Fernando Alonso (Alpine F"
  }
]

```



