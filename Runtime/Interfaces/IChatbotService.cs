using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Interfaces
{
    /// <summary>
    /// Interface for chatbot services
    /// </summary>
    public interface IChatbotService
    {
        /// <summary>
        /// Get all messages in the conversation history
        /// </summary>
        IReadOnlyList<ChatMessage> Messages { get; }

        /// <summary>
        /// 是否正在发送消息
        /// </summary>
        bool IsSending { get; }

        /// <summary>
        /// Add a message to the conversation
        /// </summary>
        /// <param name="message">Message</param>
        void AddMessage(ChatMessage message);

        /// <summary>
        /// Add a user message and get the assistant's response
        /// </summary>
        /// <param name="message">User message content</param>
        /// <param name="model">Optional model override</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Assistant's response message</returns>
        Task<ChatMessage> SendMessage(string message, string model = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear Message History
        /// </summary>
        /// <param name="keepSystemMessage">Whether to keep the system message</param>
        void ClearHistory(bool keepSystemMessage = true);
    }
}
