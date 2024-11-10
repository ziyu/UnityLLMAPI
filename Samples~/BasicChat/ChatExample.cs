using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System;
using UnityLLMAPI.Services;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Examples
{
    /// <summary>
    /// Example chat implementation using Unity LLM API with IMGUI
    /// </summary>
    public class ChatExample : MonoBehaviour
    {
        [Header("Chat Settings")]
        [Tooltip("System message to set AI behavior")]
        [SerializeField] private string systemMessage = "You are a helpful assistant that can use tools.";

        private OpenAIService openAIService;
        private List<ChatMessage> chatHistory = new();
        private List<Tool> tools = new();
        private bool isProcessing = false;
        private string userInput = "";
        private Vector2 scrollPosition;
        private Rect windowRect = new Rect(10, 10, 400, 600);
        private bool useStreaming = true;
        private bool useTools = true;
        private StringBuilder currentStreamingMessage;
        
        // GUI Styles
        private GUIStyle boldLabelStyle;
        private GUIStyle wordWrappedLabelStyle;

        private void Start()
        {
            // Initialize service and chat history
            openAIService = new OpenAIService();

            // Initialize tools
            InitializeTools();

            // Add system message
            chatHistory.Add(OpenAIService.CreateSystemMessage(systemMessage));
        }

        private void InitializeTools()
        {
            tools = new List<Tool>
            {
                new Tool
                {
                    name = "get_current_time",
                    description = "Get the current time",
                    parameters = new ToolParameters
                    {
                        type = "object",
                        properties = new ToolParameterProperty[]
                        {
                            new ToolParameterProperty
                            {
                                name = "format",
                                type = "string",
                                description = "The format to return the time in (e.g., 'HH:mm', 'hh:mm tt')"
                            }
                        },
                        required = new string[] { "format" }
                    }
                },
                new Tool
                {
                    name = "get_weather",
                    description = "Get the current weather",
                    parameters = new ToolParameters
                    {
                        type = "object",
                        properties = new ToolParameterProperty[]
                        {
                            new ToolParameterProperty
                            {
                                name = "location",
                                type = "string",
                                description = "The location to get weather for"
                            }
                        },
                        required = new string[] { "location" }
                    }
                }
            };
        }
        
        private void OnGUI()
        {
            if (openAIService == null) return; 
            // Initialize GUI styles
            InitializeGUIStyles();
            windowRect = GUILayout.Window(0, windowRect, DrawChatWindow, "Chat with AI");
        }
        
        private void InitializeGUIStyles()
        {
            if (boldLabelStyle != null) return;
            boldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };

            wordWrappedLabelStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
        }
        
        private void DrawChatWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Chat history area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(450));
            foreach (var message in chatHistory)
            {
                if (message.role != "system") // Don't display system messages
                {
                    string role = char.ToUpper(message.role[0]) + message.role.Substring(1);
                    GUILayout.Label($"{role}:", boldLabelStyle);
                    GUILayout.TextArea(message.content, wordWrappedLabelStyle);

                    // Display tool calls if present
                    if (message.tool_calls != null)
                    {
                        foreach (var toolCall in message.tool_calls)
                        {
                            GUILayout.Label($"Tool Call: {toolCall.function.name}", boldLabelStyle);
                            GUILayout.TextArea($"Arguments: {toolCall.function.arguments}", wordWrappedLabelStyle);
                        }
                    }

                    GUILayout.Space(10);
                }
            }

            // Show current streaming message if any
            if (isProcessing && useStreaming && currentStreamingMessage != null)
            {
                GUILayout.Label("Assistant:", boldLabelStyle);
                GUILayout.TextArea(currentStreamingMessage.ToString(), wordWrappedLabelStyle);
            }

            GUILayout.EndScrollView();

            // Toggles
            GUILayout.BeginHorizontal();
            useStreaming = GUILayout.Toggle(useStreaming, "Use Streaming");
            useTools = GUILayout.Toggle(useTools, "Use Tools");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Input area
            GUI.enabled = !isProcessing;
            GUILayout.BeginHorizontal();
            
            // Input field
            userInput = GUILayout.TextField(userInput, GUILayout.ExpandWidth(true));
            
            // Send button
            if (GUILayout.Button("Send", GUILayout.Width(60)) && !string.IsNullOrEmpty(userInput))
            {
                SendMessage();
            }
            
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            // Processing indicator
            if (isProcessing)
            {
                GUILayout.Label("Processing...");
            }

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        private async void SendMessage()
        {
            if (isProcessing || string.IsNullOrEmpty(userInput))
                return;

            try
            {
                isProcessing = true;
                string userMessage = userInput;
                userInput = ""; // Clear input field

                // Add user message to history
                chatHistory.Add(OpenAIService.CreateUserMessage(userMessage));

                if (useStreaming)
                {
                    // Initialize streaming message
                    currentStreamingMessage = new StringBuilder();
                    
                    // Get streaming response
                    if (useTools)
                    {
                        await openAIService.ChatCompletionStreamingWithTools(
                            chatHistory, 
                            tools,
                            HandleToolCall,
                            chunk => currentStreamingMessage.Append(chunk)
                        );
                    }
                    else
                    {
                        await openAIService.ChatCompletionStreaming(
                            chatHistory,
                            chunk => currentStreamingMessage.Append(chunk)
                        );
                    }

                    // Add completed message to history
                    chatHistory.Add(OpenAIService.CreateAssistantMessage(currentStreamingMessage.ToString()));
                    currentStreamingMessage = null;
                }
                else
                {
                    // Get regular response
                    string response;
                    if (useTools)
                    {
                        response = await openAIService.ChatCompletionWithTools(
                            chatHistory,
                            tools,
                            HandleToolCall
                        );
                    }
                    else
                    {
                        response = await openAIService.ChatCompletion(chatHistory);
                    }
                    chatHistory.Add(OpenAIService.CreateAssistantMessage(response));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in chat: {e.Message}");
                chatHistory.Add(OpenAIService.CreateAssistantMessage("Error: Failed to get response from AI."));
                currentStreamingMessage = null;
            }
            finally
            {
                isProcessing = false;
            }
        }

        private async Task<string> HandleToolCall(ToolCall toolCall)
        {
            try
            {
                switch (toolCall.function.name)
                {
                    case "get_current_time":
                        var timeArgs = JsonUtility.FromJson<GetTimeArgs>(toolCall.function.arguments);
                        return DateTime.Now.ToString(timeArgs.format);

                    case "get_weather":
                        var weatherArgs = JsonUtility.FromJson<GetWeatherArgs>(toolCall.function.arguments);
                        // In a real app, you would call a weather API here
                        return $"Simulated weather for {weatherArgs.location}: 72°F, Sunny";

                    default:
                        throw new ArgumentException($"Unknown tool: {toolCall.function.name}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling tool call: {e.Message}");
                return $"Error: {e.Message}";
            }
        }

        [Serializable]
        private class GetTimeArgs
        {
            public string format;
        }

        [Serializable]
        private class GetWeatherArgs
        {
            public string location;
        }
    }
}
