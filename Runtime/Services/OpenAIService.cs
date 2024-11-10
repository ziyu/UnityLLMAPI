using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityLLMAPI.Config;
using UnityLLMAPI.Models;
using UnityLLMAPI.Utils;
using UnityLLMAPI.Interfaces;

namespace UnityLLMAPI.Services
{
    /// <summary>
    /// Service for interacting with OpenAI API
    /// </summary>
    public class OpenAIService : ILLMService
    {
        private readonly OpenAIConfig config;

        /// <summary>
        /// Initialize service with default config from Resources
        /// </summary>
        public OpenAIService()
        {
            config = OpenAIConfig.Instance;
            if (config == null)
            {
                throw new InvalidOperationException("OpenAIConfig not found. Please create and configure it in the Resources folder.");
            }
        }

        /// <summary>
        /// Initialize service with custom config
        /// </summary>
        /// <param name="customConfig">Custom OpenAI configuration</param>
        public OpenAIService(OpenAIConfig customConfig)
        {
            config = customConfig ?? throw new ArgumentNullException(nameof(customConfig));
        }

        /// <summary>
        /// Send a chat completion request to OpenAI
        /// </summary>
        public async Task<string> ChatCompletion(List<ChatMessage> messages, string model = null)
        {
            return await ChatCompletionWithTools(messages, null, null, model);
        }

        /// <summary>
        /// Send a chat completion request with tools to OpenAI
        /// </summary>
        public async Task<string> ChatCompletionWithTools(List<ChatMessage> messages, List<Tool> tools, Func<ToolCall, Task<string>> toolHandler, string model = null)
        {
            try
            {
                string useModel = model ?? config.defaultModel;
                var request = new ChatCompletionRequest
                {
                    model = useModel,
                    messages = messages.ToArray(),
                    temperature = config.temperature,
                    max_tokens = config.maxTokens,
                    tools = tools?.ToArray(),
                    tool_choice = tools != null ? "auto" : null
                };

                string jsonRequest = JsonUtility.ToJson(request);
                string url = $"{config.apiBaseUrl}/chat/completions";
                
                string jsonResponse = await HttpClient.PostJsonAsync(url, jsonRequest, config.apiKey);
                var response = JsonUtility.FromJson<ChatCompletionResponse>(jsonResponse);

                if (response?.choices == null || response.choices.Length == 0)
                {
                    throw new Exception("No response choices available");
                }

                var choice = response.choices[0];
                var message = choice.message;

                // Handle tool calls if present
                if (toolHandler != null && message.tool_calls != null && message.tool_calls.Length > 0)
                {
                    var toolResponses = new List<ChatMessage>(messages);
                    toolResponses.Add(message); // Add the assistant's message with tool calls

                    foreach (var toolCall in message.tool_calls)
                    {
                        string toolResult = await toolHandler(toolCall);
                        toolResponses.Add(ChatMessage.CreateToolResponse(
                            toolCall.id,
                            toolCall.function.name,
                            toolResult
                        ));
                    }

                    // Get final response after tool calls
                    return await ChatCompletionWithTools(toolResponses, tools, toolHandler, model);
                }

                return message.content;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ChatCompletion: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send a streaming chat completion request to OpenAI
        /// </summary>
        public async Task ChatCompletionStreaming(List<ChatMessage> messages, Action<string> onChunk, string model = null)
        {
            await ChatCompletionStreamingWithTools(messages, null, null, onChunk, model);
        }

        /// <summary>
        /// Send a streaming chat completion request with tools to OpenAI
        /// </summary>
        public async Task ChatCompletionStreamingWithTools(List<ChatMessage> messages, List<Tool> tools, Func<ToolCall, Task<string>> toolHandler, Action<string> onChunk, string model = null)
        {
            try
            {
                string useModel = model ?? config.defaultModel;
                var request = new ChatCompletionRequest
                {
                    model = useModel,
                    messages = messages.ToArray(),
                    temperature = config.temperature,
                    max_tokens = config.maxTokens,
                    stream = true,
                    tools = tools?.ToArray(),
                    tool_choice = tools != null ? "auto" : null
                };

                string jsonRequest = JsonUtility.ToJson(request);
                string url = $"{config.apiBaseUrl}/chat/completions";

                List<ToolCall> currentToolCalls = new List<ToolCall>();
                ChatMessage currentMessage = new ChatMessage("assistant", "");

                await HttpClient.PostJsonStreamAsync(url, jsonRequest, config.apiKey, async line =>
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) return;

                    string jsonData = line.Substring(6);
                    if (jsonData == "[DONE]")
                    {
                        // Handle tool calls if present
                        if (toolHandler != null && currentToolCalls.Count > 0)
                        {
                            var toolResponses = new List<ChatMessage>(messages);
                            toolResponses.Add(currentMessage);

                            foreach (var toolCall in currentToolCalls)
                            {
                                string toolResult = await toolHandler(toolCall);
                                toolResponses.Add(ChatMessage.CreateToolResponse(
                                    toolCall.id,
                                    toolCall.function.name,
                                    toolResult
                                ));
                            }

                            // Get final response after tool calls
                            await ChatCompletionStreamingWithTools(toolResponses, tools, toolHandler, onChunk, model);
                        }
                        return;
                    }

                    try
                    {
                        var response = JsonUtility.FromJson<ChatCompletionChunkResponse>(jsonData);
                        if (response?.choices != null && response.choices.Length > 0)
                        {
                            var delta = response.choices[0].delta;
                            if (!string.IsNullOrEmpty(delta?.content))
                            {
                                currentMessage.content += delta.content;
                                onChunk?.Invoke(delta.content);
                            }
                            if (delta?.tool_calls != null)
                            {
                                currentToolCalls.AddRange(delta.tool_calls);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing streaming response: {e.Message}\nJSON: {jsonData}");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ChatCompletionStreaming: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send a completion request to OpenAI
        /// </summary>
        public async Task<string> Completion(string prompt, string model = null)
        {
            // Convert the prompt to a chat message for consistency
            var messages = new List<ChatMessage>
            {
                new ChatMessage("user", prompt)
            };
            
            return await ChatCompletion(messages, model);
        }

        /// <summary>
        /// Create a system message
        /// </summary>
        public static ChatMessage CreateSystemMessage(string content)
        {
            return new ChatMessage("system", content);
        }

        /// <summary>
        /// Create a user message
        /// </summary>
        public static ChatMessage CreateUserMessage(string content)
        {
            return new ChatMessage("user", content);
        }

        /// <summary>
        /// Create an assistant message
        /// </summary>
        public static ChatMessage CreateAssistantMessage(string content)
        {
            return new ChatMessage("assistant", content);
        }
    }
}
