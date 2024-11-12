using System;
using System.Threading;
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
        /// <param name="tools">List of available tools</param>
        /// <param name="model">Model name to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Response message from the language model</returns>
        Task<ChatMessage> ChatCompletion(List<ChatMessage> messages,  string model = null,List<Tool> tools=null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Send a streaming chat completion request  to the language model
        /// </summary>
        /// <param name="messages">List of chat messages</param>
        /// <param name="tools">List of available tools</param>
        /// <param name="onChunk">Callback for receiving message chunks</param>
        /// <param name="model">Model name to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task that completes when the stream ends</returns>
        Task<ChatMessage> ChatCompletionStreaming(List<ChatMessage> messages, Action<ChatMessage> onChunk, string model = null,List<Tool> tools=null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a completion request to the language model
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="model">Model name to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Response from the language model</returns>
        Task<string> Completion(string prompt, string model, CancellationToken cancellationToken = default);
    }
}
