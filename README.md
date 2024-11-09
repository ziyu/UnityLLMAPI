# Unity LLM API

A Unity plugin for interacting with OpenAI and other Language Model APIs.

## Installation

1. Open the Unity Package Manager
2. Click the "+" button in the top-left corner
3. Select "Add package from git URL"
4. Enter: `https://github.com/yourusername/UnityLLMAPI.git`

## Setup

1. Create an OpenAI Configuration asset:
   - Right-click in the Project window
   - Select Create > UnityLLMAPI > OpenAI Configuration
   - Place it in a Resources folder
2. Configure your OpenAI API key in the created asset

## Basic Usage

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

## Features

- Chat Completion API support
- Completion API support
- Async operations
- Error handling
- Configurable API parameters
- Chat history management

## Examples

Check the Samples folder for complete examples of:
- Basic chat implementation
- UI integration
- Advanced usage patterns

## License

This project is licensed under the MIT License - see the LICENSE.md file for details.
