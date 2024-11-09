# Unity LLM API Documentation

## Overview
Unity LLM API is a plugin that provides easy integration with OpenAI and other Language Model APIs in Unity projects. It offers a clean, async-first API design with proper error handling and configuration management.

## Installation

### Using Git URL
1. Open the Unity Package Manager (Window > Package Manager)
2. Click the "+" button in the top-left corner
3. Select "Add package from git URL"
4. Enter: `https://github.com/yourusername/UnityLLMAPI.git`

### Manual Installation
1. Download the latest release
2. Extract it into your Unity project's `Packages` folder

## Configuration

1. Create an OpenAI Configuration asset:
   - Right-click in the Project window
   - Select Create > UnityLLMAPI > OpenAI Configuration
   - Place it in a Resources folder
2. Configure your OpenAI API key in the created asset
3. (Optional) Adjust other settings like:
   - Default model
   - Temperature
   - Max tokens

## Basic Usage

### Chat Completion

```csharp
using UnityLLMAPI.Services;
using UnityLLMAPI.Models;
using System.Collections.Generic;

// Create service instance
var openAIService = new OpenAIService();

// Create message list
var messages = new List<ChatMessage>();
messages.Add(OpenAIService.CreateSystemMessage("You are a helpful assistant."));
messages.Add(OpenAIService.CreateUserMessage("Hello!"));

// Send request
string response = await openAIService.ChatCompletion(messages);
Debug.Log(response);
```

### Simple Completion

```csharp
var openAIService = new OpenAIService();
string response = await openAIService.Completion("What is Unity?");
Debug.Log(response);
```

## Advanced Topics

### Error Handling
The API uses async/await pattern and throws exceptions when errors occur. Always wrap API calls in try-catch blocks:

```csharp
try
{
    string response = await openAIService.ChatCompletion(messages);
    // Handle success
}
catch (Exception e)
{
    Debug.LogError($"API Error: {e.Message}");
    // Handle error
}
```

### Custom Configuration
You can modify the configuration at runtime:

```csharp
var config = OpenAIConfig.Instance;
config.temperature = 0.8f;
config.maxTokens = 2000;
```

## Examples
Check the Samples folder for complete examples including:
- Basic chat implementation
- UI integration
- Advanced usage patterns

## Troubleshooting

### Common Issues

1. "OpenAIConfig not found"
   - Ensure you've created the config asset
   - Make sure it's placed in a Resources folder

2. "API Key Invalid"
   - Verify your API key in the OpenAIConfig asset
   - Check if the key has proper permissions

3. "Request Failed"
   - Check your internet connection
   - Verify API endpoint is accessible
   - Check Unity console for detailed error message

## Support
For issues and feature requests, please use the GitHub issue tracker.
