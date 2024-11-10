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
            try
            {
                string useModel = model ?? config.defaultModel;
                var request = new ChatCompletionRequest
                {
                    model = useModel,
                    messages = messages.ToArray(),
                    temperature = config.temperature,
                    max_tokens = config.maxTokens
                };

                string jsonRequest = JsonUtility.ToJson(request);
                string url = $"{config.apiBaseUrl}/chat/completions";
                
                string jsonResponse = await HttpClient.PostJsonAsync(url, jsonRequest, config.apiKey);
                var response = JsonUtility.FromJson<ChatCompletionResponse>(jsonResponse);

                if (response?.choices != null && response.choices.Length > 0)
                {
                    return response.choices[0].message.content;
                }
                
                throw new Exception("No response choices available");
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
            try
            {
                string useModel = model ?? config.defaultModel;
                var request = new ChatCompletionRequest
                {
                    model = useModel,
                    messages = messages.ToArray(),
                    temperature = config.temperature,
                    max_tokens = config.maxTokens,
                    stream = true
                };

                string jsonRequest = JsonUtility.ToJson(request);
                string url = $"{config.apiBaseUrl}/chat/completions";

                await HttpClient.PostJsonStreamAsync(url, jsonRequest, config.apiKey, line =>
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) return;

                    string jsonData = line.Substring(6);
                    if (jsonData == "[DONE]") return;

                    try
                    {
                        var response = JsonUtility.FromJson<ChatCompletionChunkResponse>(jsonData);
                        if (response?.choices != null && response.choices.Length > 0 && 
                            !string.IsNullOrEmpty(response.choices[0].delta?.content))
                        {
                            onChunk?.Invoke(response.choices[0].delta.content);
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
