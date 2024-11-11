using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using UnityLLMAPI.Services;
using UnityLLMAPI.Models;
using UnityLLMAPI.Utils;
using UnityLLMAPI.Config;

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
        private ChatbotService chatbotService;
        private OpenAIConfig config;
        private ToolSet toolSet;
        private string userInput = "";
        private Vector2 scrollPosition;
        private Rect windowRect = new Rect(10, 10, 400, 600);
        private bool useStreaming = true;
        private StringBuilder currentStreamingMessage;
        private string errorMessage;
        private CancellationTokenSource cancellationTokenSource;

        // Tool confirmation dialog
        private bool showToolConfirmation = false;
        private ToolCall pendingToolCall = null;
        private TaskCompletionSource<bool> toolConfirmationTask = null;
        private Rect toolConfirmationRect = new Rect(420, 10, 300, 200);
        
        // GUI Styles
        private GUIStyle boldLabelStyle;
        private GUIStyle wordWrappedLabelStyle;
        private GUIStyle errorStyle;

        private void Start()
        {
            try
            {
                // Initialize OpenAI service
                config = OpenAIConfig.Instance;
                if (config == null)
                {
                    throw new LLMConfigurationException("OpenAIConfig not found in Resources folder");
                }
                openAIService = new OpenAIService();

                // Initialize tools
                InitializeTools();

                // Initialize chatbot service
                var chatbotConfig = new ChatbotConfig
                {
                    systemPrompt = systemMessage,
                    toolSet = toolSet,
                    useStreaming = useStreaming,
                    onStreamingChunk = OnStreamingChunk,
                    shouldExecuteTool = ShouldExecuteTool,
                    skipToolMessage = "Tool execution skipped by user"
                };

                chatbotService = new ChatbotService(openAIService, chatbotConfig);
            }
            catch (LLMException e)
            {
                HandleError(e);
            }
            catch (Exception e)
            {
                HandleError(new LLMException("Initialization failed", e));
            }
        }

        private void OnDestroy()
        {
            // 确保在销毁时取消所有进行中的操作
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        private void InitializeTools()
        {
            toolSet = new ToolSet();

            // Register time tool
            var timeTool = new Tool
            {
                type = "function",
                function = new ToolFunction
                {
                    name = "get_current_time",
                    description = "Get the current time",
                    parameters = new ToolParameters
                    {
                        type = "object",
                        properties = new Dictionary<string, ToolParameterProperty>()
                        {
                            ["format"] = new()
                            {
                                type = "string",
                                description = "The format to return the time in (e.g., 'HH:mm', 'hh:mm tt')"
                            }
                        },
                        required = new string[] { "format" }
                    }
                }
            };
            toolSet.RegisterTool(timeTool, HandleTimeToolCall);

            // Register weather tool
            var weatherTool = new Tool
            {
                type = "function",
                function = new ToolFunction
                {
                    name = "get_weather",
                    description = "Get the current weather",
                    parameters = new ToolParameters
                    {
                        type = "object",
                        properties = new Dictionary<string, ToolParameterProperty>()
                        {
                            ["location"] = new()
                            {
                                type = "string",
                                description = "The location to get weather for"
                            }
                        },
                        required = new string[] { "location" }
                    }
                }
            };
            toolSet.RegisterTool(weatherTool, HandleWeatherToolCall);
        }
        
        private void OnGUI()
        {
            if (chatbotService == null) return; 
            // Initialize GUI styles
            InitializeGUIStyles();
            
            // Draw main chat window
            windowRect = GUILayout.Window(0, windowRect, DrawChatWindow, "Chat with AI");

            // Draw tool confirmation dialog if needed
            if (showToolConfirmation && pendingToolCall != null)
            {
                toolConfirmationRect = GUILayout.Window(1, toolConfirmationRect, DrawToolConfirmationWindow, "Tool Call Confirmation");
            }
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

            errorStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                normal = { textColor = Color.red }
            };
        }
        
        private void DrawChatWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Error message display
            if (!string.IsNullOrEmpty(errorMessage))
            {
                GUILayout.Label(errorMessage, errorStyle);
                if (GUILayout.Button("Clear Error"))
                {
                    errorMessage = null;
                }
                GUILayout.Space(10);
            }

            // Chat history area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(400));
            foreach (var message in chatbotService.Messages)
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
            if (chatbotService.IsSending && useStreaming && currentStreamingMessage != null)
            {
                GUILayout.Label("Assistant:", boldLabelStyle);
                GUILayout.TextArea(currentStreamingMessage.ToString(), wordWrappedLabelStyle);
            }

            GUILayout.EndScrollView();

            // Toggle for streaming
            useStreaming = GUILayout.Toggle(useStreaming, "Use Streaming");

            // Input area
            GUILayout.BeginHorizontal();
            
            // Input field (只在非处理状态时可用)
            GUI.enabled = !chatbotService.IsSending;
            userInput = GUILayout.TextField(userInput, GUILayout.ExpandWidth(true));
            
            // Send button (只在非处理状态时可用)
            if (GUILayout.Button("Send", GUILayout.Width(60)) && !string.IsNullOrEmpty(userInput))
            {
                SendMessage();
            }
            GUI.enabled = true;

            // Cancel button (只在处理状态时可用)
            GUI.enabled = chatbotService.IsSending;
            if (GUILayout.Button("Cancel", GUILayout.Width(60)))
            {
                CancelOperation();
            }
            GUI.enabled = true;
            
            GUILayout.EndHorizontal();

            // Processing indicator
            if (chatbotService.IsSending)
            {
                GUILayout.Label("Processing...");
            }

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        private void DrawToolConfirmationWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Tool information
            GUILayout.Label("Tool Call Details:", boldLabelStyle);
            GUILayout.Label($"Tool Name: {pendingToolCall.function.name}");
            GUILayout.Label("Arguments:", boldLabelStyle);
            GUILayout.TextArea(pendingToolCall.function.arguments, wordWrappedLabelStyle, GUILayout.Height(80));

            GUILayout.Space(10);

            // Confirmation buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Execute", GUILayout.Width(100)))
            {
                CompleteToolConfirmation(true);
            }
            if (GUILayout.Button("Skip", GUILayout.Width(100)))
            {
                CompleteToolConfirmation(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        private void HandleError(Exception e)
        {
            string message = e switch
            {
                OperationCanceledException => "操作已取消",
                LLMConfigurationException => $"Configuration Error: {e.Message}",
                LLMNetworkException => $"Network Error: {e.Message}",
                LLMResponseException rex => $"API Error: {e.Message}\nResponse: {rex.ResponseContent}",
                LLMToolException tex => $"Tool Error ({tex.ToolName}): {e.Message}",
                LLMException => $"LLM Error: {e.Message}",
                _ => $"Unexpected Error: {e.Message}"
            };

            LLMLogging.Log(message, LogType.Error);
            errorMessage = message;
        }

        private void CancelOperation()
        {
            if (chatbotService.IsSending && cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        private async void SendMessage()
        {
            if (chatbotService.IsSending || string.IsNullOrEmpty(userInput))
                return;

            // 创建新的CancellationTokenSource
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                errorMessage = null;
                string userMessage = userInput;
                userInput = ""; // Clear input field

                // Initialize streaming message if needed
                if (useStreaming)
                {
                    currentStreamingMessage = new StringBuilder();
                }

                // Send message with cancellation token
                await chatbotService.SendMessage(userMessage, null, cancellationTokenSource.Token);

                // Clear streaming message
                currentStreamingMessage = null;
            }
            catch (Exception e)
            {
                HandleError(e);
            }
            finally
            {
                // 清理CancellationTokenSource
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
            }
        }

        private void OnStreamingChunk(ChatMessage message, bool isDone)
        {
            if (currentStreamingMessage != null)
            {
                currentStreamingMessage.Clear();
                currentStreamingMessage.Append(message.content);
            }
        }

        private async Task<bool> ShouldExecuteTool(ToolCall toolCall)
        {
            // Create a new task completion source
            toolConfirmationTask = new TaskCompletionSource<bool>();

            // Show the confirmation dialog
            pendingToolCall = toolCall;
            showToolConfirmation = true;

            // Wait for user response
            bool result = await toolConfirmationTask.Task;

            // Clean up
            showToolConfirmation = false;
            pendingToolCall = null;
            toolConfirmationTask = null;

            return result;
        }

        private void CompleteToolConfirmation(bool execute)
        {
            if (toolConfirmationTask != null)
            {
                toolConfirmationTask.SetResult(execute);
            }
        }

        private async Task<string> HandleTimeToolCall(ToolCall toolCall)
        {
            try
            {
                var timeArgs = JsonUtility.FromJson<GetTimeArgs>(toolCall.function.arguments);
                return DateTime.Now.ToString(timeArgs.format);
            }
            catch (Exception e)
            {
                throw new LLMToolException($"Time tool failed: {e.Message}", toolCall.function.name);
            }
        }

        private async Task<string> HandleWeatherToolCall(ToolCall toolCall)
        {
            try
            {
                var weatherArgs = JsonUtility.FromJson<GetWeatherArgs>(toolCall.function.arguments);
                // In a real app, you would call a weather API here
                return $"Simulated weather for {weatherArgs.location}: 72°F, Sunny";
            }
            catch (Exception e)
            {
                throw new LLMToolException($"Weather tool failed: {e.Message}", toolCall.function.name);
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
