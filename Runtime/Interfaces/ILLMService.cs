using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Interfaces
{
    /// <summary>
    /// Interface for Language Model services
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Send a chat completion request to the language model
        /// </summary>
        /// <param name="messages">List of chat messages</param>
        /// <param name="model">Model name to use</param>
        /// <returns>Response from the language model</returns>
        Task<string> ChatCompletion(List<ChatMessage> messages, string model);

        /// <summary>
        /// Send a streaming chat completion request to the language model
        /// </summary>
        /// <param name="messages">List of chat messages</param>
        /// <param name="onChunk">Callback for receiving text chunks</param>
        /// <param name="model">Model name to use</param>
        /// <returns>Task that completes when the stream ends</returns>
        Task ChatCompletionStreaming(List<ChatMessage> messages, Action<string> onChunk, string model = null);

        /// <summary>
        /// Send a completion request to the language model
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="model">Model name to use</param>
        /// <returns>Response from the language model</returns>
        Task<string> Completion(string prompt, string model);
    }
}
