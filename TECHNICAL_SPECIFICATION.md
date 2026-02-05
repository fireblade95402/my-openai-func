# Technical Specification: Azure OpenAI Motorsport Chatbot Function

## 1. Executive Summary

### 1.1 Application Overview
The OpenAI Motorsport Chatbot Function is a serverless REST API built on Azure Functions that provides an intelligent conversational interface powered by Azure OpenAI. The application enables users to have contextual conversations about motorsport topics by maintaining conversation history and leveraging GPT-based language models.

### 1.2 Primary Use Case
Enable users to interact with Azure OpenAI's GPT models through a simple HTTP API, with support for:
- Stateless single-question queries
- Stateful multi-turn conversations with history tracking
- Customizable system prompts for domain-specific contexts
- Optional AI-generated image responses

### 1.3 Technology Stack
- **Runtime**: .NET 6 (Isolated Worker Model)
- **Cloud Platform**: Microsoft Azure
- **Compute**: Azure Functions v4
- **AI Service**: Azure OpenAI Service
- **Development**: C#, Visual Studio Code
- **Deployment**: GitHub Actions CI/CD
- **Monitoring**: Azure Application Insights

---

## 2. Architecture

### 2.1 High-Level Architecture

```
┌─────────────┐         ┌──────────────────────┐         ┌─────────────────┐
│   Client    │────────▶│  Azure Functions     │────────▶│  Azure OpenAI   │
│ (HTTP/REST) │         │  (call_openai)       │         │    Service      │
└─────────────┘         └──────────────────────┘         └─────────────────┘
                                │                                │
                                │                                │
                                ▼                                ▼
                        ┌──────────────────┐           ┌──────────────────┐
                        │  Application     │           │  Image           │
                        │  Insights        │           │  Generation      │
                        └──────────────────┘           └──────────────────┘
```

### 2.2 Component Architecture

#### 2.2.1 Function App Structure
- **Function Name**: `call_openai`
- **Trigger Type**: HTTP POST
- **Authorization Level**: Function-level (requires function key)
- **Entry Point**: `Program.cs` (Minimal hosting model)
- **Handler**: `call-openai.cs` (Main function logic)

#### 2.2.2 Execution Model
- **Process Model**: Isolated Worker Process
- **Isolation**: Out-of-process execution for enhanced stability
- **Concurrency**: Multiple parallel executions supported
- **Scalability**: Automatic scaling based on HTTP load

### 2.3 Data Flow

1. **Request Reception**: Client sends HTTP POST to `/api/call_openai`
2. **Parameter Extraction**: Query string parameters extracted (question, user, system_message)
3. **History Processing**: Optional request body contains conversation history
4. **Message Assembly**: System message, history, and current question assembled
5. **OpenAI Invocation**: Messages sent to Azure OpenAI Chat Completions API
6. **Response Processing**: AI response appended to conversation history
7. **Image Generation** (Optional): If enabled, generates image based on response
8. **Response Return**: Complete conversation history returned as JSON

---

## 3. API Specification

### 3.1 Endpoint

**URL**: `/api/call_openai`  
**Method**: `POST`  
**Authentication**: Function Key (via query parameter or header)

### 3.2 Request Parameters

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `question` | string | No | `AZURE_OPENAI_USER_MESSAGE` | The user's question to ask the AI |
| `user` | string | No | `"user"` | Identifier for the requesting user (for logging/tracking) |
| `system_message` | string | No | `AZURE_OPENAI_SYSTEM_MESSAGE` | System message to set AI behavior context |

#### Request Body (Optional)

**Format**: JSON Array  
**Content-Type**: `application/json`

Structure for conversation history:
```json
[
  {
    "Role": "system|user|assistant",
    "Text": "Message content",
    "Image": "Optional image URL"
  }
]
```

**Example - First Interaction** (No Request Body):
```http
POST /api/call_openai?question=What are the tyre suppliers in F1?
```

**Example - Continued Conversation** (With History):
```http
POST /api/call_openai?question=Who are the drivers?
Content-Type: application/json

[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What teams are in F1?"
  },
  {
    "Role": "assistant",
    "Text": "The Formula One grid has ten teams..."
  }
]
```

### 3.3 Response Format

**Content-Type**: `application/json; charset=utf-8`  
**Status Code**: 200 OK (on success)

**Response Body**: Array of conversation messages including the new AI response

```json
[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What teams are in F1?"
  },
  {
    "Role": "assistant",
    "Text": "The Formula One grid has ten teams...",
    "Image": "https://example.com/generated-image.png"
  }
]
```

### 3.4 Error Handling

The function throws exceptions for:
- Missing required environment variables
- Invalid conversation history format
- OpenAI API failures
- Image generation failures (if enabled)

Error responses will return appropriate HTTP status codes with error details.

---

## 4. Configuration

### 4.1 Environment Variables

All configuration is managed through environment variables, supporting different environments (development, staging, production).

#### Required Configuration

| Variable | Type | Example | Description |
|----------|------|---------|-------------|
| `AZURE_OPENAI_ENDPOINT` | URL | `https://my-openai.openai.azure.com/` | Azure OpenAI service endpoint |
| `AZURE_OPENAI_KEY` | String | `abc123...` | Azure OpenAI API key (secret) |
| `AZURE_OPENAI_MODEL` | String | `gpt-35-turbo` | Deployed model name in Azure OpenAI |
| `AZURE_OPENAI_SYSTEM_MESSAGE` | String | `You are a helpful assistant on motorsport.` | Default system prompt |
| `AZURE_OPENAI_USER_MESSAGE` | String | `What can you help me with?` | Default user message when none provided |
| `AZURE_OPENAI_MAX_TOKENS` | Integer | `150` | Maximum tokens in completion response |
| `AZURE_OPENAI_TEMPERATURE` | Decimal | `0.9` | Sampling temperature (0-2) for response creativity |

#### Optional Configuration

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `AZURE_OPENAI_IMAGE_GENERATION` | Boolean | `false` | Enable AI image generation for responses |

### 4.2 Configuration Behavior

- **Temperature**: Controls randomness (0 = deterministic, 2 = highly creative)
  - Recommended: 0.9 for creative applications, 0 for factual responses
- **Max Tokens**: Limits response length
  - Note: Combined prompt + max_tokens must not exceed model's context window
- **System Message**: Sets the AI's persona and behavior
  - Can be overridden per-request via query parameter

### 4.3 Local Development Settings

For local development, create `local.settings.json` (excluded from source control):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_OPENAI_ENDPOINT": "https://your-instance.openai.azure.com/",
    "AZURE_OPENAI_KEY": "your-key-here",
    "AZURE_OPENAI_MODEL": "gpt-35-turbo",
    "AZURE_OPENAI_SYSTEM_MESSAGE": "You are a helpful assistant on motorsport.",
    "AZURE_OPENAI_USER_MESSAGE": "What can you help me with?",
    "AZURE_OPENAI_MAX_TOKENS": "150",
    "AZURE_OPENAI_TEMPERATURE": "0.9",
    "AZURE_OPENAI_IMAGE_GENERATION": "false"
  }
}
```

---

## 5. Implementation Details

### 5.1 Core Components

#### 5.1.1 Program.cs
Minimal hosting model configuration for Azure Functions isolated worker.

```csharp
// Configures the function host with default settings
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();
```

#### 5.1.2 call-openai.cs

**Class Structure**:
- **Namespace**: `Company.Function`
- **Class**: `call_openai`
- **Dependencies**: Injected `ILoggerFactory`

**Key Methods**:

1. **Run()**: Main HTTP-triggered function
   - Reads environment configuration
   - Processes request parameters and body
   - Manages conversation history
   - Calls OpenAI API
   - Optionally generates images
   - Returns formatted JSON response

2. **GetEnvironmentVariable()**: Helper for environment variable retrieval
   - Supports default values
   - Validation options (throwIfNotFound, throwIfEmpty)
   - Centralized configuration management

**Internal Classes**:
- **ChatMessage**: Data model for conversation messages
  - Properties: Role, Text, Image
  - Supports serialization/deserialization

### 5.2 Dependencies

#### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Azure.AI.OpenAI` | 1.0.0-beta.7 | Azure OpenAI SDK for chat and image generation |
| `Microsoft.Azure.Functions.Worker` | 1.19.0 | Core Azure Functions worker runtime |
| `Microsoft.Azure.Functions.Worker.Extensions.Http` | 3.0.13 | HTTP trigger support for isolated worker |
| `Microsoft.Azure.Functions.Worker.Sdk` | 1.14.0 | Build and deployment SDK |
| `Newtonsoft.Json` | 13.0.1 | JSON serialization/deserialization |

### 5.3 OpenAI Integration

#### Chat Completions Flow

1. Create `OpenAIClient` with endpoint and credentials
2. Build `ChatCompletionsOptions`:
   - Add message history (system, user, assistant messages)
   - Set max_tokens, temperature, user identifier
3. Call `GetChatCompletions()` with deployment name
4. Extract response content from `ChatCompletions.Choices`

#### Image Generation Flow (Optional)

1. Construct prompt from question + AI response
2. Call `GetImageGenerationsAsync()` with:
   - Combined prompt
   - Image size: 512x512
3. Extract image URL from response
4. Attach URL to message in conversation history

### 5.4 Logging and Monitoring

#### Application Insights Integration

**Configuration** (host.json):
```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    }
  }
}
```

**Logged Information**:
- System message used
- User identifier
- Questions asked
- AI responses
- Generated image URLs
- Errors and exceptions

#### Log Levels
- **Information**: Normal operation (questions, responses)
- **Error**: Failures (API errors, invalid input)

---

## 6. Deployment

### 6.1 Deployment Target

**Azure Function App**: `my-motorsport-openai-mwg-func`  
**Slot**: Production  
**Region**: Configured in Azure Portal

### 6.2 CI/CD Pipeline

**Platform**: GitHub Actions  
**Workflow File**: `.github/workflows/master_my-motorsport-openai-mwg-func.yml`

#### Pipeline Stages

1. **Trigger**: 
   - Push to `master` branch
   - Manual workflow dispatch

2. **Build Environment**:
   - Runner: `ubuntu-latest`
   - .NET Version: 6.0.x

3. **Build Steps**:
   - Checkout code
   - Setup .NET 6.0 SDK
   - Restore dependencies
   - Build with `Release` configuration
   - Output to `./output` directory

4. **Deployment Steps**:
   - Use `Azure/functions-action@v1`
   - Deploy to production slot
   - Authenticate with publish profile (stored in secrets)

#### Required Secrets

| Secret Name | Description |
|-------------|-------------|
| `AZUREAPPSERVICE_PUBLISHPROFILE_59721B202ECE4F64845999E7F0FFFBA2` | Azure Function App publish profile |

### 6.3 Deployment Considerations

- **Zero Downtime**: Function Apps support slot swapping for blue-green deployments
- **Configuration**: Environment variables must be configured in Azure Portal
- **Secrets Management**: Use Azure Key Vault for production secrets
- **Scaling**: Configure scale-out rules based on expected load

---

## 7. Development Environment

### 7.1 Prerequisites

- .NET 6.0 SDK
- Azure Functions Core Tools v4
- Visual Studio Code
- Azure subscription with:
  - Azure Functions resource
  - Azure OpenAI Service resource
  - Application Insights resource

### 7.2 Dev Container Support

**Configuration**: `.devcontainer/devcontainer.json`

**Container Features**:
- Base Image: .NET 6 with Azure Functions tools
- Forwarded Port: 7071 (Functions runtime)
- Pre-installed Extensions:
  - Azure Functions
  - C# (OmniSharp)
  - REST Client
  - GitHub Copilot

**Benefits**:
- Consistent development environment
- Pre-configured tooling
- Isolated dependencies

### 7.3 Local Development Workflow

1. **Clone Repository**:
   ```bash
   git clone https://github.com/fireblade95402/my-openai-func.git
   ```

2. **Configure Settings**:
   - Create `local.settings.json` with Azure OpenAI credentials

3. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

4. **Run Locally**:
   ```bash
   func start
   # Or via VS Code: F5
   ```

5. **Test API**:
   - Use `test.http` file with REST Client extension
   - Default endpoint: `http://localhost:7071/api/call_openai`

### 7.4 Testing Tools

**REST Client**: VS Code extension for HTTP testing

Sample requests provided in `test.http`:
- Basic request (no history)
- Custom system message
- Multi-turn conversation with history
- Different domain contexts (HR, coding, creative writing)

---

## 8. Security Considerations

### 8.1 Authentication & Authorization

- **Function-level Auth**: Requires function key in request
  - Query parameter: `?code=<function-key>`
  - Header: `x-functions-key: <function-key>`
- **Azure OpenAI**: Authenticated via API key (credential-based)

### 8.2 Security Best Practices

1. **Secrets Management**:
   - Store API keys in Azure Key Vault
   - Reference secrets via Key Vault references in Function App settings
   - Never commit secrets to source control

2. **Network Security**:
   - Consider Private Endpoints for Azure OpenAI
   - Implement IP restrictions on Function App if needed
   - Use Azure Front Door or API Management for rate limiting

3. **Data Privacy**:
   - User identifier logged for tracking
   - Conversation history transmitted in requests
   - Consider data residency requirements for OpenAI service

4. **Input Validation**:
   - Validate conversation history format
   - Sanitize user inputs before logging
   - Implement request size limits

### 8.3 Compliance Considerations

- **Azure OpenAI**: Review Microsoft's data processing agreements
- **GDPR**: Consider user data retention policies
- **Logging**: Ensure sensitive data not logged in Application Insights

---

## 9. Operational Considerations

### 9.1 Monitoring

**Key Metrics to Track**:
- Request count and latency
- OpenAI API response times
- Error rates and failure types
- Token usage (cost monitoring)
- Function execution duration

**Application Insights Queries**:
```kusto
// Failed requests
requests
| where success == false
| summarize count() by resultCode

// Average response time
requests
| summarize avg(duration) by bin(timestamp, 5m)

// Top users
traces
| where message contains "User:"
| summarize count() by tostring(customDimensions.User)
```

### 9.2 Cost Management

**Cost Drivers**:
1. **Azure Functions**: Execution time and count (Consumption plan)
2. **Azure OpenAI**: Token usage (prompt + completion tokens)
3. **Application Insights**: Data ingestion and retention
4. **Image Generation**: Per-image generation cost (if enabled)

**Cost Optimization**:
- Monitor token usage patterns
- Adjust max_tokens to minimize waste
- Implement request throttling for high-volume scenarios
- Consider Reserved Capacity for predictable workloads

### 9.3 Performance Optimization

1. **Token Efficiency**:
   - Trim conversation history to essential messages
   - Implement history length limits
   - Use shorter system messages when possible

2. **Caching**:
   - Consider caching common questions/responses
   - Implement Azure Redis Cache for frequent queries

3. **Timeout Management**:
   - Configure appropriate function timeout
   - Implement retry logic for transient OpenAI failures

### 9.4 Scaling Considerations

**Auto-scaling Triggers**:
- HTTP request queue length
- CPU/Memory utilization
- Custom metrics (token usage rate)

**Scaling Limits**:
- Azure OpenAI rate limits (requests per minute, tokens per minute)
- Function App plan limits (Consumption vs. Premium)

**Recommendations**:
- Premium Plan for predictable traffic
- Monitor OpenAI quota usage
- Implement request queuing for burst scenarios

---

## 10. Feature Capabilities

### 10.1 Conversation Management

**Stateless Mode**:
- Single question-answer interactions
- No conversation context maintained
- Suitable for independent queries

**Stateful Mode**:
- Client maintains conversation history
- Each request includes full message array
- Enables contextual, multi-turn conversations

### 10.2 Customization Options

**Per-Request Customization**:
- `system_message`: Override AI behavior/persona
- `question`: The user's query
- `user`: User identifier for tracking

**Use Cases**:
- Different AI personalities (motorsport, HR, coding)
- Department-specific assistants
- Multi-tenant scenarios with user tracking

### 10.3 Image Generation

**Optional Feature**: Disabled by default

**When Enabled**:
- Generates 512x512 image based on conversation
- Prompt combines user question + AI response
- Image URL included in response
- Increases latency and cost

**Use Cases**:
- Visual explanations
- Creative content generation
- Educational applications

---

## 11. Limitations and Constraints

### 11.1 Technical Limitations

1. **Model Context Window**:
   - GPT-3.5-turbo: 4,096 tokens
   - Long conversations may exceed limit
   - Client must manage history truncation

2. **Synchronous Processing**:
   - Each request waits for OpenAI response
   - Long responses may approach timeout limits
   - No async/callback mechanism

3. **No Built-in History Storage**:
   - Client responsible for maintaining conversation state
   - No server-side session management
   - Stateless architecture requires full history in each request

### 11.2 Operational Constraints

1. **Azure OpenAI Quotas**:
   - Requests per minute (RPM) limits
   - Tokens per minute (TPM) limits
   - Regional capacity constraints

2. **Cold Start Latency**:
   - Isolated worker process has initialization overhead
   - First request after idle period may be slower

3. **Request Size Limits**:
   - Azure Functions: 100 MB request size limit
   - Large conversation histories may approach limit

### 11.3 Security Limitations

1. **Function Key Authentication**:
   - Shared secret model
   - No user-level authentication
   - Consider API Management for enhanced security

2. **No Content Filtering**:
   - Azure OpenAI has built-in content filters
   - No additional application-level filtering
   - Relies on OpenAI's safety mechanisms

---

## 12. Future Enhancements

### 12.1 Potential Improvements

1. **Server-Side History Management**:
   - Implement Azure Cosmos DB for conversation storage
   - Session-based conversation tracking
   - Automatic history truncation

2. **Enhanced Authentication**:
   - Azure AD integration
   - User-level permissions
   - API Management integration

3. **Advanced Features**:
   - Streaming responses (Server-Sent Events)
   - Multi-modal support (document upload)
   - Function calling / tool use
   - Semantic caching

4. **Operational Improvements**:
   - Health check endpoint
   - Metrics endpoint for Prometheus
   - Structured logging (JSON format)

### 12.2 Scalability Enhancements

1. **Async Processing**:
   - Queue-based request handling
   - Webhook callbacks for long-running requests
   - Azure Service Bus integration

2. **Rate Limiting**:
   - Per-user quotas
   - Throttling middleware
   - Priority queuing

3. **Multi-Region Deployment**:
   - Geographic distribution
   - Failover capabilities
   - Traffic Manager integration

---

## 13. References

### 13.1 External Documentation

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Azure OpenAI Service](https://docs.microsoft.com/azure/cognitive-services/openai/)
- [.NET Isolated Worker](https://docs.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [OpenAI Chat Completions API](https://platform.openai.com/docs/guides/chat)

### 13.2 Repository Resources

- **README.md**: User guide and quick start
- **test.http**: API testing examples
- **.devcontainer/**: Development container configuration
- **.github/workflows/**: CI/CD pipeline definitions

### 13.3 Azure Resources

Required Azure services:
- Azure Function App (Consumption or Premium plan)
- Azure OpenAI Service (with deployed GPT model)
- Application Insights (for monitoring)
- Storage Account (for Functions runtime)

---

## 14. Glossary

| Term | Definition |
|------|------------|
| **Azure Functions** | Serverless compute service for event-driven applications |
| **Isolated Worker** | Out-of-process execution model for Azure Functions |
| **Chat Completions** | OpenAI API endpoint for conversational AI |
| **System Message** | Initial message that sets AI behavior and context |
| **Temperature** | Parameter controlling randomness in AI responses (0-2) |
| **Max Tokens** | Maximum length of AI-generated response |
| **Context Window** | Maximum total tokens (prompt + response) the model can process |
| **Function Key** | Secret used to authenticate requests to Azure Functions |
| **Consumption Plan** | Serverless pricing model with pay-per-execution |

---

## 15. Appendices

### Appendix A: Sample Conversation Flow

**Request 1**: Initial question
```http
POST /api/call_openai?question=What is F1?
```

**Response 1**:
```json
[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What is F1?"
  },
  {
    "Role": "assistant",
    "Text": "F1 stands for Formula One, which is the highest level of single-seater auto racing..."
  }
]
```

**Request 2**: Follow-up with history
```http
POST /api/call_openai?question=What teams are there?
Content-Type: application/json

[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What is F1?"
  },
  {
    "Role": "assistant",
    "Text": "F1 stands for Formula One, which is the highest level of single-seater auto racing..."
  }
]
```

**Response 2**: Updated history with new response
```json
[
  {
    "Role": "system",
    "Text": "You are a helpful assistant on motorsport."
  },
  {
    "Role": "user",
    "Text": "What is F1?"
  },
  {
    "Role": "assistant",
    "Text": "F1 stands for Formula One..."
  },
  {
    "Role": "user",
    "Text": "What teams are there?"
  },
  {
    "Role": "assistant",
    "Text": "The Formula One grid has ten teams, each with two drivers..."
  }
]
```

### Appendix B: Environment Variable Validation

The `GetEnvironmentVariable()` helper provides:

- **Default Value Support**: Return default if variable not set
- **Required Validation**: Throw exception if critical variable missing
- **Empty Check**: Throw exception if variable exists but is empty

Example usage:
```csharp
// Required with exception on missing
string endpoint = GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", null, true, true);

// Optional with default
string temp = GetEnvironmentVariable("AZURE_OPENAI_TEMPERATURE", "0.9", true, true);
```

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-05  
**Author**: Technical Architect  
**Status**: Final
