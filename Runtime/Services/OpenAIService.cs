using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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
            config.ValidateConfig();
            LLMLogging.EnableLogging(config.enableLogging);
        }

        /// <summary>
        /// Initialize service with custom config
        /// </summary>
        /// <param name="customConfig">Custom OpenAI configuration</param>
        public OpenAIService(OpenAIConfig customConfig)
        {
            config = customConfig ?? throw new LLMConfigurationException("Config cannot be null");
            config.ValidateConfig();
            LLMLogging.EnableLogging(config.enableLogging);
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
                if (message.content == null) // Allow empty content for function calls
                {
                    throw new LLMValidationException("Message content cannot be null", "content");
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
        /// Send a chat completion request to OpenAI
        /// </summary>
        public async Task<string> ChatCompletion(List<ChatMessage> messages, string model = null)
        {
            return await ChatCompletionWithTools(messages, null, null, model);
        }

        /// <summary>
        /// Create a tool response message with validation
        /// </summary>
        private ChatMessage CreateValidatedToolResponse(string toolCallId, string functionName, string content)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new LLMValidationException("Function name cannot be empty for tool response", "function.name");
            }
            return ChatMessage.CreateToolResponse(toolCallId, functionName, content);
        }

        /// <summary>
        /// Convert a ToolCallChunk to a ToolCall
        /// </summary>
        private ToolCall ConvertToolCallChunk(ToolCallChunk chunk)
        {
            return new ToolCall
            {
                id = chunk.id,
                type = chunk.type,
                function = new ToolCallFunction
                {
                    name = chunk.function?.name,
                    arguments = chunk.function?.arguments
                }
            };
        }

        /// <summary>
        /// Send a chat completion request with tools to OpenAI
        /// </summary>
        public async Task<string> ChatCompletionWithTools(List<ChatMessage> messages, List<Tool> tools, Func<ToolCall, Task<string>> toolHandler, string model = null)
        {
            try
            {
                ValidateMessages(messages);

                string useModel = model ?? config.defaultModel;
                LLMLogging.Log($"Starting chat completion with model: {useModel}", LogType.Log);

                var request = new ChatCompletionRequest
                {
                    model = useModel,
                    messages = messages.ToArray(),
                    temperature = config.temperature,
                    max_tokens = config.maxTokens,
                    tools = tools?.ToArray(),
                    tool_choice = tools != null ? "auto" : null
                };

                string jsonRequest = JsonConverter.SerializeObject(request);
                string url = $"{config.apiBaseUrl}/chat/completions";
                
                LLMLogging.Log($"Request Body: {jsonRequest}", LogType.Log);
                LLMLogging.Log($"Sending request to OpenAI API", LogType.Log);
                string jsonResponse = await HttpClient.PostJsonAsync(url, jsonRequest, config.apiKey);
                
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

                var choice = response.choices[0];
                var message = choice.message;

                // Handle tool calls if present
                if (toolHandler != null && message.tool_calls != null && message.tool_calls.Length > 0)
                {
                    LLMLogging.Log($"Processing {message.tool_calls.Length} tool calls", LogType.Log);
                    var toolResponses = new List<ChatMessage>(messages);
                    toolResponses.Add(message);

                    foreach (var toolCall in message.tool_calls)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(toolCall.function?.name))
                            {
                                throw new LLMValidationException("Tool call function name cannot be empty", "function.name");
                            }
                            
                            LLMLogging.Log($"Executing tool: {toolCall.function.name}", LogType.Log);
                            string toolResult = await toolHandler(toolCall);
                            toolResponses.Add(CreateValidatedToolResponse(
                                toolCall.id,
                                toolCall.function.name,
                                toolResult
                            ));
                        }
                        catch (Exception e)
                        {
                            throw new LLMToolException($"Tool execution failed: {e.Message}", toolCall.function?.name ?? "unknown");
                        }
                    }

                    // Get final response after tool calls
                    return await ChatCompletionWithTools(toolResponses, tools, toolHandler, model);
                }

                LLMLogging.Log("Chat completion successful", LogType.Log);
                return message.content;
            }
            catch (Exception e) when (!(e is LLMException))
            {
                string errorMessage = $"Error in ChatCompletion: {e.Message}";
                throw new LLMException(errorMessage, e);
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
                ValidateMessages(messages);

                if (onChunk == null)
                {
                    throw new LLMValidationException("Chunk handler cannot be null for streaming completion", "onChunk");
                }

                string useModel = model ?? config.defaultModel;
                LLMLogging.Log($"Starting streaming chat completion with model: {useModel}", LogType.Log);

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

                string jsonRequest = JsonConverter.SerializeObject(request);
                string url = $"{config.apiBaseUrl}/chat/completions";

                LLMLogging.Log($"Request Body: {jsonRequest}", LogType.Log);
                LLMLogging.Log("Starting streaming request", LogType.Log);

                List<ToolCallChunk> currentToolCallChunks = new List<ToolCallChunk>();
                ChatMessage currentMessage = new ChatMessage("assistant", "");

                await HttpClient.PostJsonStreamAsync(url, jsonRequest, config.apiKey, async line =>
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) return;

                    string jsonData = line.Substring(6);
                    if (jsonData == "[DONE]")
                    {
                        LLMLogging.Log("Stream completed", LogType.Log);
                        // Handle tool calls if present
                        if (toolHandler != null && currentToolCallChunks.Count > 0)
                        {
                            LLMLogging.Log($"Processing {currentToolCallChunks.Count} tool calls from stream", LogType.Log);
                            var toolResponses = new List<ChatMessage>(messages);
                            toolResponses.Add(currentMessage);

                            foreach (var toolCallChunk in currentToolCallChunks)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(toolCallChunk.function?.name))
                                    {
                                        throw new LLMValidationException("Tool call function name cannot be empty", "function.name");
                                    }

                                    var toolCall = ConvertToolCallChunk(toolCallChunk);
                                    LLMLogging.Log($"Executing tool: {toolCall.function.name}", LogType.Log);
                                    string toolResult = await toolHandler(toolCall);
                                    toolResponses.Add(CreateValidatedToolResponse(
                                        toolCall.id,
                                        toolCall.function.name,
                                        toolResult
                                    ));
                                }
                                catch (Exception e)
                                {
                                    throw new LLMToolException($"Tool execution failed: {e.Message}", toolCallChunk.function?.name ?? "unknown");
                                }
                            }

                            // Get final response after tool calls
                            await ChatCompletionStreamingWithTools(toolResponses, tools, toolHandler, onChunk, model);
                        }
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
                                onChunk?.Invoke(delta.content);
                            }
                            if (delta?.tool_calls != null)
                            {
                                currentToolCallChunks.AddRange(delta.tool_calls);
                            }
                        }
                    }
                    catch (Exception e) when (!(e is LLMException))
                    {
                        string errorMessage = $"Error parsing streaming response: {e.Message}\nJSON: {jsonData}";
                        LLMLogging.Log(errorMessage, LogType.Error);
                        throw new LLMResponseException(errorMessage, jsonData);
                    }
                });
            }
            catch (Exception e) when (!(e is LLMException))
            {
                string errorMessage = $"Error in ChatCompletionStreaming: {e.Message}";
                LLMLogging.Log(errorMessage, LogType.Error);
                throw new LLMException(errorMessage, e);
            }
        }

        /// <summary>
        /// Send a completion request to OpenAI
        /// </summary>
        public async Task<string> Completion(string prompt, string model = null)
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
