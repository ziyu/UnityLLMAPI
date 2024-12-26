# Unity LLM API

[中文文档](README_CN.md)

A Unity plugin for integrating large language model APIs like OpenAI. Provides clean API design, comprehensive error handling, and flexible configuration management.

## Features

- OpenAI Chat Completion API support
- Tool Calling functionality
- Streaming response support
- Asynchronous operations
- Comprehensive error handling
- Configurable API parameters
- Conversation history management
- Custom JSON serialization
- State management system
  - Real-time state tracking
  - State change event notifications
  - Session-level state synchronization
  - Interruption recovery mechanism

## Installation

1. Open Unity Package Manager
2. Click the "+" button in the top-left corner
3. Select "Add package from git URL"
4. Enter: `https://github.com/ziyu/UnityLLMAPI.git`

## Configuration

1. Create OpenAI configuration asset:
   - Right-click in the Project window
   - Select Create > UnityLLMAPI > OpenAI Configuration
   - Place it in the Resources folder
2. Configure your OpenAI API key
3. (Optional) Adjust other settings:
   - Default model
   - Temperature
   - Maximum tokens
   - Log level

## Basic Usage

### Simple Conversation
```csharp
// Create service instance
var openAIService = new OpenAIService();

// Create message list
var messages = new List<ChatMessage>();
messages.Add(OpenAIService.CreateSystemMessage("You are a helpful assistant."));
messages.Add(OpenAIService.CreateUserMessage("Hello!"));

// Send request
string response = await openAIService.ChatCompletion(messages);
```

### Streaming Response
```csharp
// Create configuration
var config = new ChatbotConfig 
{
    useStreaming = true,
    onStreamingChunk = (chunk) => {
        Debug.Log(chunk.content); // Display response content in real-time
    }
};

// Create service
var chatbot = new ChatbotService(new OpenAIService(), config);

// Send message
await chatbot.SendMessage("Generate a long story");
```

### Tool Calling
```csharp
// Define tools
var tools = new List<Tool> 
{
    new Tool 
    {
        name = "get_weather",
        description = "Get weather information",
        parameters = new 
        {
            type = "object",
            properties = new 
            {
                location = new { type = "string", description = "Location" }
            },
            required = new[] { "location" }
        }
    }
};

// Create configuration
var config = new ChatbotConfig 
{
    toolSet = new ToolSet(tools, async (toolCall) => {
        // Implement tool calling logic
        return "Sunny, 25°C";
    })
};

// Create service
var chatbot = new ChatbotService(new OpenAIService(), config);

// Send message that might trigger tool calling
await chatbot.SendMessage("What's the weather like in Beijing today?");
```

### State Management
```csharp
// Subscribe to state change events
chatbot.OnStateChanged += (sender, args) => {
    Debug.Log($"State changed: {args.OldState} -> {args.NewState}");
    Debug.Log($"Message ID: {args.MessageId}");
};

// Get message state
var messageState = chatbot.GetMessageState(messageId);

// Get all pending messages
var pendingMessages = chatbot.GetPendingMessages();

// Interruption recovery
await chatbot.ResumeAsync(messageId);
```

## Examples

Check the Samples folder for complete examples:
- Basic chat implementation
- UI integration
- Tool Calling examples
- Streaming response demonstration
- State management examples

## Error Handling

```csharp
try 
{
    await chatbot.SendMessage("Hello");
} 
catch (LLMConfigurationException e) 
{
    // Handle configuration errors
} 
catch (LLMValidationException e) 
{
    // Handle validation errors
} 
catch (LLMResponseException e) 
{
    // Handle API response errors
} 
catch (LLMException e) 
{
    // Handle other errors
}
```

## License

This project is open-sourced under the MIT License - see the LICENSE.md file for details