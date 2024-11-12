using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityLLMAPI.Config;
using UnityLLMAPI.Models;
using UnityLLMAPI.Utils;
using UnityLLMAPI.Interfaces;
using UnityLLMAPI.Utils.Json;

namespace UnityLLMAPI.Services
{
    /// <summary>
    /// Service for interacting with OpenAI API
    /// </summary>
    public class OpenAIService : ILLMService
    {
        private readonly OpenAIConfig config;
        private const string API_ENDPOINT = "/chat/completions";

        /// <summary>
        /// Initialize service with default config from Resources
        /// </summary>
        public OpenAIService()
        {
            config = OpenAIConfig.Instance;
            if (config == null)
            {
                throw new LLMConfigurationException("OpenAIConfig not found. Please create and configure it in the Resources folder.");
            }
            InitByConfig();
        }

        /// <summary>
        /// Initialize service with custom config
        /// </summary>
        /// <param name="customConfig">Custom OpenAI configuration</param>
        public OpenAIService(OpenAIConfig customConfig)
        {
            config = customConfig ?? throw new LLMConfigurationException("Config cannot be null");
            InitByConfig();
        }

        void InitByConfig()
        {
            config.ValidateConfig();
            LLMLogging.EnableLogging(config.enableLogging);
            LLMLogging.SetLogLevel(config.minimumLogLevel);
        }

        /// <summary>
        /// Validate chat messages before sending to API
        /// </summary>
        private void ValidateMessages(List<ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                throw new LLMValidationException("Messages cannot be null or empty", "messages");
            }

            foreach (var message in messages)
            {
                if (string.IsNullOrEmpty(message.role))
                {
                    throw new LLMValidationException("Message role cannot be empty", "role");
                }
                if (string.IsNullOrEmpty(message.content)&&message.tool_calls==null) // Allow empty content for function calls
                {
                    throw new LLMValidationException("Message content or tool use cannot be null", "content");
                }
                // Validate tool response messages
                if (message.role == "tool")
                {
                    if (string.IsNullOrEmpty(message.tool_call_id))
                    {
                        throw new LLMValidationException("Tool call ID cannot be empty for tool response", "tool_call_id");
                    }
                    if (string.IsNullOrEmpty(message.name))
                    {
                        throw new LLMValidationException("Tool name cannot be empty for tool response", "name");
                    }
                }
            }
        }

        /// <summary>
        /// Create base chat completion request
        /// </summary>
        private ChatCompletionRequest CreateBaseRequest(List<ChatMessage> messages, string model, bool isStreaming = false, List<Tool> tools = null)
        {
            string useModel = model ?? config.defaultModel;
            return new ChatCompletionRequest
            {
                model = useModel,
                messages = messages.ToArray(),
                temperature = config.temperature,
                max_tokens = config.maxTokens,
                stream = isStreaming,
                tools = tools?.ToArray(),
                tool_choice = tools != null ? "auto" : null
            };
        }

        /// <summary>
        /// Handle API response and extract message
        /// </summary>
        private ChatMessage HandleResponse(string jsonResponse)
        {
            var response = JsonConverter.DeserializeObject<ChatCompletionResponse>(jsonResponse);
            
            // Check for API errors
            if (response?.error != null)
            {
                throw LLMResponseException.FromErrorResponse(response.error, jsonResponse);
            }

            if (response?.choices == null || response.choices.Length == 0)
            {
                throw new LLMResponseException("No response choices available", jsonResponse);
            }

            return response.choices[0].message;
        }

        /// <summary>
        /// Send a chat completion request to OpenAI
        /// </summary>
        public async Task<ChatMessage> ChatCompletion(List<ChatMessage> messages, string model = null, List<Tool> tools=null, CancellationToken cancellationToken = default)
        {
            return await SendChatRequest(messages, model, tools, cancellationToken);
        }

        /// <summary>
        /// Core method to send chat requests
        /// </summary>
        private async Task<ChatMessage> SendChatRequest(List<ChatMessage> messages, string model = null, List<Tool> tools = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateMessages(messages);

                var request = CreateBaseRequest(messages, model, false, tools);
                string jsonRequest = JsonConverter.SerializeObject(request);
                string url = $"{config.apiBaseUrl}{API_ENDPOINT}";
                
                LLMLogging.Log($"Send Request: {jsonRequest}", LogType.Log);
                
                string jsonResponse = await HttpClient.PostJsonAsync(url, jsonRequest, config.apiKey, cancellationToken);
                LLMLogging.Log($"Receive Response: {jsonResponse}", LogType.Log);
                return HandleResponse(jsonResponse);
            }
            catch (OperationCanceledException)
            {
                LLMLogging.Log("Chat completion was cancelled", LogType.Log);
                throw;
            }
            catch (Exception e) when (!(e is LLMException))
            {
                string errorMessage = $"Error in ChatCompletion: {e.Message}";
                throw new LLMException(errorMessage, e);
            }
        }

        /// <summary>
        /// Send a streaming chat completion request with tools to OpenAI
        /// </summary>
        public async Task<ChatMessage> ChatCompletionStreaming(List<ChatMessage> messages, Action<ChatMessage> onChunk, string model = null, List<Tool> tools=null, CancellationToken cancellationToken = default)
        {
            return await SendStreamingRequest(messages, onChunk, model, tools, cancellationToken);
        }

        /// <summary>
        /// Core method to send streaming requests
        /// </summary>
        private async Task<ChatMessage> SendStreamingRequest(List<ChatMessage> messages, Action<ChatMessage> onChunk, string model = null, List<Tool> tools = null, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateMessages(messages);

                if (onChunk == null)
                {
                    throw new LLMValidationException("Chunk handler cannot be null for streaming completion", "onChunk");
                }

                var request = CreateBaseRequest(messages, model, true, tools);
                string jsonRequest = JsonConverter.SerializeObject(request);
                string url = $"{config.apiBaseUrl}{API_ENDPOINT}";

                LLMLogging.Log($"Request Body: {jsonRequest}", LogType.Log);
                LLMLogging.Log("Starting streaming request", LogType.Log);

                var currentMessage = CreateAssistantMessage("");
                List<ToolCallChunk> currentToolCallChunks = new List<ToolCallChunk>();

                await HttpClient.PostJsonStreamAsync(url, jsonRequest, config.apiKey, async line =>
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) return;

                    string jsonData = line.Substring(6);
                    if (jsonData == "[DONE]")
                    {
                        LLMLogging.Log("Stream completed", LogType.Log);
                        if (currentToolCallChunks.Count > 0)
                        {
                            currentMessage.tool_calls = MergeToolCallChunks(currentToolCallChunks);
                        }
                        onChunk(currentMessage);
                        return;
                    }

                    try
                    {
                        var response = JsonConverter.DeserializeObject<ChatCompletionChunkResponse>(jsonData);
                        
                        // Check for API errors
                        if (response?.error != null)
                        {
                            throw LLMResponseException.FromErrorResponse(response.error, jsonData);
                        }

                        if (response?.choices is { Length: > 0 })
                        {
                            var delta = response.choices[0].delta;
                            if (!string.IsNullOrEmpty(delta?.content))
                            {
                                currentMessage.content += delta.content;
                                onChunk(currentMessage);
                            }
                            if (delta?.tool_calls != null)
                            {
                                currentToolCallChunks.AddRange(delta.tool_calls);
                            }
                        }
                    }
                    catch (Exception e) when (!(e is LLMException))
                    {
                        string errorMessage = $"Error parsing streaming response: {e}\nJSON: {jsonData}";
                        LLMLogging.Log(errorMessage, LogType.Error);
                        throw new LLMResponseException(errorMessage, jsonData);
                    }
                }, cancellationToken);
                return currentMessage;
            }
            catch (OperationCanceledException)
            {
                LLMLogging.Log("Streaming request was cancelled", LogType.Log);
                throw;
            }
            catch (Exception e) when (!(e is LLMException))
            {
                string errorMessage = $"Error in ChatCompletionStreaming: {e}";
                LLMLogging.Log(errorMessage, LogType.Error);
                throw new LLMException(errorMessage, e);
            }
        }

        private ToolCall[] MergeToolCallChunks(List<ToolCallChunk> chunks)
        {
            Dictionary<int, ToolCall> indexedToolCalls = new();
            Dictionary<int, StringBuilder> indexedToolCallArgs = new();
            foreach (var chunk in chunks)
            {
                if (!indexedToolCalls.TryGetValue(chunk.index, out var toolCall))
                {
                    toolCall = new();
                    toolCall.type = chunk.type;
                    toolCall.function = new();
                    indexedToolCalls[chunk.index] = toolCall;
                    StringBuilder sb = new();
                    indexedToolCallArgs[chunk.index] = sb;
                }

                if (!string.IsNullOrEmpty(chunk.id))
                {
                    toolCall.id = chunk.id;
                }

                if (chunk.function?.name != null)
                {
                    toolCall.function.name = chunk.function.name;
                }

                if (chunk.function?.arguments != null)
                {
                    indexedToolCallArgs[chunk.index].Append(chunk.function?.arguments);
                }
            }

            foreach (var (index,toolCall) in indexedToolCalls)
            {
                toolCall.function.arguments = indexedToolCallArgs[index].ToString();
            }
            
            return indexedToolCalls.Values.ToArray();
        }

        /// <summary>
        /// Send a completion request to OpenAI
        /// </summary>
        public async Task<string> Completion(string prompt, string model, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                throw new LLMValidationException("Prompt cannot be null or empty", "prompt");
            }

            // Convert the prompt to a chat message for consistency
            var messages = new List<ChatMessage>
            {
                new ChatMessage("user", prompt)
            };
            
            var response = await ChatCompletion(messages, model, cancellationToken:cancellationToken);
            return response.content;
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
